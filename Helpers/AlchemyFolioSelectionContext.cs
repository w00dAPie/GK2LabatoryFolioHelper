using System;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class AlchemyFolioSelectionContext
{
    private static UIAlchemyWindow alchemyWindow;

    internal static bool IsActive => alchemyWindow != null;

    internal static UIAlchemyWindow Window => alchemyWindow;

    internal static void Begin(UIAlchemyWindow window)
    {
        alchemyWindow = window;

        Plugin.Log.LogDebug("Alchemy folio recipe selection started.");
    }

    internal static void End()
    {
        alchemyWindow = null;

        Plugin.Log.LogDebug("Alchemy folio recipe selection ended.");
    }
}
