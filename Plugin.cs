using BepInEx;
using BepInEx.Logging;
using GK2LaboratoryFolioHelper.Helpers;
using GK2LaboratoryFolioHelper.Patches;
using HarmonyLib;

namespace GK2LaboratoryFolioHelper;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(
    ExternalHudCompatibility.RecipePinGuid,
    BepInDependency.DependencyFlags.SoftDependency
)]
[BepInDependency(
    ExternalHudCompatibility.ShoppingListGuid,
    BepInDependency.DependencyFlags.SoftDependency
)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "w00dst0ckOos.GK2LaboratoryFolioHelper";
    public const string PluginName = "GK2 Laboratory Folio Helper";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource Log;

    private Harmony _harmony;

    private void Awake()
    {
        Log = Logger;

        Log.LogInfo($"{PluginName} {PluginVersion} loading...");

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();

        RecipePinCompatibilityPatch.TryInstall(_harmony);
        ShoppingListCompatibilityPatch.TryInstall(_harmony);

        Log.LogInfo($"{PluginName} loaded.");
    }

    private void OnDestroy()
    {
        ShoppingListCompatibilityPatch.Shutdown();
        LaboratoryPinsHud.Shutdown();
        _harmony?.UnpatchSelf();
    }
}
