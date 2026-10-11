using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace MultiRecipePin
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "gyk.multirecipepin";

        public const string NAME = "Multi Recipe Pin";

        public const string VERSION = "0.1.0";

        internal static ConfigEntry<KeyCode> Hotkey;

        internal static ConfigEntry<int> MaxPins;

        internal static ConfigEntry<int> HudWidthPercent;

        internal static ConfigEntry<bool> DebugLog;

        internal static PinManager Pins;

        private bool _was_started;

        private bool _first_update_logged;

        private bool _gui_dumped;

        private Hud.PinHud _hud;

        private void Awake()
        {
            Log.Init(Logger);
            Hotkey = Config.Bind("General", "Hotkey", KeyCode.P, "Pin/unpin the recipe under the mouse in the craft or build menu.");
            MaxPins = Config.Bind("General", "MaxPins", 10, new ConfigDescription("Maximum number of pinned recipes.", new AcceptableValueRange<int>(1, 10)));
            HudWidthPercent = Config.Bind("HUD", "WidthPercent", 14, new ConfigDescription("HUD width in percent of screen width.", new AcceptableValueRange<int>(5, 25)));
            DebugLog = Config.Bind("General", "Debug", false, "Detailed logging.");
            Pins = new PinManager();
            _hud = new Hud.PinHud(Pins);
            Log.Info($"{NAME} {VERSION} loaded, game version {GameApi.GameVersion()}, hotkey {Hotkey.Value}");
        }

        private void Update()
        {
            if (!_first_update_logged)
            {
                _first_update_logged = true;
                Log.Dbg("Update loop is running");
            }
            if (!GameApi.IsGameStarted())
            {
                if (_was_started)
                {
                    _was_started = false;
                    int count = Pins.Count;
                    Pins.Clear();
                    Log.Info($"Game stopped, pins cleared ({count})");
                }
                _hud.Tick();
                return;
            }
            if (!_was_started)
            {
                Log.Info("Game started");
                if (DebugLog.Value && !_gui_dumped)
                {
                    _gui_dumped = true;
                    Hud.HierarchyDump.DumpAll();
                }
            }
            _was_started = true;
            if (Input.GetKeyDown(Hotkey.Value))
            {
                OnHotkey();
            }
            _hud.Tick();
        }

        private void OnHotkey()
        {
            if (!GameApi.TryGetHoveredCraft(out var craft, out var craftery))
            {
                Log.Dbg("Hotkey pressed, no recipe under cursor");
                return;
            }
            ToggleResult toggleResult = Pins.Toggle(craft, craftery, MaxPins.Value);
            switch (toggleResult)
            {
            case ToggleResult.Added:
            case ToggleResult.Removed:
                GameApi.PlayClick();
                break;
            case ToggleResult.LimitReached:
                GameApi.PlayDeny();
                break;
            }
            Log.Info($"Pin {toggleResult}: {GameApi.DescribeCraft(craft, craftery)} (pins: {Pins.Count}/{MaxPins.Value})");
        }
    }
}
