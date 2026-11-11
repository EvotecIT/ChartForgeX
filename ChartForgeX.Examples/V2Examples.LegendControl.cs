using ChartForgeX.Themes;

public static partial class V2Examples {
    /// <summary>Shows custom legend entries, value-formatted entries and authored keys on the line family page at full and compact widths.</summary>
    private static void WriteLegendControl(string output, ICollection<ProofArtifact> artifacts) {
        var recipe = ExampleRecipes.Named("legend-control");
        foreach (var option in LegendControlRecipe.Options)
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
                foreach (var compact in new[] { false, true }) {
                    var variant = (compact ? "compact-" : "") + option;
                    var (title, subtitle) = option switch {
                        "legend-reference" => ("Revenue against target", "The legend names the target line and its tolerance band"),
                        "legend-values" => ("Revenue and cost totals", "Each legend entry carries its annual total"),
                        _ => ("Revenue with marker keys", "Larger legend keys draw each series' marker")
                    };
                    var id = "family-line-" + variant + "-" + mode.ToString().ToLowerInvariant();
                    WriteModel(output, artifacts, recipe.Create(variant, mode), id, "line", title, variant, subtitle, mode,
                        compact ? 360 : 800, compact ? 360 : 440, true, recipe, new[] { "Line" }, compact);
                }
    }
}
