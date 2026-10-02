using GK2LaboratoryFolioHelper.Alchemy;
using GK2LaboratoryFolioHelper.Compatibility;
using GK2LaboratoryFolioHelper.Input;
using GK2LaboratoryFolioHelper.UI;
using GK2LaboratoryFolioHelper.UI.Typography;
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
