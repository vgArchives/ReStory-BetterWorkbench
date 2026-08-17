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
    private static readonly AccessTools.FieldRef<ElementView, OutlinableAdapter> AdapterField =
        AccessTools.FieldRefAccess<ElementView, OutlinableAdapter>("outlineAdapter");

    private static readonly AccessTools.FieldRef<ElementView, ElementBase> ElementField =
        AccessTools.FieldRefAccess<ElementView, ElementBase>("element");

    private static readonly MethodInfo ApplyConditionPreset =
        AccessTools.DeclaredMethod(typeof(ElementView), "OutlineSelectedElement");

    private static readonly MethodInfo ResolveSelection =
        AccessTools.DeclaredMethod(typeof(ElementView), "ResolveSelectionStateChanged");

    internal static bool IsOn;

    internal static void Toggle()
    {
        IsOn = !IsOn;
        RefreshAll();
        BetterWorkbenchPlugin.LogDebug($"Bench highlights {(IsOn ? "on" : "off")}.");
    }

    internal static void Reset(string reason)
    {
        if (!IsOn)
            return;

        IsOn = false;
        BetterWorkbenchPlugin.LogDebug($"Bench highlights off ({reason}).");
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
            ElementBase element = ElementField(view);

            if (!element)
                continue;

            if (!IsOn)
            {
                ResolveSelection.Invoke(view, null);
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
        ApplyConditionPreset.Invoke(view, null);
        AdapterField(view).IsActive = true;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return ResolveSelection;
        yield return AccessTools.DeclaredMethod(typeof(ThreadedElementView), "ResolveSelectionStateChanged");
        yield return AccessTools.DeclaredMethod(typeof(FlipElementView), "ResolveSelectionStateChanged");
    }

    private static void Postfix(ElementView __instance)
    {
        if (!IsOn)
            return;

        ElementBase element = ElementField(__instance);

        if (element && element.IsOnSurface && !element.IsSelected)
        {
            Light(__instance);
        }
    }
}
