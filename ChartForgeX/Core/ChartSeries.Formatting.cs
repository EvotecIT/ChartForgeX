using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private Func<ChartValueContext, string?>? _dataLabelFormatter;
    private Func<ChartValueContext, string?>? _tooltipFormatter;
    private IReadOnlyList<string?>? _preparedLabels;
    private IReadOnlyList<string?>? _preparedTooltips;
    private IReadOnlyList<string?>? _preparedRelationshipLabels;
    private IReadOnlyList<string?>? _preparedRelationshipTooltips;

    /// <summary>Sets the per-observation data-label formatter. Null clears the callback.</summary>
    /// <remarks>Prepare invokes the callback once per authored observation and relationship link, including hidden captions.
    /// A null result retains the authored label or family default; an empty result suppresses the caption.
    /// Prepared exports reuse captured strings. Exceptions propagate from Prepare without changing authored labels.
    /// Keep callbacks pure and captured configuration immutable.</remarks>
    /// <param name="formatter">The formatter, or null to restore authored labels and family defaults.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithDataLabelFormatter(Func<ChartValueContext, string?>? formatter) {
        _dataLabelFormatter = formatter; return this;
    }

    /// <summary>Sets the per-observation plain-text tooltip formatter. Null clears the callback.</summary>
    /// <remarks>Prepare invokes the callback once per authored observation and relationship link. Null retains the existing tooltip;
    /// empty suppresses it. Captured text is escaped by static SVG export and reused by the optional HTML adapter.
    /// The formatter does not change raw values, accessible labels or observation identity.</remarks>
    /// <param name="formatter">The formatter, or null to restore existing tooltip behavior.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithTooltipFormatter(Func<ChartValueContext, string?>? formatter) {
        _tooltipFormatter = formatter; return this;
    }

    internal bool HasFormatters => _dataLabelFormatter != null || _tooltipFormatter != null;
    internal IReadOnlyList<string?> PreparedLabels => _preparedLabels ?? PointLabels;
    internal IReadOnlyList<string?> CapturedLabels => _preparedLabels ?? Array.Empty<string?>();
    internal IReadOnlyList<string?> PreparedTooltips => _preparedTooltips ?? Array.Empty<string?>();
    internal IReadOnlyList<string?> PreparedRelationshipLabels => _preparedRelationshipLabels ?? Array.Empty<string?>();
    internal IReadOnlyList<string?> PreparedRelationshipTooltips => _preparedRelationshipTooltips ?? Array.Empty<string?>();
    internal string? PreparedTooltip(int index) => index >= 0 && _preparedTooltips != null && index < _preparedTooltips.Count
        ? _preparedTooltips[index] : null;
    internal string? PreparedLabel(int index) => index >= 0 && index < PreparedLabels.Count ? PreparedLabels[index] : null;
    internal static object? CopyFormattingDatum(object? datum) => datum is ChartRadialLayer layer ? CopyRadialLayer(layer) : datum;

    // The chart owns the detached series copy. Never edit the shallow copy's shared authored collections.
    internal void ResolveFormatting(Chart chart) {
        if (!HasFormatters) return;
        var labels = _dataLabelFormatter == null ? null : new string?[Math.Max(ObservationCount, PointLabels.Count)];
        if (labels != null) for (var index = 0; index < PointLabels.Count; index++) labels[index] = PointLabels[index];
        var tooltips = _tooltipFormatter == null ? null : new string?[ObservationCount];
        var contexts = new ChartValueContexts(chart, this);
        var info = new ChartSeriesInfo(this);
        for (var index = 0; index < ObservationCount; index++) {
            var datum = ObservationDatum(index);
            var context = contexts.Create(info, index, datum);
            if (labels != null) labels[index] = _dataLabelFormatter!(context) ?? labels[index];
            if (tooltips != null) tooltips[index] = _tooltipFormatter!(context);
        }
        var relationshipCount = RelationshipObservationCount;
        var relationshipLabels = _dataLabelFormatter == null ? null : new string?[relationshipCount];
        var relationshipTooltips = _tooltipFormatter == null ? null : new string?[relationshipCount];
        for (var index = 0; index < relationshipCount; index++) {
            var datum = RelationshipDatum(index);
            var context = contexts.CreateRelationship(info, index, datum);
            if (relationshipLabels != null) relationshipLabels[index] = _dataLabelFormatter!(context);
            if (relationshipTooltips != null) relationshipTooltips[index] = _tooltipFormatter!(context);
        }
        _preparedLabels = labels == null ? null : Array.AsReadOnly(labels);
        _preparedTooltips = tooltips == null ? null : Array.AsReadOnly(tooltips);
        _preparedRelationshipLabels = relationshipLabels == null ? null : Array.AsReadOnly(relationshipLabels);
        _preparedRelationshipTooltips = relationshipTooltips == null ? null : Array.AsReadOnly(relationshipTooltips);
        _dataLabelFormatter = null;
        _tooltipFormatter = null;
    }
}
