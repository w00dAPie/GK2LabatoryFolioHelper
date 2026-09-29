using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using GK2LaboratoryFolioHelper.Helpers;
using HarmonyLib;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.Patches;

internal static class ShoppingListCompatibilityPatch
{
    private static ConfigFile config;
    private static BaseUnityPlugin plugin;
    private static Coroutine configRefresh;
    private static bool installed;

    public static void TryInstall(Harmony harmony)
    {
        if (
            installed
            || !Chainloader.PluginInfos.TryGetValue(
                ExternalHudCompatibility.ShoppingListGuid,
                out var pluginInfo
            )
            || pluginInfo.Instance == null
        )
        {
            return;
        }

        try
        {
            BaseUnityPlugin instance = pluginInfo.Instance;
            Type pluginType = instance.GetType();
            Type hudType = AccessTools.Field(pluginType, "_hud")?.FieldType;
            Type listType = AccessTools.Field(pluginType, "List")?.FieldType;
            MethodInfo tick =
                hudType == null || listType == null
                    ? null
                    : AccessTools.Method(
                        hudType,
                        "Tick",
                        new[] { listType, typeof(bool), typeof(bool) }
                    );

            if (tick == null || AccessTools.Field(hudType, "_sig")?.FieldType != typeof(string))
            {
                Plugin.Log.LogWarning("Shopping List HudPanel.Tick layout hooks were not found.");
                return;
            }

            harmony.Patch(
                tick,
                transpiler: new HarmonyMethod(
                    AccessTools.Method(
                        typeof(ShoppingListCompatibilityPatch),
                        nameof(NotifyLayoutChanges)
                    )
                )
            );

            config = instance.Config;
            plugin = instance;
            config.SettingChanged += OnSettingChanged;
            installed = true;
            Plugin.Log.LogInfo("Shopping List HUD change hooks installed (no frame polling).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not install Shopping List HUD hooks: {ex.Message}");
        }
    }

    // 0.7.0 Tick runs every frame, but its existing branches guard visibility
    // changes and the _sig write guards redraws. Notify only inside those branches;
    // do not add a postfix that scans transforms or rebuilds our HUD every frame.
    private static IEnumerable<CodeInstruction> NotifyLayoutChanges(
        IEnumerable<CodeInstruction> instructions,
        MethodBase original
    )
    {
        FieldInfo signature = AccessTools.Field(original.DeclaringType, "_sig");
        MethodInfo setActive = AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive));
        MethodInfo setActiveAndNotify = AccessTools.Method(
            typeof(ShoppingListCompatibilityPatch),
            nameof(SetActiveAndNotify)
        );
        MethodInfo notify = AccessTools.Method(
            typeof(LaboratoryPinsHud),
            nameof(LaboratoryPinsHud.RequestRefresh)
        );
        List<CodeInstruction> result = new();
        int visibilitySites = 0;
        int redrawSites = 0;

        foreach (CodeInstruction instruction in instructions)
        {
            CodeInstruction copy = new CodeInstruction(instruction);
            if (copy.Calls(setActive))
            {
                // The static wrapper consumes the same GameObject/bool stack values.
                copy.opcode = OpCodes.Call;
                copy.operand = setActiveAndNotify;
                visibilitySites++;
            }

            result.Add(copy);
            if (instruction.opcode == OpCodes.Stfld && Equals(instruction.operand, signature))
            {
                result.Add(new CodeInstruction(OpCodes.Call, notify));
                redrawSites++;
            }
        }

        if (visibilitySites == 0 || redrawSites != 1)
        {
            throw new InvalidOperationException(
                "Shopping List Tick no longer matches the inspected visibility/redraw branches."
            );
        }

        return result;
    }

    private static void SetActiveAndNotify(GameObject target, bool active)
    {
        bool changed = target.activeSelf != active;
        target.SetActive(active);
        if (changed)
        {
            LaboratoryPinsHud.RequestRefresh();
        }
    }

    private static void OnSettingChanged(object sender, SettingChangedEventArgs args)
    {
        if (
            args.ChangedSetting.Definition.Section == "HUD"
            && configRefresh == null
            && plugin != null
        )
        {
            // Settings can change after Shopping List's Update in this frame.
            // Wait once for its next Tick to apply scale/offsets, then measure.
            configRefresh = plugin.StartCoroutine(RefreshAfterSettingsApplied());
        }
    }

    private static IEnumerator RefreshAfterSettingsApplied()
    {
        yield return null;
        configRefresh = null;
        LaboratoryPinsHud.RequestRefresh();
    }

    internal static void Shutdown()
    {
        if (plugin != null && configRefresh != null)
        {
            plugin.StopCoroutine(configRefresh);
        }

        configRefresh = null;
        plugin = null;
        if (config != null)
        {
            config.SettingChanged -= OnSettingChanged;
            config = null;
        }

        installed = false;
    }
}
