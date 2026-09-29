using System;
using System.Reflection;
using BepInEx.Bootstrap;
using GK2LaboratoryFolioHelper.Helpers;
using HarmonyLib;

namespace GK2LaboratoryFolioHelper.Patches;

internal static class RecipePinCompatibilityPatch
{
    private static bool installed;

    public static void TryInstall(Harmony harmony)
    {
        if (installed)
        {
            return;
        }

        if (
            !Chainloader.PluginInfos.TryGetValue(
                ExternalHudCompatibility.RecipePinGuid,
                out var pluginInfo
            )
        )
        {
            return;
        }

        object instance = pluginInfo.Instance;

        if (instance == null)
        {
            return;
        }

        Type type = instance.GetType();

        PatchPostfix(harmony, type, "ToggleRecipe");

        PatchPostfix(harmony, type, "RemoveRecipe");

        PatchPostfix(harmony, type, "ArrangeRecipes");

        PatchPostfix(harmony, type, "ResetSession");

        installed = true;

        Plugin.Log.LogInfo("RecipePin HUD change hooks installed.");
    }

    private static void PatchPostfix(Harmony harmony, Type type, string methodName)
    {
        MethodInfo original = AccessTools.Method(type, methodName);

        if (original == null)
        {
            Plugin.Log.LogWarning($"RecipePin method '{methodName}' not found.");

            return;
        }

        MethodInfo postfix = AccessTools.Method(
            typeof(RecipePinCompatibilityPatch),
            nameof(AfterRecipePinChanged)
        );

        harmony.Patch(original, postfix: new HarmonyMethod(postfix));
    }

    private static void AfterRecipePinChanged()
    {
        LaboratoryPinsHud.Refresh();
        LaboratoryPinsHud.RequestPositionUpdate();
    }
}
