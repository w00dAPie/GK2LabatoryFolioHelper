using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class LaboratoryPinsHud
{
    private static readonly FieldInfo RightUpGroupField = AccessTools.Field(
        typeof(HUD),
        "rightUpGroup"
    );

    private static readonly FieldInfo HappinessLabelField = AccessTools.Field(
        typeof(HUD),
        "happinessLabel"
    );

    private static GameObject panelObject;
    private static RectTransform contentRoot;

    private static TMP_FontAsset font;
    private static Material fontMaterial;

    private const float ExternalSpacingPixels = 12f;
    private static readonly Vector2 DefaultPosition = new Vector2(-16f, -85f);
    private static bool layoutUpdateQueued;
    private static bool refreshPending;
    private static bool updatingPosition;

    public static void Initialize(HUD hud)
    {
        if (hud == null)
        {
            return;
        }

        DestroyExisting();

        GameObject rightUpGroup = RightUpGroupField?.GetValue(hud) as GameObject;

        if (rightUpGroup == null)
        {
            Plugin.Log.LogWarning("Could not find HUD.rightUpGroup.");

            return;
        }

        TextMeshProUGUI sourceLabel = HappinessLabelField?.GetValue(hud) as TextMeshProUGUI;

        if (sourceLabel != null)
        {
            font = sourceLabel.font;
            fontMaterial = sourceLabel.fontSharedMaterial;
        }

        panelObject = new GameObject("GK2LaboratoryPins", typeof(RectTransform));

        panelObject.transform.SetParent(rightUpGroup.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);

        panelRect.anchoredPosition = DefaultPosition;

        panelRect.sizeDelta = new Vector2(540f, 430f);

        LaboratoryPinsLayoutListener.Observe(panelRect);

        GameObject contentObject = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup)
        );

        contentObject.transform.SetParent(panelObject.transform, false);

        contentRoot = contentObject.GetComponent<RectTransform>();

        contentRoot.anchorMin = new Vector2(0f, 1f);

        contentRoot.anchorMax = new Vector2(1f, 1f);

        contentRoot.pivot = new Vector2(0.5f, 1f);

        contentRoot.anchoredPosition = Vector2.zero;

        contentRoot.sizeDelta = new Vector2(0f, 430f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(0, 0, 0, 0);

        layout.spacing = 7f;

        layout.childAlignment = TextAnchor.UpperLeft;

        layout.childControlWidth = true;
        layout.childControlHeight = true;

        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        PinnedFormulaManager.PinsChanged -= Refresh;
        PinnedFormulaManager.PinsChanged += Refresh;

        PinsInventoryWatcher.Initialize();

        Refresh();

        Plugin.Log.LogInfo("Laboratory pins HUD initialized.");
    }

    public static void Refresh()
    {
        if (panelObject == null || contentRoot == null)
        {
            return;
        }

        UpdatePosition();

        ClearChildren(contentRoot);

        List<PinnedFormulaManager.PinnedFormulaViewData> pins =
            PinnedFormulaManager.GetPinnedViewData();

        panelObject.SetActive(pins.Count > 0);

        foreach (PinnedFormulaManager.PinnedFormulaViewData pin in pins)
        {
            CreateFormulaBlock(pin);
        }
    }

    internal static void RequestRefresh()
    {
        if (panelObject == null)
        {
            return;
        }

        refreshPending = true;
        RequestPositionUpdate();
    }

    internal static void RequestPositionUpdate()
    {
        if (panelObject == null || layoutUpdateQueued || updatingPosition)
        {
            return;
        }

        // Subscribe only while work is pending, after Unity's layout registry.
        _ = CanvasUpdateRegistry.instance;
        layoutUpdateQueued = true;
        Canvas.willRenderCanvases += AfterLayout;
    }

    private static void AfterLayout()
    {
        Canvas.willRenderCanvases -= AfterLayout;
        layoutUpdateQueued = false;
        bool refresh = refreshPending;
        refreshPending = false;

        if (refresh)
        {
            Refresh();
        }
        else
        {
            UpdatePosition();
        }
    }

    internal static void Shutdown()
    {
        DestroyExisting();
        LaboratoryPinsLayoutListener.StopObserving();
    }

    private static void CreateFormulaBlock(PinnedFormulaManager.PinnedFormulaViewData pin)
    {
        GameObject blockObject = new GameObject(
            $"Pin_{pin.FormulaId}",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(LayoutElement)
        );

        blockObject.transform.SetParent(contentRoot, false);

        VerticalLayoutGroup blockLayout = blockObject.GetComponent<VerticalLayoutGroup>();

        blockLayout.spacing = 0f;

        blockLayout.childAlignment = TextAnchor.UpperLeft;

        blockLayout.childControlWidth = true;
        blockLayout.childControlHeight = true;

        blockLayout.childForceExpandWidth = true;
        blockLayout.childForceExpandHeight = false;

        CreateText(
            blockObject.transform,
            pin.Name,
            15f,
            19f,
            TextAlignmentOptions.Left,
            Color.white
        );

        Color statusColor = pin.IsCraftable ? new Color(0.50f, 0.84f, 0.42f) : Color.white;

        CreateText(
            blockObject.transform,
            pin.StatusText,
            13f,
            17f,
            TextAlignmentOptions.Left,
            statusColor
        );

        foreach (PinnedFormulaManager.PinnedIngredientViewData ingredient in pin.Ingredients)
        {
            CreateIngredientRow(blockObject.transform, ingredient);
        }

        float ingredientsHeight = 0f;

        foreach (PinnedFormulaManager.PinnedIngredientViewData ingredient in pin.Ingredients)
        {
            ingredientsHeight +=
                ingredient.IsBuyable && !string.IsNullOrEmpty(ingredient.VendorName) ? 33f : 18f;
        }

        float blockHeight = 19f + 17f + ingredientsHeight + 2f;

        LayoutElement blockElement = blockObject.GetComponent<LayoutElement>();

        blockElement.minHeight = blockHeight;
        blockElement.preferredHeight = blockHeight;
    }

    private static void CreateIngredientRow(
        Transform parent,
        PinnedFormulaManager.PinnedIngredientViewData ingredient
    )
    {
        bool showVendor = ingredient.IsBuyable && !string.IsNullOrEmpty(ingredient.VendorName);

        float rowHeight = showVendor ? 33f : 18f;

        GameObject containerObject = new GameObject(
            $"Ingredient_{ingredient.ItemId}",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(LayoutElement)
        );

        containerObject.transform.SetParent(parent, false);

        LayoutElement containerElement = containerObject.GetComponent<LayoutElement>();

        containerElement.minHeight = rowHeight;

        containerElement.preferredHeight = rowHeight;

        VerticalLayoutGroup containerLayout = containerObject.GetComponent<VerticalLayoutGroup>();

        containerLayout.spacing = 0f;

        containerLayout.padding = new RectOffset(0, 0, 0, 0);

        containerLayout.childAlignment = TextAnchor.UpperLeft;

        containerLayout.childControlWidth = true;

        containerLayout.childControlHeight = true;

        containerLayout.childForceExpandWidth = true;

        containerLayout.childForceExpandHeight = false;

        GameObject rowObject = new GameObject(
            "IngredientRow",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );

        rowObject.transform.SetParent(containerObject.transform, false);

        LayoutElement rowElement = rowObject.GetComponent<LayoutElement>();

        rowElement.minHeight = 18f;
        rowElement.preferredHeight = 18f;

        HorizontalLayoutGroup rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();

        rowLayout.spacing = 6f;

        rowLayout.padding = new RectOffset(10, 0, 0, 0);

        rowLayout.childAlignment = TextAnchor.MiddleLeft;

        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;

        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        Color ingredientColor;

        if (ingredient.IsAvailable)
        {
            ingredientColor = new Color(0.50f, 0.84f, 0.42f);
        }
        else if (ingredient.IsBuyable)
        {
            ingredientColor = new Color(0.45f, 0.72f, 0.95f);
        }
        else
        {
            ingredientColor = new Color(0.91f, 0.70f, 0.42f);
        }

        TextMeshProUGUI nameLabel = CreateText(
            rowObject.transform,
            ingredient.Name,
            13f,
            18f,
            TextAlignmentOptions.Left,
            ingredientColor
        );

        LayoutElement nameLayout = nameLabel.GetComponent<LayoutElement>();

        nameLayout.minWidth = 0f;
        nameLayout.preferredWidth = 0f;
        nameLayout.flexibleWidth = 1f;

        nameLabel.textWrappingMode = TextWrappingModes.NoWrap;

        nameLabel.overflowMode = TextOverflowModes.Overflow;

        TextMeshProUGUI countLabel = CreateText(
            rowObject.transform,
            ingredient.CountText,
            13f,
            18f,
            TextAlignmentOptions.Right,
            ingredientColor
        );

        LayoutElement countLayout = countLabel.GetComponent<LayoutElement>();

        countLayout.minWidth = 35f;
        countLayout.preferredWidth = 35f;
        countLayout.flexibleWidth = 0f;

        countLabel.textWrappingMode = TextWrappingModes.NoWrap;

        countLabel.overflowMode = TextOverflowModes.Overflow;

        if (showVendor)
        {
            GameObject vendorRowObject = new GameObject(
                "VendorRow",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement)
            );

            vendorRowObject.transform.SetParent(containerObject.transform, false);

            LayoutElement vendorRowElement = vendorRowObject.GetComponent<LayoutElement>();

            vendorRowElement.minHeight = 15f;
            vendorRowElement.preferredHeight = 15f;

            HorizontalLayoutGroup vendorRowLayout =
                vendorRowObject.GetComponent<HorizontalLayoutGroup>();

            vendorRowLayout.padding = new RectOffset(28, 0, 0, 0);

            vendorRowLayout.spacing = 0f;

            vendorRowLayout.childAlignment = TextAnchor.MiddleLeft;

            vendorRowLayout.childControlWidth = true;
            vendorRowLayout.childControlHeight = true;

            vendorRowLayout.childForceExpandWidth = true;
            vendorRowLayout.childForceExpandHeight = false;

            TextMeshProUGUI vendorLabel = CreateText(
                vendorRowObject.transform,
                $"{ingredient.VendorName} ({ingredient.VendorStock})",
                11f,
                15f,
                TextAlignmentOptions.Left,
                ingredientColor
            );
        }
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string text,
        float fontSize,
        float height,
        TextAlignmentOptions alignment,
        Color color
    )
    {
        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement)
        );

        textObject.transform.SetParent(parent, false);

        LayoutElement layout = textObject.GetComponent<LayoutElement>();

        layout.minHeight = height;
        layout.preferredHeight = height;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();

        if (font != null)
        {
            label.font = font;
        }

        if (fontMaterial != null)
        {
            label.fontSharedMaterial = fontMaterial;
        }

        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;

        label.richText = true;
        label.raycastTarget = false;

        label.textWrappingMode = TextWrappingModes.NoWrap;

        return label;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            // Nested RecipePin hooks can refresh more than once before Destroy runs.
            parent.GetChild(i).gameObject.SetActive(false);
            Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    private static void DestroyExisting()
    {
        Canvas.willRenderCanvases -= AfterLayout;
        layoutUpdateQueued = false;
        refreshPending = false;
        PinnedFormulaManager.PinsChanged -= Refresh;

        if (panelObject != null)
        {
            Object.Destroy(panelObject);
        }

        panelObject = null;
        contentRoot = null;
    }

    private static void UpdatePosition()
    {
        if (panelObject == null)
        {
            return;
        }

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();

        if (panelRect == null)
        {
            return;
        }

        RectTransform parentRect = panelRect.parent as RectTransform;
        if (parentRect == null || updatingPosition)
        {
            return;
        }

        updatingPosition = true;
        try
        {
            panelRect.anchoredPosition = DefaultPosition;
            if (!ExternalHudCompatibility.TryGetExternalBottomScreenY(out float bottomScreenY))
            {
                return;
            }

            Camera camera = ExternalHudCompatibility.GetCanvasCamera(parentRect);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                camera,
                panelRect.position
            );
            screenPoint.y = bottomScreenY - ExternalSpacingPixels;

            if (
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPoint,
                    camera,
                    out Vector2 localPoint
                )
            )
            {
                // ScreenPointToLocalPoint returns parent-pivot coordinates.
                // anchoredPosition is relative to the panel's top-right anchor.
                panelRect.anchoredPosition = new Vector2(
                    DefaultPosition.x,
                    localPoint.y - parentRect.rect.yMax
                );
            }
        }
        finally
        {
            updatingPosition = false;
        }
    }
}

internal sealed class LaboratoryPinsLayoutListener : MonoBehaviour
{
    private static readonly List<LaboratoryPinsLayoutListener> Listeners = new();

    internal static void Observe(RectTransform root)
    {
        if (root.GetComponent<LaboratoryPinsLayoutListener>() == null)
        {
            Listeners.Add(root.gameObject.AddComponent<LaboratoryPinsLayoutListener>());
        }
    }

    internal static void StopObserving()
    {
        foreach (LaboratoryPinsLayoutListener listener in Listeners.ToArray())
        {
            if (listener != null)
            {
                Destroy(listener);
            }
        }

        Listeners.Clear();
    }

    private void OnRectTransformDimensionsChange()
    {
        LaboratoryPinsHud.RequestPositionUpdate();
    }

    private void OnEnable()
    {
        LaboratoryPinsHud.RequestPositionUpdate();
    }

    private void OnDisable()
    {
        LaboratoryPinsHud.RequestPositionUpdate();
    }

    private void OnDestroy()
    {
        Listeners.Remove(this);
        LaboratoryPinsHud.RequestPositionUpdate();
    }
}
