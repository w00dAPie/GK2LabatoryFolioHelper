using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.Helpers;

// These mods are loaded by the game's mod loader / Kebo, not BepInEx.
internal static class AdditionalRecipePinHuds
{
    private sealed class HudBinding
    {
        internal FieldInfo Instance;
        internal FieldInfo Panel;
        internal bool Failed;
    }

    private static readonly Queue<Assembly> PendingAssemblies = new();
    private static readonly HashSet<Assembly> SeenAssemblies = new();
    private static readonly Dictionary<Type, HudBinding> Bindings = new();
    private static Harmony harmony;

    internal static void Initialize(Harmony owner)
    {
        harmony = owner;
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Enqueue(assembly);
        }

        ProcessPendingAssemblies();
    }

    private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
        // AssemblyLoad can run on a loader thread. Unity and Harmony work is
        // deferred until the plugin's next Update on the main thread.
        Enqueue(args.LoadedAssembly);
    }

    private static void Enqueue(Assembly assembly)
    {
        string name = assembly.GetName().Name;
        if (name != "GK2RecipePin.Core" && name != "KeboRecipePins")
        {
            return;
        }

        lock (PendingAssemblies)
        {
            PendingAssemblies.Enqueue(assembly);
        }
    }

    internal static void ProcessPendingAssemblies()
    {
        while (true)
        {
            Assembly assembly;
            lock (PendingAssemblies)
            {
                if (PendingAssemblies.Count == 0)
                {
                    return;
                }

                assembly = PendingAssemblies.Dequeue();
            }

            if (!SeenAssemblies.Add(assembly))
            {
                continue;
            }

            try
            {
                if (assembly.GetName().Name == "GK2RecipePin.Core")
                {
                    Install(assembly, "GK2RecipePin.TrackerOverlay", "instance", "BuildUI");
                }
                else
                {
                    Install(assembly, "KeboRecipePins.Ui.Hud", "Current", "Dock", "Place");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"Could not install {assembly.GetName().Name} HUD hooks: {ex.Message}"
                );
            }
        }
    }

    private static void Install(
        Assembly assembly,
        string typeName,
        string instanceField,
        params string[] methods
    )
    {
        Type type = assembly.GetType(typeName);
        FieldInfo instance = type == null ? null : AccessTools.Field(type, instanceField);
        FieldInfo panel = type == null ? null : AccessTools.Field(type, "panel");
        if (instance == null || !instance.IsStatic || panel?.FieldType != typeof(RectTransform))
        {
            Plugin.Log.LogWarning($"{typeName} HUD fields do not match the supported layout.");
            return;
        }

        var binding = new HudBinding { Instance = instance, Panel = panel };
        Bindings.Add(type, binding);
        foreach (string methodName in methods)
        {
            MethodInfo method = AccessTools.Method(type, methodName);
            if (method == null)
            {
                Plugin.Log.LogWarning($"{typeName}.{methodName} HUD hook was not found.");
                continue;
            }

            harmony.Patch(
                method,
                postfix: new HarmonyMethod(
                    typeof(AdditionalRecipePinHuds),
                    nameof(AfterLayoutChanged)
                )
            );
        }

        // The HUD may already exist when this assembly is first discovered.
        ObservePanel(binding);
        LaboratoryPinsHud.RequestPositionUpdate();
        Plugin.Log.LogDebug($"{typeName} HUD compatibility installed.");
    }

    private static void AfterLayoutChanged(object __instance)
    {
        if (Bindings.TryGetValue(__instance.GetType(), out HudBinding binding))
        {
            ObservePanel(binding, __instance);
            LaboratoryPinsHud.RequestPositionUpdate();
        }
    }

    private static RectTransform ObservePanel(HudBinding binding, object instance = null)
    {
        if (binding.Failed)
        {
            return null;
        }

        try
        {
            instance ??= binding.Instance.GetValue(null);
            if (instance == null || (instance is UnityEngine.Object obj && obj == null))
            {
                return null;
            }

            RectTransform panel = binding.Panel.GetValue(instance) as RectTransform;
            if (panel != null)
            {
                LaboratoryPinsLayoutListener.Observe(panel);
            }

            return panel;
        }
        catch (Exception ex)
        {
            binding.Failed = true;
            Plugin.Log.LogWarning(
                $"Could not read {binding.Panel.DeclaringType?.FullName} HUD: {ex.Message}"
            );
            return null;
        }
    }

    internal static void IncludeBottom(float left, float right, ref float bottom, ref bool found)
    {
        foreach (HudBinding binding in Bindings.Values)
        {
            RectTransform panel = ObservePanel(binding);
            ExternalHudCompatibility.IncludeOverlappingBottom(
                panel,
                left,
                right,
                ref bottom,
                ref found
            );
        }
    }

    internal static void Shutdown()
    {
        AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        lock (PendingAssemblies)
        {
            PendingAssemblies.Clear();
        }

        SeenAssemblies.Clear();
        Bindings.Clear();
        harmony = null;
    }
}
