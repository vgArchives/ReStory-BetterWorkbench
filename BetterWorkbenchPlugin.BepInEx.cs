#if !MELONLOADER
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ReStoryBetterWorkbench;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public partial class BetterWorkbenchPlugin : BaseUnityPlugin
{
    private const string GeneralSection = "General";
    private const string HotkeysSection = "Hotkeys";
    private const string LayoutSection = "Layout";
    private const string SectionSeparator = "# ----------------------------------------------------------------";

    internal static ConfigEntry<BenchAnchorSide> Anchor;
    internal static ConfigEntry<float> CellGap;
    internal static ConfigEntry<float> SafetyMargin;
    internal static ConfigEntry<float> ShelfSlack;
    internal static ConfigEntry<float> ControlsDisplayOffset;
    internal static ConfigEntry<bool> UpdateCheckEnabled;

    private static ConfigEntry<float>[] _topMarginsPerAnchor;
    private static ConfigEntry<float>[] _sideMarginsPerAnchor;

    private void Awake()
    {
        Log.Source = Logger;

        BindConfig();
        AddSectionSeparators();
        new Harmony(PluginGuid).PatchAll();

        RunSelfChecks();

        if (UpdateCheckEnabled.Value)
        {
            StartCoroutine(UpdateCheck.Run(PluginVersion));
        }
    }

    private void Update() => HandleHotkeys();

    private void BindConfig()
    {
        UpdateCheckEnabled = Config.Bind(GeneralSection, UpdateCheckName, DefaultUpdateCheck,
            UpdateCheckPurpose);

        OrganizeKey = BindHotkey(OrganizeKeyName, DefaultOrganizeKey, OrganizeKeyPurpose);
        HighlightsKey = BindHotkey(HighlightsKeyName, DefaultHighlightsKey, HighlightsKeyPurpose);

        Anchor = Config.Bind(LayoutSection, AnchorName, DefaultAnchor, AnchorPurpose);
        CellGap = Config.Bind(LayoutSection, CellGapName, DefaultCellGap, new ConfigDescription(
            CellGapPurpose, new AcceptableValueRange<float>(0f, MaxCellGap)));
        SafetyMargin = Config.Bind(LayoutSection, SafetyMarginName, DefaultSafetyMargin, new ConfigDescription(
            SafetyMarginPurpose, new AcceptableValueRange<float>(0f, MaxSafetyMargin)));
        ShelfSlack = Config.Bind(LayoutSection, ShelfSlackName, DefaultShelfSlack, new ConfigDescription(
            ShelfSlackPurpose, new AcceptableValueRange<float>(MinShelfSlack, MaxShelfSlack)));
        ControlsDisplayOffset = Config.Bind(LayoutSection, ControlsDisplayOffsetName,
            DefaultControlsDisplayOffset, new ConfigDescription(
                ControlsDisplayOffsetPurpose, new AcceptableValueRange<float>(0f, MaxControlsDisplayOffset)));

        _topMarginsPerAnchor = BindMarginPerAnchor(TopMarginName, DefaultTopMargins, TopMarginPurpose);
        _sideMarginsPerAnchor = BindMarginPerAnchor(SideMarginName, DefaultSideMargins, SideMarginPurpose);
    }

    private void AddSectionSeparators()
    {
        List<string> lines = new List<string>(File.ReadAllLines(Config.ConfigFilePath));
        bool hasChanged = false;

        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            bool isUnseparatedSectionHeader = lines[lineIndex].StartsWith("[")
                                              && (lineIndex == 0 || lines[lineIndex - 1] != SectionSeparator);

            if (!isUnseparatedSectionHeader)
                continue;

            lines.Insert(lineIndex, SectionSeparator);
            lineIndex++;
            hasChanged = true;
        }

        if (hasChanged)
        {
            File.WriteAllLines(Config.ConfigFilePath, lines);
        }
    }

    private KeyCode BindHotkey(string settingName, KeyCode defaultKey, string purpose)
    {
        ConfigEntry<string> entry = Config.Bind(HotkeysSection, settingName, defaultKey.ToString(),
            HotkeyDescription(purpose));

        return ParseHotkey(settingName, entry.Value, defaultKey);
    }

    private ConfigEntry<float>[] BindMarginPerAnchor(string settingName, float[] defaults, string purpose)
    {
        ConfigEntry<float>[] entries = new ConfigEntry<float>[defaults.Length];

        for (int anchorIndex = 0; anchorIndex < defaults.Length; anchorIndex++)
        {
            BenchAnchorSide anchor = (BenchAnchorSide)anchorIndex;

            entries[anchorIndex] = Config.Bind(LayoutSection, $"{settingName}{anchor}", defaults[anchorIndex],
                new ConfigDescription(MarginDescription(purpose, anchor),
                    new AcceptableValueRange<float>(0f, MaxMargin)));
        }

        return entries;
    }
}
#endif
