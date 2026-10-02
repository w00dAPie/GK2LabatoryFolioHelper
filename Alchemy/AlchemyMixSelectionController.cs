using System.Collections.Generic;
using GK2LaboratoryFolioHelper.Infrastructure;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class AlchemyMixSelectionController
{
    private const int MaxOptions = 10;

    private static readonly AlchemyVariantControls Controls = new();
    private static readonly AlchemyVariantNavigation Navigation = new();

    private static UIAlchemyWindow window;
    private static List<PinnedFormulaManager.LaboratoryMixOption> options;
    private static string formulaId;
    private static int index;

    internal static bool IsActiveFor(UIAlchemyWindow candidate)
    {
        return candidate != null && ReferenceEquals(window, candidate) && options?.Count > 0;
    }

    internal static bool Begin(
        UIAlchemyWindow target,
        string targetFormulaId,
        int selectedIndex = 0
    )
    {
        if (
            target == null
            || !PinnedFormulaManager.TryGetBestMixesForLaboratory(
                targetFormulaId,
                MaxOptions,
                out List<PinnedFormulaManager.LaboratoryMixOption> mixes
            )
        )
        {
            return false;
        }

        Clear(target);
        window = target;
        formulaId = targetFormulaId;
        options = mixes;
        index = Mathf.Clamp(selectedIndex, 0, options.Count - 1);

        LoadCurrent();
        if (Controls.Build(window, SelectPrevious, SelectNext))
        {
            Controls.UpdateCounter(index, options);
            Navigation.Refresh(window, Controls.Root);
            ModLog.Info(
                $"Alchemy mix variant controls created beside ingredient slots | options={options.Count}"
            );
        }

        return true;
    }

    internal static void Tick(UIAlchemyWindow candidate)
    {
        if (!IsActiveFor(candidate))
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Comma))
        {
            Select(index - 1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Period))
        {
            Select(index + 1);
        }

        Controls.Position();
        Navigation.Refresh(window, Controls.Root);
    }

    internal static bool TryHandleDpad(
        UIAlchemyWindow candidate,
        int direction,
        System.Func<bool> fallback
    )
    {
        if (!IsActiveFor(candidate))
        {
            return fallback?.Invoke() ?? false;
        }

        Select(index + direction);
        return true;
    }

    internal static void Clear(UIAlchemyWindow candidate = null)
    {
        if (candidate != null && window != null && !ReferenceEquals(candidate, window))
        {
            return;
        }

        Navigation.Clear();
        Controls.Destroy();
        PinnedFormulaManager.ClearSelectedLaboratoryMix(formulaId);

        window = null;
        formulaId = null;
        options = null;
        index = 0;
    }

    private static void SelectPrevious() => Select(index - 1);

    private static void SelectNext() => Select(index + 1);

    private static void Select(int requested)
    {
        if (options == null || options.Count == 0)
        {
            return;
        }

        index = (requested % options.Count + options.Count) % options.Count;
        LoadCurrent();
    }

    private static void LoadCurrent()
    {
        if (window == null || options == null || options.Count == 0)
        {
            return;
        }

        PinnedFormulaManager.LaboratoryMixOption option = options[index];
        if (AlchemyRecipeLoader.LoadMix(window, option.MixId))
        {
            PinnedFormulaManager.SetSelectedLaboratoryMix(
                formulaId,
                option.MixId,
                option.IsCraftable
            );
        }

        Controls.UpdateCounter(index, options);
        ModLog.Info(
            $"Alchemy mix variant selected | index={index + 1}/{options.Count} | mix='{option.MixId}'"
        );
    }
}
