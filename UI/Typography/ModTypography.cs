using TMPro;

namespace GK2LaboratoryFolioHelper.UI.Typography;

internal static class ModTypography
{
    internal static void Initialize(TextMeshProUGUI source) =>
        TypographyFontResolver.Initialize(source);

    internal static void ApplyTitle(TMP_Text text) => ApplyReadableText(text);

    internal static void ApplyText(TMP_Text text) => ApplyReadableText(text);

    internal static void ApplyStatus(TMP_Text text)
    {
        ApplyReadableText(text);
        if (TypographyFontResolver.IsSharp && text != null)
        {
            text.lineSpacing = -1f;
        }
    }

    internal static void ApplyCount(TMP_Text text) => ApplyReadableText(text);

    internal static void ApplyVendorText(TMP_Text text) => ApplyReadableText(text);

    internal static void ApplyPreviewText(TMP_Text text) => ApplyReadableText(text);

    internal static void Shutdown() => TypographyFontResolver.Shutdown();

    private static void ApplyReadableText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        TMP_FontAsset font = TypographyFontResolver.ResolveFont();
        if (font != null)
        {
            text.font = font;
            UnityEngine.Material material =
                TypographyFontResolver.BundledFont == font
                    ? font.material
                    : TypographyFontResolver.ReferenceMaterial;
            if (material != null)
            {
                text.fontSharedMaterial = material;
            }
        }
        else
        {
            TypographyFontResolver.WarnMissingReferenceOnce();
        }

        if (TypographyFontResolver.ReferenceSpriteAsset != null)
        {
            text.spriteAsset = TypographyFontResolver.ReferenceSpriteAsset;
        }

        text.richText = true;
        text.raycastTarget = false;

        if (!TypographyFontResolver.IsSharp)
        {
            return;
        }

        text.enableAutoSizing = false;
        text.fontWeight = FontWeight.Regular;
        text.wordSpacing = 0f;
        text.lineSpacingAdjustment = 0f;
        text.extraPadding = false;
        text.fontStyle = FontStyles.Normal;
        text.characterSpacing = -1f;
        text.lineSpacing = 0f;
    }
}
