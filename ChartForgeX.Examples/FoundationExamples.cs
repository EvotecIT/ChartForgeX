using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Data;
using ChartForgeX.Themes;

internal static class FoundationExamples {
    internal static void Write(string output, ChartPngOutputScale pngOutputScale) {
        foreach (var dark in new[] { false, true }) {
            var clipped = Chart.Create().WithTitle("Series at the plot boundary")
                .WithSubtitle("Out-of-domain geometry is clipped; edge markers remain visible")
                .WithSize(640, 400).WithTheme(dark ? ChartTheme.ReportDark() : ChartTheme.ReportLight())
                .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).WithLegend(false)
                .AddArea("Range", new[] { new ChartForgeX.Primitives.ChartPoint(-4, -4), new ChartForgeX.Primitives.ChartPoint(5, 16), new ChartForgeX.Primitives.ChartPoint(14, -4) })
                .AddScatter("Edge", new[] { new ChartForgeX.Primitives.ChartPoint(0, 5), new ChartForgeX.Primitives.ChartPoint(10, 5) });
            SaveChart(clipped, output, "foundation-plot-clipping-" + (dark ? "dark" : "light"), pngOutputScale);
            var histogram = Chart.Create().WithTitle("Aligned histogram intervals")
                .WithSize(640, 400).WithTheme(dark ? ChartTheme.ReportDark() : ChartTheme.ReportLight())
                .WithDataLabels().AddHistogram("Measurements", new[] { 28d, 32d, 45d, 52d, 73d, 80d, 98d }, 3);
            SaveChart(histogram, output, "foundation-histogram-rounding-" + (dark ? "dark" : "light"), pngOutputScale);
        }
        var samples = ChartDataset<FoundationSample>.From(new[] {
            new FoundationSample("Warsaw", 1, 18),
            new FoundationSample("Warsaw", 2, 42),
            new FoundationSample("Warsaw", 3, 126),
            new FoundationSample("Warsaw", 4, 640),
            new FoundationSample("London", 1, 12),
            new FoundationSample("London", 2, 31),
            new FoundationSample("London", 3, 88),
            new FoundationSample("London", 4, 390)
        });

        var tokens = VisualDesignTokens.Dark();
        tokens.Accent = ChartColor.FromHex("#A78BFA");
        tokens.SecondaryAccent = ChartColor.FromHex("#22D3EE");
        tokens.Palette = new[] {
            ChartColor.FromHex("#A78BFA"),
            ChartColor.FromHex("#22D3EE"),
            ChartColor.FromHex("#34D399")
        };

        var logScale = Chart.Create()
            .WithTitle("Typed Throughput Growth")
            .WithSubtitle("One dataset, shared design tokens, and a logarithmic scale")
            .WithSize(920, 540)
            .WithDesignTokens(tokens)
            .WithXAxis("Sample")
            .WithYAxis("Requests per second")
            .ConfigureYAxis(axis => {
                axis.WithScale(ChartScaleKind.Logarithmic).WithBounds(10, 1000);
                axis.TickCount = 5;
            })
            .WithAccessibility(accessibility => accessibility.WithTextAlternative(
                "Typed throughput growth",
                "Requests per second increase for Warsaw and London across four samples.",
                "en"))
            .AddLine("Warsaw", samples.Filter(sample => sample.Site == "Warsaw"), sample => sample.Index, sample => sample.Value)
            .AddLine("London", samples.Filter(sample => sample.Site == "London"), sample => sample.Index, sample => sample.Value);
        SaveChart(logScale, output, "foundation-typed-log-scale", pngOutputScale);

        var rampTokens = new VisualDesignTokens();
        rampTokens.SequentialRamp = new[] {
            ChartColor.FromHex("#9AB8DC"), ChartColor.FromHex("#5598E7"),
            ChartColor.FromHex("#2A78D6"), ChartColor.FromHex("#1C5CAB"),
            ChartColor.FromHex("#104281")
        };
        var rampHeatmap = Chart.Create()
            .WithTitle("Sequential Design Token Ramp")
            .WithSubtitle("Count intensity uses the token ramp, from the weakest to strongest shade")
            .WithSize(920, 420)
            .WithDesignTokens(rampTokens)
            .WithLegend(false)
            .WithXAxis("Weekday")
            .WithYAxis("Service")
            .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
            .AddHeatmapRow("API", new[] { 4d, 12d, 27d, 53d, 82d, 46d, 9d })
            .AddHeatmapRow("Worker", new[] { 1d, 7d, 21d, 39d, 68d, 97d, 18d })
            .AddHeatmapRow("Queue", new[] { 14d, 33d, 56d, 74d, 100d, 61d, 24d });
        rampHeatmap.Options.HeatmapRelativeScale = true;
        SaveChart(rampHeatmap, output, "foundation-sequential-ramp-heatmap", pngOutputScale);

        var facets = ChartGrid.FromFacets(
                samples,
                sample => sample.Site,
                (site, rows) => Chart.Create()
                    .WithTitle(site)
                    .WithSize(430, 320)
                    .WithDesignTokens(tokens)
                    .WithYAxis("Requests/s")
                    .ConfigureYAxis(axis => axis.WithBounds(0, 700))
                    .AddArea("Throughput", rows, sample => sample.Index, sample => sample.Value),
                columns: 2)
            .WithTitle("Typed Facet Grid")
            .WithSubtitle("Deterministic small multiples from product-owned records")
            .WithPanelSize(430, 320);
        facets.WithPngOutputScale(pngOutputScale);
        facets.SaveSvg(Path.Combine(output, "foundation-typed-facets.svg"));
        facets.SavePng(Path.Combine(output, "foundation-typed-facets.png"));
        facets.SaveHtml(Path.Combine(output, "foundation-typed-facets.html"));
    }

    private static void SaveChart(Chart chart, string output, string name, ChartPngOutputScale pngOutputScale) {
        chart.WithPngOutputScale(pngOutputScale);
        chart.SaveSvg(Path.Combine(output, name + ".svg"));
        chart.SavePng(Path.Combine(output, name + ".png"));
        chart.SaveHtml(Path.Combine(output, name + ".html"));
    }

    private sealed record FoundationSample(string Site, double Index, double Value);
}
