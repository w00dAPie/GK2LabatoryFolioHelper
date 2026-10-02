using GK2LaboratoryFolioHelper.Alchemy;
using GK2LaboratoryFolioHelper.UI.Typography;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2LaboratoryFolioHelper.UI;

internal static class LaboratoryPinCardFactory
{
    private static readonly Color CardColor = new(0.14f, 0.15f, 0.18f, 0.96f);

    internal static void Create(
        RectTransform contentRoot,
        PinnedFormulaManager.PinnedFormulaViewData pin
    )
    {
        GameObject blockObject = new(
            $"Pin_{pin.FormulaId}",
            typeof(RectTransform),
            typeof(Image),
            typeof(LazyButton),
            typeof(VerticalLayoutGroup)
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

        VerticalLayoutGroup blockLayout = blockObject.GetComponent<VerticalLayoutGroup>();
        blockLayout.padding = new RectOffset(6, 6, 4, 4);
        blockLayout.spacing = 4f;
        blockLayout.childAlignment = TextAnchor.UpperLeft;
        blockLayout.childControlWidth = true;
        blockLayout.childControlHeight = true;
        blockLayout.childForceExpandWidth = true;
        blockLayout.childForceExpandHeight = false;

        /*
 * Header:
 *
 * [formula icon] [title]
 *                [status]
 */
        GameObject header = new(
            "Header",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );

        AttachToHud(header, blockObject.transform);

        LayoutElement headerSize = header.GetComponent<LayoutElement>();
        headerSize.minWidth = 0f;
        headerSize.preferredWidth = 0f;
        headerSize.flexibleWidth = 1f;
        headerSize.minHeight = 30f;

        HorizontalLayoutGroup headerLayout = header.GetComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 8f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        CreateItemIcon(header.transform, formula?.ItemDef, 30f, framed: true);

        GameObject titleGroup = new(
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
        titleLayout.spacing = 0f;
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
            wrap: false
        );

        TextMeshProUGUI statusLabel = CreateText(
            titleGroup.transform,
            pin.StatusText,
            13f,
            16f,
            TextAlignmentOptions.Left,
            pin.IsCraftable ? new Color(0.50f, 0.84f, 0.42f) : Color.white,
            wrap: false
        );

        ModTypography.ApplyStatus(statusLabel);

        /*
         * Ingredients:
         *
         * [ingredient icon] [ingredient name..................] [count]
         * [ingredient icon] [ingredient name..................] [count]
         */
        GameObject ingredients = new(
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

        if (pin.NoPowderIngredients.Count <= 0)
        {
            return;
        }

        TextMeshProUGUI alternativeLabel = CreateText(
            ingredients.transform,
            "Alt. recipe",
            10f,
            13f,
            TextAlignmentOptions.Left,
            new Color(0.72f, 0.72f, 0.72f)
        );

        ModTypography.ApplyText(alternativeLabel);

        foreach (
            PinnedFormulaManager.PinnedIngredientViewData ingredient in pin.NoPowderIngredients
        )
        {
            CreateIngredientRow(ingredients.transform, ingredient, alternative: true);
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

        GameObject row = new(
            $"Ingredient_{ingredient.ItemId}",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );

        AttachToHud(row, parent);

        row.GetComponent<LayoutElement>().minHeight = alternative ? 14f : 16f;

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ItemDef item = GameBalance.Me.GetDataOrNull<ItemDef>(ingredient.ItemId);

        CreateItemIcon(row.transform, item, alternative ? 12f : 14f, framed: false);

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

        if (ingredient.IsBuyable && !string.IsNullOrEmpty(ingredient.VendorName))
        {
            ModTypography.ApplyVendorText(name);
        }
        else
        {
            ModTypography.ApplyText(name);
        }

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

        ModTypography.ApplyCount(count);

        LayoutElement countLayout = count.GetComponent<LayoutElement>();
        countLayout.minWidth = 30f;
        countLayout.preferredWidth = 30f;
        countLayout.flexibleWidth = 0f;
    }

    private static void CreateItemIcon(Transform parent, ItemDef item, float side, bool framed)
    {
        GameObject slot = new("ItemIcon", typeof(RectTransform), typeof(LayoutElement));

        AttachToHud(slot, parent);

        LayoutElement layout = slot.GetComponent<LayoutElement>();
        layout.minWidth = side;
        layout.preferredWidth = side;
        layout.minHeight = side;
        layout.preferredHeight = side;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        if (framed)
        {
            Image background = slot.AddComponent<Image>();
            background.color = new Color(0.22f, 0.23f, 0.28f);
            background.raycastTarget = false;
        }

        GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));

        AttachToHud(iconObject, slot.transform);

        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;

        float inset = framed ? 3f : 0f;

        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);

        Image icon = iconObject.GetComponent<Image>();

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
        GameObject textObject = new(
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
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;

        ModTypography.ApplyTitle(label);

        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

        label.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;

        return label;
    }

    private static void AttachToHud(GameObject child, Transform parent)
    {
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
    }
}
