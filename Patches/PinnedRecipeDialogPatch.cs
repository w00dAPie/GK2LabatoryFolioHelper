using System;
using System.Collections.Generic;
using System.Reflection;
using GK2LaboratoryFolioHelper.Helpers;
using HarmonyLib;
using LazyBearTechnology;

namespace GK2LaboratoryFolioHelper.Patches;

[HarmonyPatch]
internal static class PinnedRecipeDialogPatch
{
    internal static bool IsActive;

    private static readonly FieldInfo LazyButtonTipsField = AccessTools.Field(
        typeof(LazyWindow<UIDialogWindowData>),
        "lazyButtonTips"
    );

    [HarmonyPatch(typeof(UIDialogWindow), "PrintTips")]
    [HarmonyPrefix]
    private static bool PrintTipsPrefix(UIDialogWindow __instance)
    {
        if (!IsActive || __instance == null)
        {
            return true;
        }

        ShowTips(__instance);

        return false;
    }

    [HarmonyPatch(typeof(UIDialogWindow), "GetGameKeyDelegates")]
    [HarmonyPatch(typeof(UIDialogWindow), "GetGameKeyDelegates")]
    [HarmonyPostfix]
    private static void GetGameKeyDelegatesPostfix(
        UIDialogWindow __instance,
        ref Dictionary<GameKey, Func<bool>> __result
    )
    {
        if (__instance == null || __result == null)
        {
            return;
        }

        /*
         * WICHTIG:
         *
         * GetGameKeyDelegates wird bereits beim Initialisieren
         * des Fensters aufgerufen.
         *
         * Deshalb hier NICHT auf IsActive prüfen.
         * Der Delegate bleibt dauerhaft registriert und prüft
         * erst beim tatsächlichen Tastendruck.
         */
        __result[GameKey.Select] = () =>
        {
            if (!IsActive)
            {
                return false;
            }

            return PressFocusedButton(__instance);
        };
    }

    private static bool PressFocusedButton(UIDialogWindow dialog)
    {
        GamepadNavigationController controller = GamepadNavigationHelper.GetController(dialog);

        if (controller == null)
        {
            return false;
        }

        GamepadNavigationItem focused = controller.FocusedItem;

        if (focused == null)
        {
            return false;
        }

        UIDialogWindowButton dialogButton = focused.GetComponent<UIDialogWindowButton>();

        if (dialogButton == null)
        {
            return false;
        }

        LazyButton button = dialogButton.LazyButton;

        if (button == null || !button.interactable)
        {
            return false;
        }

        button.onClick.Invoke();

        Plugin.Log.LogInfo("Pinned recipe selected by gamepad.");

        return true;
    }

    internal static void ShowTips(UIDialogWindow dialog)
    {
        if (dialog == null || !LazyInput.IsGamepadActive)
        {
            return;
        }

        LazyButtonTipsStr tips = LazyButtonTipsField?.GetValue(dialog) as LazyButtonTipsStr;

        if (tips == null)
        {
            return;
        }

        tips.Print(LazyGameKeyTip.Select(), LazyGameKeyTip.Back());
    }
}
