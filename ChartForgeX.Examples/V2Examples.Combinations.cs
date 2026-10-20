using ChartForgeX.Themes;

public static partial class V2Examples {
    /// <summary>Exercises ordinary and updated supported compositions with the same compiled recipe shown in the gallery.</summary>
    private static void WriteSupportedCombinations(string output, ICollection<ProofArtifact> artifacts) {
        var recipe = ExampleRecipes.Named("supported-combinations");
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var variant in new[] { "vertical-secondary", "horizontal", "mixed-radial", "updated-vertical-secondary", "updated-horizontal", "updated-mixed-radial" }) {
                foreach (var compact in new[] { false, true }) {
                    var chart = (Chart)recipe.Create(variant, mode);
                    var id = "combination-" + variant + (compact ? "-compact-" : "-") + mode.ToString().ToLowerInvariant();
                    WriteModel(output, artifacts, chart, id, variant.EndsWith("mixed-radial", StringComparison.Ordinal) ? "radial-column" : "cartesian",
                        "Orders by location", variant, "Grouped categories, stable observations and independent configuration", mode,
                        compact ? 390 : 800, compact ? 360 : 460, true, recipe,
                        chart.Series.Select(series => series.Kind.ToString()).Distinct().ToArray(), compact);
                }
            }
        }
    }
}
