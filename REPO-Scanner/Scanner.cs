using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPO_Scanner;

public class Scanner {
    private const ValuableDiscoverGraphic.State DiscoverValuableState = ValuableDiscoverGraphic.State.Discover;
    private const ValuableDiscoverGraphic.State DiscoverEnemyState = ValuableDiscoverGraphic.State.Bad;
    // While identical to the Bad state, this allows you not to get stuck in the Bad state for like 4 seconds
    private static ValuableDiscoverGraphic.State discoverHeadState = NewCustomState(
        new Color(1f, 0.0f, 0.067f, 0.17f), 
        new Color(1f, 0.1f, 0.067f, 0.75f));
    private static ValuableDiscoverGraphic.State discoverItemState = NewCustomState(
        new Color(0.0f, 0.5f, 0.8f, 0.17f), 
        new Color(0.1f, 0.6f, 0.9f, 0.75f));
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
        //Plugin.Logger.LogInfo("Scanner initializing...");
        lastScanTime = ConfigManager.cooldown.Value;
        
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

        var scannedPositions = new System.Collections.Generic.List<Vector3>();
        foreach (Collider collider in colliders) {
            if (collider == null || collider.transform == null) continue;

            // Valuables
            if (ConfigManager.shouldScanValuables.Value) {
                ValuableObject valuable = collider.GetComponentInParent<ValuableObject>() ??
                                          collider.GetComponentInChildren<ValuableObject>();
                if (valuable != null) {
                    float scanRadius = ConfigManager.scanRadiusValuablesOverride.Value > 0f 
                        ? ConfigManager.scanRadiusValuablesOverride.Value 
                        : ConfigManager.scanRadius.Value;
                    if (Vector3.Distance(valuable.transform.position, scanPosition) > scanRadius) continue;
                    
                    if (ConfigManager.onlyScanDiscoveredValuables.Value && !valuable.discovered) continue;
                    if (HasScannedItemsNearby(valuable.transform.position, scannedPositions)) continue;
                    
                    if (ConfigManager.multiplayerReveal.Value) {
                        valuable.Discover(DiscoverValuableState);
                    } else {
                        ValuableDiscover.instance.New(valuable.physGrabObject, DiscoverValuableState);
                    }
                    continue;
                }
            }

            // Enemies
            if (ConfigManager.shouldScanEnemies.Value) {
                EnemyRigidbody enemy = collider.GetComponentInParent<EnemyRigidbody>() ??
                                       collider.GetComponentInChildren<EnemyRigidbody>();
                if (enemy != null) {
                        float scanRadius = ConfigManager.scanRadiusEnemiesOverride.Value > 0f 
                            ? ConfigManager.scanRadiusEnemiesOverride.Value 
                            : ConfigManager.scanRadius.Value;
                        if (Vector3.Distance(enemy.transform.position, scanPosition) > scanRadius) continue;
                        
                    if (HasScannedItemsNearby(enemy.transform.position, scannedPositions)) continue;
                    ValuableDiscover.instance.New(enemy.physGrabObject, DiscoverEnemyState);
                    continue;
                }
            }
            
            // Heads
            if (ConfigManager.shouldScanHeads.Value) {
                PlayerDeathHead head = collider.GetComponentInParent<PlayerDeathHead>() ??
                                       collider.GetComponentInChildren<PlayerDeathHead>();
                if (head != null) {
                    float scanRadius = ConfigManager.scanRadiusHeadsOverride.Value > 0f 
                        ? ConfigManager.scanRadiusHeadsOverride.Value 
                        : ConfigManager.scanRadius.Value;
                    if (Vector3.Distance(head.transform.position, scanPosition) > scanRadius) continue;
                    
                    if (HasScannedItemsNearby(head.transform.position, scannedPositions)) continue;
                    ValuableDiscover.instance.New(head.physGrabObject, discoverHeadState);
                    continue;
                }
            }
            
            // Items
            if (ConfigManager.shouldScanItems.Value) {
                ItemAttributes item = collider.GetComponentInParent<ItemAttributes>() ??
                                      collider.GetComponentInChildren<ItemAttributes>();
                if (item != null) {
                    float scanRadius = ConfigManager.scanRadiusItemsOverride.Value > 0f 
                        ? ConfigManager.scanRadiusItemsOverride.Value 
                        : ConfigManager.scanRadius.Value;
                    if (Vector3.Distance(item.transform.position, scanPosition) > scanRadius) continue;
                    
                    if (HasScannedItemsNearby(item.transform.position, scannedPositions)) continue;
                    ValuableDiscover.instance.New(item.physGrabObject, discoverItemState);
                    continue;
                }
            }
        }

        lastScanTime = Time.time;
        if (ScannerGUI.Instance != null) {
            ScannerGUI.Instance.NotifyScan();
        }
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