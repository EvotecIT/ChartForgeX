using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

public static partial class V2Examples {
    private static void WriteFamilies(string output, ICollection<ProofArtifact> artifacts, bool curated) {
        foreach (var kind in Enum.GetValues<ChartSeriesKind>()) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var selection in ExampleRecipes.FamilySelections(kind, curated)) {
                var variant = selection.Variant;
                var precision = variant is "precision" or "compact-precision";
                var chart = V2GalleryModels.Create(kind, variant, mode); var title = precision ? kind == ChartSeriesKind.Gauge ? "Measured tolerance" : "Small signed drift"
                    : variant is "concentric" or "compact-concentric" ? "Service mix across two periods"
                    : variant is "variable-radius" or "compact-variable-radius" ? "Service shares with authored radii"
                    : variant is "automatic-rings" or "compact-automatic-rings" ? "Three independent service mixes"
                    : variant is "quadrants" or "compact-quadrants" ? "Effort and impact"
                    : variant is "plot-regions" or "compact-plot-regions" ? "Capacity and delay"
                    : variant is "partial-color-range" or "compact-partial-color-range" ? kind == ChartSeriesKind.TileMap ? "Requests by state" : "Processing time and completion"
                    : variant is "positioned-color-scale" or "compact-positioned-color-scale" && kind is ChartSeriesKind.TileMap or ChartSeriesKind.Heatmap
                        ? kind == ChartSeriesKind.TileMap ? "Temperature by state" : "Temperature across service areas"
                    : variant is "color-dimension" or "compact-color-dimension" or "gradient-color-legend" or "compact-gradient-color-legend" or "swatch-color-legend" or "compact-swatch-color-legend" ? kind switch {
                        ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap => "Temperature by state", ChartSeriesKind.Heatmap => "Temperature by grid position",
                        ChartSeriesKind.CalendarHeatmap => "Daily average temperature", ChartSeriesKind.Treemap => "Team workload and temperature",
                        ChartSeriesKind.Chord => "Transfer volume and latency",
                        ChartSeriesKind.Scatter => "Temperature by workload", _ => "Workload, requests and temperature"
                    } : V2GalleryModels.Title(kind);
                if (!chart.Series.Any(series => series.Kind == kind)) throw new InvalidOperationException("The gallery factory did not create its declared chart kind: " + kind);
                var family = FamilyName(kind); var id = "family-" + family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                var compact = IsCompactVariant(variant);
                var width = compact ? 360 : 800; var height = compact ? 360 : 440;
                if (kind == ChartSeriesKind.Pyramid && variant == "compact-dense") { width = 390; height = 280; }
                if (variant is "variable-radius" or "compact-variable-radius" or "automatic-rings" or "compact-automatic-rings") {
                    width = compact ? 390 : 900; height = variant.EndsWith("automatic-rings", StringComparison.Ordinal) ? 600 : 480;
                }
                var rangeAcceptance = kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea or ChartSeriesKind.DifferenceArea or ChartSeriesKind.RangeBar
                    && variant is "singleton" or "compact-singleton" or "dense" or "compact-dense" or "dense-horizontal" or "compact-dense-horizontal" or "sparse-catalog" or "compact-sparse-catalog";
                var scalarAcceptance = kind is ChartSeriesKind.Bullet or ChartSeriesKind.Gauge
                    && variant is "dense" or "compact-dense" or "dense-scale" or "compact-dense-scale";
                if (rangeAcceptance || scalarAcceptance) { width = compact ? 390 : 760; height = compact ? scalarAcceptance ? 300 : 320 : 440; }
                if (kind == ChartSeriesKind.Pyramid && variant is "dense" or "compact-dense") title = "Allocation by service";
                // Let the shared policy decide whether a legend adds information. The indicator
                // options example intentionally demonstrates an explicitly requested legend.
                bool? legend = variant == "options" && kind is ChartSeriesKind.Gauge or ChartSeriesKind.Bullet ? true : null;
                if (kind == ChartSeriesKind.Scatter && variant is "options" or "compact-options") legend = false;
                var subtitle = kind == ChartSeriesKind.DifferenceArea ? variant is "dated-gaps" or "compact-dated-gaps"
                    ? "Named paired values retain a missing section on an independent scale"
                    : variant is "step-start" ? "Paired values change at the preceding observation; missing sections stay open"
                    : variant is "step-middle" or "compact-step-middle" ? "Paired values change halfway between observations; missing sections stay open"
                    : variant is "step-end" ? "Paired values change at the following observation; missing sections stay open"
                    : "Shading compares Actual above and below Forecast"
                    : variant is "concentric" or "compact-concentric" ? "Independent totals share categories; disjoint bands preserve each series"
                    : variant is "variable-radius" or "compact-variable-radius" ? "Values determine angles; explicit fractions determine band thickness"
                    : variant is "automatic-rings" or "compact-automatic-rings" ? "Source order allocates rings; every period keeps its own total"
                    : precision ? kind == ChartSeriesKind.Gauge ? "Close bounds, measurement and target retain distinct captions" : "Measured differences stay in their original units"
                    : kind == ChartSeriesKind.RangeBar && variant.EndsWith("endpoint-labels", StringComparison.Ordinal) ? "Start and end captions retain the authored interval order"
                    : variant is "angular-reversal" or "compact-angular-reversal" ? kind == ChartSeriesKind.Polar ? "Clockwise authored radians retain their source directions and values"
                        : "Reversed category angles retain source labels, values and paint"
                    : variant is "dashed" or "compact-dashed" ? "Direction-specific dash patterns and phase preserve the source prices"
                    : variant is "quadrants" or "compact-quadrants" ? "Four background regions share an authored effort and impact pivot"
                    : variant is "plot-regions" or "compact-plot-regions" ? "Bounded operating guidance leaves source values and bubble sizes independent"
                    : variant is "axis-labels" or "compact-axis-labels" ? "Upright axis-relative labels with independent density"
                    : variant is "all-labels" or "compact-all-labels" ? "All captions fit individually; caption overlaps are intentional"
                    : variant == "cone-vertical" ? "Vertical cone; stage lines encode source values"
                    : variant == "stage-bars-horizontal" ? "Horizontal stage bars; extents remain proportional"
                    : kind == ChartSeriesKind.Waterfall && variant is "horizontal" or "compact-horizontal" ? "Horizontal changes and checkpoints share a cumulative value axis"
                    : kind == ChartSeriesKind.RangeBar && variant is "horizontal" or "compact-horizontal" ? "Grouped horizontal intervals preserve both authored endpoints"
                    : kind == ChartSeriesKind.BoxPlot && variant is "horizontal" or "compact-horizontal" ? "Grouped horizontal summaries retain all five statistics"
                    : kind == ChartSeriesKind.Sunburst && variant is "authored-total" or "compact-authored-total" ? "Inclusive parent totals retain unallocated remainder"
                    : kind == ChartSeriesKind.Sunburst && variant is "options" or "compact-options" ? "Leaf totals size sectors; measured color stays independent"
                    : kind == ChartSeriesKind.Lollipop && variant is "horizontal" or "compact-horizontal" ? "Delivery stages share a horizontal order-count axis"
                    : kind == ChartSeriesKind.Dumbbell && variant is "horizontal" or "compact-horizontal" ? "Horizontal Start and End glyphs retain both authored capacity values"
                    : kind == ChartSeriesKind.Dumbbell && variant is "endpoint-markers" or "compact-endpoint-markers" ? "Independent endpoint shapes, sizes and colors preserve source order"
                    : kind == ChartSeriesKind.Sankey && variant.EndsWith("source-colors", StringComparison.Ordinal) ? "Source endpoint colors retain explicit flow paint overrides"
                    : kind == ChartSeriesKind.Sankey && variant.EndsWith("target-colors", StringComparison.Ordinal) ? "Target endpoint colors retain explicit flow paint overrides"
                    : kind == ChartSeriesKind.Sankey && variant.EndsWith("quantitative-colors", StringComparison.Ordinal) ? "Request counts size ribbons; independent latency measurements color them"
                    : kind == ChartSeriesKind.Sankey && variant is "options" or "compact-options" ? "Aligned and ordered weighted flows"
                    : variant is "partial-color-range" or "compact-partial-color-range" ? kind == ChartSeriesKind.TileMap
                        ? "Zero stays fixed; the color peak follows reported counts" : "The color ceiling stays at 100%; the lower bound follows observations"
                    : variant is "gradient-color-legend" or "compact-gradient-color-legend" ? "A continuous key retains every authored color anchor"
                    : variant is "swatch-color-legend" or "compact-swatch-color-legend" ? "A sampled key keeps the continuous measurement and its color domain"
                    : variant is "color-dimension" or "compact-color-dimension" ? kind == ChartSeriesKind.Chord
                        ? "Illustrative latency colors flows; transfer counts retain their weights" : "Temperature captions use their own format and culture"
                    : variant is "positioned-color-scale" or "compact-positioned-color-scale" ? "Absolute color anchors use uneven numeric intervals"
                    : variant is "color-scale" or "compact-color-scale" ? kind == ChartSeriesKind.Scatter ? "Temperature has its own fixed color domain; processing time stays independent"
                        : kind == ChartSeriesKind.Bubble ? "Size shows requests; named color bands show temperature"
                        : kind == ChartSeriesKind.Heatmap ? "Signed measurements share a fixed color domain"
                        : kind == ChartSeriesKind.HexbinHeatmap ? "Named numeric intervals keep their exact boundaries" : "Every observed value shares one scale; missing days stay distinct"
                    : variant is "options" or "compact-options" && ExampleRecipes.HasGeometryOptions(kind) ? GeometrySubtitle(kind)
                    : variant switch { "sparse" => "Missing observations remain visible as gaps", "options" => "Explore configured marks, scales and labels", "compact" => "The same data in a compact view", _ => "Explore the data, then download the chart" };
                WriteModel(output, artifacts, chart, id, family, title, variant, subtitle, mode, width, height, legend,
                    selection.Recipe, chart.Series.Select(series => series.Kind.ToString()).Distinct().ToArray(), compact: compact);
            }
            if (kind == ChartSeriesKind.RadialBar) WriteNumericRadialAxes(output, artifacts, mode);
        }
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) WriteMixedRadial(output, artifacts, mode);
        WriteSupportedCombinations(output, artifacts);
        WriteExpandedDiagrams(output, artifacts, curated);
    }

    private static void WriteMixedRadial(string output, ICollection<ProofArtifact> artifacts, VisualThemeMode mode) {
        foreach (var compact in new[] { false, true }) {
          foreach (var curved in new[] { false, true }) {
            var variant = compact ? "compact-mixed" : "mixed";
            if (curved) variant += "-curves";
            WriteModel(output, artifacts, V2GalleryModels.CreateMixedRadial(curved ? 1 : null), "family-radial-column-" + variant + "-" + mode.ToString().ToLowerInvariant(),
                "radial-column", "Regional counts and targets", variant, "Columns, reference area and target line share categories and radius", mode,
                compact ? 360 : 800, compact ? 360 : 440, true, ExampleRecipes.Named("mixed-radial"), new[] { "RadialColumn", "Radar" }, compact: compact);
          }
            var reversedVariant = compact ? "compact-mixed-angular-reversal" : "mixed-angular-reversal";
            WriteModel(output, artifacts, V2GalleryModels.CreateMixedRadial().ConfigureXAxis(axis => axis.WithReversal()),
                "family-radial-column-" + reversedVariant + "-" + mode.ToString().ToLowerInvariant(), "radial-column", "Regional counts and targets", reversedVariant,
                "Columns, reference area and target line share reversed category angles", mode, compact ? 360 : 800, compact ? 360 : 440, true,
                ExampleRecipes.Named("mixed-radial"), new[] { "RadialColumn", "Radar" }, compact: compact);
        }
    }

    private static void WriteNumericRadialAxes(string output, ICollection<ProofArtifact> artifacts, VisualThemeMode mode) {
        foreach (var compact in new[] { false, true }) {
            var variant = compact ? "compact-dual-axes" : "dual-axes";
            var id = "family-radial-bar-" + variant + "-" + mode.ToString().ToLowerInvariant();
            WriteModel(output, artifacts, V2GalleryModels.CreateRadialBarScales(), id, "radial-bar", "Requests and resolution rate", variant,
                "Request counts and percentages use independent scales", mode, compact ? 360 : 800, compact ? 360 : 440, true,
                ExampleRecipes.Named("radial-bar-scales"), new[] { "RadialBar" }, compact: compact);
        }
    }

    private static string GeometrySubtitle(ChartSeriesKind kind) => kind switch {
        ChartSeriesKind.Line => "Step transitions and three configured marker shapes",
        ChartSeriesKind.Bar or ChartSeriesKind.HorizontalBar => "Two independent stacks; each reaches 100%; source values remain counts",
        ChartSeriesKind.StackedArea => "A normalized stack with middle-step boundaries",
        ChartSeriesKind.RangeArea => "Lower, middle and upper bounds share middle-step transitions",
        ChartSeriesKind.RangeBar => "Two grouped interval series retain independent endpoints and caps",
        ChartSeriesKind.Funnel => "Horizontal cone; stage lines encode values including zero",
        ChartSeriesKind.Waterfall => "An opening balance, changes and checkpoints retain the running balance on reversed axes",
        ChartSeriesKind.Scatter => "Nine marker shapes with source labels",
        ChartSeriesKind.Bubble => "Equal request volumes share a filled area; values above 100 retain their facts",
        ChartSeriesKind.DottedMap => "Location keys retain coordinates and optional weights",
        ChartSeriesKind.Candlestick => "Hollow rising bodies, filled falling bodies and independent wick strokes",
        ChartSeriesKind.Ohlc => "Rising and falling stroke colors, alpha and width retain source prices",
        ChartSeriesKind.Radar => "A filled area and an unfilled target line share one radial scale",
        ChartSeriesKind.Pyramid => "Reversed horizontal pyramid; areas encode values including zero",
        ChartSeriesKind.RadialBar or ChartSeriesKind.RadialColumn => "Named stacks reach 100; labels retain source counts",
        _ => "Configured chart geometry"
    };

    private static void WriteModel(string output, ICollection<ProofArtifact> artifacts, IVisualRenderable model, string id, string family, string title,
        string variant, string subtitle, VisualThemeMode mode, int width, int height, bool? legend, ExampleRecipe recipe, string[]? kinds = null, bool compact = false) {
        var context = GalleryContext(width, height, mode, title, subtitle, legend);
        if (model is TopologyChart topology && topology.DefaultRenderOptions?.FitContentToViewport == false) {
            // Keep the complete natural canvas; compact hosts scroll instead of shrinking authored actors.
            var natural = topology.WithTitle(title).WithSubtitle(subtitle)
                .WithTheme(context.Theme.WithFontFamily(ProofFont), mode).ToVisualArtifact();
            ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), natural.ToSvg());
            File.WriteAllBytes(Path.Combine(output, id + ".png"), natural.ToPng());
            WriteThumbnail(output, model, id, title, family, mode, legend);
            width = (int)Math.Ceiling(natural.NaturalSize!.Value.Width);
            height = (int)Math.Ceiling(natural.NaturalSize.Value.Height);
            WriteRecipe(output, id, recipe, variant, title, subtitle, mode, width, height, legend, "Topology");
            artifacts.Add(new ProofArtifact(id, family, title, variant, mode.ToString().ToLowerInvariant(), width, height,
                new[] { "example.native-natural-size" }, natural.Regions.Count, kinds,
                new[] { "Complete authored geometry retains natural size; compact hosts may scroll." }, compact));
            return;
        }
        var prepared = model.Prepare(context);
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
        File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
        WriteThumbnail(output, model, id, title, family, mode, legend);
        var artifactKind = family is "topology" or "flow" or "sequence" ? char.ToUpperInvariant(family[0]) + family.Substring(1) : "Chart";
        WriteRecipe(output, id, recipe, variant, title, subtitle, mode, width, height, legend, artifactKind);
        artifacts.Add(new ProofArtifact(id, family, title, variant, mode.ToString().ToLowerInvariant(), width, height,
            prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count, kinds,
            prepared.Diagnostics.Select(diagnostic => diagnostic.Message).ToArray(), compact));
    }

    private static VisualRenderContext GalleryContext(int width, int height, VisualThemeMode mode, string title, string subtitle, bool? legend) =>
        new(new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode, new VisualFrame(title, subtitle, showLegend: legend), FontSpec.FromFamily(ProofFont));
    private static string FamilyName(ChartSeriesKind kind) => System.Text.RegularExpressions.Regex.Replace(kind.ToString(), "([a-z])([A-Z])", "$1-$2").ToLowerInvariant();
}
