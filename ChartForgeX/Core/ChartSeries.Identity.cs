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
        var count = IsDecimated ? SourcePointCount : AuthoredObservationCount;
        if (sourceIndex < 0 || sourceIndex >= count) throw new ArgumentOutOfRangeException(nameof(sourceIndex), "Source index must refer to an existing observation.");
        foreach (var entry in _observationIds)
            if (entry.Key != sourceIndex && entry.Value == id) throw new ArgumentException("Observation IDs must be unique within a series.", nameof(id));
        _observationIds[sourceIndex] = id;
        return this;
    }

    internal string InteractionIdentityKey => Id.Value;
    internal bool HasIdentity => _hasId;
    internal Chart? Owner => _owner;
    internal Chart? AutomaticIdentityOwner => _automaticIdentityOwner;
    internal void AssignAutomaticIdentity(ChartSeriesId id, Chart owner) { _id = id; _hasId = true; _automaticIdentityOwner = owner; }
    internal void AttachTo(Chart owner) => _owner = owner;
    internal void DetachFrom(Chart owner) { if (ReferenceEquals(_owner, owner)) _owner = null; }
    internal void ClearObservationKeys() => _observationIds.Clear();
}
