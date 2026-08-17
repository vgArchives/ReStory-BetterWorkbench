using BepInEx.Logging;
using FMODUnity;
using HarmonyLib;
using Restory.Audio;
using Restory.UI.Presenters.Notepad;
using Restory.UI.Views.Notepad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReStoryBetterWorkbench;

[HarmonyPatch]
internal static class NotepadSectionCollapse
{
    private const string ArrowName = "CollapseArrow";
    private const float ArrowFontScale = 0.85f;
    private const float ArrowRightMarginInEm = 0.4f;
    private static readonly char[] ExpandedGlyphs = { '▼', '▾', 'v' };
    private static readonly char[] CollapsedGlyphs = { '▶', '►', '▸', '>' };

    private static readonly CollapsibleSection InstalledPartsSection = new CollapsibleSection();
    private static readonly CollapsibleSection SurfacePartsSection = new CollapsibleSection();

    private static ScrollRect _scroll;
    private static IAudioPlayerService _audioPlayer;
    private static EventReference _clickSound;

    private static ManualLogSource Log => BetterWorkbenchPlugin.Log;

    private static bool Prepare()
    {
        bool hasEveryField = AccessTools.Field(typeof(GUI_NotepadElementsPanelView), "installedElementsContainer") != null
                             && AccessTools.Field(typeof(GUI_NotepadElementsPanelView), "onSurfaceElementsContainer") != null
                             && AccessTools.Field(typeof(GUI_NotepadElementsPanelView), "installedElementsCount") != null
                             && AccessTools.Field(typeof(GUI_NotepadElementsPanelView), "onSurfaceElementsCount") != null
                             && AccessTools.Method(typeof(GUI_NotepadElementsPanelView),
                                 nameof(GUI_NotepadElementsPanelView.SetElements)) != null;

        if (hasEveryField)
            return true;

        Log.LogWarning("Notepad section collapsing is disabled: GUI_NotepadElementsPanelView is missing a "
                       + "container or count field, most likely renamed by a game update. "
                       + "Bench organizing and parts sorting are unaffected.");

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GUI_NotepadElementsPanelView), nameof(GUI_NotepadElementsPanelView.SetElements))]
    private static void BindAndApply(GUI_NotepadElementsPanelView __instance)
    {
        bool isPlayerFacingNotepad = __instance.GetComponentInParent<GUI_NotepadWindowView>(true) != null;

        if (!isPlayerFacingNotepad)
            return;

        _scroll = __instance.GetComponentInChildren<ScrollRect>(true);

        CaptureClickSound(__instance);

        if (_scroll == null)
        {
            _scroll = __instance.GetComponentInParent<ScrollRect>(true);
        }

        Bind(InstalledPartsSection, ReadField<RectTransform>(__instance, "installedElementsContainer"),
            ReadField<TMP_Text>(__instance, "installedElementsCount"));
        Bind(SurfacePartsSection, ReadField<RectTransform>(__instance, "onSurfaceElementsContainer"),
            ReadField<TMP_Text>(__instance, "onSurfaceElementsCount"));
    }

    private static void Bind(CollapsibleSection section, RectTransform container, TMP_Text countLabel)
    {
        if (container == null || countLabel == null)
            return;

        section.ItemsContainer = container.gameObject;

        Transform header = countLabel.transform.parent;

        if (header != null && header.GetComponent<Button>() == null)
        {
            section.Arrow = AddArrow(header, countLabel);
            MakeClickable(header, section);
        }

        section.Apply();
    }

    private static TMP_Text AddArrow(Transform header, TMP_Text countLabel)
    {
        GameObject arrowObject = Object.Instantiate(countLabel.gameObject, header);
        arrowObject.name = ArrowName;
        arrowObject.transform.SetAsFirstSibling();

        ControlsDisplay.DisableInheritedLocalization(arrowObject);

        TMP_Text arrow = arrowObject.GetComponent<TMP_Text>();
        arrow.raycastTarget = false;
        arrow.enableAutoSizing = false;
        arrow.fontSize *= ArrowFontScale;
        arrow.margin = new Vector4(0f, 0f, arrow.fontSize * ArrowRightMarginInEm, 0f);

        return arrow;
    }

    private static void MakeClickable(Transform header, CollapsibleSection section)
    {
        bool hasNothingToClickOn = header.GetComponent<Graphic>() == null;

        if (hasNothingToClickOn)
        {
            Image raycastCatcher = header.gameObject.AddComponent<Image>();
            raycastCatcher.color = Color.clear;
        }

        Button button = header.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(section.Toggle);
    }

    private static string PickSupportedGlyph(TMP_Text label, char[] glyphsByPreference)
    {
        foreach (char glyph in glyphsByPreference)
        {
            if (label.font == null
                || label.font.HasCharacter(glyph, searchFallbacks: true, tryAddCharacter: true))
                return glyph.ToString();
        }

        return glyphsByPreference[glyphsByPreference.Length - 1].ToString();
    }

    private static void CaptureClickSound(GUI_NotepadElementsPanelView view)
    {
        if (_audioPlayer != null && !_clickSound.IsNull)
            return;

        GUI_NotepadElementsPanelSFX panelSfx = view.GetComponentInParent<GUI_NotepadElementsPanelSFX>(true);

        _audioPlayer = ReadField<IAudioPlayerService>(panelSfx, "audioPlayer");
        _clickSound = ReadField<EventReference>(panelSfx, "itemSelectedSound");

        if (_audioPlayer == null || _clickSound.IsNull)
        {
            BetterWorkbenchPlugin.LogDebug("Notepad collapse click sound is unavailable.");
        }
    }

    private static void PlayClickSound()
    {
        if (_audioPlayer == null || _clickSound.IsNull)
            return;

        _audioPlayer.PlaySoundEventOneShot(_clickSound);
    }

    private static T ReadField<T>(object target, string fieldName)
    {
        object fieldValue = target == null ? null : AccessTools.Field(target.GetType(), fieldName)?.GetValue(target);

        return fieldValue is T typed ? typed : default;
    }

    private sealed class CollapsibleSection
    {
        private bool _isCollapsed;

        internal GameObject ItemsContainer { get; set; }

        internal TMP_Text Arrow { get; set; }

        internal void Apply()
        {
            if (ItemsContainer != null)
            {
                ItemsContainer.SetActive(!_isCollapsed);
            }

            if (Arrow != null)
            {
                Arrow.text = PickSupportedGlyph(Arrow, _isCollapsed ? CollapsedGlyphs : ExpandedGlyphs);
            }
        }

        internal void Toggle()
        {
            _isCollapsed = !_isCollapsed;
            Apply();
            PlayClickSound();

            if (_scroll != null)
            {
                Canvas.ForceUpdateCanvases();
                _scroll.verticalNormalizedPosition = 1f;
            }

            BetterWorkbenchPlugin.LogDebug($"Notepad section {(_isCollapsed ? "collapsed" : "expanded")}.");
        }
    }
}
