using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Data;
using ChartForgeX.Themes;

internal static class FoundationExamples {
    internal static void Write(string output, ChartPngOutputScale pngOutputScale) {
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
