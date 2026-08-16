using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RestoryBenchOrganizer;

[BepInPlugin(PluginGuid, "ReStory Bench Organizer", "1.0.0")]
public class BenchOrganizerPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.archives.restorybenchorganizer";
    private const string SectionSeparator = "# ----------------------------------------------------------------";

    internal static KeyCode OrganizeKey;
    internal static KeyCode HighlightsKey;
    internal static ConfigEntry<BenchAnchorSide> Anchor;
    internal static ConfigEntry<float> CellGap;
    internal static ConfigEntry<float> SafetyMargin;
    internal static ConfigEntry<float> ShelfSlack;
    internal static ConfigEntry<float> ControlsDisplayOffset;
    internal static ManualLogSource Log;

    private static ConfigEntry<float>[] _topMarginsPerAnchor;
    private static ConfigEntry<float>[] _sideMarginsPerAnchor;

    private void Awake()
    {
        Log = Logger;

        BindConfig();
        AddSectionSeparators();
        new Harmony(PluginGuid).PatchAll();

        Log.LogInfo(BenchPacker.SelfCheck()
            ? "Loaded. Self-check passed)."
            : "Loaded. SELF-CHECK FAILED.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(OrganizeKey))
        {
            bool isPackingAgainstOppositeSide =
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            BenchOrganizer.Organize(isPackingAgainstOppositeSide ? Opposite(Anchor.Value) : Anchor.Value);
        }

        if (Input.GetKeyDown(HighlightsKey))
        {
            SurfaceHighlighter.Toggle();
        }
    }

    /// Chatty per-action logging. Compiled away entirely in Release builds --
    /// call and arguments both vanish, so string interpolation never runs.
    /// Warnings and errors are never routed through here; players need those.
    [System.Diagnostics.Conditional("DEBUG")]
    internal static void LogDebug(string message) => Log.LogInfo(message);

    internal static float TopMarginFor(BenchAnchorSide anchor) => _topMarginsPerAnchor[(int)anchor].Value;

    internal static float SideMarginFor(BenchAnchorSide anchor) => _sideMarginsPerAnchor[(int)anchor].Value;

    private static BenchAnchorSide Opposite(BenchAnchorSide anchor) =>
        anchor == BenchAnchorSide.Left ? BenchAnchorSide.Right : BenchAnchorSide.Left;

    private void BindConfig()
    {
        OrganizeKey = BindHotkey("OrganizeKey", KeyCode.F,
            "Packs the loose parts on the bench. Hold Shift to pack against the opposite side.");
        HighlightsKey = BindHotkey("HighlightsKey", KeyCode.G,
            "Toggles the outline highlight on every part lying on the bench.");
        Anchor = Config.Bind("Layout", "PackAgainstSide", BenchAnchorSide.Left,
            "Bench side the parts are packed against.");
        CellGap = Config.Bind("Layout", "CellGap", 0.025f, new ConfigDescription(
            "Gap between packed parts, in meters.",
            new AcceptableValueRange<float>(0f, 0.1f)));
        SafetyMargin = Config.Bind("Layout", "SafetyMargin", 0.005f, new ConfigDescription(
            "Extra collision padding safety around each part spot, in meters.",
            new AcceptableValueRange<float>(0f, 0.05f)));
        ShelfSlack = Config.Bind("Layout", "ShelfSlack", 1f, new ConfigDescription(
            "How loosely the packed block spreads over the bench.",
            new AcceptableValueRange<float>(1f, 2.5f)));
        ControlsDisplayOffset = Config.Bind("Layout", "ControlsDisplayOffset", 0.07f, new ConfigDescription(
            "How far the controls UI display position is adjusted when switching sides.",
            new AcceptableValueRange<float>(0f, 0.4f)));

        _topMarginsPerAnchor = BindMarginPerAnchor("TopMargin", new[] { 0.05f, 0.10f },
            "Margin kept clear along the top edge of the bench.");
        _sideMarginsPerAnchor = BindMarginPerAnchor("SideMargin", new[] { 0.05f, 0.00f },
            "Margin kept clear along both the left and right edges of the bench.");
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
        ConfigEntry<string> entry = Config.Bind("Hotkeys", settingName, defaultKey.ToString(),
            $"{purpose} Takes any Unity KeyCode name, e.g. F, G, R, Tab, F6, Keypad5.");

        if (Enum.TryParse(entry.Value, true, out KeyCode key))
            return key;

        Log.LogWarning($"{settingName} \"{entry.Value}\" is not a key name; falling back to {defaultKey}.");
        return defaultKey;
    }

    private ConfigEntry<float>[] BindMarginPerAnchor(string settingName, float[] defaults, string purpose)
    {
        ConfigEntry<float>[] entries = new ConfigEntry<float>[defaults.Length];

        for (int anchorIndex = 0; anchorIndex < defaults.Length; anchorIndex++)
        {
            BenchAnchorSide anchor = (BenchAnchorSide)anchorIndex;

            entries[anchorIndex] = Config.Bind("Layout", $"{settingName}{anchor}", defaults[anchorIndex],
                new ConfigDescription($"{purpose} Applies when packing against the {anchor} side.",
                    new AcceptableValueRange<float>(0f, 0.4f)));
        }

        return entries;
    }
}
