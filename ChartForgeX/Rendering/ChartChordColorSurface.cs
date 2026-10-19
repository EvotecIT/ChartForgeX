using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Captures every authored flow's independent color fact without consulting weight or paint overrides.</summary>
internal sealed class ChartChordColorSurface {
    private readonly double?[] _values;

    internal ChartChordColorSurface(Chart chart, VisualThemeColors colors, ChartColorPresentation presentation) {
        Presentation = presentation;
        _values = chart.Series[0].FlowLinks.Select(link => link.ColorValue).ToArray();
        var values = _values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        HasValues = values.Length > 0;
        HasMissing = values.Length < _values.Length;
        Minimum = HasValues ? values.Min() : 0;
        Maximum = HasValues ? values.Max() : 0;
        Scale = chart.Options.Chord.ColorScale ?? (HasValues ? ChartColorScaleSurface.Default(colors) : null);
    }

    internal ChartColorPresentation Presentation { get; }
    internal ChartColorScale? Scale { get; }
    internal bool HasValues { get; }
    internal bool HasMissing { get; }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal bool HasDomain => ChartColorScaleSurface.HasDomain(Scale, HasValues ? Minimum : null, HasValues ? Maximum : null);

    internal ChartColorBlend? Blend(int index, VisualThemeColors colors) => Scale == null ? null
        : _values[index] is double value ? Scale.BlendFor(value, Minimum, Maximum) : ChartColorScaleSurface.NoData(Scale, colors);

    internal string AddFacts(Dictionary<string, string> metadata, int index, string description) {
        if (Scale == null) return description;
        metadata["data-cfx-label-color"] = Presentation.Name;
        metadata["data-cfx-label-no-data"] = Presentation.NoData;
        if (_values[index] is double value) {
            var formatted = Presentation.Value(value);
            metadata["data-cfx-color-value"] = VisualStateSceneTools.Number(value);
            metadata["data-cfx-formatted-color-value"] = formatted;
            return description + "; " + Presentation.Name + ": " + formatted;
        }
        metadata["data-cfx-color-missing"] = "true";
        return description + "; " + Presentation.Name + ": " + Presentation.NoData;
    }
}
