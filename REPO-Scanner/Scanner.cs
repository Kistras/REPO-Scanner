using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace REPO_Scanner;

public class Scanner {
    private const ValuableDiscoverGraphic.State DiscoverValuableState = ValuableDiscoverGraphic.State.Discover;
    // I feel like having extra linger on each scan specifically for enemies might be beneficial
    private const ValuableDiscoverGraphic.State DiscoverEnemyState = ValuableDiscoverGraphic.State.Bad;
    // While identical to the Bad state, this allows you not to get stuck in the Bad state for like 4 seconds
    private static ValuableDiscoverGraphic.State discoverHeadState = NewCustomState(
        new Color(1f, 0.0f, 0.067f, 0.17f), 
        new Color(1f, 0.1f, 0.067f, 0.75f));
    private static ValuableDiscoverGraphic.State discoverItemState = NewCustomState(
        new Color(0.0f, 0.5f, 0.8f, 0.17f), 
        new Color(0.1f, 0.6f, 0.9f, 0.75f));
    private static ValuableDiscoverGraphic.State discoverCosmeticState = NewCustomState( // as fallback
        new Color(0.1f, 0.8f, 0.5f, 0.17f), 
        new Color(0.2f, 0.9f, 0.6f, 0.75f));
    private static float lastScanTime;
    private static bool isInitialized = false;
    
    // I have no idea how Thunderstore handles missing dependencies if they appear unexpectedly,
    //     so I wanted to make this lib optional
    private static ValuableDiscoverGraphic.State NewCustomState(Color middle, Color corner) {
        if (Plugin.isCustomDiscoverStateLibLoaded) {
            try {
                Type customStateType = AccessTools.TypeByName("CustomDiscoverStateLib.CustomDiscoverState");
                if (customStateType != null) {
                    MethodInfo method = AccessTools.Method(customStateType, "AddNewDiscoverGraphic");
                    if (method != null) {
                        return (ValuableDiscoverGraphic.State)method.Invoke(null, new object[] { middle, corner });
                    }
                }
            } catch (Exception ex) {
                Plugin.Logger.LogError($"Error accessing CustomDiscoverState: {ex.Message}");
            }
        }
        return ValuableDiscoverGraphic.State.Reminder;
    }
    
    private static void Initialize() {
        if (isInitialized) return;
        lastScanTime = ConfigManager.cooldown.Value;
        
        isInitialized = true;
        Plugin.Logger.LogInfo("Scanner initialized successfully");
    }

    [CanBeNull]
    private static T GetComponentOfType<T>(Collider gameObject) where T : Component {
        T comp = gameObject.GetComponentInParent<T>() ?? gameObject.GetComponentInChildren<T>();
        return comp;
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

        var scannedPositions = new List<Vector3>();
        foreach (Collider collider in colliders) {
            if (collider == null || collider.transform == null) continue;
            
            // Valuables
            if (ScanItems<ValuableObject>(collider, ConfigManager.shouldScanValuables, scanPosition, ConfigManager.scanRadiusValuablesOverride, scannedPositions, DiscoverValuableState))
                continue;
            
            // Enemies
            if (ScanItems<EnemyRigidbody>(collider, ConfigManager.shouldScanEnemies, scanPosition, ConfigManager.scanRadiusEnemiesOverride, scannedPositions, DiscoverEnemyState))
                continue;

            // Heads
            if (ScanItems<PlayerDeathHead>(collider, ConfigManager.shouldScanHeads, scanPosition, ConfigManager.scanRadiusHeadsOverride, scannedPositions, discoverHeadState))
                continue;
            
            // Items
            if (ScanItems<ItemAttributes>(collider, ConfigManager.shouldScanItems, scanPosition, ConfigManager.scanRadiusItemsOverride, scannedPositions, discoverItemState))
                continue;
            
            // Cosmetics
            if (ScanItems<CosmeticWorldObject>(collider, ConfigManager.shouldScanCosmetics, scanPosition, ConfigManager.scanRadiusCosmeticsOverride, scannedPositions, discoverCosmeticState))
                continue;
            
            // HAS TO BE THE LAST
            // Anything that has ValuableDiscoverCustom
            if (GetComponentOfType<ValuableDiscoverCustom>(collider) == null && ScanItems<ValuableDiscoverCustom>(collider, ConfigManager.shouldScanCustom, scanPosition, ConfigManager.scanRadiusCustomOverride, scannedPositions, ValuableDiscoverGraphic.State.Custom))
                continue;
        }

        lastScanTime = Time.time;
        if (ScannerGUI.Instance != null) {
            ScannerGUI.Instance.NotifyScan();
        }
    }

    private static bool ScanItems<TComponent>(
            Collider collider, ConfigEntry<bool> shouldScan, Vector3 scanPosition, 
            ConfigEntry<float> scanRadiusOverride, List<Vector3> scannedPositions, ValuableDiscoverGraphic.State state
        ) where TComponent : MonoBehaviour {
        if (!shouldScan.Value) return false;
        
        TComponent component = GetComponentOfType<TComponent>(collider);
        if (component == null) return false;
        
        float scanRadius = scanRadiusOverride.Value > 0f 
            ? scanRadiusOverride.Value 
            : ConfigManager.scanRadius.Value;
        if (Vector3.Distance(component.transform.position, scanPosition) > scanRadius) return false;
                    
        if (HasScannedItemsNearby(component.transform.position, scannedPositions)) return false;

        // TODO: Cosmetics?
        if (ConfigManager.onlyScanDiscoveredValuables.Value && component is ValuableObject { discovered: false }) return false;
        
        // TODO: Cosmetics.
        if (ConfigManager.multiplayerReveal.Value && component is ValuableObject { discovered: false } discoverable) {
            discoverable.Discover(state);
        } else {
            PhysGrabObject physGrabObject = GetComponentOfType<PhysGrabObject>(collider);
            if (physGrabObject == null) return true; // No point in trying any further
            ValuableDiscover.instance.New(physGrabObject, state, GetComponentOfType<ValuableDiscoverCustom>(collider));
        }
        return true;
    }

    private static bool HasScannedItemsNearby(Vector3 itemPos, System.Collections.Generic.List<Vector3> scannedPositions) {
        foreach (Vector3 pos in scannedPositions) {
            if (Vector3.Distance(pos, itemPos) < 0.1f) {
                return true;
            }
        }
        scannedPositions.Add(itemPos);
        return false;
    }

    public static void Update() {}

    public static bool IsOnCooldown() {
        return Time.time - lastScanTime < ConfigManager.cooldown.Value;
    }

    public static float GetRemainingCooldown() {
        return Mathf.Max(0, ConfigManager.cooldown.Value - (Time.time - lastScanTime));
    }
}