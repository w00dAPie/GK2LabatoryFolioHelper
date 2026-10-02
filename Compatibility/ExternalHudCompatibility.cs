using UnityEngine;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class ExternalHudCompatibility
{
    internal const string RecipePinGuid = "br.pedro.gk2.recipepin";
    internal const string ShoppingListGuid = "gk2.shoppinglist";

    public static bool TryGetExternalBottomScreenY(float left, float right, out float bottomScreenY)
    {
        bottomScreenY = float.MaxValue;
        bool found = false;

        RecipePinHudAdapter.IncludeBottom(ref bottomScreenY, ref found);
        ShoppingListHudAdapter.IncludeBottom(ref bottomScreenY, ref found);
        AdditionalRecipePinHuds.IncludeBottom(left, right, ref bottomScreenY, ref found);
        HudPositionPreview.IncludeBottom(left, right, ref bottomScreenY, ref found);
        return found;
    }

    internal static Camera GetCanvasCamera(RectTransform rect) =>
        HudScreenGeometry.GetCanvasCamera(rect);

    internal static bool TryGetRecipePinScreenRect(out Rect screenRect) =>
        RecipePinHudAdapter.TryGetWidestScreenRect(out screenRect);

    internal static bool TryGetScreenRect(RectTransform root, out Rect screenRect) =>
        HudScreenGeometry.TryGetScreenRect(root, out screenRect);

    internal static void IncludeOverlappingBottom(
        RectTransform root,
        float left,
        float right,
        ref float bottom,
        ref bool found
    ) => HudScreenGeometry.IncludeOverlappingBottom(root, left, right, ref bottom, ref found);
}
