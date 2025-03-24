using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace REPO_Scanner;

[HarmonyPatch]
public class Patches {
    private static float lastGuiCheckTime = 0f;

    [HarmonyPatch(typeof(InputManager), "InitializeInputs")]
    [HarmonyPostfix]
    private static void InputManagerInitializeInputsPostfix(InputManager __instance) {
        try {
            Plugin.Logger.LogInfo("Setting up scan input action...");
            
            if (!InputManager.instance.inputActions.ContainsKey(ConfigManager.scanKey)) {
                InputAction scanAction = new InputAction("Scan", binding: ConfigManager.KeyCodeToBindingPath(ConfigManager.keyBind.Value));
                InputManager.instance.inputActions.Add(ConfigManager.scanKey, scanAction);
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
        
        if (SemiFunc.InputDown(ConfigManager.scanKey)) {
            //Plugin.Logger.LogInfo("Scan key pressed - activating scanner");
            Scanner.Scan();
        }
        
        EnsureScannerGUIExists();
    }

    // An attempt to fix keybinding
    [HarmonyPatch(typeof(PlayerController), "Start")]
    [HarmonyPostfix]
    private static void PlayerControllerStartPostfix() {
        ConfigManager.RebindScan();
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