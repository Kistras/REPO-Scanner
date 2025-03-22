using Unity.VisualScripting;
using UnityEngine;

namespace REPO_Scanner;

public class ScannerGUI : MonoBehaviour {
    public static ScannerGUI Instance { get; private set; }
    
    // UI textures
    private Texture2D barTexture;
    private Texture2D backgroundTexture;
    
    // UI settings
    // TODO: Perhaps move to config
    private readonly float barWidth = 400f; // Doubled the width to 400f
    private readonly float barHeight = 15f;
    private float lastDisplayTime;
    private readonly float displayDuration = 3f; // How long to show "Ready" message after cooldown ends
    
    private readonly Color barColor = new Color(1f, 0.6f, 0.1f, 0.8f);
    private readonly Color readyColor = new Color(1f, 0.8f, 0.2f, 0.8f);
    
    void Awake() {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
        //Plugin.Logger.LogInfo("ScannerGUI instance initialized");
    }

    void Initialize() {
        lastDisplayTime = Plugin.cooldown.Value;
        
        // Create textures for UI elements
        barTexture = new Texture2D(1, 1);
        barTexture.SetPixel(0, 0, Color.white);
        barTexture.Apply();
        
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, Color.white);
        backgroundTexture.Apply();
        
        //Plugin.Logger.LogInfo("Scanner GUI textures initialized");
    }

    void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
        
        // Clean up textures
        if (barTexture != null)
            Destroy(barTexture);
        if (backgroundTexture != null)
            Destroy(backgroundTexture);
    }
    
    void OnGUI() {
        bool shouldShow = ShouldShowGUI();
        if (!shouldShow)
            return;
            
        float remainingCooldown = Scanner.GetRemainingCooldown();
        float cooldownRatio = remainingCooldown / Plugin.cooldown.Value;
        bool isOnCooldown = Scanner.IsOnCooldown();
        
        // Calculate fade based on cooldown state
        float displayAlpha;
        if (isOnCooldown) {
            displayAlpha = 1f;
            lastDisplayTime = Time.time;
        } else {
            float timeAfterCooldown = Time.time - lastDisplayTime;
            displayAlpha = Mathf.Clamp01(1f - (timeAfterCooldown / displayDuration));
        }
        
        if (displayAlpha <= 0.01f) return;

        // Bar position
        float xPos = (Screen.width - barWidth) / 2;
        float yPos = 30f; // 30 pixels from top
        
        // Background
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.6f * displayAlpha);
        GUI.DrawTexture(new Rect(xPos, yPos, barWidth, barHeight), backgroundTexture);
        
        // Foreground (filled portion) with orange color
        if (isOnCooldown) {
            GUI.color = new Color(barColor.r, barColor.g, barColor.b, barColor.a * displayAlpha);
            GUI.DrawTexture(new Rect(xPos, yPos, barWidth * (1f - cooldownRatio), barHeight), barTexture);
        } else {
            // Show full bar briefly
            GUI.color = new Color(readyColor.r, readyColor.g, readyColor.b, readyColor.a * displayAlpha);
            GUI.DrawTexture(new Rect(xPos, yPos, barWidth, barHeight), barTexture);
        }
        
        // Reset color
        GUI.color = Color.white;
        
        // Caption
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.font = GUI.skin.font;
        textStyle.fontSize = 14;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = new Color(1f, 1f, 1f, displayAlpha);
        
        string cooldownText = isOnCooldown 
            ? $"Scanner: {remainingCooldown:F1}s" 
            : "Scanner Ready";
            
        GUI.Label(new Rect(xPos, yPos + barHeight + 5f, barWidth, 20f), cooldownText, textStyle);
    }
    
    private bool ShouldShowGUI() {
        return PlayerController.instance != null && 
               LevelGenerator.Instance != null && 
               LevelGenerator.Instance.Generated && 
               !SemiFunc.MenuLevel();
    }
    
    // Called when a scan occurs
    public void NotifyScan() {
        lastDisplayTime = Time.time;
        //Plugin.Logger.LogInfo("ScannerGUI notified of scan");
    }
}
