using System;
using System.Reflection;
using GK2LaboratoryFolioHelper.Alchemy;
using GK2LaboratoryFolioHelper.Compatibility;
using GK2LaboratoryFolioHelper.Infrastructure;
using GK2LaboratoryFolioHelper.Input;
using GK2LaboratoryFolioHelper.UI;
using GK2LaboratoryFolioHelper.UI.Typography;
using HarmonyLib;
using LazyBearTechnology;

namespace GK2LaboratoryFolioHelper.Patches;

[HarmonyPatch(typeof(UIAlchemyWindow), "RedrawAlchemyTabLite", new[] { typeof(AlchemyMixDef) })]
internal static class AlchemyWindowPatch
{
    private static readonly FieldInfo ResultField = AccessTools.Field(
        typeof(UIAlchemyWindow),
        "result"
    );

    private static readonly FieldInfo MixCraftIdField = AccessTools.Field(
        typeof(UIAlchemyWindow),
        "mixCraftId"
    );

    private static readonly FieldInfo DataField = AccessTools.Field(
        typeof(UIAlchemyWindow),
        "data"
    );

    private static void Postfix(UIAlchemyWindow __instance, AlchemyMixDef mix)
    {
        UIAlchemyWindowData data = DataField?.GetValue(__instance) as UIAlchemyWindowData;

        if (data?.Wgo?.Data != null)
        {
            MultiInventory multiInventory = data.Wgo.Data.GetCraftableMultiInventory(
                excludeWorkerInventory: true
            );

            AlchemyInventoryWatcher.Attach(__instance, multiInventory);
        }

        UIItemCell result = ResultField?.GetValue(__instance) as UIItemCell;

        if (result == null)
        {
            return;
        }

        string mixCraftId = MixCraftIdField?.GetValue(__instance) as string;

        AlchemyMixDef currentMix = string.IsNullOrEmpty(mixCraftId)
            ? null
            : GameBalance.GetAlchemyMixDef(mixCraftId);

        if (currentMix != null)
        {
            return;
        }

        result.DrawEmpty(drawAsNonInteractable: false);

        result.NoSelectionFrames = false;
        result.LazyButton.interactable = true;

        result.OnItemCellPress = _ => OpenAlchemyFolio(__instance, data);

        if (result.GamepadNavigationItem != null)
        {
            result.GamepadNavigationItem.Active = true;

            result.GamepadNavigationItem.SetCallbacks(
                result.OnGamepadOver,
                result.OnGamepadOut,
                result.OnGamepadPress
            );
        }
    }

    private static void OpenAlchemyFolio(
        UIAlchemyWindow alchemyWindow,
        UIAlchemyWindowData alchemyData
    )
    {
        if (alchemyWindow == null || alchemyData?.Wgo?.Data == null)
        {
            return;
        }

        UIAlchemyFolioWindow folio = LazyUI.GetWindow<UIAlchemyFolioWindow>();

        if (folio == null)
        {
            ModLog.Warning("Could not obtain UIAlchemyFolioWindow.");

            return;
        }

        UIAlchemyFolioWindowData folioData = new UIAlchemyFolioWindowData();

        folioData.FillFromGaveSave(alchemyData.Wgo.Data);

        AlchemyFolioSelectionContext.Begin(alchemyWindow);

        folio.Open(folioData);

        ModLog.Debug("Opened alchemy folio from laboratory.");
    }
}
