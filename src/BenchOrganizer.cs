using System.Collections.Generic;
using HarmonyLib;
using Restory.Constants;
using Restory.Data.Elements;
using Restory.Gameplay.Disassemble;
using Restory.Gameplay.Disassemble.StateMachine;
using Restory.Gameplay.Elements;
using Restory.Gameplay.Equipment;
using Restory.Gameplay.GameCursor;
using Restory.Gameplay.Workplace;
using UnityEngine;

namespace ReStoryBetterWorkbench;

[HarmonyPatch]
internal static class BenchOrganizer
{
    private const float WidenedRetryFactor = 1.5f;
    private const float SlotCastDistance = 0.01f;
    private const float MinBenchDepth = 0.0001f;

    private static readonly RaycastHit[] CastHits = new RaycastHit[8];
    private static readonly int BlockingLayers =
        ProjectConstants.Layers.ElementsMask | ProjectConstants.Layers.DeviceMask |
        ProjectConstants.Layers.DeviceContainerMask | ProjectConstants.Layers.ObstaclesMask |
        ProjectConstants.Layers.EquipmentMask;

    private static SmallElementBin _smallElementBin;
    private static WorkSurface _surface;
    private static ElementPlacementController _placement;
    private static DisassembleStateMachine _stateMachine;
    private static DisassembleGameMode _gameMode;

    public static void Organize(BenchAnchorSide anchor)
    {
        if (!IsReady(out string reason))
        {
            Log.Debug($"Organize skipped: {reason}.");
            return;
        }

        List<ElementBase> parts = new List<ElementBase>();
        int draggablePartCount = 0;

        foreach (ElementBase element in _surface.PlacedElements)
        {
            if (!IsDraggablePart(element))
                continue;

            draggablePartCount++;

            if (element.PlacementPositionHandler?.PlacementPositionData != null)
            {
                parts.Add(element);
            }
        }

        parts.Sort(CompareByFootprintThenId);

        int partsWithoutPlacementData = draggablePartCount - parts.Count;

        if (partsWithoutPlacementData > 0)
        {
            Log.Warning($"Skipping {partsWithoutPlacementData} part(s) with no placement data.");
        }

        if (parts.Count == 0)
        {
            Log.Debug("Organize skipped: no loose parts on the bench.");
            return;
        }

        Bounds bench = BenchPacker.WorldBounds(_surface.SurfaceBoundary);
        Bounds usable = BenchPacker.UsableBounds(bench,
            BetterWorkbenchPlugin.SideMarginFor(anchor),
            BetterWorkbenchPlugin.TopMarginFor(anchor));

        float gap = BetterWorkbenchPlugin.CellGap.Value;
        float slotY = _surface.DefaultPlacementPosition.y;
        ControlsDisplay.PlaceAwayFrom(anchor, bench);

        List<float> candidateWidths = CandidateRowWidths(parts, usable, gap);

        PassResult result = default;
        float usedRowWidth = 0f;
        int passNumber = 0;
        int strandedCount = 0;

        try
        {
            foreach (float rowWidth in candidateWidths)
            {
                passNumber++;

                foreach (ElementBase part in parts)
                {
                    part.BehaviorSwitcher.SwitchToDraggingBehavior();
                }

                result = PlacePass(parts, usable, slotY, gap, anchor, rowWidth);
                usedRowWidth = rowWidth;

                if (result.Unplaced.Count == 0)
                    break;
            }

            foreach (ElementBase part in result.Unplaced)
            {
                _placement.SetTargetElement(part);
                _placement.ResetPlacementPosition();

                if (_placement.TryFindAvailablePlacementPosition(_surface.DefaultPlacementPosition, out _))
                {
                    _placement.SetPlacementPosition();
                }
                else
                {
                    Log.Warning($"No free spot for {part.Info.ID}; left where it was.");
                    strandedCount++;
                }

                _placement.Clear();
            }
        }
        finally
        {
            foreach (ElementBase part in parts)
            {
                part.BehaviorSwitcher.SwitchToPlacedBehavior();
            }
        }

        int screwsReturned = ReturnStrayScrewsToBin();

        Log.Debug(
            $"{anchor}: {result.Packed} packed, {result.Unplaced.Count} overflow, {strandedCount} stranded, " +
            $"{screwsReturned} screws re-binned (slack {BetterWorkbenchPlugin.ShelfSlack.Value:F2} -> " +
            $"row width {usedRowWidth:F2}, pass {passNumber} of {candidateWidths.Count}).");
    }

    internal static bool IsDraggablePart(ElementBase element)
    {
        if (!element || element.Info == null)
            return false;

        return element.Info.Category == ElementCategory.Draggable;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorkSurface), nameof(WorkSurface.Initialize))]
    private static void CaptureSurface(WorkSurface __instance)
    {
        _surface = __instance;
        ControlsDisplay.AddRows(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ElementPlacementController), MethodType.Constructor,
        typeof(GameCursorDetector), typeof(PlacementPositionFinder), typeof(WorkSurface))]
    private static void CapturePlacement(ElementPlacementController __instance)
    {
        _placement = __instance;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DisassembleStateMachine), nameof(DisassembleStateMachine.Initialize))]
    private static void CaptureStateMachine(DisassembleStateMachine __instance)
    {
        _stateMachine = __instance;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlacedElementsHandler), nameof(PlacedElementsHandler.Construct))]
    private static void CaptureSmallElementBin(SmallElementBin smallElementBin)
    {
        _smallElementBin = smallElementBin;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DisassembleGameMode), nameof(DisassembleGameMode.Initialize))]
    private static void CaptureGameMode(DisassembleGameMode __instance)
    {
        _gameMode = __instance;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorkSurface), nameof(WorkSurface.AddElement))]
    private static void LightNewlyPlacedElement(ElementBase element)
    {
        if (!SurfaceHighlighter.IsOn)
            return;

        SurfaceHighlighter.HighlightElement(element);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorkSurface), nameof(WorkSurface.RemoveElement))]
    private static void UnlightElementLeavingBench(ElementBase element)
    {
        SurfaceHighlighter.UnhighlightElement(element);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorkSurface), nameof(WorkSurface.ClearElements))]
    private static void ResetHighlightsOnBenchCleared()
    {
        SurfaceHighlighter.Reset("device finished");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorkSurface), nameof(WorkSurface.Dispose))]
    private static void ResetHighlightsOnWorkplaceClosed()
    {
        SurfaceHighlighter.Reset("workplace closed");
    }

    private static int ReturnStrayScrewsToBin()
    {
        if (_smallElementBin == null)
            return 0;

        List<ElementBase> strayScrews = new List<ElementBase>();

        foreach (ElementBase element in _surface.PlacedElements)
        {
            if (!IsStrayScrew(element))
                continue;

            strayScrews.Add(element);
        }

        foreach (ElementBase screw in strayScrews)
        {
            _smallElementBin.PutElement(screw);
        }

        return strayScrews.Count;
    }

    private static bool IsStrayScrew(ElementBase element)
    {
        if (!element || element.Info == null)
            return false;

        bool isSmallElement = element.Info.Category == ElementCategory.Small;
        bool isOutsideBin = element.transform.parent != _smallElementBin.transform;

        return isSmallElement && isOutsideBin;
    }

    private static int CompareByFootprintThenId(ElementBase left, ElementBase right)
    {
        int byLargestFootprint = Footprint(right).CompareTo(Footprint(left));

        if (byLargestFootprint != 0)
            return byLargestFootprint;

        int byPartId = left.Info.ID.CompareTo(right.Info.ID);

        if (byPartId != 0)
            return byPartId;

        return left.GetInstanceID().CompareTo(right.GetInstanceID());
    }

    private static List<float> CandidateRowWidths(List<ElementBase> parts, Bounds usable, float gap)
    {
        float compactRowWidth = CompactRowWidth(parts, usable, gap);
        float fullRowWidth = usable.size.x;
        float widenedRowWidth = Mathf.Min(compactRowWidth * WidenedRetryFactor, fullRowWidth);

        float[] rowWidthsToTry = { compactRowWidth, widenedRowWidth, fullRowWidth };
        List<float> candidateWidths = new List<float>();

        foreach (float rowWidth in rowWidthsToTry)
        {
            if (candidateWidths.Contains(rowWidth))
                continue;

            candidateWidths.Add(rowWidth);
        }

        return candidateWidths;
    }

    private static PassResult PlacePass(List<ElementBase> parts, Bounds usable, float slotY, float gap,
        BenchAnchorSide anchor, float rowWidth)
    {
        BenchPacker.Shelf shelf = new BenchPacker.Shelf(usable, slotY, gap, anchor, rowWidth, usable.max.z);
        PassResult result = new PassResult { Unplaced = new List<ElementBase>() };

        Physics.SyncTransforms();

        foreach (ElementBase part in parts)
        {
            Vector3 size = PartSize(part);
            bool isPositioned = false;

            while (!isPositioned)
            {
                if (!shelf.TryNext(new Vector2(size.x, size.z), out Vector3 slot))
                    break;

                if (!IsSlotFree(part, slot))
                    continue;

                PlaceAt(part, slot);
                Physics.SyncTransforms();

                result.Packed++;
                isPositioned = true;

                part.BehaviorSwitcher.SetPhysicsLayer(ProjectConstants.Layers.Elements);
                part.BehaviorSwitcher.PhysicsCollider.enabled = true;
            }

            if (!isPositioned)
            {
                result.Unplaced.Add(part);
            }
        }

        return result;
    }

    private static bool IsSlotFree(ElementBase part, Vector3 slot)
    {
        ElementPlacementPositionData placementData = part.PlacementPositionHandler.PlacementPositionData;

        Vector3 castCentre = slot + placementData.BoxColliderCenter;
        Vector3 halfExtents = placementData.BoxColliderSize * 0.5f
                              + Vector3.one * BetterWorkbenchPlugin.SafetyMargin.Value;

        int blockerCount = Physics.BoxCastNonAlloc(castCentre, halfExtents, placementData.PlacementDirection,
            CastHits, placementData.PlacementRotation, SlotCastDistance, BlockingLayers);

        return blockerCount == 0;
    }

    private static void PlaceAt(ElementBase part, Vector3 slot)
    {
        ElementPlacementPositionData placementData = part.PlacementPositionHandler.PlacementPositionData;
        part.transform.rotation = placementData.PlacementRotation;
        part.transform.position = slot;
    }

    private static float CompactRowWidth(List<ElementBase> parts, Bounds bounds, float gap)
    {
        float area = 0f;
        float widestPart = 0f;

        foreach (ElementBase part in parts)
        {
            Vector3 size = PartSize(part);
            area += (size.x + gap) * (size.z + gap);
            widestPart = Mathf.Max(widestPart, size.x + gap);
        }

        area *= BetterWorkbenchPlugin.ShelfSlack.Value;

        float benchAspect = bounds.size.x / Mathf.Max(bounds.size.z, MinBenchDepth);
        float rowWidth = Mathf.Sqrt(area * benchAspect);

        return Mathf.Min(Mathf.Max(rowWidth, widestPart), bounds.size.x);
    }

    private static float Footprint(ElementBase part)
    {
        Vector3 size = PartSize(part);
        return size.x * size.z;
    }

    private static Vector3 PartSize(ElementBase part)
    {
        ElementPlacementPositionData placementData = part.PlacementPositionHandler?.PlacementPositionData;

        if (placementData == null)
            return Vector3.zero;

        return BenchPacker.RotatedSize(placementData.PlacementRotation, placementData.BoxColliderSize);
    }

    private static bool IsReady(out string reason)
    {
        reason = null;

        if (!_surface)
        {
            reason = "not at the workbench";
        }
        else if (_placement == null)
        {
            reason = "placement controller not available yet";
        }
        else if (_gameMode && _gameMode.IsInCompetition)
        {
            reason = "competition positions are scored, leaving them alone";
        }
        else if (!_stateMachine || !(_stateMachine.ActiveState is DetectionDisassembleState))
        {
            reason = "bench is busy, put down what you're holding first";
        }

        return reason == null;
    }

    private struct PassResult
    {
        public int Packed;
        public List<ElementBase> Unplaced;
    }
}
