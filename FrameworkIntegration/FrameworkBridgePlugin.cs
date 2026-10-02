using BepInEx;
using GK2.Framework;
using GK2LaboratoryFolioHelper;
using GK2LaboratoryFolioHelper.Integration;

namespace GK2KnownFormulaHelper.FrameworkIntegration;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Plugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(FrameworkPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
public sealed class FrameworkBridgePlugin : BaseUnityPlugin
{
    public const string PluginGuid = "w00dst0ckOos.GK2LaboratoryFolioHelper.framework";

    public const string PluginName = "GK2 Known Formula Helper - GK2 Framework Integration";

    public const string PluginVersion = Plugin.PluginVersion;

    private void Awake()
    {
        Plugin main = Plugin.Instance;

        if (main == null)
        {
            Logger.LogError("Main mod instance is unavailable.");
            return;
        }

        FrameworkApi.RegisterMod(new FrameworkBridge(), main.Config);

        Logger.LogInfo("GK2 Mod Framework integration registered.");
    }

    private sealed class FrameworkBridge : Gk2ModBase
    {
        private readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
            Plugin.PluginGuid,
            Plugin.PluginName,
            "w00dst0ckOos",
            Plugin.PluginVersion,
            "Optional GK2 Mod Framework integration for GK2 Known Formula Helper.",
            supportsRuntimeToggle: false,
            requiresKnownBuild: false,
            frameworkManagesEnabledState: false
        );

        public override Gk2ModMetadata Metadata => metadata;

        public override void OnRegister(Gk2ModContext context)
        {
            context.Settings.AddIntSlider(
                KnownFormulaHelperIntegrationApi.MaxMixVariants,
                1,
                30,
                "Maximum Mix Variants",
                "Maximum number of laboratory mix variants shown for a selected formula."
            );

            context.Settings.AddEnum(
                KnownFormulaHelperIntegrationApi.FontStyle,
                "Font Style",
                "Selects the text style used by GK2 Known Formula Helper."
            );
        }
    }
}
