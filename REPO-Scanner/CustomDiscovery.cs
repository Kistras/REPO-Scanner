using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using Photon.Pun;
using UnityEngine;

namespace REPO_Scanner;

internal interface ICustomDiscovery {
    public bool ScanItems(Collider collider, Vector3 scanPosition, List<Vector3> scannedPositions);
}

internal class CustomDiscovery<T> : ICustomDiscovery where T : MonoBehaviour {
    [CanBeNull] public GameObject gameObject;
    [CanBeNull] public ValuableDiscoverCustom component;

    private ConfigCategory config;

    internal void UpdateData() {
        if (!component) return;
        component.colorCorner = config.CornerColor;
        component.colorMiddle = config.MiddleColor;
        component.waitTime = config.ScanLinger.Value;
    }

    public ValuableDiscoverCustom GetDiscover() {
        if (gameObject == null || !gameObject.activeSelf) {
            gameObject = new GameObject("ScannerCustomDiscover");
        }
        if (component == null) {
            component = gameObject.GetComponent<ValuableDiscoverCustom>();
            if (component == null) {
                try {
                    Patches.shouldSuppressValuableDiscoverCustomAwake = true;
                    component = gameObject.AddComponent<ValuableDiscoverCustom>();
                } finally {
                    Patches.shouldSuppressValuableDiscoverCustomAwake = false;
                }
                UpdateData();
                // Plugin.Logger.LogWarning($"Created new ValuableDiscoverCustom component for {typeof(T).Name} discovery with colors {config.CornerColor} and {config.MiddleColor} and linger {config.ScanLinger.Value}");
            }
        }
        return component;
    }
    
    [CanBeNull]
    private static TC GetComponentOfType<TC>(GameObject gameObject) where TC : Component {
        // I honestly don't remember why I was looking for gameComponent everywhere
        TC comp = gameObject.GetComponent<TC>() ?? gameObject.GetComponentInParent<TC>();
        // TC comp = gameObject.GetComponent<TC>();
        return comp;
    }
    // This might be useless ↓
    [CanBeNull]
    private static TC GetComponentOfType<TC>(Collider collider) where TC : Component {
        TC comp = collider.GetComponent<TC>() ?? collider.GetComponentInParent<TC>();
        return comp;
    }
    
    private static bool HasScannedItemsNearby(Vector3 itemPos, List<Vector3> scannedPositions) {
        foreach (Vector3 pos in scannedPositions) {
            if (Vector3.Distance(pos, itemPos) < 0.1f) {
                return true;
            }
        }
        scannedPositions.Add(itemPos);
        return false;
    }
    
    public bool ScanItems(Collider collider, Vector3 scanPosition, List<Vector3> scannedPositions) {
        if (!config.ShouldScan.Value) return false;
        
        T comp = GetComponentOfType<T>(collider);
        if (comp == null) return false;
        //Plugin.Logger.LogWarning($"Checking collider {collider.name} for component {typeof(T).Name}");
        GameObject go = comp.gameObject;
        
        float scanRadius = config.ScanRadius.Value > 0f 
            ? config.ScanRadius.Value 
            : ConfigManager.scanRadius.Value;
        if (Vector3.Distance(comp.transform.position, scanPosition) > scanRadius) return false;
        //Plugin.Logger.LogWarning("Passed radius check");

        if (HasScannedItemsNearby(comp.transform.position, scannedPositions)) return false;
        //Plugin.Logger.LogWarning("Dupliscan check");
        
        ValuableDiscoverCustom customDiscover = GetComponentOfType<ValuableDiscoverCustom>(go);
        if (config.OnlyScanDiscovered?.Value == true && (comp is ValuableObject { discovered: false } || customDiscover != null && !customDiscover.discovered)) return false;
        //Plugin.Logger.LogWarning("Passed only scan discovered check");
        
        if (config.MultiplayerReveal?.Value == true && (comp is ValuableObject { discovered: false } or ValuableDiscoverCustom { discovered: false } || customDiscover != null && !customDiscover.discovered)) {
            // TODO: add try except
            // Modified from vanilla ValuableObject.cs file. If it ever changes this is expected to break
            if (!GameManager.Multiplayer())
                switch (comp) {
                    case ValuableObject valuable: valuable.DiscoverRPC(); break;
                    case ValuableDiscoverCustom custom: custom.DiscoverRPC(); break;
                    default: customDiscover.DiscoverRPC(); break;
                }
            else {
                PhotonView view;
                switch (comp) {
                    case ValuableObject valuable: view = valuable.photonView; break;
                    case ValuableDiscoverCustom custom: view = custom.photonView; break;
                    default: view = customDiscover.photonView; break;
                }
                if (view.ViewID == 0)
                    return false;
                view.RPC("DiscoverRPC", RpcTarget.All);
            }
        }
        //Plugin.Logger.LogWarning("Passed multiplayer reveal check");
        PhysGrabObject physGrabObject = GetComponentOfType<PhysGrabObject>(go);
        if (physGrabObject == null) return true; // No point in trying any further
        ValuableDiscover.instance.New(physGrabObject, ValuableDiscoverGraphic.State.Custom, customDiscover ?? GetDiscover());
        //Plugin.Logger.LogWarning("Discovery complete");
        return true;
    }

    public CustomDiscovery(ConfigCategory config) {
        this.config = config;
        if (config.CornerColorConfig == null) return;
        
        config.CornerColorConfig.SettingChanged += (_, _) => UpdateData();
        config.MiddleColorConfig.SettingChanged += (_, _) => UpdateData();
        config.ScanLinger.SettingChanged += (_, _) => UpdateData();
    }
}