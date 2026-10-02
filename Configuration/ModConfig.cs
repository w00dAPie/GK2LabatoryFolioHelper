using BepInEx.Configuration;

namespace GK2LaboratoryFolioHelper.Configuration;

internal static class ModConfig
{
    internal static void Bind(ConfigFile config)
    {
        InputConfig.Bind(config);
        UiConfig.Bind(config);
        HudPreviewConfig.Bind(config);
        LaboratoryConfig.Bind(config);
    }
}
