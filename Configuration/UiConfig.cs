using BepInEx.Configuration;

namespace GK2LaboratoryFolioHelper.Configuration;

internal static class UiConfig
{
    internal static ConfigEntry<UiFontStyle> FontStyle { get; private set; }

    internal static void Bind(ConfigFile config)
    {
        FontStyle = config.Bind(
            "UI",
            "FontStyle",
            UiFontStyle.Sharp,
            "Game uses the loaded Graveyard Keeper 2 UI font. Sharp uses the optional Alegreya Sans TMP bundle."
        );
    }
}
