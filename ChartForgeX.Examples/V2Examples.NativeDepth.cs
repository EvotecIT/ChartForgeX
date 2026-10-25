/// <summary>Complete native-depth recipes use the existing gallery and standalone-source owner.</summary>
public static partial class V2Examples {
    private static void WriteNativeDepth(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var variant in new[] { "wide", "compact", "plain", "compact-plain" }) {
                var compact = variant.StartsWith("compact", StringComparison.Ordinal);
                WriteNativeDepthModel(output, artifacts, "topology-path-decorations", variant, "topology", "Independent maintenance boundary",
                    "One native path decorates the graph without adding source nodes or relationships", mode,
                    compact ? 390 : 760, 400, null, compact);
            }
            foreach (var variant in new[] { "wide", "compact" }) {
                var compact = variant == "compact";
                WriteNativeDepthModel(output, artifacts, "usecase-source-depth", variant, "topology", "Native actor and use case captions",
                    "Native stereotypes, attached approval notes and independently styled named associations", mode,
                    compact ? 390 : 900, compact ? 620 : 520, null, compact);
                WriteNativeDepthModel(output, artifacts, "usecase-advanced-actors", variant, "topology", "Finite native actor forms",
                    "Hollow, business, silhouette and retained native icon participants share measured captions and routes", mode,
                    compact ? 390 : 1100, compact ? 1160 : 680, null, compact);
            }
            foreach (var variant in new[] { "wide", "compact" }) {
                var compact = variant == "compact";
                WriteNativeDepthModel(output, artifacts, "quadrant-point-styles", variant, "scatter", "Native point marker authoring",
                    "One native source series retains independent markers and located axis captions", mode,
                    compact ? 390 : 800, 500, "Scatter", compact);
            }
            foreach (var variant in new[] { "wide", "compact", "reversed", "compact-reversed", "time", "compact-time", "time-reversed", "compact-time-reversed" }) {
                var compact = variant.StartsWith("compact", StringComparison.Ordinal);
                WriteNativeDepthModel(output, artifacts, "mixed-schedule", variant, "gantt", "Shared schedule evidence",
                    "Source-ordered ranges, tasks, state intervals and packed resource items retain one native time axis", mode,
                    compact ? 390 : 900, 660, "Gantt", compact);
            }
            foreach (var variant in new[] { "wide", "compact", "meet", "slice", "collapsed", "horizontal", "reordered" }) {
                var compact = variant == "compact";
                WriteNativeDepthModel(output, artifacts, "org-portraits", variant, "topology", "People in the example studio",
                    "Embedded fictional portrait artwork follows the native hierarchy and crop policy", mode,
                    compact ? 390 : 900, 480, null, compact);
            }
            foreach (var variant in new[] { "wide", "compact", "automatic", "compact-automatic", "vertical", "compact-vertical", "mixed", "compact-mixed" }) {
                var compact = variant.StartsWith("compact", StringComparison.Ordinal);
                WriteNativeDepthModel(output, artifacts, "multi-ring-callouts", variant, "donut", "Independent periods with inner captions",
                    "Exact displaced sectors share one frame; source-owned overlay leaders retain every ring association", mode,
                    compact ? 390 : 900, compact ? 580 : 560, "Donut", compact);
            }
            foreach (var variant in new[] { "wide", "compact", "negative-log", "compact-negative-log", "sym-log", "compact-sym-log", "connect", "compact-connect" }) {
                var compact = variant.StartsWith("compact", StringComparison.Ordinal);
                WriteNativeDepthModel(output, artifacts, "aligned-difference", variant, "difference-area", "Independent source comparison",
                    "Exact shared coordinates retain both complete inputs and nonlinear native crossing geometry", mode,
                    compact ? 390 : 800, compact ? 500 : 520, "DifferenceArea", compact);
            }
            foreach (var variant in new[] { "wide", "compact", "cone", "compact-cone", "horizontal", "compact-horizontal", "cone-horizontal", "compact-cone-horizontal" }) {
                var compact = variant.StartsWith("compact", StringComparison.Ordinal);
                WriteNativeDepthModel(output, artifacts, "funnel-presentation", variant, "funnel", "Independent stage and connection presentation",
                    "Measured component captions and source-owned connection paint retain every ordered stage", mode,
                    compact ? 390 : 800, compact ? 560 : 620, "Funnel", compact);
            }
            foreach (var variant in new[] { "wide", "compact", "horizontal", "compact-horizontal", "reversed", "compact-reversed" }) {
                var compact = variant.StartsWith("compact", StringComparison.Ordinal);
                WriteNativeDepthModel(output, artifacts, "pyramid-presentation", variant, "pyramid", "Independent allocation captions and borders",
                    "Area encoding retains source order with separate category and value placement", mode,
                    compact ? 390 : 800, compact ? 500 : 560, "Pyramid", compact);
            }
            foreach (var variant in new[] { "wide", "compact", "straight", "compact-straight", "center", "compact-center" }) {
                var compact = variant.StartsWith("compact", StringComparison.Ordinal);
                WriteNativeDepthModel(output, artifacts, "chord-depth", variant, "chord", "Explicit directed transfer layout",
                    "Authored display permutations and connector tension preserve source identities and weights", mode,
                    compact ? 390 : 800, compact ? 460 : 520, "Chord", compact);
            }
            foreach (var variant in new[] { "wide", "compact", "right", "bottom", "top", "image", "contour" }) {
                var compact = variant == "compact";
                WriteNativeDepthModel(output, artifacts, "pictorial-artwork", variant, "pictorial", "Completed assessments by team",
                    "Captured self-contained artwork and explicit fractional clipping retain the complete source counts", mode,
                    compact ? 390 : 900, 460, "Pictorial", compact);
            }
            foreach (var variant in new[] { "wide", "compact", "stacked", "compact-stacked", "radius", "compact-radius" }) {
                var compact = variant.StartsWith("compact", StringComparison.Ordinal);
                WriteNativeDepthModel(output, artifacts, "categorical-radial-area", variant, "radial-column", "Requests by region and intake channel",
                    "Sector area measures the visible interval while source observations retain their values", mode, compact ? 390 : 900,
                    compact ? 520 : 600, "RadialColumn", compact);
            }
            foreach (var variant in new[] { "wide", "compact" }) {
                var compact = variant == "compact";
                WriteNativeDepthModel(output, artifacts, "combined-gauge", variant, "gauge", "Pressure against operating bands",
                    "Arc, needle and independently painted semantic bands", mode, compact ? 390 : 760, compact ? 360 : 440, "Gauge", compact);
                WriteNativeDepthModel(output, artifacts, "independent-progress", variant, "progress-bar", "Progress in each row's units",
                    "Retained row domains preserve fraction, count and signed measurements", mode, compact ? 390 : 760, 340, "ProgressBar", compact);
                WriteNativeDepthModel(output, artifacts, "topology-interchange", variant, "topology", "An editable saved route",
                    "Native JSON is loaded into detached owners before editing", mode, compact ? 390 : 900, compact ? 520 : 400, null, compact);
            }
            foreach (var variant in new[] { "wide", "compact", "vertical", "compact-vertical" }) {
                var compact = variant.StartsWith("compact", StringComparison.Ordinal);
                var vertical = variant.EndsWith("vertical", StringComparison.Ordinal);
                WriteNativeDepthModel(output, artifacts, "independent-bullet", variant, "bullet", "Independent operating measurements",
                    "Separate domains and source units share horizontal or vertical tracks", mode, compact ? 390 : 760, vertical ? 480 : 340, "Bullet", compact);
            }
            foreach (var variant in new[] { "wide", "compact", "states-wide", "states-compact" }) {
                var compact = variant.EndsWith("compact", StringComparison.Ordinal);
                var states = variant.StartsWith("states", StringComparison.Ordinal);
                WriteNativeDepthModel(output, artifacts, "heatmap-hierarchical-axes", variant, "heatmap",
                    states ? "Service states by region and work window" : "Service measurements by region and work window",
                    "Columns bind to raw coordinates; row groups bind to one-based source slots", mode, compact ? 390 : 1000, 560, "Heatmap", compact);
            }
        }
    }

    private static void WriteNativeDepthModel(string output, ICollection<ProofArtifact> artifacts, string name, string variant, string family,
        string title, string subtitle, VisualThemeMode mode, int width, int height, string? kind, bool compact) {
        var recipe = ExampleRecipes.Named(name);
        WriteModel(output, artifacts, recipe.Create(variant, mode), name + "-" + variant + "-" + mode.ToString().ToLowerInvariant(),
            family, title, variant, subtitle, mode, width, height, null, recipe, kind == null ? null : new[] { kind }, compact);
    }
}
