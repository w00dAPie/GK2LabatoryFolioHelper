using System.Collections.Generic;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class PinnedFormulaContentComparer
{
    internal static bool AreEqual(
        List<PinnedFormulaManager.PinnedFormulaViewData> previous,
        List<PinnedFormulaManager.PinnedFormulaViewData> current
    )
    {
        if (previous == null || previous.Count != current.Count)
        {
            return false;
        }

        for (int i = 0; i < current.Count; i++)
        {
            PinnedFormulaManager.PinnedFormulaViewData before = previous[i];
            PinnedFormulaManager.PinnedFormulaViewData after = current[i];
            if (
                before.FormulaId != after.FormulaId
                || before.Name != after.Name
                || before.StatusText != after.StatusText
                || before.IsCraftable != after.IsCraftable
                || !IngredientsEqual(before.Ingredients, after.Ingredients)
                || !IngredientsEqual(before.NoPowderIngredients, after.NoPowderIngredients)
            )
            {
                return false;
            }
        }

        return true;
    }

    private static bool IngredientsEqual(
        List<PinnedFormulaManager.PinnedIngredientViewData> previous,
        List<PinnedFormulaManager.PinnedIngredientViewData> current
    )
    {
        if (previous == null || current == null)
        {
            return previous == current;
        }

        if (previous.Count != current.Count)
        {
            return false;
        }

        for (int i = 0; i < current.Count; i++)
        {
            PinnedFormulaManager.PinnedIngredientViewData before = previous[i];
            PinnedFormulaManager.PinnedIngredientViewData after = current[i];
            if (
                before.ItemId != after.ItemId
                || before.Name != after.Name
                || before.CountText != after.CountText
                || before.IsAvailable != after.IsAvailable
                || before.IsBuyable != after.IsBuyable
                || before.VendorName != after.VendorName
                || before.VendorStock != after.VendorStock
            )
            {
                return false;
            }
        }

        return true;
    }
}
