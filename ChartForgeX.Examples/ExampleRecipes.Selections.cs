/// <summary>Gallery and standalone qualification selections for compiled example recipes.</summary>
public static partial class ExampleRecipes {
    private static readonly IReadOnlyCollection<ChartSeriesKind> SparseFamilies = Array.AsReadOnly(new[] {
        ChartSeriesKind.Line, ChartSeriesKind.Area, ChartSeriesKind.Scatter, ChartSeriesKind.RangeArea, ChartSeriesKind.Heatmap, ChartSeriesKind.CalendarHeatmap
    });
    private static readonly IReadOnlyCollection<ChartSeriesKind> RelationshipOptionFamilies = Array.AsReadOnly(new[] {
        ChartSeriesKind.Tree, ChartSeriesKind.Sunburst, ChartSeriesKind.Sankey, ChartSeriesKind.Treemap, ChartSeriesKind.Chord
    });
    private static readonly IReadOnlyCollection<ChartSeriesKind> OptionFamilies = Array.AsReadOnly(new[] {
        ChartSeriesKind.Bar, ChartSeriesKind.Area, ChartSeriesKind.HorizontalBar, ChartSeriesKind.Pie, ChartSeriesKind.Donut, ChartSeriesKind.Gauge, ChartSeriesKind.Bullet,
        ChartSeriesKind.ProgressRing, ChartSeriesKind.Heatmap, ChartSeriesKind.RegionMap, ChartSeriesKind.TileMap, ChartSeriesKind.Pictorial, ChartSeriesKind.ProgressBar
    });
    private static readonly IReadOnlyCollection<ChartSeriesKind> GeometryOptionFamilies = Array.AsReadOnly(new[] {
        ChartSeriesKind.Line, ChartSeriesKind.Bar, ChartSeriesKind.HorizontalBar, ChartSeriesKind.StackedArea, ChartSeriesKind.RangeArea, ChartSeriesKind.RangeBar, ChartSeriesKind.BoxPlot, ChartSeriesKind.Funnel, ChartSeriesKind.Waterfall,
        ChartSeriesKind.Scatter, ChartSeriesKind.Bubble, ChartSeriesKind.Radar, ChartSeriesKind.Pyramid,
        ChartSeriesKind.RadialBar, ChartSeriesKind.RadialColumn, ChartSeriesKind.Candlestick, ChartSeriesKind.Ohlc, ChartSeriesKind.DottedMap
    });

    /// <summary>Gets the exact family selections advertised by the full or curated gallery.</summary>
    public static IEnumerable<ExampleFamilySelection> FamilySelections(ChartSeriesKind kind, bool curated = false) {
        var variants = new List<string> { "wide", "compact" };
        if (kind == ChartSeriesKind.DifferenceArea) {
            variants.Add("dated-gaps"); variants.Add("compact-dated-gaps");
            variants.Add("step-start"); variants.Add("step-middle"); variants.Add("step-end"); variants.Add("compact-step-middle");
        }
        if (kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble or ChartSeriesKind.Heatmap or ChartSeriesKind.HexbinHeatmap or ChartSeriesKind.CalendarHeatmap) {
            variants.Add("color-scale"); variants.Add("compact-color-scale");
        }
        if (kind is ChartSeriesKind.TileMap or ChartSeriesKind.Heatmap or ChartSeriesKind.Scatter) {
            variants.Add("positioned-color-scale"); variants.Add("compact-positioned-color-scale");
        }
        if (kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap or ChartSeriesKind.Heatmap or ChartSeriesKind.CalendarHeatmap or ChartSeriesKind.Treemap or ChartSeriesKind.Scatter or ChartSeriesKind.Bubble or ChartSeriesKind.Chord) {
            variants.Add("color-dimension"); variants.Add("compact-color-dimension");
        }
        if (kind is ChartSeriesKind.TileMap or ChartSeriesKind.Heatmap or ChartSeriesKind.Scatter) {
            variants.Add("gradient-color-legend"); variants.Add("compact-gradient-color-legend");
            variants.Add("swatch-color-legend"); variants.Add("compact-swatch-color-legend");
        }
        if (kind == ChartSeriesKind.Pyramid) { variants.Add("dense"); variants.Add("compact-dense"); }
        if (kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea or ChartSeriesKind.DifferenceArea) {
            variants.AddRange(new[] { "singleton", "compact-singleton" });
            if (kind != ChartSeriesKind.RangeArea) variants.AddRange(new[] { "dense", "compact-dense" });
        }
        if (kind == ChartSeriesKind.RangeBar) variants.AddRange(new[] {
            "dense", "compact-dense", "dense-horizontal", "compact-dense-horizontal", "sparse-catalog", "compact-sparse-catalog"
        });
        if (kind == ChartSeriesKind.Bullet) variants.AddRange(new[] { "dense", "compact-dense" });
        if (kind == ChartSeriesKind.Gauge) variants.AddRange(new[] { "dense-scale", "compact-dense-scale" });
        if (kind == ChartSeriesKind.Scatter) { variants.Add("quadrants"); variants.Add("compact-quadrants"); }
        if (kind == ChartSeriesKind.Bubble) { variants.Add("plot-regions"); variants.Add("compact-plot-regions"); }
        if (kind is ChartSeriesKind.TileMap or ChartSeriesKind.Scatter) {
            variants.Add("partial-color-range"); variants.Add("compact-partial-color-range");
        }
        if (GeometryOptionFamilies.Contains(kind)) { variants.Add("options"); variants.Add("compact-options"); }
        if (kind is ChartSeriesKind.Radar or ChartSeriesKind.Polar or ChartSeriesKind.PolarArea) {
            variants.Add("angular-reversal"); variants.Add("compact-angular-reversal");
        }
        if (kind is ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc) { variants.Add("dashed"); variants.Add("compact-dashed"); }
        if (kind is ChartSeriesKind.RadialBar or ChartSeriesKind.RadialColumn) {
            variants.Add("axis-labels"); variants.Add("compact-axis-labels");
            variants.Add("all-labels"); variants.Add("compact-all-labels");
        }
        if (kind == ChartSeriesKind.Funnel) { variants.Add("cone-vertical"); variants.Add("stage-bars-horizontal"); }
        if (kind is ChartSeriesKind.Waterfall or ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot) { variants.Add("horizontal"); variants.Add("compact-horizontal"); }
        if (kind == ChartSeriesKind.RangeBar) variants.AddRange(new[] { "endpoint-labels", "compact-endpoint-labels", "horizontal-endpoint-labels", "compact-horizontal-endpoint-labels" });
        if (RelationshipOptionFamilies.Contains(kind)) {
            variants.Add("options");
            if (kind is ChartSeriesKind.Sankey or ChartSeriesKind.Sunburst) variants.Add("compact-options");
            if (kind == ChartSeriesKind.Sunburst) { variants.Add("authored-total"); variants.Add("compact-authored-total"); }
        }
        if (kind is ChartSeriesKind.TrendLine or ChartSeriesKind.Gauge) { variants.Add("precision"); variants.Add("compact-precision"); }
        if (kind == ChartSeriesKind.Gauge) variants.AddRange(new[] { "vertical", "compact-vertical", "semicircle", "compact-semicircle", "full-circle", "compact-full-circle" });
        if (!curated) {
            if (SparseFamilies.Contains(kind)) variants.Add("sparse");
            if (OptionFamilies.Contains(kind) && !variants.Contains("options")) variants.Add("options");
        }
        foreach (var variant in variants) yield return new ExampleFamilySelection(kind, variant);
    }

    /// <summary>Qualifies every advertised selection together with the ordinary and option cases retained by standalone consumers.</summary>
    public static IEnumerable<ExampleFamilySelection> StandaloneFamilySelections() {
        foreach (var family in ChartFamilies) {
            var visited = new HashSet<ExampleFamilySelection>();
            foreach (var variant in new[] { "wide", "compact", "sparse", "options", "compact-options" }) {
                var selection = new ExampleFamilySelection(family.Key, variant);
                if (visited.Add(selection)) yield return selection;
            }
            foreach (var option in OptionVariants.Where(option => option.Key.Kind == family.Key)) {
                foreach (var variant in new[] { option.Key.Variant, "compact-" + option.Key.Variant }) {
                    var selection = new ExampleFamilySelection(family.Key, variant);
                    if (visited.Add(selection)) yield return selection;
                }
            }
            foreach (var selection in FamilySelections(family.Key))
                if (visited.Add(selection)) yield return selection;
        }
    }

    internal static bool HasGeometryOptions(ChartSeriesKind kind) => GeometryOptionFamilies.Contains(kind);
}

/// <summary>An immutable family and variant selection shared by gallery generation and standalone qualification.</summary>
public readonly record struct ExampleFamilySelection(ChartSeriesKind Kind, string Variant) {
    /// <summary>Gets the compiled factory for this selection.</summary>
    public ExampleRecipe Recipe => ExampleRecipes.ForKind(Kind, Variant);
}
