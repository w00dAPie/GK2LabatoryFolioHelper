using System.Collections.Generic;
using System.Reflection;
using GK2LaboratoryFolioHelper.Infrastructure;
using HarmonyLib;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class AlchemyRecipeLoader
{
    private static readonly FieldInfo IngredientsField = AccessTools.Field(
        typeof(UIAlchemyWindow),
        "ingredients"
    );

    private static readonly FieldInfo CraftCountField = AccessTools.Field(
        typeof(UIAlchemyWindow),
        "craftCount"
    );

    private static readonly MethodInfo RedrawMethod = AccessTools.Method(
        typeof(UIAlchemyWindow),
        "RedrawAlchemyTabLite",
        new[] { typeof(AlchemyMixDef) }
    );

    public static bool LoadMix(UIAlchemyWindow window, string mixId)
    {
        if (window == null || string.IsNullOrEmpty(mixId))
        {
            return false;
        }

        AlchemyMixDef mix = GameBalance.GetAlchemyMixDef(mixId);

        if (mix == null)
        {
            ModLog.Warning($"Cannot load alchemy mix '{mixId}': mix not found.");

            return false;
        }

        List<UIAlchemyIngredient> ingredients =
            IngredientsField?.GetValue(window) as List<UIAlchemyIngredient>;

        if (ingredients == null || ingredients.Count == 0)
        {
            ModLog.Warning("Could not access UIAlchemyWindow.ingredients.");

            return false;
        }

        CraftCountField?.SetValue(window, 1);

        for (int i = 0; i < ingredients.Count; i++)
        {
            UIAlchemyIngredient slot = ingredients[i];

            if (slot == null)
            {
                continue;
            }

            if (i < mix.ingredients.Length && !string.IsNullOrEmpty(mix.ingredients[i]))
            {
                string itemId = mix.ingredients[i];

                int availableCount = MainGame.PlayerController.WorkerMultiInventory.GetTotalCount(
                    itemId
                );

                Item item = new Item(itemId);

                slot.cell.Draw(item, isNeedItem: true, availableCount);

                slot.plusObj.SetActive(false);
            }
            else
            {
                slot.cell.DrawEmptyInteractable();
                slot.plusObj.SetActive(true);
            }
        }

        RedrawMethod?.Invoke(window, new object[] { mix });

        ModLog.Debug($"Loaded pinned alchemy mix '{mixId}'.");

        return true;
    }

    public static void RefreshAvailableCounts(UIAlchemyWindow window)
    {
        if (window == null)
        {
            return;
        }

        List<UIAlchemyIngredient> ingredients =
            IngredientsField?.GetValue(window) as List<UIAlchemyIngredient>;

        if (ingredients == null)
        {
            return;
        }

        foreach (UIAlchemyIngredient ingredient in ingredients)
        {
            if (
                ingredient == null
                || ingredient.cell == null
                || ingredient.cell.DisplayingItem == null
                || ingredient.cell.DisplayingItem.IsEmpty
            )
            {
                continue;
            }

            string itemId = ingredient.cell.DisplayingItem.id;

            int availableCount = MainGame.PlayerController.WorkerMultiInventory.GetTotalCount(
                itemId
            );

            ingredient.cell.Draw(new Item(itemId), isNeedItem: true, availableCount);

            ingredient.plusObj.SetActive(false);
        }
    }
}
