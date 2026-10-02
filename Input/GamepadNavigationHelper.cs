using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;
using UnityEngine.Events;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class GamepadNavigationHelper
{
    private static readonly AccessTools.FieldRef<
        GamepadNavigationController,
        List<GamepadNavigationItem>
    > ReadItems = AccessTools.FieldRefAccess<
        GamepadNavigationController,
        List<GamepadNavigationItem>
    >("selectableItems");

    private static readonly ConditionalWeakTable<LazyButton, ButtonPress> PressCallbacks = new();

    private static class ControllerAccessor<T>
    {
        internal static readonly MethodInfo Getter = AccessTools.PropertyGetter(
            typeof(T),
            "GamepadNavigationController"
        );
    }

    private sealed class ButtonPress
    {
        private readonly LazyButton button;

        internal readonly UnityAction Callback;

        internal ButtonPress(LazyButton button)
        {
            this.button = button;
            Callback = Press;
        }

        private void Press()
        {
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }
    }

    internal static GamepadNavigationController GetController<T>(T window)
        where T : Object
    {
        if (window == null)
        {
            return null;
        }

        MethodInfo getter = ControllerAccessor<T>.Getter;

        if (getter == null)
        {
            return null;
        }

        return getter.Invoke(window, null) as GamepadNavigationController;
    }

    internal static List<GamepadNavigationItem> GetItems(GamepadNavigationController controller)
    {
        if (controller == null)
        {
            return null;
        }

        return ReadItems(controller);
    }

    internal static void Unregister(
        GamepadNavigationController controller,
        GamepadNavigationItem item
    )
    {
        if (controller == null || item == null)
        {
            return;
        }

        GetItems(controller)?.Remove(item);
    }

    internal static void Register(
        GamepadNavigationController controller,
        GamepadNavigationItem item,
        float guiScale
    )
    {
        if (controller == null || item == null)
        {
            return;
        }

        List<GamepadNavigationItem> items = GetItems(controller);

        if (items == null || items.Contains(item))
        {
            return;
        }

        items.Add(item);

        item.Init(items.Count - 1, controller, guiScale);
    }

    internal static void BindButtonPress(GamepadNavigationItem nav, LazyButton button)
    {
        if (nav == null || button == null)
        {
            return;
        }

        if (!PressCallbacks.TryGetValue(button, out ButtonPress press))
        {
            press = new ButtonPress(button);

            PressCallbacks.Add(button, press);
        }

        // Use the button's normal hover transitions as well as its click
        // action. This makes dynamically added buttons look and behave like
        // the vanilla Craft Max button when selected by a controller.
        nav.SetCallbacks(button.ForceOnEnter, button.ForceOnExit, press.Callback);
    }
}
