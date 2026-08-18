#if MELONLOADER
using MelonLoader;
using MelonLoader.Preferences;
using ReStoryBetterWorkbench;
using UnityEngine;

[assembly: MelonInfo(typeof(BetterWorkbenchPlugin), BetterWorkbenchPlugin.PluginName,
    BetterWorkbenchPlugin.PluginVersion, BetterWorkbenchPlugin.PluginAuthor)]
[assembly: MelonGame("Mandragora", "Restory")]

namespace ReStoryBetterWorkbench;

public partial class BetterWorkbenchPlugin : MelonMod
{
    private const string GeneralSection = "ReStoryBetterWorkbenchGeneral";
    private const string HotkeysSection = "ReStoryBetterWorkbenchHotkeys";
    private const string LayoutSection = "ReStoryBetterWorkbenchLayout";

    internal static MelonPreferences_Entry<BenchAnchorSide> Anchor;
    internal static MelonPreferences_Entry<float> CellGap;
    internal static MelonPreferences_Entry<float> SafetyMargin;
    internal static MelonPreferences_Entry<float> ShelfSlack;
    internal static MelonPreferences_Entry<float> ControlsDisplayOffset;
    internal static MelonPreferences_Entry<bool> UpdateCheckEnabled;

    private static MelonPreferences_Entry<float>[] _topMarginsPerAnchor;
    private static MelonPreferences_Entry<float>[] _sideMarginsPerAnchor;

    private static MelonPreferences_Category _general;
    private static MelonPreferences_Category _hotkeys;
    private static MelonPreferences_Category _layout;

    public override void OnEarlyInitializeMelon() => Log.Source = LoggerInstance;

    public override void OnInitializeMelon()
    {
        BindConfig();

        RunSelfChecks();

        if (UpdateCheckEnabled.Value)
        {
            MelonCoroutines.Start(UpdateCheck.Run(PluginVersion));
        }
    }

    public override void OnUpdate() => HandleHotkeys();

    private void BindConfig()
    {
        _general = MelonPreferences.CreateCategory(GeneralSection, $"{PluginName} - General");
        _hotkeys = MelonPreferences.CreateCategory(HotkeysSection, $"{PluginName} - Hotkeys");
        _layout = MelonPreferences.CreateCategory(LayoutSection, $"{PluginName} - Layout");

        UpdateCheckEnabled = _general.CreateEntry(UpdateCheckName, DefaultUpdateCheck,
            description: UpdateCheckPurpose);

        OrganizeKey = BindHotkey(OrganizeKeyName, DefaultOrganizeKey, OrganizeKeyPurpose);
        HighlightsKey = BindHotkey(HighlightsKeyName, DefaultHighlightsKey, HighlightsKeyPurpose);

        Anchor = _layout.CreateEntry(AnchorName, DefaultAnchor, description: AnchorPurpose);
        CellGap = BindRanged(CellGapName, DefaultCellGap, CellGapPurpose, 0f, MaxCellGap);
        SafetyMargin = BindRanged(SafetyMarginName, DefaultSafetyMargin, SafetyMarginPurpose,
            0f, MaxSafetyMargin);
        ShelfSlack = BindRanged(ShelfSlackName, DefaultShelfSlack, ShelfSlackPurpose,
            MinShelfSlack, MaxShelfSlack);
        ControlsDisplayOffset = BindRanged(ControlsDisplayOffsetName, DefaultControlsDisplayOffset,
            ControlsDisplayOffsetPurpose, 0f, MaxControlsDisplayOffset);

        _topMarginsPerAnchor = BindMarginPerAnchor(TopMarginName, DefaultTopMargins, TopMarginPurpose);
        _sideMarginsPerAnchor = BindMarginPerAnchor(SideMarginName, DefaultSideMargins, SideMarginPurpose);

        MelonPreferences.Save();
    }

    private KeyCode BindHotkey(string settingName, KeyCode defaultKey, string purpose)
    {
        MelonPreferences_Entry<string> entry = _hotkeys.CreateEntry(settingName, defaultKey.ToString(),
            description: HotkeyDescription(purpose));

        return ParseHotkey(settingName, entry.Value, defaultKey);
    }

    private MelonPreferences_Entry<float>[] BindMarginPerAnchor(string settingName, float[] defaults,
        string purpose)
    {
        MelonPreferences_Entry<float>[] entries = new MelonPreferences_Entry<float>[defaults.Length];

        for (int anchorIndex = 0; anchorIndex < defaults.Length; anchorIndex++)
        {
            BenchAnchorSide anchor = (BenchAnchorSide)anchorIndex;

            entries[anchorIndex] = BindRanged($"{settingName}{anchor}", defaults[anchorIndex],
                MarginDescription(purpose, anchor), 0f, MaxMargin);
        }

        return entries;
    }

    private MelonPreferences_Entry<float> BindRanged(string settingName, float defaultValue, string purpose,
        float min, float max) =>
        _layout.CreateEntry(settingName, defaultValue, description: purpose,
            validator: new ValueRange<float>(min, max));
}
#endif
