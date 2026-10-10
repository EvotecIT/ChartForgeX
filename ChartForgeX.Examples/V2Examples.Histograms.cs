using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteHistograms(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
            foreach (var variant in new[] { "wide", "compact", "density", "compact-density", "sum", "compact-sum", "mean", "compact-mean" }) {
                var compact = IsCompactVariant(variant);
                var width = compact ? 360 : 800; var height = compact ? 360 : 440;
                var title = "Measurements across intervals";
                var option = variant.StartsWith("compact-", StringComparison.Ordinal) ? variant.Substring(8) : variant;
                var subtitle = option switch {
                    "density" => "Height is count per unit; rectangular area represents count",
                    "sum" => "Signed quantities sum within each measurement interval",
                    "mean" => "Arithmetic means; the empty interval has no mean",
                    _ => "Observation counts in unequal measurement intervals"
                };
                var chart = V2GalleryModels.CreateHistogram(variant);
                var id = "histogram-" + variant + "-" + mode.ToString().ToLowerInvariant();
                WriteModel(output, artifacts, chart, id, "histogram", title, variant, subtitle, mode, width, height, false,
                    "V2GalleryModels.CreateHistogram(" + Literal(variant) + ")", new[] { "Bar" }, compact: compact);
            }
    }
}
