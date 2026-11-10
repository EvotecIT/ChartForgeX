using ChartForgeX.Themes;

public static partial class V2Examples {
    /// <summary>Shows each axis interval and grid option on the line family page at full and compact widths.</summary>
    private static void WriteAxisControl(string output, ICollection<ProofArtifact> artifacts) {
        var recipe = ExampleRecipes.Named("axis-control");
        foreach (var option in AxisControlRecipe.Options)
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
                foreach (var compact in new[] { false, true }) {
                    var variant = (compact ? "compact-" : "") + option;
                    var (title, subtitle) = option switch {
                        "axis-step" => ("Latency on fixed tick steps", "Every 6 hours across and every 15 ms up"),
                        "axis-values" => ("Latency against budget thresholds", "Ticks at shift changes and at 20, 50 and 80 ms"),
                        "axis-spacing" => ("Latency with bounded tick spacing", "Ticks at most 45 units apart across, at least 90 up"),
                        "axis-minor" => ("Latency with minor grid lines", "Four minor lines per 20 ms, two per three hours"),
                        "axis-grid" => ("Latency with styled axis grids", "Dashed value lines over neutral alternating bands"),
                        "axis-time-step" => ("Deployments on a calendar step", "A tick every second Monday, a minor line between"),
                        "axis-stacked" => ("Data hall climate on stacked axes", "Temperature and humidity share the right side"),
                        _ => ("Payload sizes on a logarithmic step", "A tick on every second power of ten")
                    };
                    var id = "family-line-" + variant + "-" + mode.ToString().ToLowerInvariant();
                    WriteModel(output, artifacts, recipe.Create(variant, mode), id, "line", title, variant, subtitle, mode,
                        compact ? 360 : 800, compact ? 360 : 440, !compact, recipe, new[] { "Line" }, compact);
                }
    }
}
