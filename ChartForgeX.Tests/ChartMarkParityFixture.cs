using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static class ChartMarkParityFixture {
    internal static Chart Create(string family, byte alpha = 255, int density = 1) {
        var ink = ChartColor.FromRgba(30, 80, 170, alpha);
        var theme = ChartTheme.Light();
        theme.Background = theme.CardBackground = theme.PlotBackground = ChartColor.Transparent;
        theme.Positive = theme.Negative = theme.Warning = theme.Text = theme.MutedText = ink;
        theme.Palette = new[] { ink, ChartColor.FromRgba(170, 70, 30, alpha) };
        var chart = Chart.Create().WithSize(400, 280).WithTheme(theme).WithCard(false).WithPlotBackground(false).WithLegend(false);
        chart.Options.ShowAxes = chart.Options.ShowGrid = chart.Options.ShowDataLabels = false;
        chart.Options.PngOutputScale = density;
        chart.Options.XAxis.Minimum = 0; chart.Options.XAxis.Maximum = 4;
        chart.Options.YAxis.Minimum = 0; chart.Options.YAxis.Maximum = 10;
        switch (family) {
            case "error": chart.AddErrorBar("Range", new[] { new ChartErrorBar(2, 5, 2, 8) }, ink); break;
            case "box": chart.AddBoxPlot("Distribution", new[] { new ChartBoxPlot(2, 1, 3, 5, 7, 9) }, ink); break;
            case "candle": chart.AddCandlestick("Price", new[] { new ChartCandlestick(2, 3, 9, 1, 7) }, ink); break;
            case "ohlc": chart.AddOhlc("Price", new[] { new ChartCandlestick(2, 3, 9, 1, 7) }, ink); break;
            case "dumbbell": chart.AddDumbbell("Change", new[] { new ChartDumbbell(2, 2, 8) }, ink); break;
            case "band": chart.AddRangeBand("Interval", new[] { new ChartRangeBand(1, 2, 8), new ChartRangeBand(3, 2, 8) }, ink); break;
            case "range": chart.AddRangeBar("Interval", new[] { new ChartInterval(2, 2, 8) }, ink); break;
            case "waterfall": chart.AddWaterfall("Changes", new[] { new ChartPoint(1, 5), new ChartPoint(2, -2) }, ink); break;
            case "bullet": chart.AddBullet("Target", 55, 75, 0, 100, new[] { 60d, 80d }, ink); break;
            case "hatch": chart.AddBar("Value", new[] { new ChartPoint(2, 8) }, ink); chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch); break;
            case "pie": chart.AddPie("Parts", new[] { new ChartPoint(0, 1), new ChartPoint(1, 1) }); theme.CardBackground = ChartColor.FromRgba(240, 245, 250, alpha); break;
            case "donut": chart.AddDonut("Parts", new[] { new ChartPoint(0, 1), new ChartPoint(1, 1) }); theme.CardBackground = ChartColor.FromRgba(240, 245, 250, alpha); chart.Options.ShowDonutCenterLabel = false; break;
            case "pie-full": chart.AddPie("Whole", new[] { new ChartPoint(0, 1) }); theme.CardBackground = ChartColor.FromRgba(240, 245, 250, alpha); break;
            case "pie-offset": chart.AddPie("Parts", new[] { new ChartPoint(0, 1), new ChartPoint(1, 3) }); chart.Series[0].WithPointSliceOffset(0, 0.12); theme.CardBackground = ChartColor.FromRgba(240, 245, 250, alpha); break;
            case "polar": chart.AddPolarArea("Parts", new[] { new ChartPoint(0, 1), new ChartPoint(1, 4), new ChartPoint(2, 2) }); theme.CardBackground = ChartColor.FromRgba(240, 245, 250, alpha); break;
            case "polar-zero": chart.AddPolarArea("Parts", new[] { new ChartPoint(0, 0), new ChartPoint(1, 4), new ChartPoint(2, 2) }); theme.CardBackground = ChartColor.FromRgba(240, 245, 250, alpha); break;
            case "sunburst": chart.AddSunburst("Hierarchy", new[] { new ChartHierarchyItem("Root", "Root"), new ChartHierarchyItem("A", "A", "Root", 3), new ChartHierarchyItem("B", "B", "Root", 2), new ChartHierarchyItem("C", "C", "A", 2), new ChartHierarchyItem("D", "D", "A", 1) }); theme.CardBackground = ChartColor.FromRgba(240, 245, 250, alpha); break;
            default: throw new ArgumentException("Unknown fixture family.", nameof(family));
        }
        foreach (var series in chart.Series) series.ShowDataLabels = false;
        return chart;
    }
}
