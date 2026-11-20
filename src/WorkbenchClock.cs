using HarmonyLib;
using Restory.Gameplay.Competitions;
using Restory.Gameplay.InteractiveObjects;
using Restory.Gameplay.TimeSystems;
using Restory.Gameplay.Workplace;
using Restory.TimeSystems;
using Restory.UserInterface.GameplayOverlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReStoryBetterWorkbench;

[HarmonyPatch]
internal static class WorkbenchClock
{
    private const string PanelSpriteName = "ActionDescriptionBG (Sliced)";
    private const string IconSpriteName = "watch";

    private const float PanelScale = 0.75f;
    private const float IconSize = 44f;

    private static readonly Vector2 IconOffsetInsideSlot = new(6f, -2f);
    private static readonly Vector2 DigitsPadding = new(6f, 2f);
    private static readonly Color FallbackPanelColor = new(0.16f, 0.12f, 0.10f);

    private static GameObject _panel;
    private static Image _panelImage;
    private static Color _panelBaseColor;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorkSurface), nameof(WorkSurface.ActivateDisassembleControlsAdvices))]
    private static void ShowWithBenchAdvices()
    {
        ToggleWithBenchAdvices(true);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorkSurface), nameof(WorkSurface.DeactivateDisassembleControlsAdvices))]
    private static void HideWithBenchAdvices()
    {
        ToggleWithBenchAdvices(false);
    }

    private static void ToggleWithBenchAdvices(bool isActive)
    {
        if (isActive && IsCompetitionRunning())
        {
            isActive = false;
        }

        if (isActive && _panel == null)
        {
            Build();
        }

        if (isActive)
        {
            ApplyBackgroundOpacity();
        }

        if (_panel != null && _panel.activeSelf != isActive)
        {
            _panel.SetActive(isActive);
        }
    }

    private static void Build()
    {
        GUI_GameplayOverlayCanvas overlay = Object.FindObjectOfType<GUI_GameplayOverlayCanvas>();
        GameCalendar calendar = Object.FindObjectOfType<GameCalendar>();
        ClockDisplay clockSource = FindClockSource();

        bool isMissingSomethingToCloneFrom = overlay == null || calendar == null || clockSource == null;

        if (isMissingSomethingToCloneFrom)
        {
            Log.Warning("Workbench clock: overlay canvas, GameCalendar or clock widget not found; skipping.");
            return;
        }

        _panel = new GameObject("WorkbenchClock", typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));

        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        panelRect.SetParent(overlay.transform, false);
        panelRect.SetAsLastSibling();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.localScale = Vector3.one * PanelScale;

        Image panelImage = _panel.GetComponent<Image>();

        Sprite panelSprite = FindSprite(PanelSpriteName);
        Sprite roundedPanel = PanelArt.RoundedPanel();

        if (roundedPanel != null)
        {
            panelImage.sprite = roundedPanel;
            panelImage.type = Image.Type.Sliced;
        }

        _panelImage = panelImage;
        _panelBaseColor = ColorTheGameUsesFor(panelSprite, FallbackPanelColor);
        panelImage.raycastTarget = false;

        HorizontalLayoutGroup layout = _panel.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 8, 8);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = _panel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddIcon();
        AddGameClockClone(clockSource, calendar);

        Log.Debug("Workbench clock built.");
    }

    private static void ApplyBackgroundOpacity()
    {
        if (_panelImage == null)
            return;

        Color panelColor = _panelBaseColor;
        panelColor.a = BetterWorkbenchPlugin.ClockBackgroundOpacity.Value;
        _panelImage.color = panelColor;
    }

    private static bool IsCompetitionRunning()
    {
        CompetitionGameMode competition = Object.FindObjectOfType<CompetitionGameMode>();
        return competition != null && competition.HasDeviceInCompetition;
    }

    private static ClockDisplay FindClockSource()
    {
        ClockDisplay source = null;

        foreach (ClockDisplay clock in Resources.FindObjectsOfTypeAll<ClockDisplay>())
        {
            bool hasNightColorSwitcher = clock.GetComponent<ClockDisplayColorSwitcher>() != null;

            if (source == null || hasNightColorSwitcher)
            {
                source = clock;
            }
        }

        return source;
    }

    private static void AddIcon()
    {
        Sprite iconSprite = FindSprite(IconSpriteName);

        if (iconSprite == null)
        {
            Log.Warning("Workbench clock: watch icon sprite not loaded; clock shows without it.");
            return;
        }

        var iconSlot = new GameObject("WatchIcon", typeof(RectTransform));
        iconSlot.transform.SetParent(_panel.transform, false);
        iconSlot.GetComponent<RectTransform>().sizeDelta = new Vector2(IconSize, IconSize);

        var iconObject = new GameObject("Image", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(iconSlot.transform, false);
        iconRect.sizeDelta = new Vector2(IconSize, IconSize);
        iconRect.anchoredPosition = IconOffsetInsideSlot;

        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = iconSprite;
        icon.color = ColorTheGameUsesFor(iconSprite, Color.white);
        icon.raycastTarget = false;
    }

    private static void AddGameClockClone(ClockDisplay source, GameCalendar calendar)
    {
        MainDayTimeSwitchingService dayTimes = Object.FindObjectOfType<MainDayTimeSwitchingService>();

        GameObject clone = Object.Instantiate(source.gameObject, _panel.transform);
        clone.name = "GameClockClone";

        RectTransform rect = clone.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);

        RebindServicesWhileDisabled(clone, calendar, dayTimes);

        TMP_Text digits = clone.GetComponentInChildren<TMP_Text>();

        if (digits != null)
        {
            digits.ForceMeshUpdate();
            rect.sizeDelta = digits.GetPreferredValues() + DigitsPadding;
        }
    }

    private static void RebindServicesWhileDisabled(GameObject clone, GameCalendar calendar,
        MainDayTimeSwitchingService dayTimes)
    {
        clone.SetActive(false);

        AccessTools.Field(typeof(ClockDisplay), "gameCalendar")
            .SetValue(clone.GetComponent<ClockDisplay>(), calendar);

        ClockDisplayColorSwitcher switcher = clone.GetComponent<ClockDisplayColorSwitcher>();

        if (switcher != null)
        {
            AccessTools.Field(typeof(ClockDisplayColorSwitcher), "mainDayTimeSwitchingService")
                .SetValue(switcher, dayTimes);
        }

        clone.SetActive(true);
    }

    private static Sprite FindSprite(string spriteName)
    {
        foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (sprite.name == spriteName)
                return sprite;
        }

        return null;
    }

    private static Color ColorTheGameUsesFor(Sprite sprite, Color fallback)
    {
        if (sprite == null)
            return fallback;

        foreach (Image image in Resources.FindObjectsOfTypeAll<Image>())
        {
            bool isSameSprite = image.sprite == sprite;
            bool isOutsideOurPanel = _panel == null || !image.transform.IsChildOf(_panel.transform);

            if (isSameSprite && isOutsideOurPanel)
                return image.color;
        }

        return fallback;
    }
}
