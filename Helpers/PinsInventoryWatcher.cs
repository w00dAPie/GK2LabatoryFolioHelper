using System.Collections.Generic;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class PinsInventoryWatcher
{
    private static readonly HashSet<Inventory> SubscribedInventories = new();

    public static void Initialize()
    {
        DetachAll();

        SubscribePlayerInventory();
        SubscribeVendors();

        Plugin.Log.LogInfo(
            $"Pins inventory watcher initialized | " + $"inventories={SubscribedInventories.Count}"
        );
    }

    private static void SubscribePlayerInventory()
    {
        Inventory playerInventory = MainGame.PlayerData?.inventory;

        if (playerInventory == null)
        {
            return;
        }

        Subscribe(playerInventory);
    }

    private static void SubscribeVendors()
    {
        VendorSystem vendorSystem = MainGame.Instance?.GameSave?.vendorSystem;

        if (vendorSystem?.vendors == null)
        {
            return;
        }

        foreach (Vendor vendor in vendorSystem.vendors)
        {
            if (vendor?.Inventory == null)
            {
                continue;
            }

            Subscribe(vendor.Inventory);
        }
    }

    private static void Subscribe(Inventory inventory)
    {
        if (inventory == null || !SubscribedInventories.Add(inventory))
        {
            return;
        }

        inventory.OnItemsAdd += OnItemsChanged;
        inventory.OnItemsRemove += OnItemsChanged;
    }

    private static void OnItemsChanged(List<Item> items)
    {
        Plugin.Log.LogInfo(
            $"Pins inventory changed | "
                + $"items={items?.Count ?? 0} | "
                + $"playerCount="
                + MainGame.PlayerData.Inventory.Data.GetTotalCountInInventory(
                    items != null && items.Count > 0 ? items[0].id : ""
                )
        );

        LaboratoryPinsHud.Refresh();
    }

    public static void DetachAll()
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
    }
}
