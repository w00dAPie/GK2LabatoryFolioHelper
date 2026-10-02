using BepInEx.Configuration;
using BepInEx.Logging;
using GK2LaboratoryFolioHelper.Alchemy;
using GK2LaboratoryFolioHelper.Compatibility;
using GK2LaboratoryFolioHelper.Configuration;
using GK2LaboratoryFolioHelper.Infrastructure;
using GK2LaboratoryFolioHelper.UI;
using GK2LaboratoryFolioHelper.UI.Typography;
using HarmonyLib;

namespace GK2LaboratoryFolioHelper.Runtime;

internal sealed class ModRuntime
{
    private Harmony harmony;

    internal void Initialize(ManualLogSource logger, ConfigFile config)
    {
        ModLog.Initialize(logger);
        ModConfig.Bind(config);

        ModLog.Info($"{Plugin.PluginName} {Plugin.PluginVersion} loading...");
        harmony = new Harmony(Plugin.PluginGuid);
        harmony.PatchAll();
        CompatibilityBootstrap.Initialize(harmony);
        ModLog.Info($"{Plugin.PluginName} loaded.");
    }

    internal void Update()
    {
        CompatibilityBootstrap.Update();
    }

    internal void Shutdown()
    {
        PinsInventoryWatcher.DetachAll();
        AlchemyInventoryWatcher.Detach();
        CompatibilityBootstrap.Shutdown();
        LaboratoryPinsHud.Shutdown();
        ModTypography.Shutdown();
        harmony?.UnpatchSelf();
        harmony = null;
    }
}
