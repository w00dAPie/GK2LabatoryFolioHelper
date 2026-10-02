using System.Collections.Generic;
using GK2LaboratoryFolioHelper.Infrastructure;

namespace GK2LaboratoryFolioHelper.Alchemy;

internal static class AlchemyInventoryWatcher
{
    private static readonly HashSet<Inventory> SubscribedInventories = new();

    private static UIAlchemyWindow activeWindow;

    public static void Attach(UIAlchemyWindow window, MultiInventory multiInventory)
    {
        Detach();

        activeWindow = window;

        if (window == null || multiInventory?.inventoryList == null)
        {
            return;
        }

        foreach (Inventory inventory in multiInventory.inventoryList)
        {
            if (inventory == null || !SubscribedInventories.Add(inventory))
            {
                continue;
            }

            inventory.OnItemsAdd += OnItemsChanged;
            inventory.OnItemsRemove += OnItemsChanged;
        }

        ModLog.Debug(
            $"Alchemy inventory watcher attached | inventories={SubscribedInventories.Count}"
        );
    }

    public static void Detach()
    {
        foreach (Inventory inventory in SubscribedInventories)
        {
            if (inventory == null)
            {
                continue;
            }

            inventory.OnItemsAdd -= OnItemsChanged;
            inventory.OnItemsRemove -= OnItemsChanged;
        }

        SubscribedInventories.Clear();
        activeWindow = null;
    }

    private static void OnItemsChanged(List<Item> items)
    {
        if (activeWindow == null)
        {
            return;
        }

        AlchemyRecipeLoader.RefreshAvailableCounts(activeWindow);
        PinnedFormulaManager.RemoveReadyAutoPins();
    }
}
