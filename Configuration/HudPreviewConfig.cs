using BepInEx.Configuration;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.Configuration;

internal static class HudPreviewConfig
{
    internal static ConfigEntry<bool> Enabled { get; private set; }
    internal static ConfigEntry<KeyboardShortcut> NextScenario { get; private set; }
    internal static ConfigEntry<KeyboardShortcut> PreviousScenario { get; private set; }

    internal static void Bind(ConfigFile config)
    {
        Enabled = config.Bind(
            "Debug.HudPositionPreview",
            "Enabled",
            false,
            "Enable simulated pin panels for position testing without other mods. Restart the game after changing this setting."
        );

        NextScenario = config.Bind(
            "Debug.HudPositionPreview",
            "NextScenario",
            new KeyboardShortcut(KeyCode.F7),
            "Next HUD position test. The first press starts the preview; Off restores the normal HUD."
        );

        PreviousScenario = config.Bind(
            "Debug.HudPositionPreview",
            "PreviousScenario",
            new KeyboardShortcut(KeyCode.F6),
            "Previous HUD position test."
        );
    }
}
