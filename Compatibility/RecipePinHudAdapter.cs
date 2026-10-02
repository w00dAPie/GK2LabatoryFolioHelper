using System;
using System.Collections;
using System.Reflection;
using BepInEx.Bootstrap;
using GK2LaboratoryFolioHelper.Infrastructure;
using GK2LaboratoryFolioHelper.UI;
using HarmonyLib;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.Compatibility;

internal static class RecipePinHudAdapter
{
    private static object instance;
    private static FieldInfo recipesField;
    private static FieldInfo recipeRootField;
    private static FieldInfo totalsField;
    private static FieldInfo totalsRootField;

    internal static void IncludeBottom(ref float bottomScreenY, ref bool found)
    {
        try
        {
            EnsureBound();
            if (recipesField?.GetValue(instance) is IList recipes)
            {
                foreach (object recipe in recipes)
                {
                    if (recipe != null)
                    {
                        HudScreenGeometry.IncludeBottom(
                            recipeRootField?.GetValue(recipe) as RectTransform,
                            ref bottomScreenY,
                            ref found
                        );
                    }
                }
            }

            object totals = totalsField?.GetValue(instance);
            if (totals != null)
            {
                HudScreenGeometry.IncludeBottom(
                    totalsRootField?.GetValue(totals) as RectTransform,
                    ref bottomScreenY,
                    ref found
                );
            }
        }
        catch (Exception ex)
        {
            ModLog.Warning($"Could not read RecipePin HUD: {ex.Message}");
        }
    }

    internal static bool TryGetWidestScreenRect(out Rect screenRect)
    {
        screenRect = default;
        try
        {
            EnsureBound();
            if (recipesField?.GetValue(instance) is not IList recipes)
            {
                return false;
            }

            foreach (object recipe in recipes)
            {
                RectTransform root =
                    recipe == null ? null : recipeRootField?.GetValue(recipe) as RectTransform;
                if (
                    !HudScreenGeometry.TryGetScreenRect(root, out Rect bounds)
                    || bounds.width <= screenRect.width
                )
                {
                    continue;
                }
                screenRect = bounds;
            }
        }
        catch (Exception ex)
        {
            ModLog.Warning($"Could not read RecipePin card layout: {ex.Message}");
        }

        return screenRect.width > 0f;
    }

    private static void EnsureBound()
    {
        if (
            !Chainloader.PluginInfos.TryGetValue(
                ExternalHudCompatibility.RecipePinGuid,
                out var pluginInfo
            )
            || pluginInfo.Instance == null
            || ReferenceEquals(instance, pluginInfo.Instance)
        )
        {
            return;
        }

        instance = pluginInfo.Instance;
        Type type = instance.GetType();
        recipesField = AccessTools.Field(type, "recipes");
        Type[] recipeTypes = recipesField?.FieldType.GetGenericArguments();
        recipeRootField =
            recipeTypes?.Length == 1 ? AccessTools.Field(recipeTypes[0], "Root") : null;
        totalsField = AccessTools.Field(type, "totals");
        totalsRootField =
            totalsField == null ? null : AccessTools.Field(totalsField.FieldType, "Root");

        if (recipeRootField == null || totalsRootField == null)
        {
            ModLog.Warning("RecipePin HUD fields do not match the supported layout.");
        }
    }
}
