using System.Collections.Generic;
using System.Reflection;
using GK2LaboratoryFolioHelper.Helpers;
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

    private static readonly FieldInfo ActiveButtonsField = AccessTools.Field(
        typeof(UIDialogWindow),
        "activeButtons"
    );

    private static readonly PropertyInfo GamepadNavigationControllerProperty = AccessTools.Property(
        typeof(UIDialogWindow),
        "GamepadNavigationController"
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

        // Vanilla hat bereits einen echten Mix.
        // Dann nichts verändern.
        if (currentMix != null)
        {
            return;
        }

        List<PinnedFormulaManager.PinnedFormulaViewData> pins =
            PinnedFormulaManager.GetPinnedRecipesForLaboratory();

        if (pins == null || pins.Count == 0)
        {
            return;
        }

        result.DrawEmpty(drawAsNonInteractable: false);

        result.NoSelectionFrames = false;

        result.LazyButton.interactable = true;

        result.OnItemCellPress = _ => OpenPinnedRecipePicker(__instance);

        if (result.GamepadNavigationItem != null)
        {
            result.GamepadNavigationItem.Active = true;

            result.GamepadNavigationItem.SetCallbacks(
                result.OnGamepadOver,
                result.OnGamepadOut,
                result.OnGamepadPress
            );
        }

        Plugin.Log.LogInfo($"Pinned recipe picker enabled | " + $"pins={pins.Count}");
    }

    private static void OpenPinnedRecipePicker(UIAlchemyWindow alchemyWindow)
    {
        List<PinnedFormulaManager.PinnedFormulaViewData> pins =
            PinnedFormulaManager.GetPinnedRecipesForLaboratory();

        if (pins == null || pins.Count == 0)
        {
            Plugin.Log.LogInfo("Pinned recipe picker opened without pins.");

            return;
        }

        UIDialogWindow dialog = LazyUI.GetWindow<UIDialogWindow>();

        if (dialog == null)
        {
            Plugin.Log.LogWarning("Could not obtain UIDialogWindow.");

            return;
        }

        List<UIDialogWindowData.ButtonData> buttons = new();

        foreach (PinnedFormulaManager.PinnedFormulaViewData pin in pins)
        {
            string formulaId = pin.FormulaId;

            string formulaName = pin.Name;

            buttons.Add(
                new UIDialogWindowData.ButtonData(
                    () =>
                    {
                        string mixId = PinnedFormulaManager.GetBestMixId(formulaId);

                        if (string.IsNullOrEmpty(mixId))
                        {
                            Plugin.Log.LogWarning($"No best mix found for '{formulaId}'.");

                            return;
                        }

                        dialog.Close();

                        bool loaded = AlchemyRecipeLoader.LoadMix(alchemyWindow, mixId);

                        Plugin.Log.LogInfo(
                            $"Pinned recipe selected | "
                                + $"formula='{formulaId}' | "
                                + $"mix='{mixId}' | "
                                + $"loaded={loaded}"
                        );
                    },
                    formulaName,
                    null,
                    replaceForGamepad: false,
                    keyToReplace: GameKey.Select
                )
            );
        }

        UIDialogWindowData dialogData = new UIDialogWindowData(
            "Pinned Recipes",
            "Select a recipe",
            buttons[0]
        );

        dialogData.ButtonsData = buttons;

        dialogData.ShowCloseButton = true;

        dialogData.CloseButtonAction = dialog.Close;

        PinnedRecipeDialogPatch.IsActive = true;

        dialog.Open(dialogData);

        BindDialogGamepadButtons(dialog);

        PinnedRecipeDialogPatch.ShowTips(dialog);

        Plugin.Log.LogInfo($"Opened pinned recipe picker | " + $"count={buttons.Count}");
    }

    private static void BindDialogGamepadButtons(UIDialogWindow dialog)
    {
        if (dialog == null || !LazyInput.IsGamepadActive)
        {
            return;
        }

        List<UIDialogWindowButton> activeButtons =
            ActiveButtonsField?.GetValue(dialog) as List<UIDialogWindowButton>;

        if (activeButtons == null || activeButtons.Count == 0)
        {
            return;
        }

        GamepadNavigationController controller = GamepadNavigationHelper.GetController(dialog);

        if (controller == null)
        {
            Plugin.Log.LogWarning("Pinned recipe picker: no gamepad controller.");

            return;
        }

        GamepadNavigationItem first = null;

        foreach (UIDialogWindowButton dialogButton in activeButtons)
        {
            if (dialogButton == null || !dialogButton.gameObject.activeInHierarchy)
            {
                continue;
            }

            LazyButton button = dialogButton.LazyButton;

            if (button == null)
            {
                continue;
            }

            GamepadNavigationItem nav = dialogButton.GetComponent<GamepadNavigationItem>();

            if (nav == null)
            {
                nav = dialogButton.gameObject.AddComponent<GamepadNavigationItem>();
            }

            nav.enabled = true;
            nav.Active = true;

            GamepadNavigationHelper.Register(controller, nav, dialog.transform.lossyScale.x);

            GamepadNavigationHelper.BindButtonPress(nav, button);

            first ??= nav;
        }

        if (first == null)
        {
            Plugin.Log.LogWarning("Pinned recipe picker: no selectable recipe buttons.");

            return;
        }

        controller.ReinitItems(focusOnFirstActive: false);

        controller.SetFocusedItem(first);

        Plugin.Log.LogInfo($"Pinned recipe gamepad bound | " + $"buttons={activeButtons.Count}");
    }
}
