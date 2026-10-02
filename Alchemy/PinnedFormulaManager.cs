using System;
using System.Collections.Generic;

namespace GK2LaboratoryFolioHelper.Helpers;

// Compatibility facade for the rest of the mod. State, ranking and view building
// live in focused services under Alchemy/.
internal static class PinnedFormulaManager
{
    public static event Action PinsChanged
    {
        add => FormulaPinStore.Changed += value;
        remove => FormulaPinStore.Changed -= value;
    }

    public static bool IsPinned(string formulaId) => FormulaPinStore.IsPinned(formulaId);

    internal static bool RemoveLastPin() => FormulaPinStore.RemoveLast();

    public static bool Toggle(AlchemyFormulaDef formula) => FormulaPinStore.Toggle(formula);

    internal static bool PinForMissingIngredients(AlchemyFormulaDef formula) =>
        FormulaPinStore.PinForMissingIngredients(formula);

    internal static bool RemoveReadyAutoPins() => FormulaPinStore.RemoveReadyAutoPins();

    internal static void ClearSelectedLaboratoryMix(string formulaId) =>
        FormulaPinStore.ClearSelectedLaboratoryMix(formulaId);

    public static List<PinnedFormulaViewData> GetPinnedViewData() =>
        PinnedFormulaViewBuilder.Build();

    public static List<PinnedFormulaViewData> GetPinnedRecipesForLaboratory() =>
        GetPinnedViewData();

    internal static void SetSelectedLaboratoryMix(
        string formulaId,
        string mixId,
        bool isCraftable
    ) => FormulaPinStore.SetSelectedLaboratoryMix(formulaId, mixId, isCraftable);

    public static string GetBestMixId(string formulaId) =>
        LaboratoryMixSelector.GetBestMixId(formulaId);

    public static string GetBestMixIdForLaboratory(string formulaId) =>
        LaboratoryMixSelector.GetBestMixIdForLaboratory(formulaId);

    public static bool TryGetBestMixForLaboratory(
        string formulaId,
        out string mixId,
        out bool isCraftable
    ) => LaboratoryMixSelector.TryGetBestMixForLaboratory(formulaId, out mixId, out isCraftable);

    internal static bool TryGetBestMixesForLaboratory(
        string formulaId,
        int maxCount,
        out List<LaboratoryMixOption> options
    ) => LaboratoryMixSelector.TryGetBestMixesForLaboratory(formulaId, maxCount, out options);

    internal sealed class PinnedFormulaViewData
    {
        public string FormulaId { get; set; }
        public string Name { get; set; }
        public string StatusText { get; set; }
        public bool IsCraftable { get; set; }
        public List<PinnedIngredientViewData> Ingredients { get; set; } = new();
        public List<PinnedIngredientViewData> NoPowderIngredients { get; set; } = new();
    }

    internal sealed class LaboratoryMixOption
    {
        public string MixId { get; set; }
        public bool IsCraftable { get; set; }
        public int MissingItemCount { get; set; }
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
}
