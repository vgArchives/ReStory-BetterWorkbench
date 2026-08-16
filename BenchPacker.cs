using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace RestoryBenchOrganizer;

internal static class BenchPacker
{
    private const float MinCellExtent = 0.001f;
    private const float MaxSideMarginFraction = 0.4f;
    private const float MaxTopMarginFraction = 0.8f;

    private static ManualLogSource Log => BenchOrganizerPlugin.Log;

    internal static Bounds UsableBounds(Bounds bench, float sideMargin, float topMargin)
    {
        float sideInset = Mathf.Clamp(sideMargin, 0f, bench.size.x * MaxSideMarginFraction);
        float topInset = Mathf.Clamp(topMargin, 0f, bench.size.z * MaxTopMarginFraction);

        Bounds usable = new Bounds();
        usable.SetMinMax(
            new Vector3(bench.min.x + sideInset, bench.min.y, bench.min.z),
            new Vector3(bench.max.x - sideInset, bench.max.y, bench.max.z - topInset));

        return usable;
    }

    internal static Vector3 RotatedSize(Quaternion rotation, Vector3 size)
    {
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotation);
        Vector3 rotatedSize = new Vector3();

        for (int axis = 0; axis < 3; axis++)
        {
            rotatedSize[axis] = Mathf.Abs(rotationMatrix[axis, 0]) * size.x
                                + Mathf.Abs(rotationMatrix[axis, 1]) * size.y
                                + Mathf.Abs(rotationMatrix[axis, 2]) * size.z;
        }

        return rotatedSize;
    }

    internal static Bounds WorldBounds(BoxCollider box)
    {
        Transform boxTransform = box.transform;
        Vector3 halfSize = box.size * 0.5f;
        Bounds bounds = new Bounds(boxTransform.TransformPoint(box.center), Vector3.zero);

        for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
        {
            Vector3 corner = new Vector3(
                (cornerIndex & 1) == 0 ? -halfSize.x : halfSize.x,
                (cornerIndex & 2) == 0 ? -halfSize.y : halfSize.y,
                (cornerIndex & 4) == 0 ? -halfSize.z : halfSize.z);

            bounds.Encapsulate(boxTransform.TransformPoint(box.center + corner));
        }

        return bounds;
    }

    internal static bool SelfCheck()
    {
        Bounds benchBounds = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(1f, 0.1f, 1f));
        List<Vector2> partSizes = Enumerable.Repeat(new Vector2(0.25f, 0.25f), 20).ToList();

        var expectedFirstSlots = new Dictionary<BenchAnchorSide, Vector3>
        {
            { BenchAnchorSide.Left, new Vector3(-0.375f, 1f, 0.375f) },
            { BenchAnchorSide.Right, new Vector3(0.375f, 1f, 0.375f) }
        };

        bool isValid = true;

        foreach (KeyValuePair<BenchAnchorSide, Vector3> expectation in expectedFirstSlots)
        {
            List<Vector3> packedSlots =
                PackSlots(partSizes, benchBounds, 1f, 0f, expectation.Key, 1f, 0.5f);

            bool isAnchorValid = packedSlots.Count == 16
                                 && Vector3.Distance(packedSlots[0], expectation.Value) < 0.001f
                                 && packedSlots[1].z == packedSlots[0].z
                                 && Mathf.Abs(Mathf.Abs(packedSlots[1].x - packedSlots[0].x) - 0.25f) < 0.001f
                                 && Mathf.Abs(packedSlots[4].z - packedSlots[0].z + 0.25f) < 0.001f
                                 && packedSlots.TrueForAll(slot =>
                                     Mathf.Abs(slot.x) <= 0.5f && Mathf.Abs(slot.z) <= 0.5f);

            if (!isAnchorValid)
            {
                string firstSlotText = packedSlots.Count > 0 ? packedSlots[0].ToString() : "none";

                Log.LogError($"Self-check FAILED for {expectation.Key}: {packedSlots.Count} cells, " +
                             $"first {firstSlotText}, expected {expectation.Value}.");
                isValid = false;
            }
        }

        List<Vector3> narrowRowSlots =
            PackSlots(partSizes, benchBounds, 1f, 0f, BenchAnchorSide.Left, 0.5f, 0.5f);

        if (narrowRowSlots.Count < 3
            || narrowRowSlots[2].x != narrowRowSlots[0].x
            || Mathf.Abs(narrowRowSlots[2].z - narrowRowSlots[0].z + 0.25f) > 0.001f)
        {
            Log.LogError("Self-check FAILED: row width limit did not wrap the third part onto a new row.");
            isValid = false;
        }

        Bounds insetBounds = UsableBounds(benchBounds, 0.1f, 0.2f);

        if (Mathf.Abs(insetBounds.min.x + 0.4f) > 0.001f
            || Mathf.Abs(insetBounds.max.x - 0.4f) > 0.001f
            || Mathf.Abs(insetBounds.max.z - 0.3f) > 0.001f
            || Mathf.Abs(insetBounds.min.z + 0.5f) > 0.001f)
        {
            Log.LogError($"Self-check FAILED: margins gave min {insetBounds.min} max {insetBounds.max}.");
            isValid = false;
        }

        Bounds crushedBounds = UsableBounds(benchBounds, 99f, 99f);

        if (crushedBounds.size.x <= 0f || crushedBounds.size.z <= 0f)
        {
            Log.LogError($"Self-check FAILED: oversized margins collapsed the bench to {crushedBounds.size}.");
            isValid = false;
        }

        List<Vector3> oversizedPartSlots = PackSlots(
            new[] { new Vector2(2f, 0.25f) }, benchBounds, 1f, 0f, BenchAnchorSide.Left, 1f, 0.5f);

        if (oversizedPartSlots.Count != 0)
        {
            Log.LogError($"Self-check FAILED: a part wider than the bench was placed at {oversizedPartSlots[0]}.");
            isValid = false;
        }

        Vector3 diagonalSize = RotatedSize(Quaternion.Euler(0f, 45f, 0f), Vector3.one);

        if (Mathf.Abs(diagonalSize.x - Mathf.Sqrt(2f)) > 0.001f
            || Mathf.Abs(diagonalSize.z - Mathf.Sqrt(2f)) > 0.001f)
        {
            Log.LogError($"Self-check FAILED: a part rotated 45 degrees measured {diagonalSize}.");
            isValid = false;
        }

        return isValid;
    }

    private static List<Vector3> PackSlots(IList<Vector2> sizes, Bounds bounds, float slotY, float gap,
        BenchAnchorSide anchor, float rowWidth, float topZ)
    {
        Shelf shelf = new Shelf(bounds, slotY, gap, anchor, rowWidth, topZ);
        List<Vector3> slots = new List<Vector3>();

        foreach (Vector2 size in sizes)
        {
            if (!shelf.TryNext(size, out Vector3 slot))
                break;

            slots.Add(slot);
        }

        return slots;
    }

    internal sealed class Shelf
    {
        private readonly Bounds _bounds;
        private readonly float _slotY;
        private readonly float _gap;
        private readonly bool _isRightward;
        private readonly float _rowStartX;
        private readonly float _rowEndX;
        private float _cursorX;
        private float _cursorZ;
        private float _rowDepth;

        public Shelf(Bounds bounds, float slotY, float gap, BenchAnchorSide anchor, float rowWidth, float topZ)
        {
            _bounds = bounds;
            _slotY = slotY;
            _gap = gap;

            _isRightward = anchor == BenchAnchorSide.Left;
            _rowStartX = _isRightward ? bounds.min.x : bounds.max.x;
            _rowEndX = _isRightward ? _rowStartX + rowWidth : _rowStartX - rowWidth;

            _cursorX = _rowStartX;
            _cursorZ = topZ;
        }

        public bool TryNext(Vector2 size, out Vector3 slot)
        {
            slot = Vector3.zero;

            float width = Mathf.Max(size.x + _gap, MinCellExtent);
            float depth = Mathf.Max(size.y + _gap, MinCellExtent);

            bool isRowFull = _isRightward
                ? _cursorX + width > _rowEndX
                : _cursorX - width < _rowEndX;

            if (isRowFull && _rowDepth > 0f)
            {
                _cursorZ -= _rowDepth;
                _cursorX = _rowStartX;
                _rowDepth = 0f;
            }

            float cellEndX = _isRightward ? _cursorX + width : _cursorX - width;

            if (cellEndX < _bounds.min.x || cellEndX > _bounds.max.x)
                return false;

            if (_cursorZ - depth < _bounds.min.z)
                return false;

            slot = new Vector3(
                _isRightward ? _cursorX + width * 0.5f : _cursorX - width * 0.5f,
                _slotY,
                _cursorZ - depth * 0.5f);

            _cursorX += _isRightward ? width : -width;
            _rowDepth = Mathf.Max(_rowDepth, depth);

            return true;
        }
    }
}
