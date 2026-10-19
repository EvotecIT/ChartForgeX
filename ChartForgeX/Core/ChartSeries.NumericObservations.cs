using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private void RequireMutableNumericObservations() {
        if (HasSourceIndexedObservationIds)
            throw new NotSupportedException("Derived points retain source provenance. Rebuild this series from complete source observations to change its data.");
        if (UsesTypedObservations || IsRelationshipKind(Kind))
            throw new NotSupportedException("This series now owns typed observations; use its typed update/replacement API.");
    }

    /// <summary>Keeps ordinary observation identities and presentation aligned after structural edits.</summary>
    private void NumericPointInserted(int index) {
        InsertDefault(PointColors, index); InsertDefault(PointFillPatterns, index); InsertDefault(PointLabels, index);
        InsertDefault(PointDataLabelStyles, index); InsertDefault(PointSliceOffsets, index);
        ShiftNumericObservationIds(index, false);
    }

    private void NumericPointRemoved(int index) {
        RemoveOverride(PointColors, index); RemoveOverride(PointFillPatterns, index); RemoveOverride(PointLabels, index);
        RemoveOverride(PointDataLabelStyles, index); RemoveOverride(PointSliceOffsets, index);
        ShiftNumericObservationIds(index, true);
    }

    private void ShiftNumericObservationIds(int index, bool removing) {
        var affected = _observationIds.Where(entry => entry.Key >= index).ToArray();
        foreach (var entry in affected) _observationIds.Remove(entry.Key);
        foreach (var entry in affected)
            if (!removing || entry.Key != index) _observationIds.Add(entry.Key + (removing ? -1 : 1), entry.Value);
    }

    private static void InsertDefault<T>(List<T> values, int index) { if (index < values.Count) values.Insert(index, default!); }
    private static void RemoveOverride<T>(List<T> values, int index) { if (index < values.Count) values.RemoveAt(index); }

    /// <summary>Maintains source/presentation ownership behind the existing numeric IList surface.</summary>
    private sealed class NumericPointList : IList<ChartPoint>, IReadOnlyList<ChartPoint> {
        private readonly ChartSeries _owner;
        internal NumericPointList(ChartSeries owner) => _owner = owner;
        public int Count => _owner._points.Count;
        public bool IsReadOnly => _owner.UsesTypedObservations || IsRelationshipKind(_owner.Kind) || _owner.HasSourceIndexedObservationIds;
        public ChartPoint this[int index] {
            get => _owner._points[index];
            set { Writable(); _owner._points[index] = value; }
        }
        public void Add(ChartPoint point) => Insert(Count, point);
        public void Insert(int index, ChartPoint point) {
            Writable();
            var previousCount = Count;
            _owner._points.Insert(index, point);
            // Appending cannot shift an existing authored identity.
            if (index < previousCount) _owner.NumericPointInserted(index);
        }
        public bool Remove(ChartPoint point) {
            Writable();
            var index = IndexOf(point);
            if (index < 0) return false;
            RemoveAt(index); return true;
        }
        public void RemoveAt(int index) {
            Writable();
            _owner._points.RemoveAt(index);
            _owner.NumericPointRemoved(index);
        }
        public void Clear() {
            Writable();
            _owner._points.Clear(); _owner.ClearObservationMetadata();
        }
        public bool Contains(ChartPoint point) => _owner._points.Contains(point);
        public int IndexOf(ChartPoint point) => _owner._points.IndexOf(point);
        public void CopyTo(ChartPoint[] array, int arrayIndex) => _owner._points.CopyTo(array, arrayIndex);
        public IEnumerator<ChartPoint> GetEnumerator() => _owner._points.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private void Writable() => _owner.RequireMutableNumericObservations();
    }
}
