using System;
using System.Collections.Generic;
using System.Linq;
//using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace REPO_Scanner;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("Kistras-CustomDiscoverStateLib", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin {
    internal static new ManualLogSource Logger;
    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    internal static bool isCustomDiscoverStateLibLoaded;
    
    private void Awake() {
        // Plugin startup logic
        Logger = base.Logger;

        ConfigManager.Initialize(this);
        
        harmony.PatchAll(typeof(Patches));
        Logger.LogInfo($"Patched {harmony.GetPatchedMethods().Count()} methods!");
        
        isCustomDiscoverStateLibLoaded = Chainloader.PluginInfos.ContainsKey("Kistras-CustomDiscoverStateLib");
        if (!isCustomDiscoverStateLibLoaded) {
            Logger.LogInfo("CustomDiscoverStateLib is not loaded. Will fallback to \"Reminder\" graphic. Consider installing all dependencies.");
        }

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
}
