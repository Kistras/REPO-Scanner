using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using JetBrains.Annotations;
using UnityEngine;

namespace REPO_Scanner;

public class ConfigCategory {
    public ConfigEntry<bool> ShouldScan { get; }
    public ConfigEntry<float> ScanRadius { get; }
    [CanBeNull] public ConfigEntry<bool> OnlyScanDiscovered { get; }
    [CanBeNull] public ConfigEntry<bool> MultiplayerReveal { get; }
    public ConfigEntry<string> CornerColorConfig { get; }
    public ConfigEntry<string> MiddleColorConfig { get; }
    public ConfigEntry<float> ScanLinger { get;  }
    
    public Color CornerColor { get; private set; }
    public Color MiddleColor { get; private set; }

    public ConfigCategory(Plugin plugin, string categoryName, string cornerColor, string middleColor, bool hasDiscovered = false, bool hasReveal = false, bool hasColorSettings = true, bool hasCustomColor = false, float scanLinger = 1.5f) {
        string sectionLabel = $"Scanning - {categoryName.Substring(0, 1).ToUpper()}{categoryName.Substring(1)}";
        
        ShouldScan = plugin.Config.Bind(sectionLabel, "Scan", true, 
            new ConfigDescription($"Whether to scan {categoryName}"));
        ScanRadius = plugin.Config.Bind(sectionLabel, "Radius Override", 0f, 
            new ConfigDescription($"Override scan radius for {categoryName} (0 to use main radius)", new AcceptableValueRange<float>(0f, ConfigManager.MaxRadius)));
        ScanRadius.SettingChanged += (_, _) => ConfigManager.UpdateMaxScanRadius();
        if (hasDiscovered)
            OnlyScanDiscovered = plugin.Config.Bind(sectionLabel, "Discovered Only", false, 
                new ConfigDescription($"Only scan already discovered {categoryName} (does not reveal new ones)"));
        if (hasReveal)
            MultiplayerReveal = plugin.Config.Bind(sectionLabel, "Multiplayer RPC", false,
                new ConfigDescription("Whether to reveal scan results on other players' minimaps in multiplayer"));

        if (hasColorSettings) {
            string notice = hasCustomColor
                ? "For this object this (most likely) will be ignored and only serves as fallback"
                : "This may be ignored if object has it's own custom color";
            CornerColorConfig = plugin.Config.Bind(sectionLabel, "Corner Color", cornerColor,
                new ConfigDescription($"Corner color (in hex with #RRGGBBAA format, case-insensitive, eg. {cornerColor}). {notice}"));
            MiddleColorConfig = plugin.Config.Bind(sectionLabel, "Middle Color", middleColor,
                new ConfigDescription($"Middle color (in hex with #RRGGBBAA format, case-insensitive, eg. {middleColor}). {notice}"));
            ScanLinger = plugin.Config.Bind(sectionLabel, "Scan Linger", scanLinger,
                new ConfigDescription("For how long should discovered objects be highlighted, in seconds.", new AcceptableValueRange<float>(0.5f, 8f)));
            
            CornerColorConfig.SettingChanged += (sender, args) => {
                CornerColor = GetColorFromHex(CornerColorConfig.Value);
            };
            MiddleColorConfig.SettingChanged += (sender, args) => {
                MiddleColor = GetColorFromHex(MiddleColorConfig.Value);
            };
            
            CornerColor = GetColorFromHex(CornerColorConfig.Value);
            MiddleColor = GetColorFromHex(MiddleColorConfig.Value);
        }
    }

    private readonly static Color transparent = new Color(0, 0, 0, 0.5f);
    private static Color GetColorFromHex(string hex) {
        if (string.IsNullOrWhiteSpace(hex))
            return transparent;

        hex = hex.Trim();
        if (!hex.StartsWith("#")) hex = "#" + hex;
        
        if (hex.Length != 9) return transparent;
        
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
            return color;

        return transparent;
    }
}

public static class ConfigManager {
    internal const float MaxRadius = 100f;

    public static ConfigEntry<KeyCode> keyBind;
    public static ConfigEntry<float> scanRadius;
    public static ConfigEntry<float> cooldown;
    public static ConfigEntry<bool> toCreateGUI;
    public static ConfigEntry<string> versionGUI; // <string> so it could be changed through REPOConfig
    public static bool IsLegacyVersionGUI { get; private set; }

    public static ConfigCategory Valuables { get; private set; }
    public static ConfigCategory Enemies { get; private set; }
    public static ConfigCategory Heads { get; private set; }
    public static ConfigCategory Tools { get; private set; }
    public static ConfigCategory Cosmetics { get; private set; }
    public static ConfigCategory Custom { get; private set; }

    private static ConfigCategory[] Configs => [Valuables, Enemies, Heads, Tools, Cosmetics, Custom];
    
    public static float MaxScanRadius { get; private set; }
    internal static void UpdateMaxScanRadius() {
        float max = 0;
        foreach (ConfigCategory cat in Configs) {
            if (cat.ShouldScan.Value) 
                max = Mathf.Max(max, cat.ScanRadius.Value > 0 ? cat.ScanRadius.Value : scanRadius.Value);
        }
        MaxScanRadius = Mathf.Min(max, MaxRadius);
    }
    
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
        toCreateGUI = plugin.Config.Bind("Options", "Create GUI", true,
            new ConfigDescription("Whether to create cooldown GUI"));
        versionGUI = plugin.Config.Bind("Options", "GUI Version", "New",
            new ConfigDescription("New version uses REPO's screen effects", new AcceptableValueList<string>("New", "Legacy")));
        
        Valuables = new ConfigCategory(plugin, "valuables", "#FFDC00FF", "#FFDC001E", true, true, true,false, 1.5f);
        Enemies = new ConfigCategory(plugin, "enemies", "#FF1A11BF", "#FF00112B", false, false, true,false, 4f);
        Heads = new ConfigCategory(plugin, "heads", "#FF1A11BF", "#FF00112B", false, false, true,false, 1.5f);
        Tools = new ConfigCategory(plugin, "tools", "#1A99E6BF", "#0080CC2B", false, false, true,false, 1.5f);
        Cosmetics = new ConfigCategory(plugin, "cosmetics", "#33E699BF", "#1ACC802B", true, true, true,true, 1.5f);
        Custom = new ConfigCategory(plugin, "custom", "#FFFFFFBF", "#FFFFFF2B", false, false, false,false, 1.5f);
        
        IsLegacyVersionGUI = versionGUI.Value == "Legacy";
        versionGUI.SettingChanged += (sender, args) => {
            IsLegacyVersionGUI = versionGUI.Value == "Legacy";
            if (!ScannerGUI.Instance) return;
            
            GameObject.Destroy(ScannerGUI.Instance);
            Patches.lastGuiCheckTime = 0f;
        };

        UpdateMaxScanRadius();
    }
}