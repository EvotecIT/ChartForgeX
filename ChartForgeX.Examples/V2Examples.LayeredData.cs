using ChartForgeX.Themes;

public static partial class V2Examples {
    /// <summary>Uses the same compiled sources for composed data previews and their ordinary-package downloads.</summary>
    private static void WriteLayeredData(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var name in new[] { "financial-layers", "geographic-datasets", "source-drawings" }) {
                var geographic = name == "geographic-datasets";
                var recipe = ExampleRecipes.Named(name);
                foreach (var variant in geographic ? new[] { "wide", "compact", "mercator", "compact-mercator" } : new[] { "wide", "compact" }) {
                    var chart = (Chart)recipe.Create(variant, mode);
                    var compact = IsCompactVariant(variant);
                    var (family, title, subtitle, height) = name switch {
                        "financial-layers" => ("candlestick", "Prices across trading sessions", "Candles, reference close and expected range", 460),
                        "geographic-datasets" => ("dotted-map", "Coverage and delivery routes", "Independent coverage, delay and traffic dimensions", 600),
                        _ => ("line", "Review a measured trend", "Independent source drawings retain their coordinates in every export", 520)
                    };
                    WriteModel(output, artifacts, chart, name + "-" + variant + "-" + mode.ToString().ToLowerInvariant(),
                        family, title, variant, subtitle, mode,
                        compact ? 390 : 800, height, geographic || name == "financial-layers", recipe,
                        chart.Series.Select(series => series.Kind.ToString()).Distinct().ToArray(), compact);
                }
            }
        }
    }
}
