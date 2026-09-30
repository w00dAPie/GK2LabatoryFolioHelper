using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LazyBearTechnology;
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
    private static TMP_SpriteAsset spriteAsset;

    private const float ExternalSpacingPixels = 12f;
    private const float DefaultCardWidth = 310f;
    private const float CardScale = 0.8f;
    private static readonly Color CardColor = new Color(0.14f, 0.15f, 0.18f, 0.96f);
    private static readonly Vector2 DefaultPosition = new Vector2(-16f, -85f);
    private static bool layoutUpdateQueued;
    private static bool refreshPending;
    private static bool updatingPosition;
    private static List<PinnedFormulaManager.PinnedFormulaViewData> renderedPins;

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
            spriteAsset = sourceLabel.spriteAsset;
        }

        panelObject = new GameObject(
            "GK2LaboratoryPins",
            typeof(RectTransform),
            typeof(LayoutElement)
        );

        // The game owns the parent layout; this panel owns its own position and size.
        panelObject.GetComponent<LayoutElement>().ignoreLayout = true;
        AttachToHud(panelObject, rightUpGroup.transform);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);

        panelRect.anchoredPosition = DefaultPosition;

        panelRect.sizeDelta = new Vector2(DefaultCardWidth, 0f);
        // Keep the same size with zero, one or several external recipe pins.
        panelRect.localScale = new Vector3(CardScale, CardScale, 1f);

        // Observe the anchor, not our own rect: our writes must not enqueue themselves.
        if (rightUpGroup.transform is RectTransform anchor)
        {
            LaboratoryPinsLayoutListener.Observe(anchor);
        }

        GameObject contentObject = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter)
        );

        AttachToHud(contentObject, panelObject.transform);

        contentRoot = contentObject.GetComponent<RectTransform>();

        contentRoot.anchorMin = new Vector2(0f, 1f);

        contentRoot.anchorMax = new Vector2(1f, 1f);

        contentRoot.pivot = new Vector2(0.5f, 1f);

        contentRoot.anchoredPosition = Vector2.zero;

        contentRoot.sizeDelta = Vector2.zero;
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter
            .FitMode
            .PreferredSize;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(0, 0, 0, 0);

        layout.spacing = 6f;

        layout.childAlignment = TextAnchor.UpperLeft;

        layout.childControlWidth = true;
        layout.childControlHeight = true;

        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        PinnedFormulaManager.PinsChanged -= Refresh;
        PinnedFormulaManager.PinsChanged += Refresh;

        PinsInventoryWatcher.Initialize();

        HudPositionPreview.Attach(rightUpGroup, sourceLabel);

        Refresh();

        Plugin.Log.LogDebug("Laboratory pins HUD initialized.");
    }

    public static void Refresh()
    {
        if (panelObject == null || contentRoot == null)
        {
            return;
        }

        UpdatePosition();

        List<PinnedFormulaManager.PinnedFormulaViewData> pins =
            PinnedFormulaManager.GetPinnedViewData();
        HudPositionPreview.AddSampleIfEmpty(pins);

        if (HaveSameContent(renderedPins, pins))
        {
            return;
        }

        ClearChildren(contentRoot);

        foreach (PinnedFormulaManager.PinnedFormulaViewData pin in pins)
        {
            CreateFormulaBlock(pin);
        }

        renderedPins = pins;
        if (panelObject.activeSelf != (pins.Count > 0))
        {
            panelObject.SetActive(pins.Count > 0);
        }

        if (pins.Count > 0)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }
    }

    internal static void RequestRefresh()
    {
        if (panelObject == null || refreshPending)
        {
            return;
        }

        refreshPending = true;
        // Build graphics before the canvas layout/graphic rebuild, never after it.
        Canvas.preWillRenderCanvases += BeforeLayout;
    }

    private static void BeforeLayout()
    {
        Canvas.preWillRenderCanvases -= BeforeLayout;
        refreshPending = false;
        Refresh();
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
        UpdatePosition();
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
            typeof(Image),
            typeof(LazyButton),
            typeof(HorizontalLayoutGroup)
        );
        AttachToHud(blockObject, contentRoot);

        Image background = blockObject.GetComponent<Image>();
        background.color = CardColor;
        background.raycastTarget = true;

        LazyButton button = blockObject.GetComponent<LazyButton>();

        AlchemyFormulaDef formula = GameBalance.Me.GetDataOrNull<AlchemyFormulaDef>(pin.FormulaId);

        if (button != null && formula != null)
        {
            button.interactable = true;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => PinnedFormulaManager.Toggle(formula));
        }

        HorizontalLayoutGroup blockLayout = blockObject.GetComponent<HorizontalLayoutGroup>();
        blockLayout.padding = new RectOffset(6, 6, 4, 4);
        blockLayout.spacing = 8f;
        blockLayout.childAlignment = TextAnchor.UpperLeft;
        blockLayout.childControlWidth = true;
        blockLayout.childControlHeight = true;
        blockLayout.childForceExpandWidth = false;
        blockLayout.childForceExpandHeight = false;

        GameObject header = new GameObject(
            "Header",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );
        AttachToHud(header, blockObject.transform);

        LayoutElement headerSize = header.GetComponent<LayoutElement>();
        headerSize.minWidth = 108f;
        headerSize.preferredWidth = 108f;
        headerSize.flexibleWidth = 0f;
        headerSize.minHeight = 30f;

        HorizontalLayoutGroup headerLayout = header.GetComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 7f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        CreateItemIcon(header.transform, formula?.ItemDef, 30f, true);

        GameObject titleGroup = new GameObject(
            "TitleAndStatus",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(LayoutElement)
        );
        AttachToHud(titleGroup, header.transform);

        LayoutElement titleSize = titleGroup.GetComponent<LayoutElement>();
        titleSize.minWidth = 0f;
        titleSize.preferredWidth = 0f;
        titleSize.flexibleWidth = 1f;

        VerticalLayoutGroup titleLayout = titleGroup.GetComponent<VerticalLayoutGroup>();
        titleLayout.childControlWidth = true;
        titleLayout.childControlHeight = true;
        titleLayout.childForceExpandWidth = true;
        titleLayout.childForceExpandHeight = false;
        titleLayout.childAlignment = TextAnchor.MiddleLeft;

        CreateText(
            titleGroup.transform,
            pin.Name,
            16f,
            21f,
            TextAlignmentOptions.Left,
            Color.white,
            wrap: true
        );

        CreateText(
            titleGroup.transform,
            pin.StatusText,
            13f,
            16f,
            TextAlignmentOptions.Left,
            pin.IsCraftable ? new Color(0.50f, 0.84f, 0.42f) : Color.white,
            wrap: true
        );

        GameObject ingredients = new GameObject(
            "Ingredients",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(LayoutElement)
        );
        AttachToHud(ingredients, blockObject.transform);

        LayoutElement ingredientSize = ingredients.GetComponent<LayoutElement>();
        ingredientSize.minWidth = 0f;
        ingredientSize.preferredWidth = 0f;
        ingredientSize.flexibleWidth = 1f;

        VerticalLayoutGroup ingredientLayout = ingredients.GetComponent<VerticalLayoutGroup>();
        ingredientLayout.spacing = 3f;
        ingredientLayout.childAlignment = TextAnchor.MiddleLeft;
        ingredientLayout.childControlWidth = true;
        ingredientLayout.childControlHeight = true;
        ingredientLayout.childForceExpandWidth = true;
        ingredientLayout.childForceExpandHeight = false;

        foreach (PinnedFormulaManager.PinnedIngredientViewData ingredient in pin.Ingredients)
        {
            CreateIngredientRow(ingredients.transform, ingredient);
        }

        if (pin.NoPowderIngredients.Count > 0)
        {
            CreateText(
                ingredients.transform,
                "No powder",
                10f,
                13f,
                TextAlignmentOptions.Left,
                new Color(0.72f, 0.72f, 0.72f)
            );

            foreach (
                PinnedFormulaManager.PinnedIngredientViewData ingredient in pin.NoPowderIngredients
            )
            {
                CreateIngredientRow(ingredients.transform, ingredient, alternative: true);
            }
        }
    }

    private static void CreateIngredientRow(
        Transform parent,
        PinnedFormulaManager.PinnedIngredientViewData ingredient,
        bool alternative = false
    )
    {
        Color color =
            ingredient.IsAvailable ? new Color(0.50f, 0.84f, 0.42f)
            : ingredient.IsBuyable ? new Color(0.45f, 0.72f, 0.95f)
            : new Color(0.91f, 0.70f, 0.42f);

        GameObject row = new GameObject(
            $"Ingredient_{ingredient.ItemId}",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );

        AttachToHud(row, parent);

        LayoutElement rowElement = row.GetComponent<LayoutElement>();
        rowElement.minHeight = alternative ? 14f : 16f;

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();

        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ItemDef item = GameBalance.Me.GetDataOrNull<ItemDef>(ingredient.ItemId);

        CreateItemIcon(row.transform, item, alternative ? 12f : 14f, false);

        string nameText = ingredient.Name;

        if (ingredient.IsBuyable && !string.IsNullOrEmpty(ingredient.VendorName))
        {
            nameText += $"  <size=10>{ingredient.VendorName} ({ingredient.VendorStock})</size>";
        }

        TextMeshProUGUI name = CreateText(
            row.transform,
            nameText,
            alternative ? 11f : 12f,
            alternative ? 14f : 16f,
            TextAlignmentOptions.Left,
            color,
            wrap: true
        );

        LayoutElement nameLayout = name.GetComponent<LayoutElement>();

        nameLayout.minWidth = 0f;
        nameLayout.preferredWidth = 0f;
        nameLayout.flexibleWidth = 1f;

        TextMeshProUGUI count = CreateText(
            row.transform,
            ingredient.CountText,
            alternative ? 11f : 12f,
            alternative ? 14f : 16f,
            TextAlignmentOptions.Right,
            color
        );

        LayoutElement countLayout = count.GetComponent<LayoutElement>();

        countLayout.minWidth = 30f;
        countLayout.preferredWidth = 30f;
        countLayout.flexibleWidth = 0f;
    }

    private static void CreateItemIcon(Transform parent, ItemDef item, float side, bool framed)
    {
        GameObject slot = new GameObject("ItemIcon", typeof(RectTransform), typeof(LayoutElement));
        AttachToHud(slot, parent);
        LayoutElement layout = slot.GetComponent<LayoutElement>();
        layout.minWidth = side;
        layout.preferredWidth = side;
        layout.minHeight = side;
        layout.preferredHeight = side;

        if (framed)
        {
            Image background = slot.AddComponent<Image>();
            background.color = new Color(0.22f, 0.23f, 0.28f);
            background.raycastTarget = false;
        }

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        AttachToHud(iconObject, slot.transform);
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        float inset = framed ? 3f : 0f;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);

        Image icon = iconObject.GetComponent<Image>();
        // Use the same sprite collection as UIItemCell.Draw; no interactive cell is cloned.
        icon.sprite =
            item == null
                ? null
                : LazySingletonSO<EasySpritesCollection>.Instance.GetSprite(item.iconId);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = icon.sprite != null;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string text,
        float fontSize,
        float height,
        TextAlignmentOptions alignment,
        Color color,
        bool wrap = false
    )
    {
        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement)
        );

        AttachToHud(textObject, parent);

        LayoutElement layout = textObject.GetComponent<LayoutElement>();

        layout.minHeight = height;
        layout.preferredHeight = wrap ? -1f : height;

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
        label.spriteAsset = spriteAsset;

        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;

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

    private static void AttachToHud(GameObject child, Transform parent)
    {
        // SetParent does not inherit layers. Default-layer graphics in a WorldSpace
        // canvas can be picked up by the game's world-camera passes.
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
    }

    private static bool HaveSameContent(
        List<PinnedFormulaManager.PinnedFormulaViewData> previous,
        List<PinnedFormulaManager.PinnedFormulaViewData> current
    )
    {
        if (previous == null || previous.Count != current.Count)
        {
            return false;
        }

        for (int i = 0; i < current.Count; i++)
        {
            var before = previous[i];
            var after = current[i];

            if (
                before.FormulaId != after.FormulaId
                || before.Name != after.Name
                || before.StatusText != after.StatusText
                || before.IsCraftable != after.IsCraftable
                || !HaveSameIngredients(before.Ingredients, after.Ingredients)
                || !HaveSameIngredients(before.NoPowderIngredients, after.NoPowderIngredients)
            )
            {
                return false;
            }
        }

        return true;
    }

    private static bool HaveSameIngredients(
        List<PinnedFormulaManager.PinnedIngredientViewData> previous,
        List<PinnedFormulaManager.PinnedIngredientViewData> current
    )
    {
        if (previous == null || current == null)
        {
            return previous == current;
        }

        if (previous.Count != current.Count)
        {
            return false;
        }

        for (int i = 0; i < current.Count; i++)
        {
            var before = previous[i];
            var after = current[i];

            if (
                before.ItemId != after.ItemId
                || before.Name != after.Name
                || before.CountText != after.CountText
                || before.IsAvailable != after.IsAvailable
                || before.IsBuyable != after.IsBuyable
                || before.VendorName != after.VendorName
                || before.VendorStock != after.VendorStock
            )
            {
                return false;
            }
        }

        return true;
    }

    private static void DestroyExisting()
    {
        HudPositionPreview.Shutdown();
        Canvas.preWillRenderCanvases -= BeforeLayout;
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
        renderedPins = null;
        font = null;
        fontMaterial = null;
        spriteAsset = null;
    }

    private static void UpdatePosition()
    {
        if (panelObject == null || updatingPosition)
        {
            return;
        }

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        RectTransform parentRect = panelRect.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        updatingPosition = true;
        try
        {
            // Calculate first. Do not briefly reset the live rect to the fallback:
            // those intermediate writes invalidate layout and trigger dimension callbacks.
            Vector2 targetPosition = DefaultPosition;

            Camera camera = ExternalHudCompatibility.GetCanvasCamera(parentRect);
            Vector3 defaultLocalPoint = new Vector3(
                parentRect.rect.xMax + DefaultPosition.x,
                parentRect.rect.yMax + DefaultPosition.y,
                panelRect.localPosition.z
            );
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                camera,
                parentRect.TransformPoint(defaultLocalPoint)
            );
            Vector2 leftPoint = RectTransformUtility.WorldToScreenPoint(
                camera,
                parentRect.TransformPoint(
                    defaultLocalPoint
                        - new Vector3(panelRect.rect.width * panelRect.localScale.x, 0f, 0f)
                )
            );
            float screenWidth = Mathf.Abs(screenPoint.x - leftPoint.x);

            if (ExternalHudCompatibility.TryGetRecipePinScreenRect(out Rect recipeBounds))
            {
                screenPoint.x = recipeBounds.xMax;
            }

            if (
                ExternalHudCompatibility.TryGetExternalBottomScreenY(
                    screenPoint.x - screenWidth,
                    screenPoint.x,
                    out float bottomScreenY
                )
            )
            {
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
                    targetPosition = new Vector2(
                        localPoint.x - parentRect.rect.xMax,
                        localPoint.y - parentRect.rect.yMax
                    );
                }
            }

            if (panelRect.anchoredPosition != targetPosition)
            {
                panelRect.anchoredPosition = targetPosition;
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

    private void OnCanvasGroupChanged()
    {
        LaboratoryPinsHud.RequestPositionUpdate();
    }

    private void OnTransformParentChanged()
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
