using System;
using System.Collections;
using System.Reflection;
using BepInEx.Bootstrap;
using GK2LaboratoryFolioHelper.Infrastructure;
using GK2LaboratoryFolioHelper.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GK2LaboratoryFolioHelper.Compatibility;

internal static class ShoppingListHudAdapter
{
    private static object instance;
    private static FieldInfo listField;
    private static FieldInfo tasksField;
    private static FieldInfo hudField;
    private static FieldInfo hudRootField;

    internal static void IncludeBottom(ref float bottomScreenY, ref bool found)
    {
        try
        {
            EnsureBound();
            object list = listField?.GetValue(instance);
            if (
                list == null
                || tasksField?.GetValue(list) is not ICollection tasks
                || tasks.Count <= 0
            )
            {
                return;
            }

            object hud = hudField?.GetValue(instance);
            RectTransform root = hud == null ? null : hudRootField?.GetValue(hud) as RectTransform;
            if (!HudScreenGeometry.IsVisible(root))
            {
                return;
            }

            LaboratoryPinsLayoutListener.Observe(root);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            HudScreenGeometry.IncludeBottom(root, ref bottomScreenY, ref found);
        }
        catch (Exception ex)
        {
            ModLog.Warning($"Could not read Shopping List HUD: {ex.Message}");
        }
    }

    private static void EnsureBound()
    {
        if (
            !Chainloader.PluginInfos.TryGetValue(
                ExternalHudCompatibility.ShoppingListGuid,
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
        listField = AccessTools.Field(type, "List");
        tasksField = listField == null ? null : AccessTools.Field(listField.FieldType, "Tasks");
        hudField = AccessTools.Field(type, "_hud");
        hudRootField = hudField == null ? null : AccessTools.Field(hudField.FieldType, "_root");

        if (tasksField == null || hudRootField == null)
        {
            ModLog.Warning("Shopping List HUD fields do not match the supported layout.");
        }
    }
}
