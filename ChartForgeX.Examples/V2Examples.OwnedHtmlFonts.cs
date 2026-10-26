using ChartForgeX.Interactivity.Html;

public static partial class V2Examples {
    private static void WriteOwnedHtmlFonts(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var variant in new[] { "wide", "compact" }) {
            var chart = HtmlOwnedFontRecipe.Create(variant, mode, ExampleTypography.FontFamily);
            var id = "owned-html-fonts-" + variant + "-" + mode.ToString().ToLowerInvariant();
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg());
            File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
            ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".interactive.html"), chart.ToInteractiveHtmlPage(options => options.TextPresentation = HtmlChartTextPresentation.Automatic));
            ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitHtmlOwnedFonts(variant, mode));
            ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".gallery.csharp.txt"), ExampleRecipeSource.EmitHtmlOwnedFonts(variant, mode, galleryTypography:true));
            WriteThumbnail(output, chart, id, "Review progress", "cartesian", mode, legend:true);
            artifacts.Add(new ProofArtifact(id, "cartesian", "Review progress", variant, mode.ToString().ToLowerInvariant(), variant == "compact" ? 390 : 760, 440,
                prepared.Diagnostics.Select(row => row.Code).ToArray(), prepared.Regions.Count,
                DiagnosticMessages: prepared.Diagnostics.Select(row => row.Message).ToArray(), Compact: variant == "compact", HtmlAdapter: true));
        }
    }
}
