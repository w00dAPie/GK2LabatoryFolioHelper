using GK2LaboratoryFolioHelper.Helpers;
using HarmonyLib;

namespace GK2LaboratoryFolioHelper.Patches;

[HarmonyPatch(typeof(UIAlchemyWindow), "Close")]
internal static class AlchemyWindowClosePatch
{
    private static void Prefix()
    {
        AlchemyInventoryWatcher.Detach();
    }
}
