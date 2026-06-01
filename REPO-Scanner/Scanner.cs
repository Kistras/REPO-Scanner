using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using Photon.Pun;
using UnityEngine;
using Object = UnityEngine.Object;

namespace REPO_Scanner;

public class Scanner {
    private static CustomDiscovery<ValuableObject> discoverValuableCustom; 
    private static CustomDiscovery<EnemyRigidbody> discoverEnemyCustom;
    private static CustomDiscovery<PlayerDeathHead> discoverHeadCustom;
    private static CustomDiscovery<ItemAttributes> discoverToolCustom;
    private static CustomDiscovery<CosmeticWorldObject> discoverCosmeticCustom;
    private static CustomDiscovery<ValuableDiscoverCustom> discoverCustomFallback;
    private static float lastScanTime;
    private static bool isInitialized = false;

    private static ICustomDiscovery[] Discoveries => [discoverValuableCustom, discoverEnemyCustom, discoverHeadCustom, discoverToolCustom, discoverCosmeticCustom, discoverCustomFallback];
    
    private static void Initialize() {
        if (isInitialized) return;
        lastScanTime = ConfigManager.cooldown.Value;
        
        discoverValuableCustom = new (ConfigManager.Valuables); 
        discoverEnemyCustom = new (ConfigManager.Enemies);
        discoverHeadCustom = new (ConfigManager.Heads);
        discoverToolCustom = new (ConfigManager.Tools);
        discoverCosmeticCustom = new (ConfigManager.Cosmetics);
        discoverCustomFallback = new (ConfigManager.Custom);
        
        isInitialized = true;
        Plugin.Logger.LogInfo("Scanner initialized successfully");
    }

    public static void Scan() {
        if (Time.time - lastScanTime < ConfigManager.cooldown.Value) {
            return;
        }

        if (LevelGenerator.Instance == null || !LevelGenerator.Instance.Generated || SemiFunc.MenuLevel()) {
            Plugin.Logger.LogInfo("Cannot scan: No level loaded.");
            return;
        }

        if (!isInitialized) Initialize();

        PlayerController playerController = PlayerController.instance;
        if (playerController == null || Camera.main == null) {
            Plugin.Logger.LogError("Cannot scan: Player camera not found.");
            return;
        }

        Vector3 scanPosition = Camera.main.transform.position;
        Collider[] colliders = Physics.OverlapSphere(scanPosition, ConfigManager.MaxScanRadius);

        List<Vector3> scannedPositions = new();
        foreach (Collider collider in colliders) {
            if (collider == null || collider.gameObject == null) continue;
            
            foreach (ICustomDiscovery discovery in Discoveries)
                if (discovery.ScanItems(collider, scanPosition, scannedPositions)) break;
        }

        lastScanTime = Time.time;
        if (ScannerGUI.Instance != null) {
            ScannerGUI.Instance.NotifyScan();
        }
    }

    public static void Update() {}

    public static bool IsOnCooldown() {
        return Time.time - lastScanTime < ConfigManager.cooldown.Value;
    }

    public static float GetRemainingCooldown() {
        return Mathf.Max(0, ConfigManager.cooldown.Value - (Time.time - lastScanTime));
    }
}