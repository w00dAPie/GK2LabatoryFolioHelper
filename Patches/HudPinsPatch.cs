using GK2LaboratoryFolioHelper.Helpers;
using HarmonyLib;

namespace GK2LaboratoryFolioHelper.Patches;

[HarmonyPatch(typeof(HUD), nameof(HUD.Init))]
internal static class HudPinsPatch
{
    private static void Postfix(HUD __instance)
    {
        LaboratoryPinsHud.Initialize(__instance);
    }
}
