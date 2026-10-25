using ChartForgeX;
using ChartForgeX.Themes;

internal static partial class MermaidExamples {
    private static void WriteXYSourceDepthExamples(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
            foreach (var variant in new[] { "wide-vertical", "compact-vertical", "wide-horizontal", "compact-horizontal" }) {
                var source = MermaidFixtureSources.Read("xychart-native-source-depth");
                if (variant.EndsWith("-horizontal", StringComparison.Ordinal))
                    source = source.Replace("xychart-beta\n", "xychart-beta horizontal\n", StringComparison.Ordinal);
                var parsed = ChartForgeX.Mermaid.MermaidRenderer.Render(source, new ChartForgeX.Mermaid.MermaidRenderOptions {
                    WarningPolicy = ChartForgeX.Mermaid.MermaidWarningPolicy.Reject,
                    XYChart = new ChartForgeX.Mermaid.MermaidXYChartRenderOptions {
                        Width = variant.StartsWith("compact-", StringComparison.Ordinal) ? 390 : 1100, Height = 520,
                        Presentation = new ChartForgeX.Mermaid.MermaidPresentationOptions { ThemeMode = mode }
                    }
                });
                if (parsed.HasErrors || parsed.Artifact?.Model is not ChartForgeX.Core.Chart chart)
                    throw new InvalidOperationException("The shared XY source must map to one native chart: " + string.Join("; ", parsed.Diagnostics.Select(item => item.Message)));
                chart.WithTheme(VisualTheme.Graphite().WithFontFamily(ExampleTypography.FontFamily), mode);
                var id = "mermaid-xy-source-depth-" + variant + "-" + mode.ToString().ToLowerInvariant();
                File.WriteAllText(Path.Combine(output, id + ".mmd"), source);
                chart.SaveSvg(Path.Combine(output, id + ".svg")); chart.SavePng(Path.Combine(output, id + ".png"));
                File.WriteAllText(Path.Combine(output, id + ".html"), chart.ToHtmlPage().Replace("</head>", ExampleTypography.EmbeddedStyle + "</head>", StringComparison.Ordinal));
                File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitXYChart(variant, mode));
            }
    }
}
