using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Independent authored color observations; leaf sizes never supply a color domain.</summary>
internal sealed class ChartHierarchySurface {
    private readonly ChartSeries _series;
    private readonly VisualThemeColors _colors;
    internal ChartColorScale? Scale { get; }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal bool HasDomain { get; }
    internal bool HasMissing { get; }
    internal bool ShowLegend { get; }
    internal string? Title { get; }
    internal string RolePrefix { get; }

    internal ChartHierarchySurface(Chart chart, VisualThemeColors colors) {
        _series = chart.Series[0]; _colors = colors;
        var sunburst = _series.Kind == ChartSeriesKind.Sunburst;
        var configured = sunburst ? chart.Options.Sunburst.ColorScale : chart.Options.Treemap.ColorScale;
        ShowLegend = sunburst ? chart.Options.Sunburst.ShowColorScaleLegend : chart.Options.Treemap.ShowColorScaleLegend;
        Title = sunburst ? chart.Options.Sunburst.ColorLegendTitle : chart.Options.Treemap.ColorLegendTitle;
        RolePrefix = sunburst ? "sunburst" : "treemap";
        var observations = _series.HierarchyItems.Where(item => item.ColorValue.HasValue).Select(item => item.ColorValue!.Value).ToArray();
        Scale = configured ?? (observations.Length > 0 ? ChartColorScaleSurface.Default(colors) : null);
        Minimum = observations.Length > 0 ? observations.Min() : Scale?.MinimumValue ?? 0;
        Maximum = observations.Length > 0 ? observations.Max() : Scale?.MaximumValue ?? 0;
        HasDomain = observations.Length > 0 || (Scale?.MinimumValue.HasValue == true && Scale.MaximumValue.HasValue);
        HasMissing = _series.HierarchyItems.Any(item => !item.ColorValue.HasValue);
    }

    internal ChartColorBlend Blend(int index, int? paletteIndex = null) {
        if (index < _series.PointColors.Count && _series.PointColors[index] is ChartColor custom)
            return ChartColorBlend.Solid(custom, SvgColorRole.Series);
        if (Scale != null) return _series.HierarchyItems[index].ColorValue is double value
            ? Scale.BlendFor(value, Minimum, Maximum) : ChartColorScaleSurface.NoData(Scale, _colors);
        var color = ChartRelationshipPaint.Color(_series, index, _colors, paletteIndex);
        return ChartColorBlend.Solid(color, ChartRelationshipPaint.Role(_series, index));
    }
}
