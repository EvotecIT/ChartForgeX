/// <summary>Uses an explicit antimeridian point/route window while retaining raw source bearings and identities.</summary>
public static class DatelineMapRecipe {
    /// <summary>Creates compact/wide examples in either existing geographic projection.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "mercator" or "compact-mercator")) throw new ArgumentOutOfRangeException(nameof(variant));
        var width = variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 800;
        var projection = variant.EndsWith("mercator", StringComparison.Ordinal) ? ChartMapGeoJsonProjection.WebMercator : ChartMapGeoJsonProjection.Equirectangular;
        var window = ChartMapViewport.AcrossAntimeridian("Pacific service window", 170, -170, -30, 10, projection);
        var series = ChartSeries.CreateDottedMap(new ChartSeriesId("pacific"), "Source locations", new[] {
            new ChartMapPoint("Fiji", 178, -18, 10), new ChartMapPoint("Samoa", -172, -14, 20),
            new ChartMapPoint("Seam east", 180, -10, 30), new ChartMapPoint("Seam west", -180, -10, 40),
            new ChartMapPoint("Outside this window", 160, -16, 50)
        }).WithMarkerRadius(4).WithObservationIds(new[] {
            new ChartObservationId("fiji"), new ChartObservationId("samoa"), new ChartObservationId("seam-east"), new ChartObservationId("seam-west"), new ChartObservationId("outside")
        }).ReplaceMapConnectors(new[] {
            new ChartMapConnector("Across the seam", 178, -18, -172, -14),
            new ChartMapConnector("Clipped window-edge route", new[] { new ChartMapPoint("West outside", 168, -22), new ChartMapPoint("Inside", 175, -20), new ChartMapPoint("East outside", -168, -18) }),
            new ChartMapConnector("Opposite meridian is outside", -10, 0, 10, 0)
        });
        return Chart.Create().WithSize(width, 520).WithTheme(VisualTheme.Graphite(), mode).WithLegend().WithDataLabels()
            .WithTitle("One window across the dateline").WithSubtitle("Raw bearings and IDs remain intact; native curves are clipped to the view")
            .WithMapViewport(window).AddSeries(series);
    }
}
