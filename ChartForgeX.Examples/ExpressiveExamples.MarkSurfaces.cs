using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static partial class ExpressiveExamples {
    private static ChartGrid CreateFunnelSurfaceShowcase() {
        var values = new[] { new ChartPoint(1, 80), new ChartPoint(2, 55), new ChartPoint(3, 30) };
        var ink = ChartColor.FromRgba(37, 99, 235, 144);
        var palette = MarkSurfaceChart("Palette · diagonal shading").WithXLabels("Visit", "Try", "Buy").AddFunnel("Stages", values);
        var series = MarkSurfaceChart("Series colour · solid fill").WithXLabels("Visit", "Try", "Buy").AddFunnel("Stages", values, ink);
        var points = MarkSurfaceChart("Point colours · shaded transparency").WithXLabels("Visit", "Try", "Buy").AddFunnel("Stages", values);
        points.Series[0].WithPointColor(0, ink).WithPointColor(1, ChartColor.FromRgba(234, 88, 12, 144)).WithPointColor(2, ChartColor.FromRgba(22, 163, 74, 144));
        return ChartGrid.Create().WithTitle("Funnel colour surfaces").WithSubtitle("Palette, series and point colours retain the same paint policy in SVG and PNG")
            .WithTheme(ChartTheme.ReportLight()).WithColumns(3).WithPanelSize(360, 280).WithPadding(24)
            .Add(palette).Add(series).Add(points);
    }

    private static ChartGrid CreateMarkSurfaceShowcase() {
        var color = ChartColor.FromRgba(37, 99, 235, 144);
        var box = MarkSurfaceChart("Distribution · translucent body").AddBoxPlot("Latency", new[] {
            new ChartBoxPlot(1, 12, 28, 43, 58, 76), new ChartBoxPlot(2, 18, 34, 51, 67, 91)
        }, color);
        var price = MarkSurfaceChart("Price · centered outlines").AddCandlestick("Price", new[] {
            new ChartCandlestick(1, 32, 74, 19, 63), new ChartCandlestick(2, 68, 88, 31, 42)
        });
        var error = MarkSurfaceChart("Confidence · range and caps").AddErrorBar("Estimate", new[] {
            new ChartErrorBar(1, 42, 21, 65), new ChartErrorBar(2, 64, 39, 85)
        }, color);
        var band = MarkSurfaceChart("Interval · uniform surface").AddRangeBand("Expected", new[] {
            new ChartRangeBand(1, 20, 62), new ChartRangeBand(2, 30, 74), new ChartRangeBand(3, 38, 88)
        }, color);
        var bars = MarkSurfaceChart("Crosshatch · chart-relative pattern").AddBar("Value", Points(42, 68, 81), color);
        bars.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
        var pie = MarkSurfaceChart("Parts · complete slice borders").AddPie("Parts", Points(35, 45, 20));
        pie.Options.Theme.Palette = new[] { color, ChartColor.FromRgba(234, 88, 12, 144), ChartColor.FromRgba(22, 163, 74, 144) };
        pie.Series[0].WithPointSliceOffset(0, 0.08);
        return ChartGrid.Create().WithTitle("Chart mark surfaces").WithSubtitle("Shared opacity, fills, hatching and closed outlines in SVG and PNG")
            .WithTheme(ChartTheme.ReportLight()).WithColumns(2).WithPanelSize(440, 280).WithPadding(24)
            .Add(box).Add(price).Add(error).Add(band).Add(bars).Add(pie);
    }

    private static Chart MarkSurfaceChart(string title) {
        var chart = Chart.Create().WithTitle(title).WithTheme(ChartTheme.ReportLight()).WithLegend(false).WithCard(false).WithDataLabels(false);
        chart.Options.ShowAxes = false;
        return chart;
    }
}
