/// <summary>Builds the shared dashboard trend model at its gallery or original download width.</summary>
public static class DashboardPremiumTrendRecipe {
    /// <summary>Creates the 920-pixel gallery chart or the 840-pixel standalone snippet.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "snippet")) throw new ArgumentOutOfRangeException(nameof(variant));
        return Chart.Create()
            .WithHeader(false)
            .WithTransparentBackground(false)
            .WithTheme(VisualTheme.SaasDashboard()
                .WithPalette(new[] { ChartColor.FromHex("#2563EB"), ChartColor.FromHex("#14B8A6") })
                .Configure(builder => { builder.Light.Grid = builder.Dark.Grid = ChartColor.FromHex("#D7DEE8"); builder.Light.Axis = builder.Dark.Axis = ChartColor.FromHex("#94A3B8"); })
                .Configure(builder => { builder.MarkerRadius = 3.2; })
                .Configure(builder => { builder.Light.Foreground = builder.Dark.Foreground = ChartColor.FromHex("#64748B"); builder.Light.MutedForeground = builder.Dark.MutedForeground = ChartColor.FromHex("#8792A5"); }), mode)
            .WithDashboardPanelStyle()
            .WithLegend(false)
            .WithPadding(36, 30, 26, 58)
            .WithSize(variant == "snippet" ? 840 : 920, 360)
            .WithXLabels("Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday")
            .WithYAxisBounds(0, 100)
            .WithHighlightedXAxisLabel(4, paletteIndex: 0)
            .AddSmoothArea("Saved", Points(24, 36, 48, 58, 46, 68, 76))
            .AddSmoothLine("Outreach", Points(18, 28, 36, 48, 40, 58, 66));
    }

    private static IEnumerable<ChartPoint> Points(params double[] values) =>
        values.Select((value, index) => new ChartPoint(index + 1, value));
}
