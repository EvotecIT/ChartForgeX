using ChartForgeX.Core;
using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteXZoom(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var kind in new[] { ChartSeriesKind.Line, ChartSeriesKind.StepLine })
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
                foreach (var compact in new[] { false, true }) {
                    var family = FamilyName(kind);
                    var variant = (compact ? "compact-" : "") + (kind == ChartSeriesKind.StepLine ? "step-" : "") + "x-zoom";
                    var id = "family-" + family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                    var title = kind == ChartSeriesKind.StepLine ? "Reversed steps at 2× X zoom" : "Requests at 2× X zoom";
                    var subtitle = compact ? "Pan 17.3%; full-data Y; gap at sample 4" : "Pan 17.3%; full-data Y scales; utilization on secondary Y";
                    var recipe = ExampleRecipes.Named("x-zoom");
                    WriteModel(output, artifacts, recipe.Create(variant, mode), id, family, title, variant, subtitle, mode,
                        compact ? 360 : 800, compact ? 360 : 440, !compact, recipe, new[] { kind.ToString() }, compact);
                }
    }
}
