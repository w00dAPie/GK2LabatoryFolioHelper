using UnityEngine;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class HudScreenGeometry
{
    private static readonly Vector3[] Corners = new Vector3[4];

    internal static Camera GetCanvasCamera(RectTransform rect)
    {
        Canvas canvas = rect?.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return null;
        }

        canvas = canvas.rootCanvas;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera != null ? canvas.worldCamera
            : canvas.renderMode == RenderMode.WorldSpace ? Camera.main
            : null;
    }

    internal static bool TryGetScreenRect(RectTransform root, out Rect screenRect)
    {
        screenRect = default;
        if (!IsVisible(root) || root.rect.width <= 0f || root.rect.height <= 0f)
        {
            return false;
        }

        Camera camera = GetCanvasCamera(root);
        root.GetWorldCorners(Corners);
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);

        foreach (Vector3 corner in Corners)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
            if (
                float.IsNaN(point.x)
                || float.IsInfinity(point.x)
                || float.IsNaN(point.y)
                || float.IsInfinity(point.y)
            )
            {
                return false;
            }

            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return screenRect.width > 0f && screenRect.height > 0f;
    }

    internal static void IncludeBottom(RectTransform root, ref float bottomScreenY, ref bool found)
    {
        if (!TryGetScreenRect(root, out Rect screenRect))
        {
            return;
        }

        bottomScreenY = Mathf.Min(bottomScreenY, screenRect.yMin);
        found = true;
    }

    internal static void IncludeOverlappingBottom(
        RectTransform root,
        float left,
        float right,
        ref float bottom,
        ref bool found
    )
    {
        if (TryGetScreenRect(root, out Rect bounds) && bounds.xMax > left && bounds.xMin < right)
        {
            bottom = Mathf.Min(bottom, bounds.yMin);
            found = true;
        }
    }

    internal static bool IsVisible(RectTransform root)
    {
        if (root == null || !root.gameObject.activeInHierarchy)
        {
            return false;
        }

        Canvas canvas = root.GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.isActiveAndEnabled || !canvas.rootCanvas.isActiveAndEnabled)
        {
            return false;
        }

        for (Transform current = root; current != null; current = current.parent)
        {
            bool ignoreParents = false;
            foreach (CanvasGroup group in current.GetComponents<CanvasGroup>())
            {
                if (!group.enabled)
                {
                    continue;
                }

                if (group.alpha <= 0f)
                {
                    return false;
                }

                ignoreParents |= group.ignoreParentGroups;
            }

            if (ignoreParents)
            {
                break;
            }
        }

        return true;
    }
}
