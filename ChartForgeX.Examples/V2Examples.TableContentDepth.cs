/// <summary>Uses the incumbent gallery/source writer for native linked table content.</summary>
public static partial class V2Examples {
    private static void WriteTableContentDepth(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var variant in new[] { "wide", "compact" }) {
            var compact = variant == "compact"; var recipe = ExampleRecipes.Named("topology-table-content");
            WriteModel(output, artifacts, recipe.Create(variant, mode), "topology-table-content-" + variant + "-" + mode.ToString().ToLowerInvariant(),
                "topology", "Approve a captured release", variant, "The source table remains part of its graph vertex", mode,
                compact ? 390 : 1000, compact ? 1000 : 680, false, recipe, compact: compact);
        }
    }
}
