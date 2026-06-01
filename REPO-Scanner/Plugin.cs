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
public class Plugin : BaseUnityPlugin {
    internal new static ManualLogSource Logger;
    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    
    private void Awake() {
        Logger = base.Logger;

        ConfigManager.Initialize(this);
        
        harmony.PatchAll(typeof(Patches));
        Logger.LogInfo($"Patched {harmony.GetPatchedMethods().Count()} methods!");
        
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} ({MyPluginInfo.PLUGIN_VERSION}) is loaded!");
    }
}
