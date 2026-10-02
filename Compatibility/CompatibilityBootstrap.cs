using GK2LaboratoryFolioHelper.Patches;
using HarmonyLib;

namespace GK2LaboratoryFolioHelper.Compatibility;

internal static class CompatibilityBootstrap
{
    internal static void Initialize(Harmony harmony)
    {
        RecipePinCompatibilityPatch.TryInstall(harmony);
        ShoppingListCompatibilityPatch.TryInstall(harmony);
        AdditionalRecipePinHuds.Initialize(harmony);
    }

    internal static void Update()
    {
        AdditionalRecipePinHuds.ProcessPendingAssemblies();
    }

    internal static void Shutdown()
    {
        ShoppingListCompatibilityPatch.Shutdown();
        AdditionalRecipePinHuds.Shutdown();
    }
}
