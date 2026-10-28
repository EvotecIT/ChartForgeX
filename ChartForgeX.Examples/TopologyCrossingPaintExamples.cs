using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Native and source-backed static output plus complete public consumer programs.</summary>
internal static class TopologyCrossingPaintExamples {
    internal static void Write(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var variant in new[] { "compact", "wide" }) {
            foreach (var policy in new[] { "arc", "gap", "none" }) {
                var name = "crossing-native-" + variant + "-" + policy + "-" + mode; var form = variant + "-" + policy;
                var chart = TopologyCrossingPaintRecipe.Create(form, mode); Save(chart.ToVisualArtifact(), name);
                File.WriteAllText(Path.Combine(output, name + ".program.cs"), ExampleRecipeSource.EmitNativeCrossingPaint(form, mode));
            }
            foreach (var form in new[] { "basis", "false", "arc", "gap" }) {
                var name = "crossing-swimlane-" + variant + "-" + form + "-" + mode;
                Save(SwimlaneCrossingPaintRecipe.Create(form, variant, mode), name);
                File.WriteAllText(Path.Combine(output, name + ".source.mmd"), SwimlaneCrossingPaintRecipe.Source(form));
                File.WriteAllText(Path.Combine(output, name + ".program.cs"), ExampleRecipeSource.EmitSwimlaneCrossingPaint(form, variant, mode));
            }
        }
        void Save(VisualArtifact artifact, string name) {
            var stem = Path.Combine(output, name); File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".png", artifact.ToPng());
            File.WriteAllText(stem + ".html", artifact.ToHtmlPage()); File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
        }
    }
}
