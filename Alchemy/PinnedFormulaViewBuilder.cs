using System;
using System.Collections.Generic;
using System.Linq;
using LazyBearTechnology;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class PinnedFormulaViewBuilder
{
    internal static List<PinnedFormulaManager.PinnedFormulaViewData> Build()
    {
        List<PinnedFormulaManager.PinnedFormulaViewData> result = new();

        foreach (string formulaId in FormulaPinStore.PinnedIds)
        {
            AlchemyFormulaDef formula = GameBalance.Me.GetDataOrNull<AlchemyFormulaDef>(formulaId);
            if (!FormulaKnowledge.IsFormulaKnown(formula))
            {
                continue;
            }

            ItemDef output = formula.ItemDef;
            string formulaName = output != null ? LLBase.L(output.id) : formula.id;

            MixCandidate best = null;
            if (FormulaPinStore.TryGetSelectedMix(formula.id, out string selectedMixId))
            {
                best = LaboratoryMixSelector.GetSelectedHudCandidate(formula, selectedMixId);
            }

            best ??= LaboratoryMixSelector.GetBestHudCandidate(formula);

            MixCandidate bestWithoutPowder =
                best != null && best.ContainsPowder
                    ? LaboratoryMixSelector.GetBestHudCandidate(formula, excludePowder: true)
                    : null;

            if (best == null)
            {
                result.Add(
                    new PinnedFormulaManager.PinnedFormulaViewData
                    {
                        FormulaId = formula.id,
                        Name = formulaName,
                        StatusText = "No known mix",
                        IsCraftable = false,
                    }
                );
                continue;
            }

            result.Add(
                new PinnedFormulaManager.PinnedFormulaViewData
                {
                    FormulaId = formula.id,
                    Name = formulaName,
                    StatusText = best.IsCraftable ? "Ready" : $"Missing {best.MissingItemCount}",
                    IsCraftable = best.IsCraftable,
                    Ingredients = BuildIngredientViewData(best),
                    NoPowderIngredients =
                        bestWithoutPowder != null && bestWithoutPowder.MixId != best.MixId
                            ? BuildIngredientViewData(bestWithoutPowder)
                            : new List<PinnedFormulaManager.PinnedIngredientViewData>(),
                }
            );
        }

        return result;
    }

    private static List<PinnedFormulaManager.PinnedIngredientViewData> BuildIngredientViewData(
        MixCandidate candidate
    )
    {
        List<PinnedFormulaManager.PinnedIngredientViewData> result = new(
            candidate.Ingredients.Count
        );

        foreach (MixIngredient ingredient in candidate.Ingredients)
        {
            ItemDef itemDef = ingredient.ItemDef;
            IngredientAvailability.Result availability = ingredient.Availability;
            string displayName = LLBase.L(itemDef.id);
            string runeText = BuildRuneString(
                itemDef.runesRed.EvaluateInt(),
                itemDef.runesGreen.EvaluateInt(),
                itemDef.runesBlue.EvaluateInt()
            );
            bool nameAlreadyContainsRunes = displayName.Contains("<sprite");
            string quality =
                itemDef.qualityType == ItemDef.QualityType.Star
                    ? $" Q{itemDef.quality}"
                    : string.Empty;
            string ingredientName = displayName + quality;

            if (!nameAlreadyContainsRunes && !string.IsNullOrEmpty(runeText))
            {
                ingredientName += " " + runeText;
            }

            result.Add(
                new PinnedFormulaManager.PinnedIngredientViewData
                {
                    ItemId = itemDef.id,
                    Name = ingredientName,
                    CountText = $"{availability.OwnedCount}/{ingredient.RequiredCount}",
                    IsAvailable =
                        availability.State == IngredientAvailability.AvailabilityState.Owned,
                    IsBuyable = availability.BuyableCount > 0,
                    VendorStock = availability.VendorStock,
                    VendorName = GetVendorDisplayName(availability.VendorId),
                }
            );
        }

        return result;
    }

    private static string BuildRuneString(int runesRed, int runesGreen, int runesBlue)
    {
        List<string> parts = new();
        if (runesRed > 0)
        {
            parts.Add($"<sprite name=\"rune_r\">{runesRed}");
        }
        if (runesGreen > 0)
        {
            parts.Add($"<sprite name=\"rune_g\">{runesGreen}");
        }
        if (runesBlue > 0)
        {
            parts.Add($"<sprite name=\"rune_b\">{runesBlue}");
        }
        return string.Join(" ", parts);
    }

    private static string GetVendorDisplayName(string vendorId)
    {
        if (string.IsNullOrEmpty(vendorId))
        {
            return string.Empty;
        }

        string resolvedName = vendorId;
        TownBuildingDef townBuilding = GameBalance.Me.townBuildingDefs.FirstOrDefault(def =>
            def != null && def.vendorId == vendorId
        );

        if (townBuilding != null && !string.IsNullOrEmpty(townBuilding.characterId))
        {
            string localizedCharacterName = LLBase.L(townBuilding.characterId);
            if (
                !string.IsNullOrEmpty(localizedCharacterName)
                && localizedCharacterName != townBuilding.characterId
            )
            {
                resolvedName = localizedCharacterName;
            }
        }
        else if (vendorId.StartsWith("npc_", StringComparison.Ordinal))
        {
            string localizedNpcName = LLBase.L(vendorId);
            if (!string.IsNullOrEmpty(localizedNpcName) && localizedNpcName != vendorId)
            {
                resolvedName = localizedNpcName;
            }
        }

        return resolvedName;
    }
}
