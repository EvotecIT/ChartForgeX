using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Captures one shared Bubble size caption/format policy for marks, readouts and the measured numeric key.</summary>
internal sealed class ChartSizePresentation {
    private readonly ChartValueFormat _format;
    private readonly Dictionary<double, string> _values = new();

    internal ChartSizePresentation(Chart chart) {
        var options = chart.Options.Bubble.SizeDimension;
        Name = options.Name ?? chart.Options.Labels.Size;
        HasName = options.Name != null; HasFormat = options.ValueFormat != null;
        ShowLegend = options.ShowLegend;
        _format = options.ValueFormat ?? chart.Options.ValueFormat;
    }

    internal string Name { get; }
    internal bool HasName { get; }
    internal bool HasFormat { get; }
    internal bool ShowLegend { get; }

    internal string Value(double value) {
        if (!_values.TryGetValue(value, out var text)) {
            text = _format.Format(value); _values.Add(value, text);
        }
        return text;
    }

    internal string AddFacts(Dictionary<string, string> metadata, double value, string description) {
        metadata["data-cfx-label-size"] = Name;
        metadata["data-cfx-formatted-size"] = Value(value);
        return HasName || HasFormat ? description + "; " + Name + ": " + Value(value) : description;
    }
}
