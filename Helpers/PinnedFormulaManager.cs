using System;
using System.Collections.Generic;
using System.Linq;
using LazyBearTechnology;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class PinnedFormulaManager
{
    private const int MaxPins = 4;

    private static readonly List<string> PinnedFormulaIds = new();

    public static event Action PinsChanged;

    public static bool IsPinned(string formulaId)
    {
        return !string.IsNullOrEmpty(formulaId) && PinnedFormulaIds.Contains(formulaId);
    }

    public static bool Toggle(AlchemyFormulaDef formula)
    {
        if (formula == null || string.IsNullOrEmpty(formula.id))
        {
            return false;
        }

        if (!IsFormulaKnown(formula))
        {
            Plugin.Log.LogWarning($"Refused to pin unknown alchemy formula '{formula.id}'.");

            return false;
        }

        bool pinned;

        if (PinnedFormulaIds.Contains(formula.id))
        {
            PinnedFormulaIds.Remove(formula.id);
            pinned = false;
        }
        else
        {
            if (PinnedFormulaIds.Count >= MaxPins)
            {
                Plugin.Log.LogInfo(
                    $"Cannot pin '{formula.id}': maximum of {MaxPins} alchemy pins reached."
                );

                return false;
            }

            PinnedFormulaIds.Add(formula.id);

            pinned = true;
        }

        Plugin.Log.LogInfo($"{(pinned ? "Pinned" : "Unpinned")} formula '{formula.id}'");

        PinsChanged?.Invoke();

        return pinned;
    }

    public static List<PinnedFormulaViewData> GetPinnedViewData()
    {
        List<PinnedFormulaViewData> result = new();

        foreach (string formulaId in PinnedFormulaIds)
        {
            AlchemyFormulaDef formula = GameBalance.Me.GetDataOrNull<AlchemyFormulaDef>(formulaId);

            if (formula == null || !IsFormulaKnown(formula))
            {
                continue;
            }

            ItemDef output = formula.ItemDef;

            string formulaName = output != null ? LLBase.L(output.id) : formula.id;

            MixCandidate best = GetBestCandidate(formula);

            if (best == null)
            {
                result.Add(
                    new PinnedFormulaViewData
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
                new PinnedFormulaViewData
                {
                    FormulaId = formula.id,
                    Name = formulaName,
                    StatusText = best.IsCraftable ? "Ready" : $"Missing {best.MissingItemCount}",
                    IsCraftable = best.IsCraftable,
                    Ingredients = BuildIngredientViewData(best),
                }
            );
        }

        return result;
    }

    public static string GetBestMixId(string formulaId)
    {
        if (string.IsNullOrEmpty(formulaId))
        {
            return null;
        }

        AlchemyFormulaDef formula = GameBalance.Me.GetDataOrNull<AlchemyFormulaDef>(formulaId);

        if (formula == null || !IsFormulaKnown(formula))
        {
            return null;
        }

        MixCandidate best = GetBestCandidate(formula);

        return best?.MixId;
    }

    public static List<PinnedFormulaViewData> GetPinnedRecipesForLaboratory()
    {
        return GetPinnedViewData();
    }

    private static bool IsFormulaKnown(AlchemyFormulaDef formula)
    {
        if (MainGame.Instance?.GameSave?.knowledgeSystem == null)
        {
            return false;
        }

        return MainGame.Instance.GameSave.knowledgeSystem.IsAlchemyFormulaKnown(formula);
    }

    private static bool IsIngredientKnown(ItemDef itemDef)
    {
        if (itemDef == null)
        {
            return false;
        }

        SurveyDef surveyDef = GameBalance.GetSurveyDefForItemOrNull(itemDef.id);

        if (surveyDef == null)
        {
            return false;
        }

        if (surveyDef.surveyedAtStart)
        {
            return true;
        }

        return MainGame.Instance.GameSave.knowledgeSystem.IsSurveyCompleted(surveyDef);
    }

    private static MixCandidate GetBestCandidate(AlchemyFormulaDef formula)
    {
        if (formula == null)
        {
            return null;
        }

        Dictionary<string, bool> knowledgeCache = new();

        Dictionary<string, IngredientAvailability.Result[]> availabilityCache = new();

        MixCandidate best = null;

        foreach (AlchemyMixSourceDef source in GameBalance.Me.alchemyMixSourceDefs)
        {
            if (source == null || source.formulaId != formula.id)
            {
                continue;
            }

            MixCandidate candidate = CreateCandidate(source, knowledgeCache, availabilityCache);

            if (candidate == null)
            {
                continue;
            }

            if (best == null || IsBetterCandidate(candidate, best))
            {
                best = candidate;
            }
        }

        return best;
    }

    private static MixCandidate CreateCandidate(
        AlchemyMixSourceDef source,
        Dictionary<string, bool> knowledgeCache,
        Dictionary<string, IngredientAvailability.Result[]> availabilityCache
    )
    {
        Dictionary<string, int> requiredCounts = new();

        int totalIngredientCount = 0;

        AddRequiredIngredient(source.ingredient1);

        AddRequiredIngredient(source.ingredient2);

        AddRequiredIngredient(source.ingredient3);

        if (totalIngredientCount == 0)
        {
            return null;
        }

        int ownedItemCount = 0;
        int missingItemCount = 0;
        int unobtainableMissingCount = 0;

        List<MixIngredient> ingredients = new(requiredCounts.Count);

        foreach (KeyValuePair<string, int> entry in requiredCounts)
        {
            ItemDef itemDef = GameBalance.Me.GetDataOrNull<ItemDef>(entry.Key);

            if (itemDef == null)
            {
                return null;
            }

            if (!knowledgeCache.TryGetValue(itemDef.id, out bool known))
            {
                known = IsIngredientKnown(itemDef);

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

            int usableOwned = Mathf.Min(availability.OwnedCount, requiredCount);

            ownedItemCount += usableOwned;

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
            Ingredients = ingredients,
        };

        void AddRequiredIngredient(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            totalIngredientCount++;

            if (requiredCounts.TryGetValue(itemId, out int count))
            {
                requiredCounts[itemId] = count + 1;
            }
            else
            {
                requiredCounts[itemId] = 1;
            }
        }
    }

    private static bool IsBetterCandidate(MixCandidate candidate, MixCandidate currentBest)
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

    private static List<PinnedIngredientViewData> BuildIngredientViewData(MixCandidate candidate)
    {
        List<PinnedIngredientViewData> result = new(candidate.Ingredients.Count);

        foreach (MixIngredient ingredient in candidate.Ingredients)
        {
            ItemDef itemDef = ingredient.ItemDef;

            IngredientAvailability.Result availability = ingredient.Availability;

            string displayName = LLBase.L(itemDef.id);

            int runesRed = itemDef.runesRed.EvaluateInt();

            int runesGreen = itemDef.runesGreen.EvaluateInt();

            int runesBlue = itemDef.runesBlue.EvaluateInt();

            string runeText = BuildRuneString(runesRed, runesGreen, runesBlue);

            bool nameAlreadyContainsRunes = displayName.Contains("<sprite");

            string visibleRunes = nameAlreadyContainsRunes ? string.Empty : runeText;

            string quality =
                itemDef.qualityType == ItemDef.QualityType.Star
                    ? $" Q{itemDef.quality}"
                    : string.Empty;

            string ingredientName = displayName + quality;

            if (!string.IsNullOrEmpty(visibleRunes))
            {
                ingredientName += " " + visibleRunes;
            }

            result.Add(
                new PinnedIngredientViewData
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

    internal sealed class PinnedFormulaViewData
    {
        public string FormulaId { get; set; }

        public string Name { get; set; }

        public string StatusText { get; set; }

        public bool IsCraftable { get; set; }

        public List<PinnedIngredientViewData> Ingredients { get; set; } =
            new List<PinnedIngredientViewData>();
    }

    internal sealed class PinnedIngredientViewData
    {
        public string ItemId { get; set; }

        public string Name { get; set; }

        public string CountText { get; set; }

        public bool IsAvailable { get; set; }

        public bool IsBuyable { get; set; }

        public int VendorStock { get; set; }

        public string VendorName { get; set; }
    }

    private sealed class MixCandidate
    {
        public string MixId { get; set; }

        public int TotalIngredientCount { get; set; }

        public int OwnedItemCount { get; set; }

        public int MissingItemCount { get; set; }

        public int UnobtainableMissingCount { get; set; }

        public List<MixIngredient> Ingredients { get; set; }

        public bool IsCraftable => MissingItemCount == 0;
    }

    private sealed class MixIngredient
    {
        public ItemDef ItemDef { get; set; }

        public int RequiredCount { get; set; }

        public IngredientAvailability.Result Availability { get; set; }
    }
}
