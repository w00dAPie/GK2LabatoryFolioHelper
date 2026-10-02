using BepInEx.Logging;

namespace GK2LaboratoryFolioHelper.Infrastructure;

internal static class ModLog
{
    private static ManualLogSource source;

    internal static void Initialize(ManualLogSource logger)
    {
        source = logger;
    }

    internal static void Debug(string message)
    {
        source?.LogDebug(message);
    }

    internal static void Info(string message)
    {
        source?.LogInfo(message);
    }

    internal static void Warning(string message)
    {
        source?.LogWarning(message);
    }

    internal static void Error(string message)
    {
        source?.LogError(message);
    }
}
