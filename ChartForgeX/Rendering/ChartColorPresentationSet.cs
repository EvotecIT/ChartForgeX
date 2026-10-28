using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Captures retained logical color facts and each series' independent domain once per preparation.</summary>
internal sealed class ChartColorPresentationSet {
    private readonly Chart _chart;
    private readonly ChartSeriesColorSurface?[] _series;
    private readonly VisualMapCompiler.MapDataset?[] _maps;
    private readonly Dictionary<ChartValueFormat, Dictionary<double, string>> _values = new();
    private ChartHierarchySurface? _hierarchy;
    private ChartFlowColorSurface? _chord, _sankey;
    internal ChartColorPresentationSet(Chart chart) {
        _chart = chart;
        var labels = chart.Options.Labels;
        Map = new ChartColorPresentation(chart.Options.MapColorDimension, labels, labels.Value, chart.Options.ValueFormat, preparation: this);
        Heatmap = new ChartColorPresentation(chart.Options.HeatmapColorDimension, labels, labels.Value, chart.Options.ValueFormat, preparation: this);
        var hierarchy = chart.Series.FirstOrDefault()?.Kind == ChartSeriesKind.Sunburst ? chart.Options.Sunburst.ColorDimension : chart.Options.Treemap.ColorDimension;
        Hierarchy = new ChartColorPresentation(hierarchy, labels, labels.Color, chart.Options.ValueFormat, preparation: this);
        Chord = new ChartColorPresentation(chart.Options.Chord.ColorDimension, labels, labels.Color,
            ChartValueFormat.InvariantCompact, compactDefault: true, preparation: this);
        Sankey = new ChartColorPresentation(chart.Options.Sankey.ColorDimension, labels, labels.Color,
            ChartValueFormat.InvariantCompact, compactDefault: true, preparation: this);
        _series = chart.Series.Select(series => series.Kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble
            ? new ChartSeriesColorSurface(series, new ChartColorPresentation(series.ColorDimension, labels, labels.Color, ChartValueFormat.InvariantCompact, compactDefault: true, preparation: this)) : null).ToArray();
        _maps = new VisualMapCompiler.MapDataset?[chart.Series.Count];
    }
    internal ChartSeriesColorSurface? this[int index] => _series[index];
    /// <summary>Shares the same captured map observations and formatting cache across legends, keys and marks.</summary>
    internal VisualMapCompiler.MapDataset MapDataset(int index) => _maps[index] ??= new VisualMapCompiler.MapDataset(_chart, index,
        new ChartColorPresentation(_chart.Series[index].ColorDimension.HasOverrides ? _chart.Series[index].ColorDimension : _chart.Options.MapColorDimension,
            _chart.Options.Labels, _chart.Options.Labels.Value, _chart.Options.ValueFormat, preparation: this));
    internal ChartColorPresentation Map { get; }
    internal ChartColorPresentation Heatmap { get; }
    internal ChartColorPresentation Hierarchy { get; }
    internal ChartColorPresentation Chord { get; }
    internal ChartColorPresentation Sankey { get; }
    internal ChartHierarchySurface HierarchySurface(VisualThemeColors colors) =>
        _hierarchy ??= new ChartHierarchySurface(_chart, colors, Hierarchy);
    internal ChartFlowColorSurface ChordSurface(VisualThemeColors colors) =>
        _chord ??= new ChartFlowColorSurface(_chart.Series[0], colors, Chord, _chart.Options.Chord.ColorScale);
    internal ChartFlowColorSurface SankeySurface(VisualThemeColors colors) =>
        _sankey ??= new ChartFlowColorSurface(_chart.Series[0], colors, Sankey, _chart.Options.Sankey.ColorScale,
            enabled: _chart.Options.Sankey.FlowColorMode == ChartSankeyFlowColorMode.Quantitative, inferEmptyScale: true);
    internal Dictionary<double, string> Values(ChartValueFormat format) {
        if (!_values.TryGetValue(format, out var values)) _values.Add(format, values = new Dictionary<double, string>());
        return values;
    }
}

/// <summary>A detached color dimension; manual paint and marker visibility do not alter its inferred extent.</summary>
internal sealed class ChartSeriesColorSurface {
    private readonly double?[] _values;
    private readonly bool _bubble;

    internal ChartSeriesColorSurface(ChartSeries series, ChartColorPresentation presentation) {
        Scale = series.ColorScale; Presentation = presentation; SeriesName = series.Name;
        _bubble = series.Kind == ChartSeriesKind.Bubble;
        var stride = _bubble ? 2 : 1;
        _values = Enumerable.Range(0, series.RenderPoints.Count / stride).Select(index => series.RenderPoints[index * stride].ColorValue).ToArray();
        var values = _values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        HasValues = values.Length > 0; Missing = values.Length < _values.Length;
        Minimum = HasValues ? values.Min() : 0; Maximum = HasValues ? values.Max() : 0;
    }
    internal ChartColorScale? Scale { get; }
    internal ChartColorPresentation Presentation { get; }
    internal string SeriesName { get; }
    internal int Count => _values.Length;
    internal bool HasValues { get; }
    internal bool Missing { get; }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal bool HasDomain => ChartColorScaleSurface.HasDomain(Scale, HasValues ? Minimum : null, HasValues ? Maximum : null);

    internal (ChartColor Color, SvgPaint Paint) Paint(ChartSeries series, int seriesIndex, int point, VisualThemeColors colors) {
        if (Scale != null && !(point < series.PointColors.Count && series.PointColors[point].HasValue)) {
            var blend = _values[point].HasValue ? Scale.BlendFor(_values[point]!.Value, Minimum, Maximum) : ChartColorScaleSurface.NoData(Scale, colors);
            return (blend.Color, blend.Paint);
        }
        var color = ChartSeriesColours.Point(series, seriesIndex, point, colors);
        return (color, VisualChartPaint.Series(series, color, point));
    }

    internal string AddFacts(Dictionary<string, string> metadata, int point, string description) {
        if (Scale == null && !_values[point].HasValue) return description;
        metadata["data-cfx-label-color"] = Presentation.Name;
        metadata["data-cfx-label-no-data"] = Presentation.NoData;
        if (_values[point] is double value) {
            metadata["data-cfx-color-value"] = VisualStateSceneTools.Number(value);
            if (Presentation.HasFormat) metadata["data-cfx-formatted-color-value"] = Presentation.Value(value);
            description += "; " + Presentation.Name + ": " + Presentation.Value(value);
        } else if (Scale != null) {
            metadata["data-cfx-color-missing"] = "true";
            description += "; " + Presentation.Name + ": " + Presentation.NoData;
        }
        return description;
    }
}
