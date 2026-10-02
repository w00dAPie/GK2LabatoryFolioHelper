using BepInEx.Configuration;

namespace GK2LaboratoryFolioHelper.Configuration;

internal static class InputConfig
{
    internal static ConfigEntry<bool> RemoveLastPinWithR3 { get; private set; }

    internal static void Bind(ConfigFile config)
    {
        RemoveLastPinWithR3 = config.Bind(
            "Input",
            "RemoveLastPinWithR3",
            true,
            "Remove the most recently added helper pin when the controller's R3 button is pressed. Disable this if another mod uses R3."
        );
    }
}
