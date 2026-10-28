/// <summary>An ordinary native topology with a curved maintenance boundary, separate from graph facts.</summary>
public static class TopologyPathDecorationRecipe {
    /// <summary>Creates a script-free plan with two services, one relationship and an optional independent boundary.</summary>
    public static TopologyChart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "plain" or "compact-plain")) throw new ArgumentOutOfRangeException(nameof(variant));
        var compact = variant.StartsWith("compact", StringComparison.Ordinal);
        var includeBoundary = !variant.EndsWith("plain", StringComparison.Ordinal);
        var chart = TopologyChart.Create().WithId("maintenance-boundary").WithTitle("Maintenance boundary")
            .WithViewport(compact ? 390 : 760, 400, 28).WithTheme(VisualTheme.Graphite(), mode);
        if (compact) chart.WithRenderOptions(new TopologyRenderOptions { FitContentToViewport = true });
        chart.AddNode("gateway", "Gateway", 80, 130, width: 180, height: 90);
        chart.AddNode("worker", "Worker", 460, 130, width: 180, height: 90);
        chart.AddEdge("dispatch", "gateway", "worker", "Dispatch");
        if (includeBoundary) chart.PathDecorations.Add(new TopologyPathDecoration("maintenance-zone",
            "M 370 70 C 330 120 410 170 370 220 C 330 270 410 300 370 330",
            stroke: mode == VisualThemeMode.Dark ? ChartColor.FromHex("#FBBF24") : ChartColor.FromHex("#92400E"),
            strokeWidth: 3, dashPattern: new[] { 8d, 4d }));
        return chart;
    }
}
