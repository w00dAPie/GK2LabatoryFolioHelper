using GK2LaboratoryFolioHelper.Helpers;
using HarmonyLib;

namespace GK2LaboratoryFolioHelper.Patches;

[HarmonyPatch(typeof(InventoryUIItemMoveOpHandler), "TryMoveItem")]
internal static class InventoryMovePatch
{
    [HarmonyPostfix]
    private static void Postfix(Inventory from, Inventory to, bool __result)
    {
        if (!__result)
        {
            return;
        }

        Inventory playerInventory = MainGame.PlayerData?.Inventory;

        if (playerInventory == null)
        {
            return;
        }

        bool involvesPlayerInventory =
            ReferenceEquals(from, playerInventory) || ReferenceEquals(to, playerInventory);

        if (!involvesPlayerInventory)
        {
            return;
        }

        LaboratoryPinsHud.Refresh();
    }
}
