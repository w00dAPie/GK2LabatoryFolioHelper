using System;
using System.Collections.Generic;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class MixCandidateFactory
{
    internal static MixCandidate CreateForCrafting(
        AlchemyMixSourceDef source,
        MultiInventory inventory
    )
    {
        if (source == null || inventory == null)
        {
            return null;
        }

        Dictionary<string, int> requiredCounts = BuildRequiredCounts(source);
        if (requiredCounts.Count == 0)
        {
            return null;
        }

        List<MixIngredient> ingredients = new(requiredCounts.Count);
        int totalIngredientCount = 0;
        int ownedItemCount = 0;
        int missingItemCount = 0;
        bool containsPowder = false;

        foreach (KeyValuePair<string, int> entry in requiredCounts)
        {
            ItemDef itemDef = GameBalance.Me.GetDataOrNull<ItemDef>(entry.Key);
            if (itemDef == null || !FormulaKnowledge.IsIngredientKnown(itemDef))
            {
                return null;
            }

            int required = entry.Value;
            int owned = inventory.GetTotalCount(itemDef.id);
            IngredientAvailability.Result availability = IngredientAvailability.Get(
                itemDef,
                required
            );

            totalIngredientCount += required;
            ownedItemCount += Mathf.Min(owned, required);
            missingItemCount += Mathf.Max(0, required - owned);
            containsPowder |= IsAlchemyPowder(itemDef);

            ingredients.Add(
                new MixIngredient
                {
                    ItemDef = itemDef,
                    RequiredCount = required,
                    Availability = availability,
                }
            );
        }

        return new MixCandidate
        {
            MixId = source.mixId,
            TotalIngredientCount = totalIngredientCount,
            OwnedItemCount = ownedItemCount,
            MissingItemCount = missingItemCount,
            UnobtainableMissingCount = missingItemCount,
            ContainsPowder = containsPowder,
            Ingredients = ingredients,
        };
    }

    internal static MixCandidate CreateForHud(
        AlchemyMixSourceDef source,
        Dictionary<string, bool> knowledgeCache,
        Dictionary<string, IngredientAvailability.Result[]> availabilityCache
    )
    {
        if (source == null)
        {
            return null;
        }

        Dictionary<string, int> requiredCounts = BuildRequiredCounts(source);
        int totalIngredientCount = 0;
        foreach (int count in requiredCounts.Values)
        {
            totalIngredientCount += count;
        }

        if (totalIngredientCount == 0)
        {
            return null;
        }

        int ownedItemCount = 0;
        int missingItemCount = 0;
        int unobtainableMissingCount = 0;
        bool containsPowder = false;
        List<MixIngredient> ingredients = new(requiredCounts.Count);

        foreach (KeyValuePair<string, int> entry in requiredCounts)
        {
            ItemDef itemDef = GameBalance.Me.GetDataOrNull<ItemDef>(entry.Key);
            if (itemDef == null)
            {
                return null;
            }

            containsPowder |= IsAlchemyPowder(itemDef);

            if (!knowledgeCache.TryGetValue(itemDef.id, out bool known))
            {
                known = FormulaKnowledge.IsIngredientKnown(itemDef);
                knowledgeCache[itemDef.id] = known;
            }

            if (!known)
            {
                return null;
            }

            int requiredCount = entry.Value;
            if (
                !availabilityCache.TryGetValue(
                    itemDef.id,
                    out IngredientAvailability.Result[] availabilityByCount
                )
            )
            {
                availabilityByCount = new IngredientAvailability.Result[4];
                availabilityCache[itemDef.id] = availabilityByCount;
            }

            IngredientAvailability.Result availability = availabilityByCount[requiredCount];
            if (availability == null)
            {
                availability = IngredientAvailability.Get(itemDef, requiredCount);
                availabilityByCount[requiredCount] = availability;
            }

            ownedItemCount += Mathf.Min(availability.OwnedCount, requiredCount);
            missingItemCount += availability.MissingCount;
            unobtainableMissingCount += availability.UnobtainableCount;

            ingredients.Add(
                new MixIngredient
                {
                    ItemDef = itemDef,
                    RequiredCount = requiredCount,
                    Availability = availability,
                }
            );
        }

        return new MixCandidate
        {
            MixId = source.mixId,
            TotalIngredientCount = totalIngredientCount,
            OwnedItemCount = ownedItemCount,
            MissingItemCount = missingItemCount,
            UnobtainableMissingCount = unobtainableMissingCount,
            ContainsPowder = containsPowder,
            Ingredients = ingredients,
        };
    }

    private static Dictionary<string, int> BuildRequiredCounts(AlchemyMixSourceDef source)
    {
        Dictionary<string, int> result = new();
        Add(source.ingredient1);
        Add(source.ingredient2);
        Add(source.ingredient3);
        return result;

        void Add(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            if (result.TryGetValue(itemId, out int count))
            {
                result[itemId] = count + 1;
            }
            else
            {
                result[itemId] = 1;
            }
        }
    }

    private static bool IsAlchemyPowder(ItemDef itemDef)
    {
        return itemDef != null
            && !string.IsNullOrEmpty(itemDef.id)
            && itemDef.id.StartsWith("powder_", StringComparison.Ordinal);
    }
}
