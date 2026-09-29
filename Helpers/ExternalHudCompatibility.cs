using System;
using System.Collections;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class ExternalHudCompatibility
{
    internal const string RecipePinGuid = "br.pedro.gk2.recipepin";
    internal const string ShoppingListGuid = "gk2.shoppinglist";

    private static readonly Vector3[] Corners = new Vector3[4];

    private static object recipePinInstance;
    private static FieldInfo recipePinRecipesField;
    private static FieldInfo recipePinRootField;
    private static FieldInfo recipePinTotalsField;
    private static FieldInfo recipePinTotalsRootField;

    private static object shoppingListInstance;
    private static FieldInfo shoppingListField;
    private static FieldInfo shoppingListTasksField;
    private static FieldInfo shoppingListHudField;
    private static FieldInfo shoppingListHudRootField;

    public static bool TryGetExternalBottomScreenY(out float bottomScreenY)
    {
        bottomScreenY = float.MaxValue;
        bool found = false;

        try
        {
            EnsureRecipePin();
            if (recipePinRecipesField?.GetValue(recipePinInstance) is IList recipes)
            {
                foreach (object recipe in recipes)
                {
                    if (recipe != null)
                    {
                        IncludeBottom(
                            recipePinRootField?.GetValue(recipe) as RectTransform,
                            ref bottomScreenY,
                            ref found
                        );
                    }
                }
            }

            object totals = recipePinTotalsField?.GetValue(recipePinInstance);
            if (totals != null)
            {
                IncludeBottom(
                    recipePinTotalsRootField?.GetValue(totals) as RectTransform,
                    ref bottomScreenY,
                    ref found
                );
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not read RecipePin HUD: {ex.Message}");
        }

        try
        {
            EnsureShoppingList();
            object list = shoppingListField?.GetValue(shoppingListInstance);
            if (
                list != null
                && shoppingListTasksField?.GetValue(list) is ICollection tasks
                && tasks.Count > 0
            )
            {
                object hud = shoppingListHudField?.GetValue(shoppingListInstance);
                RectTransform root =
                    hud == null ? null : shoppingListHudRootField?.GetValue(hud) as RectTransform;

                if (IsVisible(root))
                {
                    LaboratoryPinsLayoutListener.Observe(root);
                    // Tick changes a ContentSizeFitter/VerticalLayoutGroup tree.
                    // Resolve pending layout before measuring its actual corners.
                    LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                    IncludeBottom(root, ref bottomScreenY, ref found);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not read Shopping List HUD: {ex.Message}");
        }

        return found;
    }

    internal static Camera GetCanvasCamera(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return null;
        }

        canvas = canvas.rootCanvas;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        // ScreenSpaceCamera without an assigned camera behaves as an overlay.
        return canvas.worldCamera != null ? canvas.worldCamera
            : canvas.renderMode == RenderMode.WorldSpace ? Camera.main
            : null;
    }

    internal static bool TryGetRecipePinScreenRect(out Rect screenRect)
    {
        screenRect = default;
        try
        {
            EnsureRecipePin();
            if (recipePinRecipesField?.GetValue(recipePinInstance) is not IList recipes)
            {
                return false;
            }

            foreach (object recipe in recipes)
            {
                RectTransform root =
                    recipe == null ? null : recipePinRootField?.GetValue(recipe) as RectTransform;
                if (!TryGetScreenRect(root, out Rect bounds) || bounds.width <= screenRect.width)
                {
                    continue;
                }

                screenRect = bounds;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not read RecipePin card layout: {ex.Message}");
        }

        return screenRect.width > 0f;
    }

    private static void IncludeBottom(RectTransform root, ref float bottomScreenY, ref bool found)
    {
        if (TryGetScreenRect(root, out Rect screenRect))
        {
            bottomScreenY = Mathf.Min(bottomScreenY, screenRect.yMin);
            found = true;
        }
    }

    private static bool TryGetScreenRect(RectTransform root, out Rect screenRect)
    {
        screenRect = default;
        if (!IsVisible(root) || root.rect.width <= 0f || root.rect.height <= 0f)
        {
            return false;
        }

        Camera camera = GetCanvasCamera(root);
        root.GetWorldCorners(Corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 corner in Corners)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
            if (
                float.IsNaN(point.x)
                || float.IsInfinity(point.x)
                || float.IsNaN(point.y)
                || float.IsInfinity(point.y)
            )
            {
                return false;
            }

            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return screenRect.width > 0f && screenRect.height > 0f;
    }

    private static bool IsVisible(RectTransform root)
    {
        if (root == null || !root.gameObject.activeInHierarchy)
        {
            return false;
        }

        Canvas canvas = root.GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.isActiveAndEnabled || !canvas.rootCanvas.isActiveAndEnabled)
        {
            return false;
        }

        for (Transform current = root; current != null; current = current.parent)
        {
            bool ignoreParents = false;
            foreach (CanvasGroup group in current.GetComponents<CanvasGroup>())
            {
                if (!group.enabled)
                {
                    continue;
                }

                if (group.alpha <= 0f)
                {
                    return false;
                }

                ignoreParents |= group.ignoreParentGroups;
            }

            if (ignoreParents)
            {
                break;
            }
        }

        return true;
    }

    private static void EnsureRecipePin()
    {
        if (
            !Chainloader.PluginInfos.TryGetValue(RecipePinGuid, out var pluginInfo)
            || pluginInfo.Instance == null
            || ReferenceEquals(recipePinInstance, pluginInfo.Instance)
        )
        {
            return;
        }

        recipePinInstance = pluginInfo.Instance;
        Type type = recipePinInstance.GetType();
        recipePinRecipesField = AccessTools.Field(type, "recipes");
        Type[] recipeTypes = recipePinRecipesField?.FieldType.GetGenericArguments();
        recipePinRootField =
            recipeTypes?.Length == 1 ? AccessTools.Field(recipeTypes[0], "Root") : null;
        recipePinTotalsField = AccessTools.Field(type, "totals");
        recipePinTotalsRootField =
            recipePinTotalsField == null
                ? null
                : AccessTools.Field(recipePinTotalsField.FieldType, "Root");

        if (recipePinRootField == null || recipePinTotalsRootField == null)
        {
            Plugin.Log.LogWarning("RecipePin HUD fields do not match the supported layout.");
        }
    }

    private static void EnsureShoppingList()
    {
        if (
            !Chainloader.PluginInfos.TryGetValue(ShoppingListGuid, out var pluginInfo)
            || pluginInfo.Instance == null
            || ReferenceEquals(shoppingListInstance, pluginInfo.Instance)
        )
        {
            return;
        }

        shoppingListInstance = pluginInfo.Instance;
        Type type = shoppingListInstance.GetType();
        shoppingListField = AccessTools.Field(type, "List");
        shoppingListTasksField =
            shoppingListField == null
                ? null
                : AccessTools.Field(shoppingListField.FieldType, "Tasks");
        shoppingListHudField = AccessTools.Field(type, "_hud");
        shoppingListHudRootField =
            shoppingListHudField == null
                ? null
                : AccessTools.Field(shoppingListHudField.FieldType, "_root");

        if (shoppingListTasksField == null || shoppingListHudRootField == null)
        {
            Plugin.Log.LogWarning("Shopping List HUD fields do not match the supported layout.");
        }
    }
}
