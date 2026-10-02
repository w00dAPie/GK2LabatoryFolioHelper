using System;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class IngredientAvailability
{
    internal enum AvailabilityState
    {
        Unknown,
        Known,
        Buyable,
        Owned,
    }

    public static Result Get(ItemDef itemDef, int requiredCount)
    {
        if (itemDef == null || requiredCount <= 0)
        {
            return new Result { State = AvailabilityState.Unknown };
        }

        int ownedCount = MainGame.PlayerData.Inventory.Data.GetTotalCountInInventory(itemDef.id);

        int usableOwned = Mathf.Min(ownedCount, requiredCount);

        int missingCount = Mathf.Max(0, requiredCount - usableOwned);

        if (missingCount == 0)
        {
            return new Result
            {
                State = AvailabilityState.Owned,
                OwnedCount = ownedCount,
                MissingCount = 0,
                BuyableCount = 0,
                UnobtainableCount = 0,
            };
        }

        VendorSystem vendorSystem = MainGame.Instance.GameSave.vendorSystem;

        int bestVendorStock = 0;
        string bestVendorId = null;

        if (vendorSystem?.vendors != null)
        {
            foreach (Vendor vendor in vendorSystem.vendors)
            {
                if (vendor == null)
                {
                    continue;
                }

                if (string.Equals(vendor.id, "test_town_vendor", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!vendor.CanSellItemToPlayer(itemDef))
                {
                    continue;
                }

                int stock = vendor.CurCount(itemDef.id);

                if (stock <= 0)
                {
                    continue;
                }

                if (stock > bestVendorStock)
                {
                    bestVendorStock = stock;
                    bestVendorId = vendor.id;
                }
            }
        }

        int buyableCount = Mathf.Min(bestVendorStock, missingCount);

        int unobtainableCount = missingCount - buyableCount;

        AvailabilityState state;

        if (buyableCount > 0)
        {
            state = AvailabilityState.Buyable;
        }
        else
        {
            state = AvailabilityState.Known;
        }

        return new Result
        {
            State = state,
            OwnedCount = ownedCount,
            MissingCount = missingCount,
            BuyableCount = buyableCount,
            UnobtainableCount = unobtainableCount,
            VendorStock = bestVendorStock,
            VendorId = bestVendorId,
        };
    }

    internal sealed class Result
    {
        public AvailabilityState State { get; set; }

        public int OwnedCount { get; set; }

        public int MissingCount { get; set; }

        public int BuyableCount { get; set; }

        public int UnobtainableCount { get; set; }

        public int VendorStock { get; set; }

        public string VendorId { get; set; }
    }
}
