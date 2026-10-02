using System.Collections.Generic;
using System.Reflection;
using GK2LaboratoryFolioHelper.Infrastructure;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class LaboratoryPinsHud
{
    private const float DefaultCardWidth = 310f;
    private const float CardScale = 0.8f;

    private static readonly FieldInfo RightUpGroupField = AccessTools.Field(
        typeof(HUD),
        "rightUpGroup"
    );
    private static readonly FieldInfo HappinessLabelField = AccessTools.Field(
        typeof(HUD),
        "happinessLabel"
    );

    private static GameObject panelObject;
    private static RectTransform contentRoot;
    private static bool layoutUpdateQueued;
    private static bool refreshPending;
    private static bool updatingPosition;
    private static List<PinnedFormulaManager.PinnedFormulaViewData> renderedPins;

    public static void Initialize(HUD hud)
    {
        if (hud == null)
        {
            return;
        }

        DestroyExisting();
        GameObject rightUpGroup = RightUpGroupField?.GetValue(hud) as GameObject;
        if (rightUpGroup == null)
        {
            ModLog.Warning("Could not find HUD.rightUpGroup.");
            return;
        }

        TextMeshProUGUI sourceLabel = HappinessLabelField?.GetValue(hud) as TextMeshProUGUI;
        if (sourceLabel != null)
        {
            ModTypography.Initialize(sourceLabel);
        }

        panelObject = new GameObject(
            "GK2LaboratoryPins",
            typeof(RectTransform),
            typeof(LayoutElement)
        );
        panelObject.GetComponent<LayoutElement>().ignoreLayout = true;
        AttachToHud(panelObject, rightUpGroup.transform);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = LaboratoryPinsPositioner.DefaultPosition;
        panelRect.sizeDelta = new Vector2(DefaultCardWidth, 0f);
        panelRect.localScale = new Vector3(CardScale, CardScale, 1f);

        if (rightUpGroup.transform is RectTransform anchor)
        {
            LaboratoryPinsLayoutListener.Observe(anchor);
        }

        GameObject contentObject = new(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter)
        );
        AttachToHud(contentObject, panelObject.transform);
        contentRoot = contentObject.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = Vector2.zero;
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter
            .FitMode
            .PreferredSize;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        PinnedFormulaManager.PinsChanged -= Refresh;
        PinnedFormulaManager.PinsChanged += Refresh;
        PinsInventoryWatcher.Initialize();
        HudPositionPreview.Attach(rightUpGroup, sourceLabel);
        Refresh();
        ModLog.Debug("Laboratory pins HUD initialized.");
    }

    public static void Refresh()
    {
        if (panelObject == null || contentRoot == null)
        {
            return;
        }

        UpdatePosition();
        List<PinnedFormulaManager.PinnedFormulaViewData> pins =
            PinnedFormulaManager.GetPinnedViewData();
        HudPositionPreview.AddSampleIfEmpty(pins);

        if (PinnedFormulaContentComparer.AreEqual(renderedPins, pins))
        {
            return;
        }

        ClearChildren(contentRoot);
        foreach (PinnedFormulaManager.PinnedFormulaViewData pin in pins)
        {
            LaboratoryPinCardFactory.Create(contentRoot, pin);
        }

        renderedPins = pins;
        if (panelObject.activeSelf != (pins.Count > 0))
        {
            panelObject.SetActive(pins.Count > 0);
        }

        if (pins.Count > 0)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }
    }

    internal static void RequestRefresh()
    {
        if (panelObject == null || refreshPending)
        {
            return;
        }

        refreshPending = true;
        Canvas.preWillRenderCanvases += BeforeLayout;
    }

    internal static void RequestPositionUpdate()
    {
        if (panelObject == null || layoutUpdateQueued || updatingPosition)
        {
            return;
        }

        _ = CanvasUpdateRegistry.instance;
        layoutUpdateQueued = true;
        Canvas.willRenderCanvases += AfterLayout;
    }

    internal static void Shutdown()
    {
        DestroyExisting();
        LaboratoryPinsLayoutListener.StopObserving();
    }

    private static void BeforeLayout()
    {
        Canvas.preWillRenderCanvases -= BeforeLayout;
        refreshPending = false;
        Refresh();
        RequestPositionUpdate();
    }

    private static void AfterLayout()
    {
        Canvas.willRenderCanvases -= AfterLayout;
        layoutUpdateQueued = false;
        UpdatePosition();
    }

    private static void UpdatePosition()
    {
        if (panelObject == null || updatingPosition)
        {
            return;
        }

        updatingPosition = true;
        try
        {
            LaboratoryPinsPositioner.Update(panelObject.GetComponent<RectTransform>());
        }
        finally
        {
            updatingPosition = false;
        }
    }

    private static void DestroyExisting()
    {
        HudPositionPreview.Shutdown();
        Canvas.preWillRenderCanvases -= BeforeLayout;
        Canvas.willRenderCanvases -= AfterLayout;
        layoutUpdateQueued = false;
        refreshPending = false;
        PinnedFormulaManager.PinsChanged -= Refresh;

        if (panelObject != null)
        {
            Object.Destroy(panelObject);
        }

        panelObject = null;
        contentRoot = null;
        renderedPins = null;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            parent.GetChild(i).gameObject.SetActive(false);
            Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    private static void AttachToHud(GameObject child, Transform parent)
    {
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
    }
}
