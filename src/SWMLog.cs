using System;
using System.IO;
using TaleWorlds.Library;

namespace SilverWandererMarket
{
    /// <summary>
    /// Greppable diagnostics for Silver Wanderer Market on a busy coop server.
    /// Every line starts with <c>[SilverWandererMarket]</c>.
    /// Writes rgl log / console, and <c>Modules/SilverWandererMarket/swm_debug.log</c>.
    /// </summary>
    public static class SWMLog
    {
        public const string Prefix = "[SilverWandererMarket]";

        /// <summary>rgl log + Console.WriteLine. Default on.</summary>
        public static bool ConsoleEnabled = true;

        /// <summary>Append to swm_debug.log. Default on.</summary>
        public static bool FileEnabled = true;

        /// <summary>High-frequency traces (pack size, intercepts). Default on until integration is stable.</summary>
        public static bool VerboseEnabled = true;

        public static void Info(string area, string message)
        {
            Write("INFO", area, message);
        }

        public static void Warn(string area, string message)
        {
            Write("WARN", area, message);
        }

        public static void Error(string area, string message)
        {
            Write("ERROR", area, message);
        }

        public static void Error(string area, string message, Exception ex)
        {
            string extra = ex != null ? (" | " + ex.GetType().Name + ": " + ex.Message) : "";
            Write("ERROR", area, message + extra);
        }

        public static void Verbose(string area, string message)
        {
            if (!VerboseEnabled)
                return;
            Write("DEBUG", area, message);
        }

        private static void Write(string level, string area, string message)
        {
            string line = Prefix + " [" + (area ?? "SWM") + "] " + level + " " + (message ?? "");
            if (ConsoleEnabled)
            {
                try { Debug.Print(line); }
                catch { }
                try { Console.WriteLine(line); }
                catch { }
            }
            if (!FileEnabled)
                return;
            try
            {
                string path = Path.Combine(BasePath.Name, "Modules", "SilverWandererMarket", "swm_debug.log");
                File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + line + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
