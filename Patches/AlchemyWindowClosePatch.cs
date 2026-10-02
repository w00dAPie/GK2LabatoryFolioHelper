using GK2LaboratoryFolioHelper.Alchemy;
using GK2LaboratoryFolioHelper.Compatibility;
using GK2LaboratoryFolioHelper.Input;
using GK2LaboratoryFolioHelper.UI;
using GK2LaboratoryFolioHelper.UI.Typography;
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
