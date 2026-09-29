using System.Collections.Generic;

namespace GK2LaboratoryFolioHelper.Helpers;

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

        Plugin.Log.LogDebug(
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
    }
}
