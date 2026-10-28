public static partial class V2Examples {
    private static void WriteSequenceSurfaceShadows(string output, ICollection<ProofArtifact> artifacts) {
        var recipe = ExampleRecipes.Named("sequence-surface-shadows");
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var variant in new[] { "compact", "wide" }) {
                var compact = variant == "compact";
                WriteModel(output, artifacts, recipe.Create(variant, mode),
                    "sequence-surface-shadows-" + variant + "-" + mode.ToString().ToLowerInvariant(),
                    "sequence", "Retained vector surface shadows", variant,
                    "Native shadows behind vector glyphs, captions and safe links", mode,
                    compact ? 390 : 1100, 900, false, recipe, new[] { "Sequence" }, compact);
            }
        }
    }
}
