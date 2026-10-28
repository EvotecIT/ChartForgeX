using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

/// <summary>Writes ordinary package-surface radial recipes in compact/wide and light/dark poses.</summary>
internal static class RadialFormsExamples {
    internal static void Write(string output, ChartPngOutputScale scale) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var width in new[] { 360, 800 }) foreach (var radar in new[] { false, true }) {
            var chart = radar ? RadialFormsRecipe.CreateRadar(width, mode) : RadialFormsRecipe.CreatePolar(width, mode);
            var name = "radial-forms-" + (radar ? "radar-" : "polar-") + mode.ToString().ToLowerInvariant() + "-" + width;
            ExampleArtifactWriter.SaveChart(chart, output, name, scale);
            File.WriteAllText(Path.Combine(output, name + ".interactive.html"), chart.ToInteractiveHtmlPage(options => {
                options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 360;
            }));
        }
    }
}
