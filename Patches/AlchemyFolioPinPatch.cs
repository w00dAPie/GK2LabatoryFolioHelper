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
        void TogglePin(UIItemCell _)
        {
            PinnedFormulaManager.Toggle(formula);
            RefreshPinVisual();
        }

        itemCell.OnItemCellPress = TogglePin;

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
}
