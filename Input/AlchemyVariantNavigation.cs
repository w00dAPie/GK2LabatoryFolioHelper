using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.Helpers;

internal sealed class AlchemyVariantNavigation
{
    private static readonly FieldInfo IngredientsField = AccessTools.Field(
        typeof(UIAlchemyWindow),
        "ingredients"
    );
    private static readonly FieldInfo ResultField = AccessTools.Field(
        typeof(UIAlchemyWindow),
        "result"
    );

    private readonly List<NavigationLink> navigationLinks = new();

    private GamepadNavigationController navigationController;
    private GamepadNavigationItem leftNavigation;
    private GamepadNavigationItem rightNavigation;

    internal void Refresh(UIAlchemyWindow window, GameObject controls)
    {
        GamepadNavigationController controller = GamepadNavigationHelper.GetController(window);
        if (controller == null || controls == null)
        {
            return;
        }

        navigationController = controller;

        foreach (LazyButton button in controls.GetComponentsInChildren<LazyButton>(true))
        {
            GamepadNavigationItem nav = button.GetComponentInChildren<GamepadNavigationItem>(true);
            nav ??= button.gameObject.GetComponent<GamepadNavigationItem>();
            nav ??= button.gameObject.AddComponent<GamepadNavigationItem>();

            nav.enabled = true;
            nav.Active = true;
            GamepadNavigationHelper.Register(controller, nav, 1f);
            GamepadNavigationHelper.BindButtonPress(nav, button);

            if (button.gameObject.name == "VariantButton_<")
            {
                leftNavigation = nav;
            }
            else if (button.gameObject.name == "VariantButton_>")
            {
                rightNavigation = nav;
            }
        }

        LinkGamepadControls(window, controller);
    }

    internal void Clear()
    {
        RestoreGamepadNavigation();

        if (navigationController != null)
        {
            GamepadNavigationHelper.Unregister(navigationController, leftNavigation);
            GamepadNavigationHelper.Unregister(navigationController, rightNavigation);
        }

        leftNavigation = null;
        rightNavigation = null;
        navigationController = null;
    }

    private void LinkGamepadControls(UIAlchemyWindow window, GamepadNavigationController controller)
    {
        if (leftNavigation == null || rightNavigation == null)
        {
            return;
        }

        List<GamepadNavigationItem> items = GamepadNavigationHelper.GetItems(controller);
        if (items == null)
        {
            return;
        }

        GamepadNavigationItem craftMax = FindCraftMaxNavigation(items);
        GamepadNavigationItem result = FindResultNavigation(window);
        List<GamepadNavigationItem> ingredients = FindIngredientNavigations(window, controller);

        if (craftMax != null && result != null && ingredients.Count > 0)
        {
            LinkLaboratoryRoute(result, ingredients, craftMax);
            return;
        }

        GamepadNavigationItem leftAnchor = FindNavigationAnchor(leftNavigation, items);
        GamepadNavigationItem rightAnchor = FindNavigationAnchor(rightNavigation, items);

        if (craftMax == null && leftAnchor == null)
        {
            leftAnchor = FindClosestNavigationItem(leftNavigation, items);
        }

        if (rightAnchor == null)
        {
            rightAnchor = FindClosestNavigationItem(rightNavigation, items);
        }

        if (craftMax != null)
        {
            leftNavigation.group = craftMax.group;
            rightNavigation.group = craftMax.group;
            SetNavigationLink(craftMax, GUIDirection.Down, leftNavigation);
            SetNavigationLink(leftNavigation, GUIDirection.Up, craftMax);
            SetNavigationLink(rightNavigation, GUIDirection.Up, craftMax);
        }

        if (leftAnchor != null && craftMax == null)
        {
            leftNavigation.group = leftAnchor.group;
            LinkVerticalNeighbors(leftAnchor, leftNavigation);
        }

        if (rightAnchor != null)
        {
            rightNavigation.group = rightAnchor.group;
            LinkVerticalNeighbors(rightAnchor, rightNavigation);
        }

        if (rightAnchor == null)
        {
            rightNavigation.group = leftNavigation.group;
        }

        SetNavigationLink(leftNavigation, GUIDirection.Right, rightNavigation);
        SetNavigationLink(rightNavigation, GUIDirection.Left, leftNavigation);
    }

    private static GamepadNavigationItem FindResultNavigation(UIAlchemyWindow window)
    {
        UIItemCell result = ResultField?.GetValue(window) as UIItemCell;
        return result?.GamepadNavigationItem;
    }

    private static List<GamepadNavigationItem> FindIngredientNavigations(
        UIAlchemyWindow window,
        GamepadNavigationController controller
    )
    {
        List<GamepadNavigationItem> result = new();
        List<UIAlchemyIngredient> ingredients =
            IngredientsField?.GetValue(window) as List<UIAlchemyIngredient>;
        if (ingredients == null)
        {
            return result;
        }

        foreach (UIAlchemyIngredient ingredient in ingredients)
        {
            GamepadNavigationItem navigation = ingredient?.cell?.GamepadNavigationItem;
            if (navigation == null)
            {
                continue;
            }

            navigation.enabled = true;
            navigation.Active = true;
            GamepadNavigationHelper.Register(controller, navigation, 1f);
            result.Add(navigation);
        }

        return result;
    }

    private void LinkLaboratoryRoute(
        GamepadNavigationItem result,
        List<GamepadNavigationItem> ingredients,
        GamepadNavigationItem craftMax
    )
    {
        List<GamepadNavigationItem> route = new(ingredients.Count + 4) { result, leftNavigation };
        route.AddRange(ingredients);
        route.Add(rightNavigation);
        route.Add(craftMax);

        leftNavigation.group = result.group;
        rightNavigation.group = result.group;

        for (int i = 0; i < route.Count; i++)
        {
            GamepadNavigationItem current = route[i];
            GamepadNavigationItem next = route[(i + 1) % route.Count];
            GamepadNavigationItem previous = route[(i - 1 + route.Count) % route.Count];
            SetNavigationLink(current, GUIDirection.Right, next);
            SetNavigationLink(current, GUIDirection.Left, previous);
        }
    }

    private void LinkVerticalNeighbors(GamepadNavigationItem anchor, GamepadNavigationItem target)
    {
        if (anchor == null || target == null)
        {
            return;
        }

        Vector2 distance = target.Pos - anchor.Pos;
        if (Mathf.Abs(distance.y) >= Mathf.Abs(distance.x))
        {
            if (distance.y >= 0f)
            {
                SetNavigationLink(anchor, GUIDirection.Down, target);
                SetNavigationLink(target, GUIDirection.Up, anchor);
            }
            else
            {
                SetNavigationLink(anchor, GUIDirection.Up, target);
                SetNavigationLink(target, GUIDirection.Down, anchor);
            }
            return;
        }

        if (distance.x >= 0f)
        {
            SetNavigationLink(anchor, GUIDirection.Right, target);
            SetNavigationLink(target, GUIDirection.Left, anchor);
        }
        else
        {
            SetNavigationLink(anchor, GUIDirection.Left, target);
            SetNavigationLink(target, GUIDirection.Right, anchor);
        }
    }

    private GamepadNavigationItem FindClosestNavigationItem(
        GamepadNavigationItem target,
        List<GamepadNavigationItem> items
    )
    {
        GamepadNavigationItem closest = null;
        float bestScore = float.MaxValue;

        foreach (GamepadNavigationItem item in items)
        {
            if (!IsUsableCandidate(item, target))
            {
                continue;
            }

            float score = (target.Pos - item.Pos).sqrMagnitude;
            if (score < bestScore)
            {
                closest = item;
                bestScore = score;
            }
        }

        return closest;
    }

    private GamepadNavigationItem FindCraftMaxNavigation(List<GamepadNavigationItem> items)
    {
        foreach (GamepadNavigationItem item in items)
        {
            if (!IsUsableCandidate(item, null))
            {
                continue;
            }

            for (Transform current = item.transform; current != null; current = current.parent)
            {
                if (
                    current.name.IndexOf("CraftMax", StringComparison.OrdinalIgnoreCase) >= 0
                    || current.name.IndexOf("MaxButton", StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    return item;
                }
            }

            foreach (TMP_Text label in item.GetComponentsInChildren<TMP_Text>(true))
            {
                if (string.Equals(label.text?.Trim(), "MAX", StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
        }

        return null;
    }

    private GamepadNavigationItem FindNavigationAnchor(
        GamepadNavigationItem target,
        List<GamepadNavigationItem> items
    )
    {
        GamepadNavigationItem best = null;
        float bestScore = float.MaxValue;

        foreach (GamepadNavigationItem item in items)
        {
            if (
                !IsUsableCandidate(item, target)
                || !item.CorrectDirection(target.Pos, GUIDirection.Down)
            )
            {
                continue;
            }

            Vector2 distance = target.Pos - item.Pos;
            float score = Mathf.Abs(distance.x) + Mathf.Abs(distance.y) * 0.35f;
            if (score < bestScore)
            {
                best = item;
                bestScore = score;
            }
        }

        return best;
    }

    private bool IsUsableCandidate(GamepadNavigationItem item, GamepadNavigationItem target)
    {
        return item != null
            && item != target
            && item != leftNavigation
            && item != rightNavigation
            && item.Active
            && item.isActiveAndEnabled;
    }

    private void SetNavigationLink(
        GamepadNavigationItem from,
        GUIDirection direction,
        GamepadNavigationItem to
    )
    {
        if (from == null || to == null)
        {
            return;
        }

        NavigationLink link = navigationLinks.Find(existing =>
            existing.From == from && existing.Direction == direction
        );

        if (link == null)
        {
            link = new NavigationLink
            {
                From = from,
                Direction = direction,
                Previous = from.GetCustomDirectionItem(direction),
            };
            navigationLinks.Add(link);
        }

        link.Applied = to;
        if (from.GetCustomDirectionItem(direction) != to)
        {
            from.SetCustomDirectionItem(direction, to);
        }
    }

    private void RestoreGamepadNavigation()
    {
        for (int i = navigationLinks.Count - 1; i >= 0; i--)
        {
            NavigationLink link = navigationLinks[i];
            if (
                link.From != null
                && link.From.GetCustomDirectionItem(link.Direction) == link.Applied
            )
            {
                link.From.SetCustomDirectionItem(link.Direction, link.Previous);
            }
        }

        navigationLinks.Clear();
    }

    private sealed class NavigationLink
    {
        internal GamepadNavigationItem From;
        internal GUIDirection Direction;
        internal GamepadNavigationItem Previous;
        internal GamepadNavigationItem Applied;
    }
}
