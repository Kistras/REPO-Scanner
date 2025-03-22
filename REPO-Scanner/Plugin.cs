using System;
using System.Collections.Generic;
using System.Linq;
//using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace REPO_Scanner;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    internal static new ManualLogSource Logger;
    public static InputKey scanKey = (InputKey)327;
    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    public static ConfigEntry<String> keyBind;
    public static ConfigEntry<float> scanRadius;
    public static ConfigEntry<float> cooldown;
    public static ConfigEntry<bool> multiplayerReveal;
    private static string storedKeybind; // Used to check if keybind has changed

    internal static bool IsKeybindValid(string keybind)
        => !string.IsNullOrEmpty(keybind)
           && keybind.StartsWith("<keyboard>/")
           && keybind.Length is >= 12 and <= 14;

    internal static void RebindScan() {
        if (keyBind?.Value == null || InputManager.instance == null) return;
        
        string keybind = keyBind.Value.ToLower();
        if (storedKeybind == keybind) return;
        storedKeybind = keybind;
        
        Logger.LogInfo($"Keybind changed to: {keybind}");
        if (IsKeybindValid(keybind)) {
            InputManager.instance.Rebind(scanKey, keybind);
        } else {
            Logger.LogInfo($"Keybind invalid, resetting to default");
            keyBind.Value = (string)keyBind.DefaultValue;
        }
    }
    
    private void Awake() {
        // Plugin startup logic
        Logger = base.Logger;
        keyBind = Config.Bind("Keybinds", "Scanner Key", "<Keyboard>/f", 
            new ConfigDescription("What you press to scan things in the format of `<Keyboard>/YOURKEY` (I.E. <Keyboard>/f)", null, "HideFromREPOConfig"));
        scanRadius = Config.Bind("Options", "Scan Radius", 10f, 
            new ConfigDescription("Radius of the scanner", new AcceptableValueRange<float>(2f, 100f)));
        cooldown = Config.Bind("Options", "Cooldown", 10f, 
            new ConfigDescription("Interval between scans", new AcceptableValueRange<float>(1f, 120f)));
        multiplayerReveal = Config.Bind("Options", "Multiplayer RPC", true,
            new ConfigDescription("Whether to reveal scan results on other players' minimaps in multiplayer"));
        
        keyBind.SettingChanged += (sender, args) => {
            RebindScan();
        };
        /*if (!IsKeybindValid(keyBind.Value)) {
            Logger.LogInfo($"Keybind from config invalid, resetting to default");
            keyBind.Value = (string)keyBind.DefaultValue;
        }*/
        
        harmony.PatchAll(typeof(Patches));

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
}

[HarmonyPatch]
public class Patches {
    private static float lastGuiCheckTime = 0f;

    [HarmonyPatch(typeof(InputManager), "InitializeInputs")]
    [HarmonyPostfix]
    private static void InputManagerInitializeInputsPostfix(InputManager __instance) {
        try {
            Plugin.Logger.LogInfo("Setting up scan input action...");
            
            if (!InputManager.instance.inputActions.ContainsKey(Plugin.scanKey)) {
                InputAction scanAction = new InputAction("Scan", binding: Plugin.keyBind.Value.ToLower());
                InputManager.instance.inputActions.Add(Plugin.scanKey, scanAction);
                scanAction.Enable();
                Plugin.Logger.LogInfo("Added scan input action");
            }
        } catch (Exception ex) {
            Plugin.Logger.LogError($"Error in InputManagerInitializeInputsPostfix: {ex.Message}");
        }
    }
    
    [HarmonyPatch(typeof(PlayerController), "Update")]
    [HarmonyPostfix]
    private static void PlayerControllerUpdatePostfix() {
        Scanner.Update();
        
        if (SemiFunc.InputDown(Plugin.scanKey)) {
            //Plugin.Logger.LogInfo("Scan key pressed - activating scanner");
            Scanner.Scan();
        }
        
        EnsureScannerGUIExists();
    }

    // An attempt to fix keybinding
    [HarmonyPatch(typeof(PlayerController), "Start")]
    [HarmonyPostfix]
    private static void PlayerControllerStartPostfix() {
        Plugin.RebindScan();
    }
    
    private static void EnsureScannerGUIExists()
    {
        // Only check periodically to avoid creating multiple objects
        if (Time.time - lastGuiCheckTime < 10f)
            return;
            
        lastGuiCheckTime = Time.time;
        
        // If GUI already exists, nothing to do
        if (ScannerGUI.Instance != null)
            return;
            
        // Only try to create when player exists
        if (PlayerController.instance == null)
            return;
            
        // Avoid multiple attempts too fast    
        GameObject guiObject = new GameObject("ScannerGUI");
        guiObject.AddComponent<ScannerGUI>();
        GameObject.DontDestroyOnLoad(guiObject);
    }
}