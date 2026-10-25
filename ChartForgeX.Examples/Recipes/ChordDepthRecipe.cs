/// <summary>Explicit display order and ribbon tension leave directed source facts and ordinal paint unchanged.</summary>
public static class ChordDepthRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var compact = variant.StartsWith("compact", StringComparison.Ordinal);
        var form = compact && variant != "compact" ? variant.Substring("compact-".Length) : variant;
        if (form is not ("wide" or "compact" or "source" or "straight" or "center"))
            throw new ArgumentException("Unknown Chord depth variant.", nameof(variant));
        var source = ChartSeries.CreateChord(new ChartSeriesId("service-transfers"), "Transfers", new[] {
            new ChartNode("api", "API"), new ChartNode("web", "Web"), new ChartNode("ops", "Operations"),
            new ChartNode("data", "Data"), new ChartNode("idle", "Standby")
        }, new[] {
            new ChartFlowLink("api-data", "api", "data", 12, 30),
            new ChartFlowLink("api-priority", "api", "data", 4, -10),
            new ChartFlowLink("web-ops", "web", "ops", 9, 15),
            new ChartFlowLink("ops-web", "ops", "web", 3, 5),
            new ChartFlowLink("data-self", "data", "data", 2, 0),
            new ChartFlowLink("zero", "ops", "api", 0, null)
        });
        var chart = Chart.Create().AddSeries(source).WithTitle("Directed service transfers")
            .WithSize(compact ? 390 : 800, compact ? 460 : 520).WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), mode);
        source.WithFlowStyle("api-priority", new ChartFlowStyle(stroke: ChartColor.FromHex("#A56421"),
            outlineStroke: new ChartStrokeStyle(width: 2, dash: new[] { 5d, 3d }), strokeDashOffset: 1.5));
        chart.ConfigureChord(options => {
            options.LabelContent = ChartChordLabelContent.LabelAndTotals;
            if (form != "source") {
                options.Ordering = new ChartChordOrdering(new[] { "api", "data", "web", "ops", "idle" },
                    new[] { "api-priority", "api-data", "web-ops", "ops-web", "data-self", "zero" },
                    new[] { "api-data", "data-self", "api-priority", "ops-web", "web-ops", "zero" });
                options.RibbonTension = form == "straight" ? 0 : form == "center" ? 1 : .55;
            }
        });
        return chart;
    }
}
