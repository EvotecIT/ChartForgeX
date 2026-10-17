using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Retains header reservations for one layout pass. Portable reservations never resolve a host face;
/// the compiler separately captures the final painted rectangles for detached scene consumers.
/// </summary>
internal sealed class TopologyGroupHeaderLayout {
    private readonly Dictionary<TopologyGroup, Entry> _parts = new();
    private TextMeasurementContext? _sourceMeasurement;
    private TextMeasurementContext? _measurement;
    private VisualTheme? _theme;
    private TextMeasurementMode _mode;

    internal IReadOnlyList<ChartRect> Parts(TopologyChart chart, TopologyGroup group, TopologyRenderOptions options) {
        if (_measurement == null || !ReferenceEquals(_sourceMeasurement, chart.TextMeasurement)
            || !ReferenceEquals(_theme, chart.Theme) || _mode != options.TextMeasurementMode) {
            _sourceMeasurement = chart.TextMeasurement;
            _theme = chart.Theme;
            _mode = options.TextMeasurementMode;
            _measurement = _sourceMeasurement != null && _sourceMeasurement.Mode == _mode ? _sourceMeasurement
                : new TextMeasurementContext(_sourceMeasurement?.Font ?? _theme.Typography.ToFontSpec(), _mode);
            _parts.Clear();
        }
        var key = new HeaderKey(group, options);
        if (_parts.TryGetValue(group, out var entry) && entry.Key.Equals(key)) return entry.Parts;
        var parts = Measure(chart, group, options, _measurement);
        _parts[group] = new Entry(key, parts);
        return parts;
    }

    private static IReadOnlyList<ChartRect> Measure(TopologyChart chart, TopologyGroup group, TopologyRenderOptions options,
        TextMeasurementContext measurement) {
        var size = chart.Theme.Typography.DataLabelSize;
        var fitTitle = size > VisualTextFitting.MaximumOrdinaryFontSize || measurement.MeasureMetrics("Ag", size, 600).LineHeight > TopologyGroupHeader.TitleHeight;
        var fitSubtitle = size > VisualTextFitting.MaximumOrdinaryFontSize || measurement.MeasureMetrics("Ag", size * .85, 400).LineHeight > TopologyGroupHeader.SubtitleHeight;
        var parts = new List<ChartRect>();
        AddCaption(group.Label, TopologyGroupHeader.TitleBounds(group, options), size, 600, fitTitle);
        if (!string.IsNullOrWhiteSpace(group.Subtitle))
            AddCaption(group.Subtitle!, TopologyGroupHeader.SubtitleBounds(group, options, fitTitle || fitSubtitle), size * .85, 400, fitSubtitle);
        if (TopologyGroupHeader.SymbolReserve(group, options) > 0) parts.Add(TopologyGroupHeader.SymbolBounds(group));
        if (options.IncludeGroupStatusDots) parts.Add(TopologyGroupHeader.StatusBounds(group));
        return Array.AsReadOnly(parts.ToArray());

        void AddCaption(string value, ChartRect allocation, double preferred, int weight, bool fit) {
            var layout = TopologyTextLayout.Prepare(value, allocation.Width, allocation.Height, preferred, weight, 1,
                fit, DotNodeSymbolFontSize, options, measurement.MeasureMetrics);
            if (layout != null && layout.Width > 0 && layout.Height > 0)
                parts.Add(new ChartRect(allocation.X, allocation.Y, layout.Width, layout.Height));
        }
    }

    private sealed class Entry {
        internal Entry(HeaderKey key, IReadOnlyList<ChartRect> parts) { Key = key; Parts = parts; }
        internal HeaderKey Key { get; }
        internal IReadOnlyList<ChartRect> Parts { get; }
    }

    private readonly struct HeaderKey : IEquatable<HeaderKey> {
        private readonly (double X, double Y, double Width, string Label, string? Subtitle, double SymbolReserve,
            bool Status, bool Multiline, bool Wrap) _value;
        internal HeaderKey(TopologyGroup group, TopologyRenderOptions options) => _value =
            (group.X, group.Y, group.Width, group.Label, group.Subtitle, TopologyGroupHeader.SymbolReserve(group, options),
                options.IncludeGroupStatusDots, options.AllowMultilineNodeLabels, options.WrapNodeLabels);
        public bool Equals(HeaderKey other) => _value.Equals(other._value);
        public override bool Equals(object? value) => value is HeaderKey other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
    }
}
