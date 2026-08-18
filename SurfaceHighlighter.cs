using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Restory.Gameplay.Common;
using Restory.Gameplay.Elements;
using UnityEngine;

namespace ReStoryBetterWorkbench;

[HarmonyPatch]
internal static class SurfaceHighlighter
{
    private const string AdapterFieldName = "outlineAdapter";
    private const string ElementFieldName = "element";

    private static AccessTools.FieldRef<ElementView, OutlinableAdapter> _adapterField;
    private static AccessTools.FieldRef<ElementView, ElementBase> _elementField;
    private static MethodInfo _applyConditionPreset;
    private static MethodInfo _resolveSelection;
    private static MethodInfo _resolveHighlight;

    internal static bool IsOn;

    private static bool Prepare()
    {
        _applyConditionPreset = AccessTools.DeclaredMethod(typeof(ElementView), "OutlineSelectedElement");
        _resolveSelection = AccessTools.DeclaredMethod(typeof(ElementView), "ResolveSelectionStateChanged");
        _resolveHighlight = AccessTools.DeclaredMethod(typeof(ElementView), "ResolveHighlightedStateChanged");

        bool hasEveryMember = _applyConditionPreset != null
                              && _resolveSelection != null
                              && _resolveHighlight != null
                              && AccessTools.Field(typeof(ElementView), AdapterFieldName) != null
                              && AccessTools.Field(typeof(ElementView), ElementFieldName) != null;

        if (!hasEveryMember)
        {
            Log.Warning("Bench highlights are disabled: ElementView is missing an outline field or "
                        + "method, most likely renamed by a game update. "
                        + "Bench organizing and the notepad features are unaffected.");

            return false;
        }

        _adapterField = AccessTools.FieldRefAccess<ElementView, OutlinableAdapter>(AdapterFieldName);
        _elementField = AccessTools.FieldRefAccess<ElementView, ElementBase>(ElementFieldName);

        return true;
    }

    internal static void Toggle()
    {
        if (_elementField == null)
            return;

        IsOn = !IsOn;
        RefreshAll();
        Log.Debug($"Bench highlights {(IsOn ? "on" : "off")}.");
    }

    internal static void Reset(string reason)
    {
        if (!IsOn)
            return;

        IsOn = false;
        Log.Debug($"Bench highlights off ({reason}).");
    }

    internal static void LightElement(ElementBase element)
    {
        if (!element || !element.IsOnSurface)
            return;

        ElementView view = element.GetComponentInChildren<ElementView>(true);

        if (view != null)
        {
            Light(view);
        }
    }

    internal static void RefreshAll()
    {
        foreach (ElementView view in
                 Object.FindObjectsByType<ElementView>(FindObjectsSortMode.None))
        {
            ElementBase element = _elementField(view);

            if (!element)
                continue;

            if (!IsOn)
            {
                _resolveSelection.Invoke(view, null);
                continue;
            }

            if (element.IsOnSurface)
            {
                Light(view);
            }
        }
    }

    private static void Light(ElementView view)
    {
        _applyConditionPreset.Invoke(view, null);
        _adapterField(view).IsActive = true;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return _resolveSelection;
        yield return _resolveHighlight;
        yield return AccessTools.DeclaredMethod(typeof(ThreadedElementView), "ResolveSelectionStateChanged");
        yield return AccessTools.DeclaredMethod(typeof(FlipElementView), "ResolveSelectionStateChanged");
    }

    private static void Postfix(ElementView __instance)
    {
        if (!IsOn)
            return;

        ElementBase element = _elementField(__instance);

        if (element && element.IsOnSurface && !element.IsSelected)
        {
            Light(__instance);
        }
    }
}
