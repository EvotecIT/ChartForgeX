using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

/// <summary>Writes the completed native angle/radius view through public package surfaces.</summary>
internal static class PolarViewportExamples {
    internal static void Write(string output) {
        Directory.CreateDirectory(output);
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var width in new[] { 360, 800 }) {
            var chart = PolarViewportRecipe.Create(width, mode); var prepared = PolarViewportRecipe.Prepare(chart);
            var name = "polar-viewport-" + mode.ToString().ToLowerInvariant() + "-" + width;
            File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".png"), prepared.ToPng());
            var section = PolarViewportRecipe.PrepareSection(chart);
            File.WriteAllText(Path.Combine(output, name + ".interactive.html"), "<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><style>"
                + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style></head><body>" + section.Html + "<script>"
                + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>");
        }
    }
}
