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
        // A mutable override is cloned for preparation, but direct reusable layout queries
        // may edit it between calls. Re-measure styled headers instead of caching an incomplete key.
        if (group.TextStyle?.HasOverrides == true || group.LabelContent.IsStructured) return Measure(chart, group, options, _measurement);
        var key = new HeaderKey(group, options);
        if (_parts.TryGetValue(group, out var entry) && entry.Key.Equals(key)) return entry.Parts;
        var parts = Measure(chart, group, options, _measurement);
        _parts[group] = new Entry(key, parts);
        return parts;
    }

    private static IReadOnlyList<ChartRect> Measure(TopologyChart chart, TopologyGroup group, TopologyRenderOptions options,
        TextMeasurementContext measurement) {
        var parts = new List<ChartRect>();
        if (group.ShowLabel) {
            var size = TopologyTextPolicy.Size(group.TextStyle, chart.Theme.Typography.DataLabelSize, chart.Theme.Typography.DataLabelSize);
            var titleLineHeight = group.LabelContent.IsStructured ? TextLayoutEngine.Measure(group.LabelContent, StructuredStyle(size)).LineHeight : Measure("Ag", size, 600).LineHeight;
            var subtitleLineHeight = Measure("Ag", size * .85, 400).LineHeight;
            var titleRows = TopologyGroupHeader.CaptionRows(group.Label, options);
            var subtitleRows = TopologyGroupHeader.CaptionRows(group.Subtitle, options);
            var fitTitle = group.TextStyle?.HasOverrides != true && titleRows == 1 && (size > VisualTextFitting.MaximumOrdinaryFontSize || titleLineHeight > TopologyGroupHeader.TitleHeight);
            var fitSubtitle = group.TextStyle?.HasOverrides != true && subtitleRows == 1 && (size > VisualTextFitting.MaximumOrdinaryFontSize || subtitleLineHeight > TopologyGroupHeader.SubtitleHeight);
            if (group.LabelContent.IsStructured) {
                var allocation = TopologyGroupHeader.TitleBounds(group, options, titleLineHeight);
                var style = StructuredStyle(size);
                var content = TopologyTextLayout.DisplayRows(group.LabelContent, options.AllowMultilineNodeLabels);
                if (fitTitle) style.FontSize = ChartForgeX.Rendering.ChartTextFitting.FitFontSize(Math.Min(size, DotNodeSymbolFontSize), size,
                    candidate => { var probe = style.Clone(); probe.FontSize = candidate; var measured = TextLayoutEngine.Measure(content, probe); return measured.Width <= allocation.Width && measured.Height <= allocation.Height; });
                var metrics = TextLayoutEngine.Measure(content, style);
                var rows = Math.Min(titleRows, (int)Math.Floor(allocation.Height / metrics.LineHeight));
                if (rows > 0 && allocation.Width > 0) {
                    var layout = TextLayoutEngine.Layout(content, allocation.Width, style,
                        !fitTitle && options.WrapNodeLabels ? TextWrapMode.Word : TextWrapMode.NoWrap, rows);
                    if (layout.Metrics.Width > 0) parts.Add(new ChartRect(allocation.X, allocation.Y, layout.Metrics.Width, layout.Metrics.Height));
                }
            } else AddCaption(group.Label, TopologyGroupHeader.TitleBounds(group, options, titleLineHeight), size, 600, fitTitle, titleRows);
            if (!string.IsNullOrWhiteSpace(group.Subtitle))
                AddCaption(group.Subtitle!, TopologyGroupHeader.SubtitleBounds(group, options, fitTitle || fitSubtitle, titleLineHeight, subtitleLineHeight), size * .85, 400, fitSubtitle, subtitleRows);
        }
        if (TopologyGroupHeader.SymbolReserve(group, options) > 0) parts.Add(TopologyGroupHeader.SymbolBounds(group));
        if (options.IncludeGroupStatusDots) parts.Add(TopologyGroupHeader.StatusBounds(group));
        return Array.AsReadOnly(parts.ToArray());

        TextStyle StructuredStyle(double size) {
            var style = TopologyTextPolicy.ResolveStructured(group.TextStyle, measurement.Font, size);
            style.FontSize = size; return style;
        }
        TextMetrics Measure(string value, double size, int weight) {
            if (group.TextStyle == null || !group.TextStyle.HasOverrides) return measurement.MeasureMetrics(value, size, weight);
            var style = TopologyTextPolicy.Resolve(group.TextStyle, measurement.Font, size, weight);
            return TopologyTextPolicy.Measurement(group.TextStyle, measurement, weight).MeasureMetrics(value, size, style.Font.Weight);
        }
        void AddCaption(string value, ChartRect allocation, double preferred, int weight, bool fit, int rows) {
            var layout = TopologyTextLayout.Prepare(value, allocation.Width, allocation.Height, preferred, weight, rows,
                fit, DotNodeSymbolFontSize, options, Measure);
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
            bool Status, bool Multiline, bool Wrap, bool ShowLabel) _value;
        internal HeaderKey(TopologyGroup group, TopologyRenderOptions options) => _value =
            (group.X, group.Y, group.Width, group.Label, group.Subtitle, TopologyGroupHeader.SymbolReserve(group, options),
                options.IncludeGroupStatusDots, options.AllowMultilineNodeLabels, options.WrapNodeLabels, group.ShowLabel);
        public bool Equals(HeaderKey other) => _value.Equals(other._value);
        public override bool Equals(object? value) => value is HeaderKey other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
    }
}
