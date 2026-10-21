/// <summary>Builds the travel map with its original named locations and routes.</summary>
public static class TravelDottedMapRecipe {
    /// <summary>Creates the dark report map shared by the gallery and standalone download.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Dark) {
        if (variant != "wide") throw new ArgumentOutOfRangeException(nameof(variant));
        return Chart.Create()
            .WithTitle("Travel Map")
            .WithSubtitle("Dotted world layer with highlighted longitude and latitude points")
            .WithTheme(VisualTheme.Report(), mode)
            .WithSize(980, 560)
            .WithLegend(false)
            .WithDataLabels()
            .AddDottedMap("Visited", new[] {
                new ChartMapPoint("Indonesia", 113.9213, -0.7893, ChartColor.FromRgb(34, 197, 94)),
                new ChartMapPoint("Spain", -3.7038, 40.4168, ChartColor.FromRgb(34, 197, 94)),
                new ChartMapPoint("United States", -98.5795, 39.8283, ChartColor.FromRgb(34, 197, 94)),
                new ChartMapPoint("Norway", 8.4689, 60.4720, ChartColor.FromRgb(59, 130, 246))
            })
            .AddMapRouteBetweenPoints("United States to Spain", "United States", "Spain", ChartColor.FromRgb(34, 197, 94))
            .AddMapRouteBetweenPoints("Spain to Indonesia", "Spain", "Indonesia", ChartColor.FromRgb(59, 130, 246));
    }
}
