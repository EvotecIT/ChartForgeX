using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static class AuthoredHandoffExamples {
    internal static void Write(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var chart = TopologyAuthoredPathBadgeRecipe.Create().WithTheme(VisualTheme.Graphite().WithFontFamily(ExampleTypography.FontFamily), mode);
            var artifact = chart.ToVisualArtifact();
            var path = Path.Combine(output, "authored-handoff-" + mode.ToString().ToLowerInvariant());
            File.WriteAllText(path + ".svg", artifact.ToSvg());
            File.WriteAllBytes(path + ".png", artifact.ToPng());
            File.WriteAllText(path + ".html", artifact.ToHtmlPage());
            File.WriteAllText(path + ".json", artifact.ToInterchangeJson());
            File.WriteAllText(path + ".csharp.txt", ExampleRecipeSource.EmitAuthoredHandoff(mode));
        }
    }
}
