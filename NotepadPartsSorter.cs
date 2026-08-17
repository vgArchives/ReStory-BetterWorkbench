using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Restory.Data.Elements.Condition;
using Restory.Gameplay.Elements;
using Restory.Gameplay.Soldering;
using Restory.UI.Presenters.Notepad;
using UnityEngine;

namespace ReStoryBetterWorkbench;

[HarmonyPatch]
internal static class NotepadPartsSorter
{
    private const int BrokenRank = 0;
    private const int SolderingRank = 1;
    private const int CleaningRank = 2;
    private const int ReadyRank = 3;

    private static ManualLogSource Log => BetterWorkbenchPlugin.Log;

    internal static bool SelfCheck()
    {
        (string CaseName, ElementConditionBase Condition,
            ScorchedCircuitProperty ScorchedCircuit, int ExpectedRank)[] cases =
        {
            ("broken", Condition<DamagedElementCondition>(), null, BrokenRank),
            ("burnt with exposed solder points",
                Condition<BurntElementCondition>(), ScorchedCircuit(SolderPointState.Burnt), SolderingRank),
            ("burnt with sooty solder points",
                Condition<BurntElementCondition>(), ScorchedCircuit(SolderPointState.Sooty), CleaningRank),
            ("dirty", Condition<DirtyElementCondition>(), null, CleaningRank),
            ("perfect", Condition<PerfectElementCondition>(), null, ReadyRank)
        };

        bool isValid = true;

        foreach ((string caseName, ElementConditionBase condition,
            ScorchedCircuitProperty scorchedCircuit, int expectedRank) in cases)
        {
            isValid &= Expect(caseName, condition, scorchedCircuit, expectedRank);
        }

        return isValid;
    }

    private static bool Prepare()
    {
        if (AccessTools.Method(typeof(GUI_NotepadElementsPanel), "UpdateElements") != null
            && AccessTools.Field(typeof(GUI_NotepadElementsPanel), "cachedPlacedElements") != null)
            return true;

        Log.LogError("Notepad parts sorting is disabled: GUI_NotepadElementsPanel.UpdateElements or "
            + "cachedPlacedElements is missing, most likely renamed by a game update. "
            + "Bench organizing is unaffected.");

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GUI_NotepadElementsPanel), "UpdateElements")]
    private static void SortByRemainingWork(List<ElementBase> ___cachedPlacedElements)
    {
        List<ElementBase> sorted = ___cachedPlacedElements
            .OrderBy(part => WorkRank(part.ConditionHandler.ElementData))
            .ToList();

        ___cachedPlacedElements.Clear();
        ___cachedPlacedElements.AddRange(sorted);
    }

    private static int WorkRank(ElementData elementData)
    {
        if (elementData.Condition is DamagedElementCondition)
            return BrokenRank;

        if (elementData.JustSolderingNeeded())
            return SolderingRank;

        if (elementData.Condition is DirtyElementCondition)
            return CleaningRank;

        return ReadyRank;
    }

    private static bool Expect(string caseName, ElementConditionBase condition,
        ScorchedCircuitProperty scorchedCircuit, int expectedRank)
    {
        var elementData = new ElementData { Condition = condition, AdditionalProperty = scorchedCircuit };
        int rank = WorkRank(elementData);

        Object.DestroyImmediate(condition);

        if (rank == expectedRank)
            return true;

        Log.LogError($"Self-check FAILED: {caseName} ranked {rank}, expected {expectedRank}.");

        return false;
    }

    private static ElementConditionBase Condition<T>() where T : ElementConditionBase =>
        ScriptableObject.CreateInstance<T>();

    private static ScorchedCircuitProperty ScorchedCircuit(SolderPointState state) =>
        new ScorchedCircuitProperty
        {
            BurntTraces = new List<BurntTraceData>
            {
                new BurntTraceData { SolderPoints = { new SolderPointData { State = state } } }
            }
        };
}
