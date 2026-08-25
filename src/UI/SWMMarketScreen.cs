using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace SilverWandererMarket.UI
{
    internal static class SWMMarketScreen
    {
        private static GauntletLayer _layer;
        private static SWMMarketVM _vm;
        private static bool _openRequested;
        private static int _openDelayFrames;

        public static bool IsOpen { get { return _layer != null; } }

        public static void RequestOpen()
        {
            _openRequested = true;
            _openDelayFrames = 8;
        }

        public static void Tick(float dt)
        {
            if (_openRequested && !IsOpen)
            {
                if (_openDelayFrames > 0)
                    _openDelayFrames--;
                else
                {
                    _openRequested = false;
                    Open();
                }
            }
            if (_vm != null)
                _vm.Tick();
            if (_layer != null && _vm != null)
            {
                if (_layer.Input.IsKeyReleased(InputKey.Escape) || _layer.Input.IsKeyReleased(InputKey.RightMouseButton))
                    _vm.HandleEscape();
            }
        }

        public static void Open()
        {
            if (IsOpen)
                return;
            ScreenBase top = ScreenManager.TopScreen;
            if (top == null)
                return;
            try
            {
                _vm = new SWMMarketVM();
                _layer = new GauntletLayer("GauntletLayer", 210, false);
                _layer.LoadMovie("SWMMarketScreen", _vm);
                _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                try
                {
                    _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
                }
                catch
                {
                }
                _layer.IsFocusLayer = true;
                top.AddLayer(_layer);
                ScreenManager.TrySetFocus(_layer);
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("Wanderer market UI failed: " + ex.Message));
                Close();
            }
        }

        public static void Close()
        {
            _openRequested = false;
            if (_layer == null)
                return;
            ScreenBase top = ScreenManager.TopScreen;
            if (top != null)
                top.RemoveLayer(_layer);
            if (_vm != null)
                _vm.OnFinalize();
            _layer = null;
            _vm = null;
            Market.SWMMarketHooks.Raise(Market.SWMMarketHookKind.MarketClosed, Market.SWMMarketHooks.LocalPlayerKey(), "", "", "", 0, null);
        }
    }
}
