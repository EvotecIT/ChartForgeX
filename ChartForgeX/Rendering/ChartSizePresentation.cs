using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Captures one shared Bubble size caption/format policy for marks, readouts and the measured numeric key.</summary>
internal sealed class ChartSizePresentation {
    private readonly ChartValueFormat _format;
    private readonly string _mapping;
    private readonly Dictionary<double, string> _values = new();

    internal ChartSizePresentation(Chart chart, double minimumValue, double maximumValue, string mapping) {
        _mapping = mapping;
        var options = chart.Options.Bubble.SizeDimension;
        Name = options.Name ?? chart.Options.Labels.Size;
        HasName = options.Name != null; HasFormat = options.ValueFormat != null;
        ShowLegend = options.ShowLegend;
        _format = options.ValueFormat ?? chart.Options.ValueFormat;
        if (ReferenceEquals(_format, ChartValueFormat.ExistingValue)) CaptureDefaultValues(chart, minimumValue, maximumValue);
    }

    internal string Name { get; }
    internal bool HasName { get; }
    internal bool HasFormat { get; }
    internal bool ShowLegend { get; }

    // Refine one batch before key measurement or point readouts can capture an ambiguous default caption.
    // Authored formats keep their exact output and retain the existing per-value, capture-once path.
    private void CaptureDefaultValues(Chart chart, double minimumValue, double maximumValue) {
        var values = new HashSet<double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bubble) continue;
            for (var index = 1; index < series.RenderPoints.Count; index += 2) values.Add(series.RenderPoints[index].Y);
        }
        if (values.Count == 0) return;
        if (ShowLegend) {
            values.Add(minimumValue); values.Add(maximumValue);
            values.Add(ChartMath.InterpolateRange(minimumValue, maximumValue, .5));
        }
        var facts = values.ToArray();
        var captions = ChartNumericFormatter.FormatScaleValues(_format, facts);
        for (var index = 0; index < facts.Length; index++) _values.Add(facts[index], captions[index]);
    }

    internal string Value(double value) {
        if (!_values.TryGetValue(value, out var text)) {
            text = _format.Format(value); _values.Add(value, text);
        }
        return text;
    }

    internal string AddFacts(Dictionary<string, string> metadata, double value, string description) {
        metadata["data-cfx-label-size"] = Name;
        metadata["data-cfx-formatted-size"] = Value(value);
        metadata["data-cfx-size-mapping"] = _mapping;
        return HasName || HasFormat ? description + "; " + Name + ": " + Value(value) : description;
    }
}
