using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace SilverWandererMarket.Market
{
    internal struct SessionSnapshot
    {
        public string Role;
        public string Source;

        public static SessionSnapshot Sp()
        {
            SessionSnapshot s = new SessionSnapshot();
            s.Role = "sp";
            s.Source = "default";
            return s;
        }

        public static SessionSnapshot Of(string role, string source)
        {
            SessionSnapshot s = new SessionSnapshot();
            s.Role = role;
            s.Source = source;
            return s;
        }
    }

    /// <summary>
    /// Detect SP vs coop/MP without a hard Coop.dll reference.
    /// Module presence is not enough — Coop installed still allows a local SP campaign.
    /// </summary>
    internal static class SessionProbe
    {
        public static SessionSnapshot Detect()
        {
            SessionSnapshot net = FromGameNetwork();
            if (net.Role != "sp")
                return net;

            SessionSnapshot coop = FromKnownTypes();
            if (coop.Role != "sp")
                return coop;

            return SessionSnapshot.Sp();
        }

        private static SessionSnapshot FromGameNetwork()
        {
            try
            {
                bool dedicated = GameNetwork.IsDedicatedServer;
                // Recorder/replay flags are not coop — campaign battle recording must stay SP.
                bool server = GameNetwork.IsServer;
                bool client = GameNetwork.IsClient;
                if (dedicated || server)
                    return SessionSnapshot.Of("host", "GameNetwork");
                if (client)
                    return SessionSnapshot.Of("client", "GameNetwork");
            }
            catch (Exception ex)
            {
                SWMLog.Verbose("SWMSession", "GameNetwork probe failed: " + ex.GetType().Name);
            }
            return SessionSnapshot.Sp();
        }

        private static SessionSnapshot FromKnownTypes()
        {
            string[] names =
            {
                "Coop.CoopMod, Coop",
                "Coop.Mod.CoopMod, Coop.Mod",
                "HexServerPack.CoopBridge.CoopCompatibilityRuntime, HexServerPack.CoopBridge"
            };
            foreach (string name in names)
            {
                Type t = Type.GetType(name, false);
                if (t == null)
                    t = FindType(name);
                if (t == null)
                    continue;
                SessionSnapshot snap = FromType(t);
                if (snap.Role != "sp")
                    return snap;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string n = assembly.GetName().Name;
                if (n != "Coop" && n != "Coop.Core" && n != "Coop.Mod"
                    && n != "GameInterface" && n != "HexServerPack.CoopBridge")
                    continue;
                SessionSnapshot snap = FromAssembly(assembly);
                if (snap.Role != "sp")
                    return snap;
            }
            return SessionSnapshot.Sp();
        }

        private static Type FindType(string assemblyQualified)
        {
            int comma = assemblyQualified.IndexOf(',');
            if (comma < 0)
                return null;
            string typeName = assemblyQualified.Substring(0, comma).Trim();
            string asmName = assemblyQualified.Substring(comma + 1).Trim();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != asmName)
                    continue;
                Type t = assembly.GetType(typeName, false);
                if (t != null)
                    return t;
            }
            return null;
        }

        private static SessionSnapshot FromAssembly(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetExportedTypes();
            }
            catch
            {
                return SessionSnapshot.Sp();
            }

            foreach (Type t in types)
            {
                if (t == null || !t.IsClass)
                    continue;
                string name = t.Name;
                if (name != "CoopMod"
                    && name != "CoopCompatibilityRuntime"
                    && name != "CoopSessionProvider"
                    && name != "CoopClient"
                    && name != "CoopServer"
                    && name != "CoopNetwork"
                    && !name.EndsWith("SessionProvider", StringComparison.Ordinal)
                    && !name.EndsWith("CoopServer", StringComparison.Ordinal)
                    && !name.EndsWith("CoopClient", StringComparison.Ordinal))
                    continue;
                SessionSnapshot snap = FromType(t);
                if (snap.Role != "sp")
                    return snap;
            }
            return SessionSnapshot.Sp();
        }

        private static SessionSnapshot FromType(Type t)
        {
            object inst = TryInstance(t);
            bool server = RoleBool(t, inst, true);
            bool client = RoleBool(t, inst, false);

            string[] nestedNames = { "CoopSession", "Session", "Network", "Connection", "CoopNetwork" };
            for (int i = 0; i < nestedNames.Length; i++)
            {
                object nested = ReadObject(t, inst, nestedNames[i]);
                if (nested == null)
                    continue;
                Type st = nested.GetType();
                server = server || RoleBool(st, nested, true);
                client = client || RoleBool(st, nested, false);
            }

            string source = t.FullName ?? t.Name;
            if (client && !server)
                return SessionSnapshot.Of("client", source);
            if (server)
                return SessionSnapshot.Of("host", source);
            return SessionSnapshot.Sp();
        }

        private static bool RoleBool(Type t, object inst, bool server)
        {
            if (server)
            {
                return ReadBool(t, inst, "IsServer")
                    || ReadBool(t, inst, "IsManagedServer")
                    || ReadBool(t, inst, "IsDedicatedServer")
                    || ReadBool(t, inst, "IsHost")
                    || ReadBool(t, inst, "IsListenServer")
                    || ReadBool(t, inst, "IsCoopServer");
            }
            return ReadBool(t, inst, "IsClient")
                || ReadBool(t, inst, "IsCoopClient");
        }

        private static object TryInstance(Type t)
        {
            string[] names = { "Instance", "Current" };
            foreach (string name in names)
            {
                PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (p != null)
                {
                    try { return p.GetValue(null, null); }
                    catch { }
                }
                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (f != null)
                {
                    try { return f.GetValue(null); }
                    catch { }
                }
            }
            return null;
        }

        private static bool ReadBool(Type t, object inst, string name)
        {
            PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (p != null && p.PropertyType == typeof(bool))
            {
                try
                {
                    bool isStatic = p.GetGetMethod(true) != null && p.GetGetMethod(true).IsStatic;
                    if (!isStatic && inst == null)
                        return false;
                    object v = p.GetValue(isStatic ? null : inst, null);
                    return v is bool && (bool)v;
                }
                catch
                {
                    return false;
                }
            }
            FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (f == null || f.FieldType != typeof(bool))
                return false;
            try
            {
                if (!f.IsStatic && inst == null)
                    return false;
                object v = f.GetValue(f.IsStatic ? null : inst);
                return v is bool && (bool)v;
            }
            catch
            {
                return false;
            }
        }

        private static object ReadObject(Type t, object inst, string name)
        {
            PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (p != null)
            {
                try
                {
                    bool isStatic = p.GetGetMethod(true) != null && p.GetGetMethod(true).IsStatic;
                    if (!isStatic && inst == null)
                        return null;
                    return p.GetValue(isStatic ? null : inst, null);
                }
                catch
                {
                    return null;
                }
            }
            FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (f == null)
                return null;
            try
            {
                if (!f.IsStatic && inst == null)
                    return null;
                return f.GetValue(f.IsStatic ? null : inst);
            }
            catch
            {
                return null;
            }
        }
    }
}
