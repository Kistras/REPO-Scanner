using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace REPO_Scanner;

public class ScannerGUI : MonoBehaviour {
    public static ScannerGUI Instance { get; private set; }

    // UI Components - New
    private GameObject uiContainer;
    private Image backgroundBar;
    private Image progressBar;
    private RectTransform barRect;
    private TextMeshProUGUI statusText;

    // UI textures - Legacy
    private Texture2D barTexture;
    private Texture2D backgroundTexture;
    
    // UI settings
    private readonly float barWidth = 150;
    private readonly float barHeight = 6f;
    private readonly float legacyBarWidth = 400;
    private readonly float legacyBarHeight = 15f;
    private float lastDisplayTime;
    private readonly float displayDuration = 3f; // How long to show "Ready" message after cooldown ends
    
    private readonly Color barColor = new Color(1f, 0.6f, 0.1f, 0.8f);
    private readonly Color readyColor = new Color(1f, 0.8f, 0.2f, 0.8f);
    private CanvasGroup canvasGroup;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.transform.SetParent(HUDCanvas.instance.transform, false);
        Initialize();
    }

    void Initialize() {
        lastDisplayTime = ConfigManager.cooldown.Value;
        
        // Legacy version
        if (ConfigManager.IsLegacyVersionGUI) {
            barTexture = new Texture2D(1, 1);
            barTexture.SetPixel(0, 0, Color.white);
            barTexture.Apply();
        
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, Color.white);
            backgroundTexture.Apply();
            
            return;
        }
        
        // New version
        RectTransform hudCanvasRect = HUDCanvas.instance.rect;
        if (hudCanvasRect == null) {
            return;
        }

        // Create container
        uiContainer = new GameObject("ScannerUI");
        RectTransform containerRect = uiContainer.AddComponent<RectTransform>();
        containerRect.SetParent(hudCanvasRect, false);
        
        // Make GUI 4 times smaller
        float adjustedBarWidth = barWidth;
        float adjustedBarHeight = barHeight;
        
        containerRect.anchorMin = new Vector2(0.5f, 1f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0, -5f);
        containerRect.sizeDelta = new Vector2(adjustedBarWidth, adjustedBarHeight + 6.25f);
        
        // Add canvas group for fading
        canvasGroup = uiContainer.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;

        // Create background
        GameObject bgObj = new GameObject("Background");
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.SetParent(containerRect, false);
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(1, 0);
        bgRect.pivot = new Vector2(0.5f, 0);
        bgRect.sizeDelta = new Vector2(0, adjustedBarHeight);
        backgroundBar = bgObj.AddComponent<Image>();
        backgroundBar.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        // Create progress bar using mask instead of filled type
        GameObject barContainerObj = new GameObject("ProgressBarContainer");
        RectTransform barContainerRect = barContainerObj.AddComponent<RectTransform>();
        barContainerRect.SetParent(bgRect, false);
        barContainerRect.anchorMin = new Vector2(0, 0);
        barContainerRect.anchorMax = new Vector2(1, 1);
        barContainerRect.offsetMin = Vector2.zero;
        barContainerRect.offsetMax = Vector2.zero;

        // Add a mask component to clip the progress bar
        var mask = barContainerObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var maskImage = barContainerObj.AddComponent<Image>();
        maskImage.color = Color.white;

        // Create actual progress bar inside the mask
        GameObject barObj = new GameObject("ProgressBar");
        barRect = barObj.AddComponent<RectTransform>();
        barRect.SetParent(barContainerRect, false);
        barRect.anchorMin = new Vector2(0, 0);
        barRect.anchorMax = new Vector2(1, 1);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;
        progressBar = barObj.AddComponent<Image>();
        progressBar.color = barColor;

        // Create text
        GameObject textObj = new GameObject("StatusText");
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.SetParent(containerRect, false);
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0);
        textRect.pivot = new Vector2(0.5f, 0);
        textRect.anchoredPosition = new Vector2(0, -12f); 
        textRect.sizeDelta = new Vector2(0, 10f); 
        statusText = textObj.AddComponent<TextMeshProUGUI>();
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 6f;
        statusText.fontStyle = FontStyles.Bold;
        statusText.color = Color.white;
    }
    
    void OnGUI() {
        if (!ConfigManager.IsLegacyVersionGUI) return;
        
        bool shouldShow = ShouldShowGUI();
        if (!shouldShow)
            return;
            
        float remainingCooldown = Scanner.GetRemainingCooldown();
        float cooldownRatio = remainingCooldown / ConfigManager.cooldown.Value;
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
        float xPos = (Screen.width - legacyBarWidth) / 2;
        float yPos = 30f; // 30 pixels from top
        
        // Background
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.6f * displayAlpha);
        GUI.DrawTexture(new Rect(xPos, yPos, legacyBarWidth, legacyBarHeight), backgroundTexture);
        
        // Foreground (filled portion) with orange color
        if (isOnCooldown) {
            GUI.color = new Color(barColor.r, barColor.g, barColor.b, barColor.a * displayAlpha);
            GUI.DrawTexture(new Rect(xPos, yPos, legacyBarWidth * (1f - cooldownRatio), legacyBarHeight), barTexture);
        } else {
            // Show full bar briefly
            GUI.color = new Color(readyColor.r, readyColor.g, readyColor.b, readyColor.a * displayAlpha);
            GUI.DrawTexture(new Rect(xPos, yPos, legacyBarWidth, legacyBarHeight), barTexture);
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
            
        GUI.Label(new Rect(xPos, yPos + legacyBarHeight + 5f, legacyBarWidth, 20f), cooldownText, textStyle);
    }
    
    void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
        
        if (uiContainer != null)
            Destroy(uiContainer);
        if (barTexture != null)
            Destroy(barTexture);
        if (backgroundTexture != null)
            Destroy(backgroundTexture);
    }

    void Update() {
        if (!ShouldShowGUI() || uiContainer == null || ConfigManager.IsLegacyVersionGUI) {
            if (uiContainer != null) uiContainer.SetActive(false);
            return;
        }

        uiContainer.SetActive(true);
        
        float remainingCooldown = Scanner.GetRemainingCooldown();
        float cooldownRatio = remainingCooldown / ConfigManager.cooldown.Value;
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

        // Update the UI
        canvasGroup.alpha = displayAlpha;
        
        if (isOnCooldown) {
            float progress = 1f - cooldownRatio;
            barRect.anchorMax = new Vector2(progress, 1);
            progressBar.color = barColor;
            statusText.text = $"Scanner: {remainingCooldown:F1}s";
        } else {
            barRect.anchorMax = new Vector2(1, 1);
            progressBar.color = readyColor;
            statusText.text = "Scanner Ready";
        }
    }

    private bool ShouldShowGUI() {
        return ConfigManager.toCreateGUI.Value &&
               PlayerController.instance != null && 
               LevelGenerator.Instance != null && 
               LevelGenerator.Instance.Generated && 
               !SemiFunc.MenuLevel();
    }

    public void NotifyScan() {
        lastDisplayTime = Time.time;
    }
}
