using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

public static partial class V2Examples {
    private static readonly ChartSeriesKind[] SparseFamilies = {
        ChartSeriesKind.Line, ChartSeriesKind.Area, ChartSeriesKind.Scatter, ChartSeriesKind.RangeArea, ChartSeriesKind.Heatmap, ChartSeriesKind.CalendarHeatmap
    };
    private static readonly ChartSeriesKind[] RelationshipOptionFamilies = {
        ChartSeriesKind.Tree, ChartSeriesKind.Sunburst, ChartSeriesKind.Sankey, ChartSeriesKind.Treemap, ChartSeriesKind.Chord
    };
    private static readonly ChartSeriesKind[] OptionFamilies = {
        ChartSeriesKind.Bar, ChartSeriesKind.Area, ChartSeriesKind.HorizontalBar, ChartSeriesKind.Pie, ChartSeriesKind.Donut, ChartSeriesKind.Gauge, ChartSeriesKind.Bullet,
        ChartSeriesKind.ProgressRing, ChartSeriesKind.Heatmap, ChartSeriesKind.RegionMap, ChartSeriesKind.TileMap, ChartSeriesKind.Pictorial, ChartSeriesKind.ProgressBar
    };
    private static readonly ChartSeriesKind[] GeometryOptionFamilies = {
        ChartSeriesKind.Line, ChartSeriesKind.Bar, ChartSeriesKind.HorizontalBar, ChartSeriesKind.StackedArea, ChartSeriesKind.RangeArea, ChartSeriesKind.Funnel, ChartSeriesKind.Waterfall,
        ChartSeriesKind.Scatter, ChartSeriesKind.Bubble, ChartSeriesKind.Radar, ChartSeriesKind.Pyramid,
        ChartSeriesKind.RadialBar, ChartSeriesKind.RadialColumn, ChartSeriesKind.Candlestick, ChartSeriesKind.Ohlc
    };

    private static void WriteFamilies(string output, ICollection<ProofArtifact> artifacts, bool curated) {
        foreach (var kind in Enum.GetValues<ChartSeriesKind>()) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var variants = new List<string> { "wide", "compact" };
            if (kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble or ChartSeriesKind.Heatmap or ChartSeriesKind.HexbinHeatmap or ChartSeriesKind.CalendarHeatmap) {
                variants.Add("color-scale"); variants.Add("compact-color-scale");
            }
            if (kind is ChartSeriesKind.TileMap or ChartSeriesKind.Heatmap or ChartSeriesKind.Scatter) {
                variants.Add("positioned-color-scale"); variants.Add("compact-positioned-color-scale");
            }
            if (kind is ChartSeriesKind.TileMap or ChartSeriesKind.Heatmap or ChartSeriesKind.CalendarHeatmap or ChartSeriesKind.Treemap or ChartSeriesKind.Scatter or ChartSeriesKind.Bubble) {
                variants.Add("color-dimension"); variants.Add("compact-color-dimension");
            }
            if (kind is ChartSeriesKind.TileMap or ChartSeriesKind.Heatmap or ChartSeriesKind.Scatter) {
                variants.Add("gradient-color-legend"); variants.Add("compact-gradient-color-legend");
                variants.Add("swatch-color-legend"); variants.Add("compact-swatch-color-legend");
            }
            if (kind is ChartSeriesKind.TileMap or ChartSeriesKind.Scatter) {
                variants.Add("partial-color-range"); variants.Add("compact-partial-color-range");
            }
            if (GeometryOptionFamilies.Contains(kind)) { variants.Add("options"); variants.Add("compact-options"); }
            if (kind is ChartSeriesKind.RadialBar or ChartSeriesKind.RadialColumn) {
                variants.Add("axis-labels"); variants.Add("compact-axis-labels");
                variants.Add("all-labels"); variants.Add("compact-all-labels");
            }
            if (kind == ChartSeriesKind.Funnel) { variants.Add("cone-vertical"); variants.Add("stage-bars-horizontal"); }
            if (kind == ChartSeriesKind.Waterfall) { variants.Add("horizontal"); variants.Add("compact-horizontal"); }
            if (RelationshipOptionFamilies.Contains(kind)) {
                variants.Add("options");
                if (kind is ChartSeriesKind.Sankey or ChartSeriesKind.Sunburst) variants.Add("compact-options");
                if (kind == ChartSeriesKind.Sunburst) { variants.Add("authored-total"); variants.Add("compact-authored-total"); }
            }
            if (kind is ChartSeriesKind.TrendLine or ChartSeriesKind.Gauge) { variants.Add("precision"); variants.Add("compact-precision"); }
            if (!curated) {
                if (SparseFamilies.Contains(kind)) variants.Add("sparse");
                if (OptionFamilies.Contains(kind) && !variants.Contains("options")) variants.Add("options");
            }
            foreach (var variant in variants) {
                var precision = variant is "precision" or "compact-precision";
                var chart = V2GalleryModels.Create(kind, variant, mode); var title = precision ? kind == ChartSeriesKind.Gauge ? "Measured tolerance" : "Small signed drift"
                    : variant is "partial-color-range" or "compact-partial-color-range" ? kind == ChartSeriesKind.TileMap ? "Requests by state" : "Processing time and completion"
                    : variant is "positioned-color-scale" or "compact-positioned-color-scale" && kind is ChartSeriesKind.TileMap or ChartSeriesKind.Heatmap
                        ? kind == ChartSeriesKind.TileMap ? "Temperature by state" : "Temperature across service areas"
                    : variant is "color-dimension" or "compact-color-dimension" or "gradient-color-legend" or "compact-gradient-color-legend" or "swatch-color-legend" or "compact-swatch-color-legend" ? kind switch {
                        ChartSeriesKind.TileMap => "Temperature by state", ChartSeriesKind.Heatmap => "Temperature by grid position",
                        ChartSeriesKind.CalendarHeatmap => "Daily average temperature", ChartSeriesKind.Treemap => "Team workload and temperature",
                        ChartSeriesKind.Scatter => "Temperature by workload", _ => "Workload, requests and temperature"
                    } : V2GalleryModels.Title(kind);
                if (!chart.Series.Any(series => series.Kind == kind)) throw new InvalidOperationException("The gallery factory did not create its declared chart kind: " + kind);
                var family = FamilyName(kind); var id = "family-" + family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                var compact = IsCompactVariant(variant);
                var width = compact ? 360 : 800; var height = compact ? 360 : 440;
                // Let the shared policy decide whether a legend adds information. The indicator
                // options example intentionally demonstrates an explicitly requested legend.
                bool? legend = variant == "options" && kind is ChartSeriesKind.Gauge or ChartSeriesKind.Bullet ? true : null;
                if (kind == ChartSeriesKind.Scatter && variant is "options" or "compact-options") legend = false;
                var subtitle = precision ? kind == ChartSeriesKind.Gauge ? "Close bounds, measurement and target retain distinct captions" : "Measured differences stay in their original units"
                    : variant is "axis-labels" or "compact-axis-labels" ? "Upright axis-relative labels with independent density"
                    : variant is "all-labels" or "compact-all-labels" ? "All captions fit individually; caption overlaps are intentional"
                    : variant == "cone-vertical" ? "Vertical cone; stage lines encode source values"
                    : variant == "stage-bars-horizontal" ? "Horizontal stage bars; extents remain proportional"
                    : kind == ChartSeriesKind.Waterfall && variant is "horizontal" or "compact-horizontal" ? "Horizontal changes and checkpoints share a cumulative value axis"
                    : kind == ChartSeriesKind.Sunburst && variant is "authored-total" or "compact-authored-total" ? "Inclusive parent totals retain unallocated remainder"
                    : kind == ChartSeriesKind.Sunburst && variant is "options" or "compact-options" ? "Leaf totals size sectors; measured color stays independent"
                    : kind == ChartSeriesKind.Sankey && variant is "options" or "compact-options" ? "Aligned and ordered weighted flows"
                    : variant is "partial-color-range" or "compact-partial-color-range" ? kind == ChartSeriesKind.TileMap
                        ? "Zero stays fixed; the color peak follows reported counts" : "The color ceiling stays at 100%; the lower bound follows observations"
                    : variant is "gradient-color-legend" or "compact-gradient-color-legend" ? "A continuous key retains every authored color anchor"
                    : variant is "swatch-color-legend" or "compact-swatch-color-legend" ? "A sampled key keeps the continuous measurement and its color domain"
                    : variant is "color-dimension" or "compact-color-dimension" ? "Temperature captions use their own format and culture"
                    : variant is "positioned-color-scale" or "compact-positioned-color-scale" ? "Absolute color anchors use uneven numeric intervals"
                    : variant is "color-scale" or "compact-color-scale" ? kind == ChartSeriesKind.Scatter ? "Temperature has its own fixed color domain; processing time stays independent"
                        : kind == ChartSeriesKind.Bubble ? "Size shows requests; named color bands show temperature"
                        : kind == ChartSeriesKind.Heatmap ? "Signed measurements share a fixed color domain"
                        : kind == ChartSeriesKind.HexbinHeatmap ? "Named numeric intervals keep their exact boundaries" : "Every observed value shares one scale; missing days stay distinct"
                    : variant is "options" or "compact-options" && GeometryOptionFamilies.Contains(kind) ? GeometrySubtitle(kind)
                    : variant switch { "sparse" => "Missing observations remain visible as gaps", "options" => "Explore configured marks, scales and labels", "compact" => "The same data in a compact view", _ => "Explore the data, then download the chart" };
                WriteModel(output, artifacts, chart, id, family, title, variant, subtitle, mode, width, height, legend,
                    "V2GalleryModels.Create(ChartSeriesKind." + kind + ", " + Literal(variant) + ", VisualThemeMode." + mode + ")", chart.Series.Select(series => series.Kind.ToString()).Distinct().ToArray(), compact: compact);
            }
            if (kind == ChartSeriesKind.RadialBar) WriteNumericRadialAxes(output, artifacts, mode);
        }
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) WriteMixedRadial(output, artifacts, mode);
        WriteExpandedDiagrams(output, artifacts, curated);
    }

    private static void WriteMixedRadial(string output, ICollection<ProofArtifact> artifacts, VisualThemeMode mode) {
        foreach (var compact in new[] { false, true }) {
            var variant = compact ? "compact-mixed" : "mixed";
            WriteModel(output, artifacts, V2GalleryModels.CreateMixedRadial(), "family-radial-column-" + variant + "-" + mode.ToString().ToLowerInvariant(),
                "radial-column", "Regional counts and targets", variant, "Columns, reference area and target line share categories and radius", mode,
                compact ? 360 : 800, compact ? 360 : 440, true, "V2GalleryModels.CreateMixedRadial()", new[] { "RadialColumn", "Radar" }, compact: compact);
        }
    }

    private static void WriteNumericRadialAxes(string output, ICollection<ProofArtifact> artifacts, VisualThemeMode mode) {
        foreach (var compact in new[] { false, true }) {
            var variant = compact ? "compact-dual-axes" : "dual-axes";
            var id = "family-radial-bar-" + variant + "-" + mode.ToString().ToLowerInvariant();
            WriteModel(output, artifacts, V2GalleryModels.CreateRadialBarScales(), id, "radial-bar", "Requests and resolution rate", variant,
                "Request counts and percentages use independent scales", mode, compact ? 360 : 800, compact ? 360 : 440, true,
                "V2GalleryModels.CreateRadialBarScales()", new[] { "RadialBar" }, compact: compact);
        }
    }

    private static string GeometrySubtitle(ChartSeriesKind kind) => kind switch {
        ChartSeriesKind.Line => "Step transitions and three configured marker shapes",
        ChartSeriesKind.Bar or ChartSeriesKind.HorizontalBar => "Two independent stacks; each reaches 100%; source values remain counts",
        ChartSeriesKind.StackedArea => "A normalized stack with middle-step boundaries",
        ChartSeriesKind.RangeArea => "Lower, middle and upper bounds share middle-step transitions",
        ChartSeriesKind.Funnel => "Horizontal cone; stage lines encode values including zero",
        ChartSeriesKind.Waterfall => "An opening balance, changes and checkpoints retain the running balance on reversed axes",
        ChartSeriesKind.Scatter => "Nine marker shapes with source labels",
        ChartSeriesKind.Bubble => "Equal request volumes share a radius; values above 100 retain their facts",
        ChartSeriesKind.Candlestick => "Hollow rising bodies, filled falling bodies and independent wick strokes",
        ChartSeriesKind.Ohlc => "Rising and falling stroke colors, alpha and width retain source prices",
        ChartSeriesKind.Radar => "A filled area and an unfilled target line share one radial scale",
        ChartSeriesKind.Pyramid => "Reversed horizontal pyramid; areas encode values including zero",
        ChartSeriesKind.RadialBar or ChartSeriesKind.RadialColumn => "Named stacks reach 100; labels retain source counts",
        _ => "Configured chart geometry"
    };

    private static void WriteModel(string output, ICollection<ProofArtifact> artifacts, IVisualRenderable model, string id, string family, string title,
        string variant, string subtitle, VisualThemeMode mode, int width, int height, bool? legend, string expression, string[]? kinds = null, bool compact = false) {
        var context = GalleryContext(width, height, mode, title, subtitle, legend);
        var prepared = model.Prepare(context);
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
        File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
        WriteThumbnail(output, model, id, title, family, mode, legend);
        var artifactKind = family is "topology" or "flow" or "sequence" ? char.ToUpperInvariant(family[0]) + family.Substring(1) : "Chart";
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".csharp.txt"), ModelSnippet(expression, title, subtitle, mode, width, height, legend, artifactKind));
        artifacts.Add(new ProofArtifact(id, family, title, variant, mode.ToString().ToLowerInvariant(), width, height,
            prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count, kinds,
            prepared.Diagnostics.Select(diagnostic => diagnostic.Message).ToArray(), compact));
    }

    private static VisualRenderContext GalleryContext(int width, int height, VisualThemeMode mode, string title, string subtitle, bool? legend) =>
        new(new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode, new VisualFrame(title, subtitle, showLegend: legend), FontSpec.FromFamily(ProofFont));
    private static string FamilyName(ChartSeriesKind kind) => System.Text.RegularExpressions.Regex.Replace(kind.ToString(), "([a-z])([A-Z])", "$1-$2").ToLowerInvariant();
}
