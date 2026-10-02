using System.Collections.Generic;
using GK2LaboratoryFolioHelper.Alchemy;
using GK2LaboratoryFolioHelper.Compatibility;
using GK2LaboratoryFolioHelper.Configuration;
using GK2LaboratoryFolioHelper.UI.Typography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2LaboratoryFolioHelper.UI;

// Opt-in visual fixture: real RectTransforms go through the production bounds
// and visibility checks. It never adds pins to the player's pin manager.
internal sealed class HudPositionPreview : MonoBehaviour
{
    private static readonly string[] Scenarios =
    {
        "Off - normal HUD",
        "Baseline - no external panel",
        "GK2RecipePin - small panel on the right",
        "GK2RecipePin - tall panel on the right",
        "Kebo - right",
        "Kebo - left (no displacement expected)",
        "Kebo - center (only displace if overlapping)",
        "Both panels - use the lowest bottom edge",
        "Kebo - transparent (no displacement expected)",
        "Kebo - inactive (no displacement expected)",
    };

    private static HudPositionPreview instance;

    private GameObject canvasObject;
    private RectTransform recipePanel;
    private RectTransform keboPanel;
    private TextMeshProUGUI status;
    private int scenario;

    private static bool IsActive =>
        instance != null && instance.isActiveAndEnabled && instance.scenario > 0;

    internal static void Attach(GameObject anchor, TextMeshProUGUI fontSource)
    {
        Shutdown();
        if (HudPreviewConfig.Enabled?.Value != true)
        {
            return;
        }

        instance = anchor.AddComponent<HudPositionPreview>();
        instance.Build(fontSource);
    }

    private void Build(TextMeshProUGUI fontSource)
    {
        // Separate overlay canvas also exercises conversion from a different
        // canvas into the game's HUD, as with GK2RecipePin's tracker.
        canvasObject = new GameObject(
            "FormulaHelper_PositionPreview",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30001;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        recipePanel = CreatePanel(
            "GK2RecipePin (simulated)",
            new Color(0.12f, 0.35f, 0.65f, 0.85f),
            fontSource
        );
        keboPanel = CreatePanel(
            "Kebo Recipe Pins (simulated)",
            new Color(0.65f, 0.32f, 0.10f, 0.85f),
            fontSource
        );
        RectTransform statusPanel = CreatePanel(
            "Position preview",
            new Color(0.08f, 0.08f, 0.08f, 0.9f),
            fontSource
        );
        Place(statusPanel, new Vector2(0f, 0f), new Vector2(16f, 100f), new Vector2(620f, 85f));
        status = statusPanel.GetComponentInChildren<TextMeshProUGUI>();
        ApplyScenario();
        canvasObject.SetActive(isActiveAndEnabled);
    }

    private RectTransform CreatePanel(string title, Color color, TextMeshProUGUI fontSource)
    {
        var panel = new GameObject(
            title,
            typeof(RectTransform),
            typeof(Image),
            typeof(CanvasGroup)
        );
        panel.transform.SetParent(canvasObject.transform, false);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 12f);
        labelRect.offsetMax = new Vector2(-12f, -12f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (fontSource != null)
        {
            label.font = fontSource.font;
            label.fontSharedMaterial = fontSource.fontSharedMaterial;
        }

        label.text = title;
        label.fontSize = 22f;
        label.color = Color.white;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        ModTypography.ApplyPreviewText(label);
        RectTransform rect = panel.GetComponent<RectTransform>();
        LaboratoryPinsLayoutListener.Observe(rect);
        return rect;
    }

    private static void Place(RectTransform panel, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        panel.anchorMin = anchor;
        panel.anchorMax = anchor;
        panel.pivot = new Vector2(anchor.x, 1f);
        panel.anchoredPosition = offset;
        panel.sizeDelta = size;
    }

    private void ApplyScenario()
    {
        Place(
            recipePanel,
            Vector2.one,
            new Vector2(-12f, -110f),
            new Vector2(380f, scenario == 3 ? 350f : 150f)
        );
        Vector2 anchor =
            scenario == 5 ? new Vector2(0f, 1f)
            : scenario == 6 ? new Vector2(0.5f, 1f)
            : Vector2.one;
        Place(
            keboPanel,
            anchor,
            new Vector2(
                scenario == 5 ? 16f
                    : scenario == 6 ? 0f
                    : -16f,
                scenario == 7 ? -280f : -110f
            ),
            new Vector2(420f, 240f)
        );
        recipePanel.gameObject.SetActive(scenario == 2 || scenario == 3 || scenario == 7);
        keboPanel.gameObject.SetActive(scenario >= 4 && scenario <= 8);
        keboPanel.GetComponent<CanvasGroup>().alpha = scenario == 8 ? 0f : 1f;
        status.text =
            $"HUD position test {scenario}/{Scenarios.Length - 1}: {Scenarios[scenario]}\n{HudPreviewConfig.PreviousScenario.Value}: previous | {HudPreviewConfig.NextScenario.Value}: next";
        LaboratoryPinsHud.RequestRefresh();
        LaboratoryPinsHud.RequestPositionUpdate();
    }

    private void Update()
    {
        int step =
            HudPreviewConfig.NextScenario.Value.IsDown() ? 1
            : HudPreviewConfig.PreviousScenario.Value.IsDown() ? -1
            : 0;
        if (step != 0)
        {
            scenario = (scenario + step + Scenarios.Length) % Scenarios.Length;
            ApplyScenario();
        }
    }

    internal static void IncludeBottom(float left, float right, ref float bottom, ref bool found)
    {
        if (!IsActive)
        {
            return;
        }

        ExternalHudCompatibility.IncludeOverlappingBottom(
            instance.recipePanel,
            left,
            right,
            ref bottom,
            ref found
        );
        ExternalHudCompatibility.IncludeOverlappingBottom(
            instance.keboPanel,
            left,
            right,
            ref bottom,
            ref found
        );
    }

    internal static void AddSampleIfEmpty(List<PinnedFormulaManager.PinnedFormulaViewData> pins)
    {
        if (IsActive && pins.Count == 0)
        {
            pins.Add(
                new PinnedFormulaManager.PinnedFormulaViewData
                {
                    FormulaId = "__hud_position_preview__",
                    Name = "Position test",
                    StatusText = "Preview only",
                }
            );
        }
    }

    private void OnEnable()
    {
        if (canvasObject != null)
        {
            canvasObject.SetActive(true);
        }

        LaboratoryPinsHud.RequestRefresh();
    }

    private void OnDisable()
    {
        if (canvasObject != null)
        {
            canvasObject.SetActive(false);
        }

        LaboratoryPinsHud.RequestRefresh();
        LaboratoryPinsHud.RequestPositionUpdate();
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    internal static void Shutdown()
    {
        if (instance != null)
        {
            instance.enabled = false;
            Destroy(instance);
            instance = null;
        }
    }
}
