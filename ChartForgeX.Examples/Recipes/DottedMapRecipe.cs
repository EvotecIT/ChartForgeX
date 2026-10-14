/// <summary>Creates a dotted map with service locations and routes across Europe.</summary>
public static class DottedMapRecipe {
    /// <summary>Creates the source model for a gallery variant without choosing an export backend.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));

        var chart = Chart.Create().WithDataLabels().WithMapViewport(ChartMapViewport.Europe())
            .AddDottedMap("Locations", new[] {
                new ChartMapPoint("Madrid", -3.7, 40.4, 18),
                new ChartMapPoint("Warsaw", 21, 52.2, 74),
                new ChartMapPoint("Oslo", 10.8, 59.9, 42)
            })
            .AddMapRouteBetweenPoints("Madrid to Warsaw", "Madrid", "Warsaw")
            .AddMapRoute("Warsaw to Oslo", new[] {
                new ChartMapPoint("Warsaw", 21, 52.2),
                new ChartMapPoint("Baltic", 17, 56),
                new ChartMapPoint("Oslo", 10.8, 59.9)
            })
            .WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
        if (variant is "options" or "compact-options") chart.WithPointLegend();
        return chart;
    }
}
