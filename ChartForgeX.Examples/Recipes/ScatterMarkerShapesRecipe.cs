/// <summary>Shows the available scatter marker shapes with captions.</summary>
public static class ScatterMarkerShapesRecipe {
    /// <summary>Creates the selected example without choosing a renderer or export backend.</summary>
    public static Chart Create(string variant = "options", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("options" or "compact-options")) throw new ArgumentOutOfRangeException(nameof(variant));
        return MarkerShapes().WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }

    private static Chart MarkerShapes() {
        var chart = Chart.Create().WithXAxis("Sample position").WithYAxis("Example level");
        var shapes = Enum.GetValues<ChartMarkerShape>();
        for (var index = 0; index < shapes.Length; index++) {
            var shape = shapes[index];
            var sample = ChartSeries.CreateScatter(new ChartSeriesId("marker-" + shape.ToString().ToLowerInvariant()), shape.ToString(),
                new[] { new ChartPoint(index % 3 + 1, index / 3 + 1) });
            sample.WithPointLabel(0, shape.ToString()).WithDataLabels()
                .ConfigureMarkers(markers => { markers.Shape = shape; markers.Radius = 8; markers.StrokeWidth = 1; });
            chart.AddSeries(sample);
        }
        chart.Options.XAxis.WithBounds(0, 4);
        chart.Options.YAxis.WithBounds(0, 4);
        return chart;
    }
}
