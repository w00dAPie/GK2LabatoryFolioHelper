using BepInEx.Configuration;

namespace GK2LaboratoryFolioHelper.Configuration;

internal static class LaboratoryConfig
{
    internal static ConfigEntry<int> MaxMixVariants { get; private set; }

    internal static void Bind(ConfigFile config)
    {
        MaxMixVariants = config.Bind(
            "Laboratory",
            "MaxMixVariants",
            10,
            new ConfigDescription(
                "Maximum number of laboratory mix variants shown for a selected formula.",
                new AcceptableValueRange<int>(1, 30)
            )
        );
    }
}
