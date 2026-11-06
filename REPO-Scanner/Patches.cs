using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace REPO_Scanner;

[HarmonyPatch]
public class Patches {
    internal static float lastGuiCheckTime = 0f;

    [HarmonyPatch(typeof(PlayerController), "Update")]
    [HarmonyPostfix]
    private static void PlayerControllerUpdatePostfix() {
        Scanner.Update();
        
        if (Input.GetKeyDown(ConfigManager.keyBind.Value))
            Scanner.Scan();
        
        EnsureScannerGUIExists();
    }
    
    private static void EnsureScannerGUIExists()
    {
        // Only check periodically to avoid creating multiple objects
        if (Time.time - lastGuiCheckTime < 10f)
            return;
            
        lastGuiCheckTime = Time.time;
        
        if (ScannerGUI.Instance != null)
            return;
        if (PlayerController.instance == null)
            return;
        
        GameObject guiObject = new GameObject("ScannerGUI");
        guiObject.AddComponent<ScannerGUI>();

        guiObject.transform.SetParent(HUDCanvas.instance.transform, false);
        //GameObject.DontDestroyOnLoad(guiObject);
    }

    // Only useful to hide an error in console basically
    // My creation of ValuableDiscoverCustom throws an error bc it's not tied to an actual object
    internal static bool shouldSuppressValuableDiscoverCustomAwake = false;
    [HarmonyPatch(typeof(ValuableDiscoverCustom), "Awake")]
    [HarmonyPrefix]
    private static bool SuppressValuableDiscoverCustomAwakePatch() 
    {
        return !shouldSuppressValuableDiscoverCustomAwake;
    }
}