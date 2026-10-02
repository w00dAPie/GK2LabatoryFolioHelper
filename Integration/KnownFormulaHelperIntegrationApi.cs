using BepInEx.Configuration;
using GK2LaboratoryFolioHelper.Configuration;

namespace GK2LaboratoryFolioHelper.Integration;

public static class KnownFormulaHelperIntegrationApi
{
    public static ConfigEntry<int> MaxMixVariants => LaboratoryConfig.MaxMixVariants;
    public static ConfigEntry<UiFontStyle> FontStyle => UiConfig.FontStyle;
}
