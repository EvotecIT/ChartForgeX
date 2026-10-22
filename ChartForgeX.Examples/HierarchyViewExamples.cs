using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

/// <summary>Native full and subtree views reuse one original hierarchy and source identity.</summary>
internal static class HierarchyViewExamples {
    internal static void Write(string output) {
        foreach (var kind in new[] { ChartSeriesKind.Treemap, ChartSeriesKind.Sunburst })
            foreach (var dark in new[] { false, true }) {
                var width = dark ? 390 : 720; var height = dark ? 420 : 460;
                // Reuse the maintained allocation facts; mapping the second chart family is authoring, not browser drill-down.
                var sunburst = SunburstBranchHighlightExamples.CreateChart(width, dark);
                if (kind == ChartSeriesKind.Sunburst) sunburst.ConfigureSunburst(options => {
                    options.SectorGap = 6; options.MinimumLabelFontSize = 10; options.CornerRadius = 4;
                });
                var chart = kind == ChartSeriesKind.Sunburst ? sunburst : Chart.Create().WithSize(width, height).WithDataLabels()
                    .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
                    .AddSeries(ChartSeries.CreateTreemap(new ChartSeriesId("allocation"), "Allocated work", sunburst.Series[0].HierarchyItems))
                    .ConfigureTreemap(options => options.ColorScale = sunburst.Options.Sunburst.ColorScale);
                foreach (var selected in new[] { false, true }) {
                    var id = "hierarchy-view-" + kind.ToString().ToLowerInvariant() + "-" + (selected ? "engineering" : "full") + "-" + (dark ? "dark" : "light");
                    var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(),
                        dark ? VisualThemeMode.Dark : VisualThemeMode.Light,
                        new VisualFrame(selected ? "Engineering allocation" : "All teams", "Original item IDs, sizes and color values", showLegend: false),
                        FontSpec.FromFamily(ExampleTypography.FontFamily));
                    var prepared = chart.PrepareHierarchy(context, selected ? new ChartHierarchyView("engineering") : null);
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
                    File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".html"), prepared.ToArtifact(id).ToHtmlPage().Replace("</head>", ExampleTypography.EmbeddedStyle + "</head>", StringComparison.Ordinal));
                }
            }
    }
}
