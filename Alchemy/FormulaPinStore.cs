using System;
using System.Collections.Generic;
using System.Linq;
using GK2LaboratoryFolioHelper.Infrastructure;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class FormulaPinStore
{
    private const int MaxPins = 4;

    private static readonly List<string> PinnedFormulaIds = new();
    private static readonly HashSet<string> AutoPinnedFormulaIds = new();
    private static readonly Dictionary<string, string> SelectedLaboratoryMixIds = new();

    internal static event Action Changed;

    internal static IReadOnlyList<string> PinnedIds => PinnedFormulaIds;

    internal static bool IsPinned(string formulaId)
    {
        return !string.IsNullOrEmpty(formulaId) && PinnedFormulaIds.Contains(formulaId);
    }

    internal static bool TryGetSelectedMix(string formulaId, out string mixId)
    {
        return SelectedLaboratoryMixIds.TryGetValue(formulaId, out mixId);
    }

    internal static bool Toggle(AlchemyFormulaDef formula)
    {
        if (formula == null || string.IsNullOrEmpty(formula.id))
        {
            return false;
        }

        if (!FormulaKnowledge.IsFormulaKnown(formula))
        {
            ModLog.Warning($"Refused to pin unknown alchemy formula '{formula.id}'.");
            return false;
        }

        bool pinned;
        if (PinnedFormulaIds.Contains(formula.id))
        {
            RemoveFormula(formula.id);
            pinned = false;
        }
        else
        {
            if (PinnedFormulaIds.Count >= MaxPins)
            {
                ModLog.Debug(
                    $"Cannot pin '{formula.id}': maximum of {MaxPins} alchemy pins reached."
                );
                return false;
            }

            PinnedFormulaIds.Add(formula.id);
            pinned = true;
        }

        ModLog.Debug($"{(pinned ? "Pinned" : "Unpinned")} formula '{formula.id}'");
        Changed?.Invoke();
        return pinned;
    }

    internal static bool RemoveLast()
    {
        if (PinnedFormulaIds.Count == 0)
        {
            return false;
        }

        string formulaId = PinnedFormulaIds[PinnedFormulaIds.Count - 1];
        RemoveFormula(formulaId);
        ModLog.Info($"Alchemy formula pin removed by controller | formula='{formulaId}'");
        Changed?.Invoke();
        return true;
    }

    internal static bool PinForMissingIngredients(AlchemyFormulaDef formula)
    {
        if (
            formula == null
            || string.IsNullOrEmpty(formula.id)
            || !FormulaKnowledge.IsFormulaKnown(formula)
        )
        {
            return false;
        }

        bool changed = SetAutoPinState(formula.id, shouldPin: true);
        if (changed)
        {
            Changed?.Invoke();
        }

        return changed;
    }

    internal static void SetSelectedLaboratoryMix(string formulaId, string mixId, bool isCraftable)
    {
        if (string.IsNullOrEmpty(formulaId) || string.IsNullOrEmpty(mixId))
        {
            return;
        }

        bool selectionChanged =
            !SelectedLaboratoryMixIds.TryGetValue(formulaId, out string selectedMixId)
            || selectedMixId != mixId;

        SelectedLaboratoryMixIds[formulaId] = mixId;
        bool pinChanged = SetAutoPinState(formulaId, shouldPin: !isCraftable);

        if (selectionChanged || pinChanged)
        {
            Changed?.Invoke();
        }
    }

    internal static void ClearSelectedLaboratoryMix(string formulaId)
    {
        if (!string.IsNullOrEmpty(formulaId) && SelectedLaboratoryMixIds.Remove(formulaId))
        {
            Changed?.Invoke();
        }
    }

    internal static bool RemoveReadyAutoPins()
    {
        if (AutoPinnedFormulaIds.Count == 0)
        {
            return false;
        }

        bool removedAny = false;
        foreach (string formulaId in AutoPinnedFormulaIds.ToArray())
        {
            if (!PinnedFormulaIds.Contains(formulaId))
            {
                AutoPinnedFormulaIds.Remove(formulaId);
                continue;
            }

            bool isCraftable;
            bool hasSelectedMix = SelectedLaboratoryMixIds.TryGetValue(
                formulaId,
                out string selectedMixId
            );
            bool resolved = hasSelectedMix
                ? LaboratoryMixSelector.TryGetCraftability(
                    formulaId,
                    selectedMixId,
                    out isCraftable
                )
                : LaboratoryMixSelector.TryGetBestMixForLaboratory(
                    formulaId,
                    out _,
                    out isCraftable
                );

            if (!resolved || !isCraftable)
            {
                continue;
            }

            removedAny |= SetAutoPinState(formulaId, shouldPin: false);
        }

        if (removedAny)
        {
            Changed?.Invoke();
        }

        return removedAny;
    }

    private static bool SetAutoPinState(string formulaId, bool shouldPin)
    {
        if (string.IsNullOrEmpty(formulaId))
        {
            return false;
        }

        if (shouldPin)
        {
            if (PinnedFormulaIds.Contains(formulaId) || PinnedFormulaIds.Count >= MaxPins)
            {
                return false;
            }

            PinnedFormulaIds.Add(formulaId);
            AutoPinnedFormulaIds.Add(formulaId);
            ModLog.Info(
                $"Alchemy formula auto-pin opened because the selected mix is missing ingredients | formula='{formulaId}'"
            );
            return true;
        }

        if (!AutoPinnedFormulaIds.Remove(formulaId))
        {
            return false;
        }

        if (!PinnedFormulaIds.Remove(formulaId))
        {
            return false;
        }

        ModLog.Info(
            $"Alchemy formula auto-pin closed because the selected mix is craftable | formula='{formulaId}'"
        );
        return true;
    }

    private static void RemoveFormula(string formulaId)
    {
        PinnedFormulaIds.Remove(formulaId);
        AutoPinnedFormulaIds.Remove(formulaId);
        SelectedLaboratoryMixIds.Remove(formulaId);
    }
}
