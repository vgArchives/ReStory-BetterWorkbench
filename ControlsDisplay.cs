using BepInEx.Logging;
using HarmonyLib;
using Restory.Data.Elements;
using Restory.Gameplay.Elements;
using Restory.Gameplay.GameSettings.Observers;
using Restory.Gameplay.Workplace;
using Restory.UserInterface;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReStoryBetterWorkbench;

[HarmonyPatch]
internal static class ControlsDisplay
{
    private const float MinKeyLabelFontSize = 4f;

    private static TMP_Text _organizeLabel;
    private static TMP_Text _organizeKeyLabel;
    private static TMP_Text _highlightsLabel;
    private static TMP_Text _highlightsKeyLabel;
    private static GameObject _display;
    private static Vector3 _displayHome;
    private static bool _isShown;
    private static WorkSurface _surface;

    private static ManualLogSource Log => BetterWorkbenchPlugin.Log;

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

        if (rows == null || rows.childCount == 0)
        {
            Log.LogWarning("Workbench controls display rows not found; skipping the hint rows.");
            return;
        }

        _display = display;
        _displayHome = display.transform.position;

        bool hasAlreadyAddedRowsToThisBench = rows.Find(OrganizeRowName) != null;

        if (hasAlreadyAddedRowsToThisBench)
            return;

        GameObject gameRowTemplate = rows.GetChild(rows.childCount - 1).gameObject;

        if (!TryCloneRow(gameRowTemplate, rows, OrganizeRowName,
                out _organizeLabel, out _organizeKeyLabel))
            return;

        TryCloneRow(gameRowTemplate, rows, HighlightsRowName,
            out _highlightsLabel, out _highlightsKeyLabel);

        RefreshRows();
        BetterWorkbenchPlugin.LogDebug("Controls display rows added.");
    }

    internal static void PlaceAwayFrom(BenchAnchorSide partsSide, Bounds bench)
    {
        if (_display == null)
            return;

        bool isDisplayOnLeft = _displayHome.x < bench.center.x;
        bool shouldDisplayBeOnLeft = partsSide == BenchAnchorSide.Right;

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

    internal static void DisableInheritedLocalization(GameObject clone)
    {
        foreach (GUI_LocalisedText localizedText in clone.GetComponentsInChildren<GUI_LocalisedText>(true))
        {
            localizedText.IsEnabled = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorkSurface), nameof(WorkSurface.ToggleDisassembleControlsAdvices))]
    private static void PositionOnShow(bool isActive)
    {
        bool wasShown = _isShown;
        _isShown = isActive;

        bool isHiddenToShownEdge = isActive && !wasShown;

        if (!isHiddenToShownEdge || _display == null || _surface == null)
            return;

        Bounds bench = BenchPacker.WorldBounds(_surface.SurfaceBoundary);
        int partsOnLeft = 0;
        int partsOnRight = 0;

        foreach (ElementBase element in _surface.PlacedElements)
        {
            if (!element || element.Info == null || element.Info.Category != ElementCategory.Draggable)
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
        }
        else
        {
            PlaceAwayFrom(partsOnLeft > partsOnRight ? BenchAnchorSide.Left : BenchAnchorSide.Right, bench);
        }
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
        if (_organizeLabel != null)
        {
            _organizeLabel.text = $"- {Strings.Organize}";
        }

        if (_organizeKeyLabel != null)
        {
            _organizeKeyLabel.text = BetterWorkbenchPlugin.OrganizeKey.ToString();
        }

        if (_highlightsLabel != null)
        {
            _highlightsLabel.text = $"- {Strings.Highlights}";
        }

        if (_highlightsKeyLabel != null)
        {
            _highlightsKeyLabel.text = BetterWorkbenchPlugin.HighlightsKey.ToString();
        }
    }

    private static bool TryCloneRow(GameObject template, Transform rows, string rowName,
        out TMP_Text label, out TMP_Text keyLabel)
    {
        label = null;
        keyLabel = null;

        GameObject row = Object.Instantiate(template, rows);
        row.name = rowName;
        row.transform.SetAsLastSibling();

        DisableInheritedLocalization(row);

        TMP_Text rowLabel = row.GetComponentInChildren<TMP_Text>(true);
        Image icon = row.GetComponentInChildren<Image>(true);

        if (rowLabel == null || icon == null)
        {
            Log.LogWarning($"Cloned controls row {rowName} is missing its label or icon; leaving it as-is.");
            Object.Destroy(row);
            return false;
        }

        icon.enabled = false;

        label = rowLabel;
        keyLabel = PutKeyNameInIconSlot(icon.transform, rowLabel);
        return true;
    }

    private static TMP_Text PutKeyNameInIconSlot(Transform iconSlot, TMP_Text rowLabel)
    {
        GameObject keyLabelObject = Object.Instantiate(rowLabel.gameObject, iconSlot);
        keyLabelObject.name = "KeyLabel";

        RectTransform keyRect = keyLabelObject.GetComponent<RectTransform>();
        keyRect.anchorMin = Vector2.zero;
        keyRect.anchorMax = Vector2.one;
        keyRect.offsetMin = Vector2.zero;
        keyRect.offsetMax = Vector2.zero;
        keyRect.localScale = Vector3.one;

        DisableInheritedLocalization(keyLabelObject);

        TMP_Text keyLabel = keyLabelObject.GetComponent<TMP_Text>();
        keyLabel.alignment = TextAlignmentOptions.Center;
        keyLabel.enableAutoSizing = true;
        keyLabel.fontSizeMin = MinKeyLabelFontSize;

        return keyLabel;
    }
}
