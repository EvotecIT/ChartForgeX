using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Captures retained logical color facts and each series' independent domain once per preparation.</summary>
internal sealed class ChartSeriesColorScaleSet {
    private readonly ChartSeriesColorSurface?[] _series;
    internal ChartSeriesColorScaleSet(Chart chart) {
        _series = chart.Series.Select(series => series.Kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble
            ? new ChartSeriesColorSurface(series, chart.Options.Labels) : null).ToArray();
    }
    internal ChartSeriesColorSurface? this[int index] => _series[index];
}

/// <summary>A detached color dimension; manual paint and marker visibility do not alter its inferred extent.</summary>
internal sealed class ChartSeriesColorSurface {
    private readonly double?[] _values;
    private readonly string _colorLabel, _noDataLabel;
    internal ChartSeriesColorSurface(ChartSeries series, ChartLabels labels) {
        Scale = series.ColorScale; _colorLabel = labels.Color; _noDataLabel = labels.NoData;
        var stride = series.Kind == ChartSeriesKind.Bubble ? 2 : 1;
        _values = Enumerable.Range(0, series.Points.Count / stride).Select(index => series.Points[index * stride].ColorValue).ToArray();
        var values = _values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        HasValues = values.Length > 0; Missing = values.Length < _values.Length;
        Minimum = HasValues ? values.Min() : 0; Maximum = HasValues ? values.Max() : 0;
    }
    internal ChartColorScale? Scale { get; }
    internal int Count => _values.Length;
    internal bool HasValues { get; }
    internal bool Missing { get; }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal bool HasDomain => HasValues || Scale?.MinimumValue.HasValue == true || Scale?.Mode == ChartColorScaleMode.Discrete;

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
        metadata["data-cfx-label-color"] = _colorLabel;
        metadata["data-cfx-label-no-data"] = _noDataLabel;
        if (_values[point] is double value) {
            metadata["data-cfx-color-value"] = VisualStateSceneTools.Number(value);
            description += "; " + _colorLabel + ": " + ChartNumericFormatter.FormatCompactAxisValue(value);
        } else if (Scale != null) {
            metadata["data-cfx-color-missing"] = "true";
            description += "; " + _colorLabel + ": " + _noDataLabel;
        }
        return description;
    }
}
