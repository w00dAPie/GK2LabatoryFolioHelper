using System.Collections.Generic;
using UnityEngine;

namespace GK2LaboratoryFolioHelper.UI;

internal sealed class LaboratoryPinsLayoutListener : MonoBehaviour
{
    private static readonly List<LaboratoryPinsLayoutListener> Listeners = new();

    internal static void Observe(RectTransform root)
    {
        if (root != null && root.GetComponent<LaboratoryPinsLayoutListener>() == null)
        {
            Listeners.Add(root.gameObject.AddComponent<LaboratoryPinsLayoutListener>());
        }
    }

    internal static void StopObserving()
    {
        foreach (LaboratoryPinsLayoutListener listener in Listeners.ToArray())
        {
            if (listener != null)
            {
                Destroy(listener);
            }
        }
        Listeners.Clear();
    }

    private void OnRectTransformDimensionsChange() => LaboratoryPinsHud.RequestPositionUpdate();

    private void OnEnable() => LaboratoryPinsHud.RequestPositionUpdate();

    private void OnCanvasGroupChanged() => LaboratoryPinsHud.RequestPositionUpdate();

    private void OnTransformParentChanged() => LaboratoryPinsHud.RequestPositionUpdate();

    private void OnDisable() => LaboratoryPinsHud.RequestPositionUpdate();

    private void OnDestroy()
    {
        Listeners.Remove(this);
        LaboratoryPinsHud.RequestPositionUpdate();
    }
}
