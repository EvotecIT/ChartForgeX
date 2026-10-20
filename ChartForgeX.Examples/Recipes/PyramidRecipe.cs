/// <summary>Builds a triangular allocation chart from ordered service values.</summary>
public static class PyramidRecipe {
    /// <summary>Creates the selected pyramid example, including area encoding and geometric reversal.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options" or "dense" or "compact-dense"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create().WithDataLabels();
        if (variant is "dense" or "compact-dense") {
            chart.WithTitle("Allocation by service")
                .WithXLabels(Enumerable.Range(1, 16).Select(index => "Regional service " + index).ToArray())
                .AddPyramid("Allocation", Enumerable.Range(1, 16).Select(index => new ChartPoint(index, 17 - index)));
        }
        else if (variant is "options" or "compact-options") {
            var allocation = new ChartSeries(new ChartSeriesId("allocation"), "Allocation", ChartSeriesKind.Pyramid, new[] {
                new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20), new ChartPoint(4, 0)
            }).WithPointColor(0, "#2F78C4").WithPointColor(1, "#E2A644").WithPointColor(2, "#34957A")
                .WithPointFillPattern(1, ChartFillPattern.DiagonalForward);
            chart.WithXLabels("Services", "Platform", "Support", "Unassigned").AddSeries(allocation)
                .ConfigurePyramid(options => {
                    options.ValueEncoding = ChartPyramidValueEncoding.Area;
                    options.Orientation = ChartOrientation.Horizontal;
                    options.Reversed = true;
                    options.AspectRatio = .75;
                });
        }
        else {
            var allocation = new ChartSeries(new ChartSeriesId("allocation"), "Allocation", ChartSeriesKind.Pyramid,
                new[] { new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20) });
            chart.WithXLabels("Services", "Platform", "Support").AddSeries(allocation);
        }
        return chart.WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}
