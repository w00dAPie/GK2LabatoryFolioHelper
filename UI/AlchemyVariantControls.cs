using System;
using System.Collections.Generic;
using System.Reflection;
using GK2LaboratoryFolioHelper.Alchemy;
using GK2LaboratoryFolioHelper.Infrastructure;
using GK2LaboratoryFolioHelper.UI.Typography;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GK2LaboratoryFolioHelper.UI;

internal sealed class AlchemyVariantControls
{
    private const float IngredientArrowGap = 10f;
    private const float CounterIngredientGap = 6f;

    private static readonly FieldInfo PlusButtonField = AccessTools.Field(
        typeof(UIAlchemyWindow),
        "plusBtn"
    );
    private static readonly FieldInfo IngredientsField = AccessTools.Field(
        typeof(UIAlchemyWindow),
        "ingredients"
    );

    private UIAlchemyWindow window;
    private GameObject controls;
    private RectTransform controlsRect;
    private RectTransform leftButtonRect;
    private RectTransform rightButtonRect;
    private RectTransform counterRect;
    private TextMeshProUGUI counter;

    internal GameObject Root => controls;

    internal bool Build(UIAlchemyWindow target, UnityAction selectPrevious, UnityAction selectNext)
    {
        Destroy();
        window = target;

        RectTransform windowRect = window?.transform as RectTransform;
        LazyButton fallbackTemplate = PlusButtonField?.GetValue(window) as LazyButton;
        LazyButton leftTemplate = FindQueueButton(useRightArrow: false) ?? fallbackTemplate;
        LazyButton rightTemplate = FindQueueButton(useRightArrow: true) ?? fallbackTemplate;

        if (windowRect == null || leftTemplate == null || rightTemplate == null)
        {
            ModLog.Warning(
                $"Could not create laboratory mix controls | window={windowRect != null} | queueTemplates={(leftTemplate != null && rightTemplate != null)}"
            );
            return false;
        }

        controls = new GameObject(
            "GK2AlchemyMixVariants",
            typeof(RectTransform),
            typeof(LayoutElement)
        );
        controls.transform.SetParent(windowRect, false);
        controlsRect = controls.GetComponent<RectTransform>();
        controlsRect.anchorMin = new Vector2(0.5f, 0.5f);
        controlsRect.anchorMax = new Vector2(0.5f, 0.5f);
        controlsRect.sizeDelta = Vector2.zero;
        controlsRect.pivot = new Vector2(0.5f, 0.5f);
        controlsRect.SetAsLastSibling();
        controls.GetComponent<LayoutElement>().ignoreLayout = true;

        leftButtonRect = CreateButtonClone(
            leftTemplate,
            "<",
            selectPrevious,
            leftTemplate == fallbackTemplate
        );
        counter = CreateLabel();
        counterRect = counter.rectTransform;
        rightButtonRect = CreateButtonClone(
            rightTemplate,
            ">",
            selectNext,
            rightTemplate == fallbackTemplate
        );
        Position();
        return true;
    }

    internal void UpdateCounter(
        int index,
        IReadOnlyList<PinnedFormulaManager.LaboratoryMixOption> options
    )
    {
        if (
            counter == null
            || options == null
            || options.Count == 0
            || index < 0
            || index >= options.Count
        )
        {
            return;
        }

        PinnedFormulaManager.LaboratoryMixOption option = options[index];
        counter.text =
            $"{index + 1}/{options.Count}\n{(option.IsCraftable ? "Ready" : $"Missing {option.MissingItemCount}")}";
    }

    internal void Position()
    {
        if (controlsRect == null || window == null || window.transform is not RectTransform)
        {
            return;
        }

        controlsRect.anchorMin = new Vector2(0.5f, 0.5f);
        controlsRect.anchorMax = new Vector2(0.5f, 0.5f);
        controlsRect.anchoredPosition = Vector2.zero;

        if (leftButtonRect == null || rightButtonRect == null || counterRect == null)
        {
            return;
        }

        List<UIAlchemyIngredient> ingredients =
            IngredientsField?.GetValue(window) as List<UIAlchemyIngredient>;
        if (ingredients == null || ingredients.Count == 0)
        {
            SetControlPositions(
                new Vector2(-48f, 126f),
                new Vector2(0f, 126f),
                new Vector2(48f, 126f)
            );
            return;
        }

        float left = float.MaxValue;
        float right = float.MinValue;
        float bottom = float.MaxValue;
        float top = float.MinValue;
        Vector3[] corners = new Vector3[4];

        foreach (UIAlchemyIngredient ingredient in ingredients)
        {
            RectTransform cellRect = ingredient?.cell?.transform as RectTransform;
            if (cellRect == null)
            {
                continue;
            }

            cellRect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 local = controlsRect.InverseTransformPoint(corner);
                left = Mathf.Min(left, local.x);
                right = Mathf.Max(right, local.x);
                bottom = Mathf.Min(bottom, local.y);
                top = Mathf.Max(top, local.y);
            }
        }

        if (left == float.MaxValue)
        {
            SetControlPositions(
                new Vector2(-48f, 126f),
                new Vector2(0f, 126f),
                new Vector2(48f, 126f)
            );
            return;
        }

        float centerY = (bottom + top) * 0.5f;
        float centerX = (left + right) * 0.5f;
        SetControlPositions(
            new Vector2(left - IngredientArrowGap - leftButtonRect.rect.width * 0.5f, centerY),
            new Vector2(centerX, top + CounterIngredientGap + counterRect.rect.height * 0.5f),
            new Vector2(right + IngredientArrowGap + rightButtonRect.rect.width * 0.5f, centerY)
        );
    }

    internal void Destroy()
    {
        if (controls != null)
        {
            UnityEngine.Object.Destroy(controls);
        }

        window = null;
        controls = null;
        controlsRect = null;
        leftButtonRect = null;
        rightButtonRect = null;
        counterRect = null;
        counter = null;
    }

    private RectTransform CreateButtonClone(
        LazyButton template,
        string label,
        UnityAction action,
        bool replaceText
    )
    {
        GameObject buttonObject = UnityEngine.Object.Instantiate(
            template.gameObject,
            controls.transform,
            false
        );
        buttonObject.name = $"VariantButton_{label}";
        buttonObject.SetActive(true);

        LazyButton button = buttonObject.GetComponent<LazyButton>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        button.interactable = true;

        if (replaceText)
        {
            foreach (TMP_Text text in buttonObject.GetComponentsInChildren<TMP_Text>(true))
            {
                text.text = label;
            }
        }

        LayoutElement size =
            buttonObject.GetComponent<LayoutElement>()
            ?? buttonObject.AddComponent<LayoutElement>();
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(28f, 28f);
        size.minWidth = 28f;
        size.preferredWidth = 28f;
        size.minHeight = 28f;
        size.preferredHeight = 28f;
        return rect;
    }

    private TextMeshProUGUI CreateLabel()
    {
        GameObject labelObject = new GameObject(
            "VariantCounter",
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement)
        );
        labelObject.transform.SetParent(controls.transform, false);

        LayoutElement size = labelObject.GetComponent<LayoutElement>();
        size.minWidth = 34f;
        size.preferredWidth = 34f;
        size.minHeight = 28f;
        size.preferredHeight = 28f;

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(34f, 28f);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 10f;
        text.color = Color.white;
        text.raycastTarget = false;
        ModTypography.ApplyStatus(text);
        return text;
    }

    private static LazyButton FindQueueButton(bool useRightArrow)
    {
        foreach (
            UICraftQueueElementWidget widget in Resources.FindObjectsOfTypeAll<UICraftQueueElementWidget>()
        )
        {
            LazyButton button = useRightArrow ? widget.QueueDownButton : widget.QueueUpButton;
            if (button != null && button.gameObject != null)
            {
                return button;
            }
        }

        return null;
    }

    private void SetControlPositions(
        Vector2 leftPosition,
        Vector2 counterPosition,
        Vector2 rightPosition
    )
    {
        leftButtonRect.anchoredPosition = leftPosition;
        counterRect.anchoredPosition = counterPosition;
        rightButtonRect.anchoredPosition = rightPosition;
    }
}
