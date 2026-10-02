using UnityEngine;

namespace GK2LaboratoryFolioHelper.Helpers;

internal static class LaboratoryPinsPositioner
{
    private const float ExternalSpacingPixels = 12f;
    internal static readonly Vector2 DefaultPosition = new(-16f, -85f);

    internal static void Update(RectTransform panelRect)
    {
        if (panelRect == null || panelRect.parent is not RectTransform parentRect)
        {
            return;
        }

        Vector2 targetPosition = DefaultPosition;
        Camera camera = ExternalHudCompatibility.GetCanvasCamera(parentRect);
        Vector3 defaultLocalPoint = new(
            parentRect.rect.xMax + DefaultPosition.x,
            parentRect.rect.yMax + DefaultPosition.y,
            panelRect.localPosition.z
        );

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            camera,
            parentRect.TransformPoint(defaultLocalPoint)
        );
        Vector2 leftPoint = RectTransformUtility.WorldToScreenPoint(
            camera,
            parentRect.TransformPoint(
                defaultLocalPoint
                    - new Vector3(panelRect.rect.width * panelRect.localScale.x, 0f, 0f)
            )
        );
        float screenWidth = Mathf.Abs(screenPoint.x - leftPoint.x);

        if (ExternalHudCompatibility.TryGetRecipePinScreenRect(out Rect recipeBounds))
        {
            screenPoint.x = recipeBounds.xMax;
        }

        if (
            !ExternalHudCompatibility.TryGetExternalBottomScreenY(
                screenPoint.x - screenWidth,
                screenPoint.x,
                out float bottomScreenY
            )
        )
        {
            Apply(panelRect, targetPosition);
            return;
        }

        screenPoint.y = bottomScreenY - ExternalSpacingPixels;
        if (
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPoint,
                camera,
                out Vector2 localPoint
            )
        )
        {
            targetPosition = new Vector2(
                localPoint.x - parentRect.rect.xMax,
                localPoint.y - parentRect.rect.yMax
            );
        }

        Apply(panelRect, targetPosition);
    }

    private static void Apply(RectTransform panelRect, Vector2 targetPosition)
    {
        if (panelRect.anchoredPosition != targetPosition)
        {
            panelRect.anchoredPosition = targetPosition;
        }
    }
}
