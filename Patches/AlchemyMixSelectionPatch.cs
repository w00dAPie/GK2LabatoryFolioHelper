using System;
using System.Collections.Generic;
using GK2LaboratoryFolioHelper.Alchemy;
using GK2LaboratoryFolioHelper.Compatibility;
using GK2LaboratoryFolioHelper.Configuration;
using GK2LaboratoryFolioHelper.Input;
using GK2LaboratoryFolioHelper.UI;
using GK2LaboratoryFolioHelper.UI.Typography;
using HarmonyLib;
using LazyBearTechnology;

namespace GK2LaboratoryFolioHelper.Patches;

[HarmonyPatch(typeof(UIAlchemyWindow), "Update")]
internal static class AlchemyMixSelectionPatch
{
    private static void Postfix(UIAlchemyWindow __instance)
    {
        AlchemyMixSelectionController.Tick(__instance);
    }
}

[HarmonyPatch(typeof(UIAlchemyWindow), "GetGameKeyDelegates")]
internal static class AlchemyMixSelectionGamepadPatch
{
    private static void Postfix(
        UIAlchemyWindow __instance,
        ref Dictionary<GameKey, Func<bool>> __result
    )
    {
        __result.TryGetValue(GameKey.DpadLeft, out Func<bool> left);
        __result[GameKey.DpadLeft] = () =>
            AlchemyMixSelectionController.TryHandleDpad(__instance, -1, left);

        __result.TryGetValue(GameKey.DpadRight, out Func<bool> right);
        __result[GameKey.DpadRight] = () =>
            AlchemyMixSelectionController.TryHandleDpad(__instance, 1, right);
    }
}

[HarmonyPatch(typeof(LazyInput), "Update")]
internal static class GlobalPinRemovalPatch
{
    private static void Postfix()
    {
        if (
            InputConfig.RemoveLastPinWithR3?.Value != true
            || !LazyInput.GetKeyDown(GameKey.RightStick)
        )
        {
            return;
        }

        // Keep the input visible to other mods that may also bind R3.
        PinnedFormulaManager.RemoveLastPin();
    }
}

[HarmonyPatch(typeof(UIAlchemyWindow), "Close")]
internal static class AlchemyMixSelectionClosePatch
{
    private static void Postfix(UIAlchemyWindow __instance)
    {
        AlchemyMixSelectionController.Clear(__instance);
    }
}

[HarmonyPatch(typeof(UIAlchemyWindow), "Hide")]
internal static class AlchemyMixSelectionHidePatch
{
    private static void Postfix(UIAlchemyWindow __instance)
    {
        AlchemyMixSelectionController.Clear(__instance);
    }
}
