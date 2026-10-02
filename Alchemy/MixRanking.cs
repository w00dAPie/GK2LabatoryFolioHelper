using System;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class MixRanking
{
    internal static bool IsBetterForCrafting(MixCandidate candidate, MixCandidate currentBest)
    {
        if (candidate.IsCraftable != currentBest.IsCraftable)
        {
            return candidate.IsCraftable;
        }

        if (candidate.MissingItemCount != currentBest.MissingItemCount)
        {
            return candidate.MissingItemCount < currentBest.MissingItemCount;
        }

        if (candidate.TotalIngredientCount != currentBest.TotalIngredientCount)
        {
            return candidate.TotalIngredientCount < currentBest.TotalIngredientCount;
        }

        if (candidate.OwnedItemCount != currentBest.OwnedItemCount)
        {
            return candidate.OwnedItemCount > currentBest.OwnedItemCount;
        }

        return string.CompareOrdinal(candidate.MixId, currentBest.MixId) < 0;
    }

    internal static bool IsBetterForHud(MixCandidate candidate, MixCandidate currentBest)
    {
        if (candidate.TotalIngredientCount != currentBest.TotalIngredientCount)
        {
            return candidate.TotalIngredientCount < currentBest.TotalIngredientCount;
        }

        if (candidate.IsCraftable != currentBest.IsCraftable)
        {
            return candidate.IsCraftable;
        }

        if (candidate.UnobtainableMissingCount != currentBest.UnobtainableMissingCount)
        {
            return candidate.UnobtainableMissingCount < currentBest.UnobtainableMissingCount;
        }

        if (candidate.MissingItemCount != currentBest.MissingItemCount)
        {
            return candidate.MissingItemCount < currentBest.MissingItemCount;
        }

        if (candidate.OwnedItemCount != currentBest.OwnedItemCount)
        {
            return candidate.OwnedItemCount > currentBest.OwnedItemCount;
        }

        return string.CompareOrdinal(candidate.MixId, currentBest.MixId) < 0;
    }
}
