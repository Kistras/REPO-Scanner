using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace REPO_Scanner;

public static class ConfigManager {
    const float MaxRadius = 100f;
        
    public static InputKey scanKey = (InputKey)327;
    
    public static ConfigEntry<KeyCode> keyBind;
    public static ConfigEntry<float> scanRadius;
    public static ConfigEntry<float> cooldown;
    public static ConfigEntry<bool> multiplayerReveal;
    public static ConfigEntry<bool> toCreateGUI;
    public static ConfigEntry<string> versionGUI; // <string> so it could be changed through REPOConfig
    public static bool IsLegacyVersionGUI { get; private set; }
    
    public static ConfigEntry<bool> shouldScanValuables;
    public static ConfigEntry<bool> onlyScanDiscoveredValuables;
    public static ConfigEntry<bool> shouldScanEnemies;
    public static ConfigEntry<bool> shouldScanHeads;
    public static ConfigEntry<bool> shouldScanItems;
    
    public static ConfigEntry<float> scanRadiusValuablesOverride;
    public static ConfigEntry<float> scanRadiusEnemiesOverride;
    public static ConfigEntry<float> scanRadiusHeadsOverride;
    public static ConfigEntry<float> scanRadiusItemsOverride;

    public static float MaxScanRadius
    {
        get {
            float max = 0;  
            if (shouldScanValuables.Value) max = Mathf.Max(max, scanRadiusValuablesOverride.Value > 0 ? scanRadiusValuablesOverride.Value : scanRadius.Value);
            if (shouldScanEnemies.Value) max = Mathf.Max(max, scanRadiusEnemiesOverride.Value > 0 ? scanRadiusEnemiesOverride.Value : scanRadius.Value);
            if (shouldScanHeads.Value) max = Mathf.Max(max, scanRadiusHeadsOverride.Value > 0 ? scanRadiusHeadsOverride.Value : scanRadius.Value);
            if (shouldScanItems.Value) max = Mathf.Max(max, scanRadiusItemsOverride.Value > 0 ? scanRadiusItemsOverride.Value : scanRadius.Value);
            return Mathf.Min(max, MaxRadius);
        }
    }
    
    private static KeyCode _storedKeybind; // Used to check if keybind has changed
    private static bool _initialized = false;
    
    internal static void Initialize(Plugin plugin) {
        if (_initialized) return;
        _initialized = true;
        
        keyBind = plugin.Config.Bind("Keybinds", "Scanner Key", KeyCode.F, 
            new ConfigDescription("What you press to scan things"));
        
        scanRadius = plugin.Config.Bind("Options", "Scan Radius", 10f, 
            new ConfigDescription("Radius of the scanner", new AcceptableValueRange<float>(2f, MaxRadius)));
        cooldown = plugin.Config.Bind("Options", "Cooldown", 10f, 
            new ConfigDescription("Interval between scans", new AcceptableValueRange<float>(0f, 120f)));
        multiplayerReveal = plugin.Config.Bind("Options", "Multiplayer RPC", true,
            new ConfigDescription("Whether to reveal scan results on other players' minimaps in multiplayer"));
        toCreateGUI = plugin.Config.Bind("Options", "Create GUI", true,
            new ConfigDescription("Whether to create cooldown GUI"));
        versionGUI = plugin.Config.Bind("Options", "GUI Version", "New",
            new ConfigDescription("New version uses REPO's screen effects", new AcceptableValueList<string>("New", "Legacy")));
        
        shouldScanValuables = plugin.Config.Bind("Features", "Valuables", true, 
            new ConfigDescription("Whether to scan valuables"));
        onlyScanDiscoveredValuables = plugin.Config.Bind("Features", "Only Scan Discovered", false, 
            new ConfigDescription("Only scan already discovered valuables (does not reveal new ones)"));
        shouldScanEnemies = plugin.Config.Bind("Features", "Enemies", true, 
            new ConfigDescription("Whether to scan enemies"));
        shouldScanHeads = plugin.Config.Bind("Features", "Heads", true, 
            new ConfigDescription("Whether to scan dead player's heads"));
        shouldScanItems = plugin.Config.Bind("Features", "Items", true, 
            new ConfigDescription("Whether to scan equippable items"));
        
        scanRadiusValuablesOverride = plugin.Config.Bind("Radius Overrides", "Valuables", 0f, 
            new ConfigDescription("Override scan radius for valuables (0 to use main radius)", new AcceptableValueRange<float>(0f, MaxRadius)));
        scanRadiusEnemiesOverride = plugin.Config.Bind("Radius Overrides", "Enemies", 0f, 
            new ConfigDescription("Override scan radius for enemies (0 to use main radius)", new AcceptableValueRange<float>(0f, MaxRadius)));
        scanRadiusHeadsOverride = plugin.Config.Bind("Radius Overrides", "Heads", 0f, 
            new ConfigDescription("Override scan radius for heads (0 to use main radius)", new AcceptableValueRange<float>(0f, MaxRadius)));
        scanRadiusItemsOverride = plugin.Config.Bind("Radius Overrides", "Items", 0f, 
            new ConfigDescription("Override scan radius for items (0 to use main radius)", new AcceptableValueRange<float>(0f, MaxRadius)));
        
        IsLegacyVersionGUI = versionGUI.Value == "Legacy";
            
        keyBind.SettingChanged += (sender, args) => {
            RebindScan();
        };
        versionGUI.SettingChanged += (sender, args) => {
            IsLegacyVersionGUI = versionGUI.Value == "Legacy";
            if (!ScannerGUI.Instance) return;
            
            GameObject.Destroy(ScannerGUI.Instance);
            Patches.lastGuiCheckTime = 0f;
        };
    }
    
    internal static void RebindScan() {
        if (keyBind?.Value == null || InputManager.instance == null) return;
        
        if (_storedKeybind == keyBind.Value) return;
        _storedKeybind = keyBind.Value;
        string keybind = KeyCodeToBindingPath(keyBind.Value);
        
        Plugin.Logger.LogInfo($"Keybind changed to: {keybind}");
        InputManager.instance.Rebind(scanKey, keybind);
    }
    
    internal static string KeyCodeToBindingPath(KeyCode keyCode) {
        // Number keys
        if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            return $"<keyboard>/{keyCode.ToString().Replace("Alpha", "")}";
        
        // Mouse buttons
        if (keyCode == KeyCode.Mouse0) return "<mouse>/leftButton";
        if (keyCode == KeyCode.Mouse1) return "<mouse>/rightButton";
        if (keyCode == KeyCode.Mouse2) return "<mouse>/middleButton";
        if (keyCode is >= KeyCode.Mouse3 and <= KeyCode.Mouse6)
            return $"<mouse>/button{(int)keyCode - (int)KeyCode.Mouse0}";
        
        // Special keys
        switch (keyCode) {
            case KeyCode.Return: return "<keyboard>/enter";
            case KeyCode.LeftControl: return "<keyboard>/leftCtrl";
            case KeyCode.RightControl: return "<keyboard>/rightCtrl";
            default:
                string keyName = keyCode.ToString();
                if (keyName.Length > 0) {
                    keyName = char.ToLower(keyName[0]) + keyName[1..];
                }
                return $"<keyboard>/{keyName}";
            //return $"<keyboard>/{keyCode.ToString().ToLower()}";
        }
    }
}