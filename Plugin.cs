using BepInEx;
using GK2LaboratoryFolioHelper.Helpers;
using GK2LaboratoryFolioHelper.Runtime;

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
    public const string PluginName = "GK2 Known Formula Helper";
    public const string PluginVersion = "0.3.0";

    private readonly ModRuntime runtime = new();

    private void Awake() => runtime.Initialize(Logger, Config);

    private void Update() => runtime.Update();

    private void OnDestroy() => runtime.Shutdown();
}
