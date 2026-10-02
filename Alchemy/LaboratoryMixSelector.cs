using System.Collections.Generic;
using System.Linq;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class LaboratoryMixSelector
{
    internal static string GetBestMixId(string formulaId)
    {
        AlchemyFormulaDef formula = ResolveKnownFormula(formulaId);
        return formula == null ? null : GetBestHudCandidate(formula)?.MixId;
    }

    internal static string GetBestMixIdForLaboratory(string formulaId)
    {
        return TryGetBestMixForLaboratory(formulaId, out string mixId, out _) ? mixId : null;
    }

    internal static bool TryGetBestMixForLaboratory(
        string formulaId,
        out string mixId,
        out bool isCraftable
    )
    {
        mixId = null;
        isCraftable = false;

        AlchemyFormulaDef formula = ResolveKnownFormula(formulaId);
        if (formula == null)
        {
            return false;
        }

        MultiInventory inventory = MainGame.PlayerController?.WorkerMultiInventory;
        MixCandidate best = inventory == null ? GetBestHudCandidate(formula) : null;

        if (inventory != null)
        {
            foreach (AlchemyMixSourceDef source in GameBalance.Me.alchemyMixSourceDefs)
            {
                if (source == null || source.formulaId != formula.id)
                {
                    continue;
                }

                MixCandidate candidate = MixCandidateFactory.CreateForCrafting(source, inventory);
                if (
                    candidate != null
                    && (best == null || MixRanking.IsBetterForCrafting(candidate, best))
                )
                {
                    best = candidate;
                }
            }
        }

        if (best == null)
        {
            return false;
        }

        mixId = best.MixId;
        isCraftable = best.IsCraftable;
        return true;
    }

    internal static bool TryGetBestMixesForLaboratory(
        string formulaId,
        int maxCount,
        out List<PinnedFormulaManager.LaboratoryMixOption> options
    )
    {
        options = new List<PinnedFormulaManager.LaboratoryMixOption>();
        AlchemyFormulaDef formula = ResolveKnownFormula(formulaId);
        if (formula == null || maxCount <= 0)
        {
            return false;
        }

        MultiInventory inventory = MainGame.PlayerController?.WorkerMultiInventory;
        List<MixCandidate> candidates = new();
        Dictionary<string, bool> knowledgeCache = new();
        Dictionary<string, IngredientAvailability.Result[]> availabilityCache = new();

        foreach (AlchemyMixSourceDef source in GameBalance.Me.alchemyMixSourceDefs)
        {
            if (source == null || source.formulaId != formula.id)
            {
                continue;
            }

            MixCandidate candidate =
                inventory != null
                    ? MixCandidateFactory.CreateForCrafting(source, inventory)
                    : MixCandidateFactory.CreateForHud(source, knowledgeCache, availabilityCache);

            if (candidate != null)
            {
                candidates.Add(candidate);
            }
        }

        candidates.Sort(CompareCraftingCandidates);

        bool hasCraftableCandidate = candidates.Any(candidate => candidate.IsCraftable);
        IEnumerable<MixCandidate> candidatesToShow = hasCraftableCandidate
            ? candidates.Where(candidate => candidate.IsCraftable)
            : candidates;

        HashSet<string> added = new();
        foreach (MixCandidate candidate in candidatesToShow)
        {
            if (!added.Add(candidate.MixId))
            {
                continue;
            }

            options.Add(
                new PinnedFormulaManager.LaboratoryMixOption
                {
                    MixId = candidate.MixId,
                    IsCraftable = candidate.IsCraftable,
                    MissingItemCount = candidate.MissingItemCount,
                }
            );

            if (options.Count >= maxCount)
            {
                break;
            }
        }

        return options.Count > 0;
    }

    internal static bool TryGetCraftability(string formulaId, string mixId, out bool isCraftable)
    {
        isCraftable = false;
        AlchemyFormulaDef formula = ResolveKnownFormula(formulaId);
        if (formula == null || string.IsNullOrEmpty(mixId))
        {
            return false;
        }

        MixCandidate candidate = GetCandidateByMixId(formula, mixId, preferCraftingInventory: true);
        if (candidate == null)
        {
            return false;
        }

        isCraftable = candidate.IsCraftable;
        return true;
    }

    internal static MixCandidate GetSelectedHudCandidate(AlchemyFormulaDef formula, string mixId)
    {
        return GetCandidateByMixId(formula, mixId, preferCraftingInventory: false);
    }

    internal static MixCandidate GetBestHudCandidate(
        AlchemyFormulaDef formula,
        bool excludePowder = false
    )
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

            MixCandidate candidate = MixCandidateFactory.CreateForHud(
                source,
                knowledgeCache,
                availabilityCache
            );

            if (candidate == null || (excludePowder && candidate.ContainsPowder))
            {
                continue;
            }

            if (best == null || MixRanking.IsBetterForHud(candidate, best))
            {
                best = candidate;
            }
        }

        return best;
    }

    private static MixCandidate GetCandidateByMixId(
        AlchemyFormulaDef formula,
        string mixId,
        bool preferCraftingInventory
    )
    {
        if (formula == null || string.IsNullOrEmpty(mixId))
        {
            return null;
        }

        MultiInventory inventory = preferCraftingInventory
            ? MainGame.PlayerController?.WorkerMultiInventory
            : null;
        Dictionary<string, bool> knowledgeCache = new();
        Dictionary<string, IngredientAvailability.Result[]> availabilityCache = new();

        foreach (AlchemyMixSourceDef source in GameBalance.Me.alchemyMixSourceDefs)
        {
            if (source == null || source.formulaId != formula.id || source.mixId != mixId)
            {
                continue;
            }

            return inventory != null
                ? MixCandidateFactory.CreateForCrafting(source, inventory)
                : MixCandidateFactory.CreateForHud(source, knowledgeCache, availabilityCache);
        }

        return null;
    }

    private static AlchemyFormulaDef ResolveKnownFormula(string formulaId)
    {
        if (string.IsNullOrEmpty(formulaId))
        {
            return null;
        }

        AlchemyFormulaDef formula = GameBalance.Me.GetDataOrNull<AlchemyFormulaDef>(formulaId);
        return FormulaKnowledge.IsFormulaKnown(formula) ? formula : null;
    }

    private static int CompareCraftingCandidates(MixCandidate left, MixCandidate right)
    {
        if (MixRanking.IsBetterForCrafting(left, right))
        {
            return -1;
        }

        if (MixRanking.IsBetterForCrafting(right, left))
        {
            return 1;
        }

        return 0;
    }
}
