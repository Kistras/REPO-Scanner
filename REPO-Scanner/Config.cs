using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace REPO_Scanner;

public static class ConfigManager {
    public static InputKey scanKey = (InputKey)327;
    
    public static ConfigEntry<KeyCode> keyBind;
    public static ConfigEntry<float> scanRadius;
    public static ConfigEntry<float> cooldown;
    public static ConfigEntry<bool> multiplayerReveal;
    public static ConfigEntry<bool> toCreateGUI;
    
    public static ConfigEntry<bool> shouldScanValuables;
    public static ConfigEntry<bool> shouldScanEnemies;
    public static ConfigEntry<bool> shouldScanHeads;
    public static ConfigEntry<bool> shouldScanItems;
    
    private static KeyCode storedKeybind; // Used to check if keybind has changed
    private static bool initialized = false;
    
    internal static void Initialize(Plugin plugin) {
        if (initialized) return;
        initialized = true;
        
        keyBind = plugin.Config.Bind("Keybinds", "Scanner Key", KeyCode.F, 
            new ConfigDescription("What you press to scan things"));
        
        scanRadius = plugin.Config.Bind("Options", "Scan Radius", 10f, 
            new ConfigDescription("Radius of the scanner", new AcceptableValueRange<float>(2f, 100f)));
        cooldown = plugin.Config.Bind("Options", "Cooldown", 10f, 
            new ConfigDescription("Interval between scans", new AcceptableValueRange<float>(0f, 120f)));
        multiplayerReveal = plugin.Config.Bind("Options", "Multiplayer RPC", true,
            new ConfigDescription("Whether to reveal scan results on other players' minimaps in multiplayer"));
        toCreateGUI = plugin.Config.Bind("Options", "Create GUI", true,
            new ConfigDescription("Whether to create cooldown GUI"));
        
        shouldScanValuables = plugin.Config.Bind("Features", "Valuables", true, 
            new ConfigDescription("Whether to scan valuables"));
        shouldScanEnemies = plugin.Config.Bind("Features", "Enemies", true, 
            new ConfigDescription("Whether to scan enemies"));
        shouldScanHeads = plugin.Config.Bind("Features", "Heads", true, 
            new ConfigDescription("Whether to scan dead player's heads"));
        shouldScanItems = plugin.Config.Bind("Features", "Items", true, 
            new ConfigDescription("Whether to scan equippable items"));
        
        keyBind.SettingChanged += (sender, args) => {
            RebindScan();
        };
    }
    
    internal static void RebindScan() {
        if (keyBind?.Value == null || InputManager.instance == null) return;
        
        if (storedKeybind == keyBind.Value) return;
        storedKeybind = keyBind.Value;
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