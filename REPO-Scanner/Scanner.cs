using UnityEngine;

namespace REPO_Scanner;

public class Scanner {
    private const ValuableDiscoverGraphic.State DiscoverState = ValuableDiscoverGraphic.State.Discover;
    private static float lastScanTime;
    private static bool isInitialized = false;
    
    private static void Initialize() {
        if (isInitialized) return;
        //Plugin.Logger.LogInfo("Scanner initializing...");
        lastScanTime = Plugin.cooldown.Value;
        
        isInitialized = true;
        Plugin.Logger.LogInfo("Scanner initialized successfully");
    }

    public static void Scan() {
        if (Time.time - lastScanTime < Plugin.cooldown.Value) {
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
        
        Collider[] colliders = Physics.OverlapSphere(scanPosition, Plugin.scanRadius.Value);
        //Plugin.Logger.LogInfo($"Found {colliders.Length} colliders in scan range");
        
        foreach (Collider collider in colliders) {
            if (collider == null || collider.transform == null) continue;
            ValuableObject valuable = collider.gameObject.GetComponentInParent<ValuableObject>();
            if (valuable == null) {
                valuable = collider.gameObject.GetComponentInChildren<ValuableObject>();
            }
            if (valuable != null) {
                if (Plugin.multiplayerReveal.Value) {
                    valuable.Discover(DiscoverState);
                } else {
                    ValuableDiscover.instance.New(valuable.physGrabObject, DiscoverState);
                }
            }
        }

        lastScanTime = Time.time;
        
        if (ScannerGUI.Instance != null) {
            ScannerGUI.Instance.NotifyScan();
        }
    }

    public static void Update() {
    }

    public static bool IsOnCooldown() {
        return Time.time - lastScanTime < Plugin.cooldown.Value;
    }

    public static float GetRemainingCooldown() {
        return Mathf.Max(0, Plugin.cooldown.Value - (Time.time - lastScanTime));
    }
}