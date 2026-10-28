using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private ChartSeriesId _id;
    private bool _hasId;
    private Chart? _owner;
    private Chart? _automaticIdentityOwner;
    private readonly Dictionary<int, ChartObservationId> _observationIds = new();
    private IReadOnlyDictionary<int, ChartObservationId>? _observationIdsView;

    /// <summary>Gets the stable identity supplied at construction or allocated by the chart on attachment.</summary>
    public ChartSeriesId Id => _hasId ? _id : throw new InvalidOperationException("An internal series must be attached before its identity is used.");

    /// <summary>Gets explicit source identities keyed by source observation index.</summary>
    public IReadOnlyDictionary<int, ChartObservationId> ObservationIds => _observationIdsView ??= new ReadOnlyDictionary<int, ChartObservationId>(_observationIds);

    /// <summary>Creates independently configurable numeric series data with an explicit stable identity.</summary>
    /// <param name="id">The stable series ID, independent of display text and chart ordering.</param>
    /// <param name="name">The display name shown in legends and accessible descriptions.</param>
    /// <param name="kind">The numeric series rendering kind.</param>
    /// <param name="points">The ordered numeric observations. Typed families use their typed factory.</param>
    public ChartSeries(ChartSeriesId id, string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points) : this(name, kind, points) {
        id.Validate();
        _id = id; _hasId = true;
    }

    /// <summary>Changes display text without changing series or observation identity.</summary>
    public ChartSeries WithName(string name) {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>Sets this series' semantic visual state independently of its display name and chart position.</summary>
    public ChartSeries WithState(ChartSeriesState state) {
        StateRole = state;
        return this;
    }

    /// <summary>Assigns an identity to an existing source observation, including a decimated source that is not currently drawn.</summary>
    /// <remarks>IDs are unique within the series. Updating a value preserves its ID; replacing the entire source clears IDs.</remarks>
    public ChartSeries WithObservationId(int sourceIndex, ChartObservationId id) {
        _ = id.Value;
        var count = IdentityObservationCount;
        if (sourceIndex < 0 || sourceIndex >= count) throw new ArgumentOutOfRangeException(nameof(sourceIndex), "Source index must refer to an existing observation.");
        foreach (var entry in _observationIds)
            if (entry.Key != sourceIndex && entry.Value == id) throw new ArgumentException("Observation IDs must be unique within a series.", nameof(id));
        _observationIds[sourceIndex] = id;
        return this;
    }

    /// <summary>Replaces source observation identities in source order with one identity per authored observation.</summary>
    /// <remarks>The sequence is captured and validated before any existing identity changes. Decimated series accept the complete source sequence.</remarks>
    /// <param name="ids">Unique identities in source observation order.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithObservationIds(IEnumerable<ChartObservationId> ids) {
        if (ids == null) throw new ArgumentNullException(nameof(ids));
        var count = IdentityObservationCount;
        var candidate = new List<ChartObservationId>();
        var unique = new HashSet<ChartObservationId>();
        foreach (var id in ids) {
            _ = id.Value;
            if (candidate.Count >= count) throw new ArgumentException("Supply exactly one identity per source observation.", nameof(ids));
            if (!unique.Add(id)) throw new ArgumentException("Observation IDs must be unique within a series.", nameof(ids));
            candidate.Add(id);
        }
        if (candidate.Count != count) throw new ArgumentException("Supply exactly one identity per source observation.", nameof(ids));
        _observationIds.Clear();
        for (var index = 0; index < candidate.Count; index++) _observationIds.Add(index, candidate[index]);
        return this;
    }

    internal string InteractionIdentityKey => Id.Value;
    private bool HasSourceIndexedObservationIds => DecimationMode.HasValue || HistogramBinLayout != null || TrendLineSourcePoints.Count > 0 || _isRawBoxPlot || _visibilityPreparation;
    private int IdentityObservationCount => HasSourceIndexedObservationIds
        ? SourcePointCount : AuthoredObservationCount;
    internal bool HasIdentity => _hasId;
    internal Chart? Owner => _owner;
    internal Chart? AutomaticIdentityOwner => _automaticIdentityOwner;
    internal void AssignAutomaticIdentity(ChartSeriesId id, Chart owner) { _id = id; _hasId = true; _automaticIdentityOwner = owner; }
    internal void AttachTo(Chart owner) => _owner = owner;
    internal void DetachFrom(Chart owner) { if (ReferenceEquals(_owner, owner)) _owner = null; }
    /// <summary>Atomically restores sparse authored identities with linear validation at a portable-data boundary.</summary>
    internal void RestoreObservationIds(IEnumerable<KeyValuePair<int, ChartObservationId>> ids) {
        if (ids == null) throw new ArgumentNullException(nameof(ids));
        var candidate = new Dictionary<int, ChartObservationId>(); var unique = new HashSet<ChartObservationId>();
        foreach (var entry in ids) {
            _ = entry.Value.Value;
            if (entry.Key < 0 || entry.Key >= IdentityObservationCount || candidate.ContainsKey(entry.Key) || !unique.Add(entry.Value))
                throw new ArgumentException("Restored IDs need unique authored indices and identities.", nameof(ids));
            candidate.Add(entry.Key, entry.Value);
        }
        _observationIds.Clear(); foreach (var entry in candidate) _observationIds.Add(entry.Key, entry.Value);
    }

    internal void ClearObservationKeys() => _observationIds.Clear();
}
