using System;
using GK2LaboratoryFolioHelper.Infrastructure;

namespace GK2LaboratoryFolioHelper.Alchemy;

internal static class AlchemyFolioSelectionContext
{
    private static UIAlchemyWindow alchemyWindow;

    internal static bool IsActive => alchemyWindow != null;

    internal static UIAlchemyWindow Window => alchemyWindow;

    internal static void Begin(UIAlchemyWindow window)
    {
        alchemyWindow = window;

        ModLog.Debug("Alchemy folio recipe selection started.");
    }

    internal static void End()
    {
        alchemyWindow = null;

        ModLog.Debug("Alchemy folio recipe selection ended.");
    }
}
