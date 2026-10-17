using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private bool _isRawBoxPlot;
    private ChartBoxPlotStatisticsOptions _boxPlotStatistics = new();
    private readonly Dictionary<int, ChartObservationId> _boxPlotGroupIds = new();
    private IReadOnlyDictionary<int, ChartObservationId>? _boxPlotGroupIdsView;

    /// <summary>Gets immutable copied raw groups; empty for supplied summaries.</summary>
    public IReadOnlyList<ChartBoxPlotSampleGroup> BoxPlotSourceGroups { get; private set; } = Array.Empty<ChartBoxPlotSampleGroup>();
    /// <summary>Gets immutable calculated summaries and original-source provenance.</summary>
    public IReadOnlyList<ChartBoxPlotDistribution> BoxPlotDistributions { get; private set; } = Array.Empty<ChartBoxPlotDistribution>();
    /// <summary>Gets raw-group identities, independent of sample observation identities.</summary>
    public IReadOnlyDictionary<int, ChartObservationId> BoxPlotGroupIds => _boxPlotGroupIdsView ??= new ReadOnlyDictionary<int, ChartObservationId>(_boxPlotGroupIds);
    /// <summary>Gets the installed raw-input quartile estimator, or null for supplied summaries.</summary>
    public ChartBoxPlotQuartileEstimator? BoxPlotQuartileEstimator => _isRawBoxPlot ? _boxPlotStatistics.Estimator : null;
    /// <summary>Gets the installed raw-input whisker policy, or null for supplied summaries.</summary>
    public ChartBoxPlotWhiskerPolicy? BoxPlotWhiskerPolicy => _isRawBoxPlot ? _boxPlotStatistics.WhiskerPolicy : null;
    /// <summary>Gets the installed raw-input multiplier, or null for supplied summaries.</summary>
    public double? BoxPlotIqrMultiplier => _isRawBoxPlot ? _boxPlotStatistics.IqrMultiplier : null;

    /// <summary>Creates a raw box-plot series from copied groups, retaining original measurement order.</summary>
    public static ChartSeries CreateBoxPlot(ChartSeriesId id, string name, IEnumerable<ChartBoxPlotSampleGroup> groups, ChartColor? color = null) =>
        new ChartSeries(id, name, ChartSeriesKind.BoxPlot, Array.Empty<ChartPoint>()) { Color = color }.ReplaceBoxPlotSampleGroups(groups);

    internal static ChartSeries CreateBoxPlot(string name, IEnumerable<ChartBoxPlotSampleGroup> groups, ChartColor? color) =>
        new ChartSeries(name, ChartSeriesKind.BoxPlot, Array.Empty<ChartPoint>()) { Color = color }.ReplaceBoxPlotSampleGroups(groups);

    /// <summary>Atomically configures and recalculates raw distributions while preserving source and group identities.</summary>
    /// <remarks>The callback receives a detached candidate; a failed callback or calculation leaves installed state unchanged.</remarks>
    public ChartSeries ConfigureBoxPlotStatistics(Action<ChartBoxPlotStatisticsOptions> configure) {
        RequireRawBoxPlot();
        var options = ChartOptionConfiguration.Configure(_boxPlotStatistics, configure, value => value.Clone());
        var results = ChartBoxPlotStatistics.Calculate(BoxPlotSourceGroups, options);
        InstallBoxPlotRaw(BoxPlotSourceGroups, results, options);
        return this;
    }

    /// <summary>Atomically replaces all raw groups, retaining statistics policy and clearing group/sample identities and point presentation.</summary>
    public ChartSeries ReplaceBoxPlotSampleGroups(IEnumerable<ChartBoxPlotSampleGroup> groups) {
        RequireObservationKind(ChartSeriesKind.BoxPlot);
        if (groups == null) throw new ArgumentNullException(nameof(groups));
        var snapshot = groups.Select(group => group == null ? throw new ArgumentException("Groups cannot contain null.", nameof(groups))
            : new ChartBoxPlotSampleGroup(group.X, group.Samples)).ToArray();
        var results = ChartBoxPlotStatistics.Calculate(snapshot, _boxPlotStatistics);
        InstallBoxPlotRaw(Array.AsReadOnly(snapshot), results, _boxPlotStatistics);
        ClearObservationMetadata(); _boxPlotGroupIds.Clear();
        return this;
    }

    /// <summary>Atomically replaces one raw group, preserving its identity and presentation and clearing only its sample IDs.</summary>
    /// <remarks>Later groups' sample ID indices shift with the changed sample count.</remarks>
    public ChartSeries UpdateBoxPlotSampleGroup(int index, ChartBoxPlotSampleGroup group) {
        RequireRawBoxPlot();
        if (index < 0 || index >= BoxPlotSourceGroups.Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (group == null) throw new ArgumentNullException(nameof(group));
        var snapshot = BoxPlotSourceGroups.ToArray(); snapshot[index] = new ChartBoxPlotSampleGroup(group.X, group.Samples);
        var results = ChartBoxPlotStatistics.Calculate(snapshot, _boxPlotStatistics);
        var start = BoxPlotDistributions[index].SourceIndices[0]; var oldCount = BoxPlotSourceGroups[index].Samples.Count;
        var delta = snapshot[index].Samples.Count - oldCount;
        var ids = new Dictionary<int, ChartObservationId>();
        foreach (var entry in _observationIds) {
            if (entry.Key < start) ids.Add(entry.Key, entry.Value);
            else if (entry.Key >= start + oldCount) ids.Add(checked(entry.Key + delta), entry.Value);
        }
        InstallBoxPlotRaw(Array.AsReadOnly(snapshot), results, _boxPlotStatistics);
        _observationIds.Clear(); foreach (var entry in ids) _observationIds.Add(entry.Key, entry.Value);
        return this;
    }

    /// <summary>Assigns a unique identity to an existing raw sample group.</summary>
    public ChartSeries WithBoxPlotGroupId(int groupIndex, ChartObservationId id) {
        RequireRawBoxPlot(); _ = id.Value;
        if (groupIndex < 0 || groupIndex >= BoxPlotSourceGroups.Count) throw new ArgumentOutOfRangeException(nameof(groupIndex));
        foreach (var entry in _boxPlotGroupIds)
            if (entry.Key != groupIndex && entry.Value == id) throw new ArgumentException("Group IDs must be unique within a series.", nameof(id));
        _boxPlotGroupIds[groupIndex] = id; return this;
    }

    /// <summary>Atomically replaces raw group identities with exactly one unique identity per group.</summary>
    public ChartSeries WithBoxPlotGroupIds(IEnumerable<ChartObservationId> ids) {
        RequireRawBoxPlot();
        if (ids == null) throw new ArgumentNullException(nameof(ids));
        var candidate = ids.ToArray(); var unique = new HashSet<ChartObservationId>();
        if (candidate.Length != BoxPlotSourceGroups.Count) throw new ArgumentException("Supply exactly one identity per group.", nameof(ids));
        foreach (var id in candidate) { _ = id.Value; if (!unique.Add(id)) throw new ArgumentException("Group IDs must be unique within a series.", nameof(ids)); }
        _boxPlotGroupIds.Clear(); for (var index = 0; index < candidate.Length; index++) _boxPlotGroupIds.Add(index, candidate[index]);
        return this;
    }

    private void RequireRawBoxPlot() {
        RequireObservationKind(ChartSeriesKind.BoxPlot);
        if (!_isRawBoxPlot) throw new InvalidOperationException("This operation requires raw box-plot sample groups.");
    }

    private void InstallBoxPlotRaw(IReadOnlyList<ChartBoxPlotSampleGroup> groups, ChartBoxPlotDistribution[] results, ChartBoxPlotStatisticsOptions options) {
        var samples = groups.SelectMany(group => group.Samples).ToArray();
        var summaries = results.Select(result => result.Summary).ToArray();
        BoxPlotSourceGroups = groups; BoxPlotDistributions = Array.AsReadOnly(results);
        BoxPlotSourceSamples = Array.AsReadOnly(samples); _boxPlots = Array.AsReadOnly(summaries);
        _boxPlotStatistics = options; _isRawBoxPlot = true; _renderPoints = null;
        SourcePointCount = samples.Length; SourcePointIndices = Array.Empty<int>(); DecimationMode = null;
    }

    private void ClearBoxPlotRaw() {
        BoxPlotSourceGroups = Array.Empty<ChartBoxPlotSampleGroup>(); BoxPlotDistributions = Array.Empty<ChartBoxPlotDistribution>();
        BoxPlotSourceSamples = Array.Empty<double>(); _boxPlotGroupIds.Clear(); _isRawBoxPlot = false;
    }
}
