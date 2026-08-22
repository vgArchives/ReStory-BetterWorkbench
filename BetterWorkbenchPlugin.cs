using System;
using UnityEngine;

namespace ReStoryBetterWorkbench;

public partial class BetterWorkbenchPlugin
{
    public const string PluginGuid = "com.archives.restorybetterworkbench";
    internal const string PluginName = "ReStory Better Workbench";
    internal const string PluginVersion = "1.3.0";
    internal const string PluginAuthor = "Archives";

    internal const string OrganizeKeyName = "OrganizeKey";
    internal const string HighlightsKeyName = "HighlightsKey";
    internal const string AnchorName = "PackAgainstSide";
    internal const string CellGapName = "CellGap";
    internal const string SafetyMarginName = "SafetyMargin";
    internal const string ShelfSlackName = "ShelfSlack";
    internal const string ControlsDisplayOffsetName = "ControlsDisplayOffset";
    internal const string TopMarginName = "TopMargin";
    internal const string SideMarginName = "SideMargin";
    internal const string UpdateCheckName = "UpdateCheck";

    internal const string OrganizeKeyPurpose =
        "Packs the loose parts on the bench. Hold Shift to pack against the opposite side.";
    internal const string HighlightsKeyPurpose =
        "Toggles the outline highlight on every part lying on the bench.";
    internal const string AnchorPurpose = "Bench side the parts are packed against.";
    internal const string CellGapPurpose = "Gap between packed parts, in meters.";
    internal const string SafetyMarginPurpose =
        "Extra collision padding safety around each part spot, in meters.";
    internal const string ShelfSlackPurpose = "How loosely the packed block spreads over the bench.";
    internal const string ControlsDisplayOffsetPurpose =
        "How far the controls UI display position is adjusted when switching sides.";
    internal const string TopMarginPurpose = "Margin kept clear along the top edge of the bench.";
    internal const string SideMarginPurpose =
        "Margin kept clear along both the left and right edges of the bench.";
    internal const string UpdateCheckPurpose =
        "Looks up the latest release on github.com once at startup and logs a line when yours is older. "
        + "Sends nothing about you. Set to false to keep the mod entirely offline.";

    internal const KeyCode DefaultOrganizeKey = KeyCode.F;
    internal const KeyCode DefaultHighlightsKey = KeyCode.G;
    internal const BenchAnchorSide DefaultAnchor = BenchAnchorSide.Left;
    internal const float DefaultCellGap = 0.025f;
    internal const float MaxCellGap = 0.1f;
    internal const float DefaultSafetyMargin = 0.005f;
    internal const float MaxSafetyMargin = 0.05f;
    internal const float DefaultShelfSlack = 1f;
    internal const float MinShelfSlack = 1f;
    internal const float MaxShelfSlack = 2.5f;
    internal const float DefaultControlsDisplayOffset = 0.07f;
    internal const float MaxControlsDisplayOffset = 0.4f;
    internal const float MaxMargin = 0.4f;
    internal const bool DefaultUpdateCheck = true;

    internal static readonly float[] DefaultTopMargins = { 0.05f, 0.10f };
    internal static readonly float[] DefaultSideMargins = { 0.05f, 0.00f };

    internal static KeyCode OrganizeKey;
    internal static KeyCode HighlightsKey;

    internal static float TopMarginFor(BenchAnchorSide anchor) => _topMarginsPerAnchor[(int)anchor].Value;

    internal static float SideMarginFor(BenchAnchorSide anchor) => _sideMarginsPerAnchor[(int)anchor].Value;

    internal static string HotkeyDescription(string purpose) =>
        $"{purpose} Takes any Unity KeyCode name, e.g. F, G, R, Tab, F6, Keypad5.";

    internal static string MarginDescription(string purpose, BenchAnchorSide anchor) =>
        $"{purpose} Applies when packing against the {anchor} side.";

    private static void RunSelfChecks()
    {
        bool hasPackerPassed = BenchPacker.SelfCheck();
        bool hasSorterPassed = NotepadPartsSorter.SelfCheck();
        bool hasUpdateCheckPassed = UpdateCheck.SelfCheck();
        bool hasTooltipPassed = PackageTooltipDetails.SelfCheck();
        bool haveChecksPassed = hasPackerPassed && hasSorterPassed && hasUpdateCheckPassed && hasTooltipPassed;

        Log.Info(haveChecksPassed
            ? "Loaded. Self-check passed."
            : "Loaded. SELF-CHECK FAILED.");
    }

    private static void HandleHotkeys()
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

    private static BenchAnchorSide Opposite(BenchAnchorSide anchor) =>
        anchor == BenchAnchorSide.Left ? BenchAnchorSide.Right : BenchAnchorSide.Left;

    private static KeyCode ParseHotkey(string settingName, string configuredKey, KeyCode defaultKey)
    {
        if (Enum.TryParse(configuredKey, true, out KeyCode key) && Enum.IsDefined(typeof(KeyCode), key))
            return key;

        Log.Warning($"{settingName} \"{configuredKey}\" is not a key name; falling back to {defaultKey}.");
        return defaultKey;
    }
}
