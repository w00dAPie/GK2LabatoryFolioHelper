using System.Reflection;
using GK2LaboratoryFolioHelper.Helpers;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;

namespace GK2LaboratoryFolioHelper.Patches;

[HarmonyPatch(typeof(UIAlchemyFolioWindow), "GetFormulaWidget")]
internal static class AlchemyFolioPinPatch
{
    private static readonly FieldInfo ItemCellField = AccessTools.Field(
        typeof(UIAlchemyFormulaWidget),
        "itemCell"
    );

    private static readonly FieldInfo NameLabelField = AccessTools.Field(
        typeof(UIAlchemyFormulaWidget),
        "nameLabel"
    );

    private static void Postfix(LazyScrollableElement parent, UIAlchemyFormulaWidget __result)
    {
        if (parent == null || __result == null)
        {
            return;
        }

        UIItemCell itemCell = ItemCellField?.GetValue(__result) as UIItemCell;

        TextMeshProUGUI nameLabel = NameLabelField?.GetValue(__result) as TextMeshProUGUI;

        if (itemCell == null || nameLabel == null)
        {
            return;
        }

        itemCell.ClearCallbacks();

        if (parent.Data is not UIAlchemyFormulaWidgetData widgetData)
        {
            return;
        }

        AlchemyFormulaDef formula = widgetData.AlchemyFormulaDef;

        if (formula == null)
        {
            return;
        }

        void RefreshPinVisual()
        {
            ItemDef displayItemDef = widgetData.GetDisplayItemDef();

            nameLabel.text = LLBase.L(displayItemDef.id);
        }

        void HandleFormulaPress(UIItemCell _)
        {
            if (AlchemyFolioSelectionContext.IsActive)
            {
                SelectFormulaForLaboratory(formula);
                return;
            }

            PinnedFormulaManager.Toggle(formula);
            RefreshPinVisual();
        }

        itemCell.OnItemCellPress = HandleFormulaPress;

        if (parent.GamepadNavigationItem != null)
        {
            parent.GamepadNavigationItem.SetCallbacks(
                itemCell.OnGamepadOver,
                itemCell.OnGamepadOut,
                itemCell.OnGamepadPress
            );
        }

        RefreshPinVisual();
    }

    private static void SelectFormulaForLaboratory(AlchemyFormulaDef formula)
    {
        if (formula == null || !AlchemyFolioSelectionContext.IsActive)
        {
            return;
        }

        UIAlchemyWindow alchemyWindow = AlchemyFolioSelectionContext.Window;

        bool found = PinnedFormulaManager.TryGetBestMixForLaboratory(
            formula.id,
            out string mixId,
            out bool isCraftable
        );

        if (!found || string.IsNullOrEmpty(mixId))
        {
            Plugin.Log.LogWarning($"No usable laboratory mix found for '{formula.id}'.");

            return;
        }

        UIAlchemyFolioWindow folio = LazyUI.GetWindow<UIAlchemyFolioWindow>();

        if (!isCraftable && !PinnedFormulaManager.IsPinned(formula.id))
        {
            PinnedFormulaManager.Toggle(formula);

            Plugin.Log.LogInfo(
                $"Alchemy formula pinned because ingredients are missing | "
                    + $"formula='{formula.id}' | "
                    + $"mix='{mixId}'"
            );
        }

        AlchemyFolioSelectionContext.End();

        folio?.Close();

        bool loaded = AlchemyRecipeLoader.LoadMix(alchemyWindow, mixId);

        Plugin.Log.LogInfo(
            $"Alchemy formula selected from folio | "
                + $"formula='{formula.id}' | "
                + $"mix='{mixId}' | "
                + $"craftable={isCraftable} | "
                + $"loaded={loaded}"
        );
    }
}
