using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteHistograms(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
            foreach (var variant in new[] { "wide", "compact", "density", "compact-density", "sum", "compact-sum", "mean", "compact-mean" }) {
                var compact = IsCompactVariant(variant);
                var width = compact ? 360 : 800; var height = compact ? 360 : 440;
                var option = variant.StartsWith("compact-", StringComparison.Ordinal) ? variant.Substring(8) : variant;
                var title = option is "sum" or "mean" ? "Measurements across intervals" : "Ticket resolution time";
                var subtitle = option switch {
                    "density" => "Height is tickets per hour; each bar's area is its ticket count",
                    "sum" => "Signed quantities sum within each measurement interval",
                    "mean" => "Arithmetic means; the empty interval has no mean",
                    _ => "Service desk tickets closed in October 2026, counted in widening intervals"
                };
                var chart = V2GalleryModels.CreateHistogram(variant);
                var id = "histogram-" + variant + "-" + mode.ToString().ToLowerInvariant();
                WriteModel(output, artifacts, chart, id, "histogram", title, variant, subtitle, mode, width, height, false,
                    ExampleRecipes.Named("histogram"), new[] { "Bar" }, compact: compact);
            }
    }
}
