using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Measured bullet row geometry consumed by both native marks and the shared text scene.</summary>
internal readonly struct ChartBulletLayout {
    private ChartBulletLayout(ChartRect content, ChartRect plot, double labelReserve, double valueReserve, int rows) {
        Content = content; Plot = plot; LabelReserve = labelReserve; ValueReserve = valueReserve;
        RowHeight = Math.Min(64, plot.Height / Math.Max(1, rows));
        BarHeight = Math.Max(16, Math.Min(26, RowHeight * 0.38));
    }
    internal ChartRect Content { get; }
    internal ChartRect Plot { get; }
    internal double LabelReserve { get; }
    internal double ValueReserve { get; }
    internal double RowHeight { get; }
    internal double BarHeight { get; }

    internal static ChartBulletLayout Create(Chart chart, ChartRect basePlot) {
        var widestLabel = 0d; var widestValue = 0d; var rows = 0; var labeled = false;
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bullet || series.Points.Count < 2) continue;
            rows++;
            if (series.ShowDataLabels == false) continue;
            labeled = true;
            var style = series.PointDataLabelStyles.Count > 0 && series.PointDataLabelStyles[0]?.HasOverrides == true ? series.PointDataLabelStyles[0]!
                : series.DataLabelStyle.HasOverrides ? series.DataLabelStyle : chart.Options.DataLabelStyle;
            double Width(string text, double size, int weight) {
                var resolved = ChartLabelScene.ResolveTextStyle(size, style.WithDefaultFontFamily(chart.Options.Theme.FontFamily), weight);
                resolved.Font.FilePath = chart.Options.PngFontPath; resolved.Font.CollectionIndex = chart.Options.PngFontCollectionIndex; resolved.Font.FaceName = chart.Options.PngFontFaceName;
                return ChartLabelScene.MeasureText(style.TransformText(text, System.Globalization.CultureInfo.InvariantCulture), resolved).Width;
            }
            widestLabel = Math.Max(widestLabel, Width(series.Name, style.FontSize ?? chart.Options.Theme.LegendFontSize, 700));
            var value = ChartNumericFormatter.FormatValue(chart.Options, series.Points[0].Y);
            widestValue = Math.Max(widestValue, Width(value, style.FontSize ?? chart.Options.Theme.DataLabelFontSize, 800));
        }
        var labelReserve = !labeled ? 10 : Math.Min(240, Math.Max(128, widestLabel + 34));
        var valueReserve = !labeled ? 12 : Math.Min(142, Math.Max(84, widestValue + 38));
        var inset = ChartVisualPrimitives.BulletContentInset;
        var content = new ChartRect(basePlot.X + inset, basePlot.Y + inset, Math.Max(1, basePlot.Width - inset * 2), Math.Max(1, basePlot.Height - inset * 2));
        var budget = Math.Max(0, content.Width - Math.Min(80, Math.Max(1, content.Width * 0.25)));
        var reserves = labelReserve + valueReserve;
        if (reserves > budget && reserves > 0) { labelReserve *= budget / reserves; valueReserve *= budget / reserves; }
        var plot = new ChartRect(content.X + labelReserve, content.Y + 18, Math.Max(1, content.Width - labelReserve - valueReserve), Math.Max(1, content.Height - 54));
        return new ChartBulletLayout(content, plot, labelReserve, valueReserve, rows);
    }
}
