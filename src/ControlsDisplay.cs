using HarmonyLib;
using Restory.Gameplay.Elements;
using Restory.Gameplay.GameSettings.Observers;
using Restory.Gameplay.Workplace;
using Restory.UserInterface.CommonElements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReStoryBetterWorkbench;

[HarmonyPatch]
internal static class ControlsDisplay
{
    private const float MinKeyWidth = 20f;
    private const float KeyHeight = 20f;

    private const float KeyFaceSideInset = 4.5f;
    private const float KeyFaceTopInset = 1f;
    private const float KeyFaceBottomInset = 5.75f;

    private const float KeyFontScale = 0.9f;
    private const float KeyFontSizeTrim = 1f;

    private const float KeyFrameOpacityOnLeft = 0.5f;
    private const float KeyFrameOpacityOnRight = 0.4f;

    private static readonly Color KeyFrameTint = Color.white;
    private static readonly Color KeyLabelColor = new(1f, 1f, 0.9f, 1f);

    private static TMP_Text _organizeLabel;
    private static TMP_Text _organizeKeyLabel;
    private static TMP_Text _highlightsLabel;
    private static TMP_Text _highlightsKeyLabel;
    private static GameObject _display;
    private static Vector3 _displayHome;
    private static bool _isShown;
    private static WorkSurface _surface;
    private static float _keyFrameOpacity = KeyFrameOpacityOnLeft;

    internal static void AddRows(WorkSurface workSurface)
    {
        const string OrganizeRowName = "OrganizeControlsRow";
        const string HighlightsRowName = "HighlightsControlsRow";

        _surface = workSurface;

        GameObject display = AccessTools.Field(typeof(WorkSurface), "disassembleControlsAdviceSign")
            ?.GetValue(workSurface) as GameObject;

        Transform rows = display == null
            ? null
            : display.GetComponentInChildren<VerticalLayoutGroup>(true)?.transform;

        GameObject gameRowTemplate = rows == null ? null : LastControlsRow(rows);

        if (gameRowTemplate == null)
        {
            Log.Warning("Workbench controls display rows not found; skipping the hint rows.");
            return;
        }

        _display = display;
        _displayHome = display.transform.position;

        bool hasAlreadyAddedRowsToThisBench = rows.Find(OrganizeRowName) != null;

        if (hasAlreadyAddedRowsToThisBench)
            return;

        int organizeRowIndex = gameRowTemplate.transform.GetSiblingIndex() + 1;

        bool hasAddedOrganizeRow = TryCloneRow(gameRowTemplate, rows, OrganizeRowName, organizeRowIndex,
            out _organizeLabel, out _organizeKeyLabel);

        if (!hasAddedOrganizeRow)
            return;

        TryCloneRow(gameRowTemplate, rows, HighlightsRowName, organizeRowIndex + 1,
            out _highlightsLabel, out _highlightsKeyLabel);

        RefreshRows();
        Log.Debug("Controls display rows added.");
    }

    internal static void PlaceAwayFrom(BenchAnchorSide partsSide, Bounds bench)
    {
        if (_display == null)
            return;

        bool isDisplayOnLeft = _displayHome.x < bench.center.x;
        bool shouldDisplayBeOnLeft = partsSide == BenchAnchorSide.Right;

        ApplyKeyFrameOpacity(shouldDisplayBeOnLeft);

        if (isDisplayOnLeft == shouldDisplayBeOnLeft)
        {
            _display.transform.position = _displayHome;
            return;
        }

        float mirroredX = 2f * bench.center.x - _displayHome.x;
        float outwardOffset =
            Mathf.Sign(mirroredX - bench.center.x) * BetterWorkbenchPlugin.ControlsDisplayOffset.Value;

        _display.transform.position =
            new Vector3(mirroredX + outwardOffset, _displayHome.y, _displayHome.z);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorkSurface), nameof(WorkSurface.ActivateDisassembleControlsAdvices))]
    private static void PositionOnShow()
    {
        bool isHiddenToShownEdge = !_isShown;
        bool hasNothingToPosition = _display == null || _surface == null;

        _isShown = true;

        if (!isHiddenToShownEdge || hasNothingToPosition)
            return;

        Bounds bench = BenchPacker.WorldBounds(_surface.SurfaceBoundary);
        int partsOnLeft = 0;
        int partsOnRight = 0;

        foreach (ElementBase element in _surface.PlacedElements)
        {
            if (!BenchOrganizer.IsDraggablePart(element))
                continue;

            if (element.transform.position.x < bench.center.x)
            {
                partsOnLeft++;
            }
            else
            {
                partsOnRight++;
            }
        }

        if (partsOnLeft == partsOnRight)
        {
            _display.transform.position = _displayHome;
            ApplyKeyFrameOpacity(_displayHome.x < bench.center.x);
        }
        else
        {
            PlaceAwayFrom(partsOnLeft > partsOnRight ? BenchAnchorSide.Left : BenchAnchorSide.Right, bench);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorkSurface), nameof(WorkSurface.DeactivateDisassembleControlsAdvices))]
    private static void MarkHidden()
    {
        _isShown = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSettingsLanguageChangeObserver),
        nameof(GameSettingsLanguageChangeObserver.Initialize))]
    private static void HookLanguageChanges(GameSettingsLanguageChangeObserver __instance)
    {
        __instance.AddSubscriber(typeof(ControlsDisplay), HandleLanguageChanged);
        HandleLanguageChanged(__instance.Localization);
    }

    private static void HandleLanguageChanged(SystemLanguage language)
    {
        Strings.SetLanguage(language);
        RefreshRows();
    }

    private static void RefreshRows()
    {
        SetText(_organizeLabel, Strings.Organize);
        SetKeyText(_organizeKeyLabel, BetterWorkbenchPlugin.OrganizeKey.ToString());
        SetText(_highlightsLabel, Strings.Highlights);
        SetKeyText(_highlightsKeyLabel, BetterWorkbenchPlugin.HighlightsKey.ToString());
    }

    private static void SetKeyText(TMP_Text keyLabel, string keyName)
    {
        if (keyLabel == null)
            return;

        keyLabel.text = keyName;
        ResizeKeyToFit(keyLabel, keyName);
    }

    private static void ResizeKeyToFit(TMP_Text keyLabel, string keyName)
    {
        float keyWidth = Mathf.Max(MinKeyWidth,
            keyLabel.GetPreferredValues(keyName).x + KeyFaceSideInset * 2f);

        if (keyLabel.transform.parent is RectTransform slotRect)
        {
            slotRect.sizeDelta = new Vector2(keyWidth, KeyHeight);
        }

        LayoutElement slotSize = keyLabel.transform.parent.GetComponent<LayoutElement>();

        if (slotSize == null)
            return;

        slotSize.preferredWidth = keyWidth;
        slotSize.preferredHeight = KeyHeight;
    }

    private static void SetText(TMP_Text label, string text)
    {
        if (label == null)
            return;

        label.text = text;
    }

    private static GameObject LastControlsRow(Transform rows)
    {
        for (int index = rows.childCount - 1; index >= 0; index--)
        {
            Transform row = rows.GetChild(index);

            if (row.GetComponent<GUI_ControlsTypeGameObjectsSwitcher>() != null)
                return row.gameObject;
        }

        return null;
    }

    private static bool TryCloneRow(GameObject template, Transform rows, string rowName, int siblingIndex,
        out TMP_Text label, out TMP_Text keyLabel)
    {
        label = null;
        keyLabel = null;

        GameObject row = Object.Instantiate(template, rows);
        row.name = rowName;
        row.transform.SetSiblingIndex(siblingIndex);

        UiClones.DisableInheritedLocalization(row);

        TMP_Text rowLabel = row.GetComponentInChildren<TMP_Text>(true);
        Image icon = TakeOverKeyboardIcon(row);

        if (rowLabel == null || icon == null)
        {
            Log.Warning($"Cloned controls row {rowName} is missing its label or icon; leaving it as-is.");
            Object.Destroy(row);
            return false;
        }

        DressIconAsKey(icon);

        label = rowLabel;
        keyLabel = PutKeyNameInIconSlot(icon.transform, rowLabel);
        return true;
    }

    private static void ApplyKeyFrameOpacity(bool isDisplayOnLeft)
    {
        _keyFrameOpacity = isDisplayOnLeft ? KeyFrameOpacityOnLeft : KeyFrameOpacityOnRight;

        FadeKeyFrame(_organizeKeyLabel);
        FadeKeyFrame(_highlightsKeyLabel);
    }

    private static void FadeKeyFrame(TMP_Text keyLabel)
    {
        if (keyLabel == null)
            return;

        Image icon = keyLabel.transform.parent.GetComponent<Image>();

        if (icon == null)
            return;

        Color frameTint = icon.color;
        frameTint.a = _keyFrameOpacity;
        icon.color = frameTint;
    }

    private static void DressIconAsKey(Image icon)
    {
        Sprite frame = KeyFrameArt.Frame();

        if (frame == null)
        {
            icon.enabled = false;
            return;
        }

        Color frameTint = KeyFrameTint;
        frameTint.a = _keyFrameOpacity;

        icon.overrideSprite = null;
        icon.sprite = frame;
        icon.type = Image.Type.Sliced;
        icon.color = frameTint;
        icon.enabled = true;
    }

    private static Image TakeOverKeyboardIcon(GameObject row)
    {
        GUI_ControlsTypeGameObjectsSwitcher switcher =
            row.GetComponentInChildren<GUI_ControlsTypeGameObjectsSwitcher>(true);

        if (switcher == null)
        {
            Log.Warning("Controls row has no controls type switcher; falling back to the shown icon.");
            return ShownIcon(row);
        }

        GameObject keyboardIcon = ReadTarget(switcher, "targetKeyboardAndMouse");
        GameObject joystickIcon = ReadTarget(switcher, "targetJoystick");
        GameObject fallbackIcon = ReadTarget(switcher, "targetNone");

        Object.Destroy(switcher);

        if (keyboardIcon == null)
            return ShownIcon(row);

        HideUnlessKept(joystickIcon, keyboardIcon);
        HideUnlessKept(fallbackIcon, keyboardIcon);

        keyboardIcon.SetActive(true);
        return keyboardIcon.GetComponentInChildren<Image>(true);
    }

    private static GameObject ReadTarget(GUI_ControlsTypeGameObjectsSwitcher switcher, string fieldName)
    {
        return AccessTools.Field(typeof(GUI_ControlsTypeGameObjectsSwitcher), fieldName)
            ?.GetValue(switcher) as GameObject;
    }

    private static void HideUnlessKept(GameObject icon, GameObject keptIcon)
    {
        if (icon == null || icon == keptIcon)
            return;

        icon.SetActive(false);
    }

    private static Image ShownIcon(GameObject row)
    {
        foreach (Image icon in row.GetComponentsInChildren<Image>(true))
        {
            if (icon.gameObject.activeSelf)
                return icon;
        }

        return null;
    }

    private static TMP_Text PutKeyNameInIconSlot(Transform iconSlot, TMP_Text rowLabel)
    {
        GameObject keyLabelObject = Object.Instantiate(rowLabel.gameObject, iconSlot);
        keyLabelObject.name = "KeyLabel";

        RectTransform keyRect = keyLabelObject.GetComponent<RectTransform>();
        keyRect.anchorMin = Vector2.zero;
        keyRect.anchorMax = Vector2.one;
        keyRect.offsetMin = new Vector2(KeyFaceSideInset, KeyFaceBottomInset);
        keyRect.offsetMax = new Vector2(-KeyFaceSideInset, -KeyFaceTopInset);
        keyRect.localScale = Vector3.one;

        UiClones.DisableInheritedLocalization(keyLabelObject);

        TMP_Text keyLabel = keyLabelObject.GetComponent<TMP_Text>();
        keyLabel.alignment = TextAlignmentOptions.Center;

        keyLabel.enableAutoSizing = false;
        keyLabel.fontSize = rowLabel.fontSize * KeyFontScale - KeyFontSizeTrim;

        keyLabel.color = KeyLabelColor;
        keyLabel.fontStyle |= FontStyles.Bold;

        return keyLabel;
    }
}
