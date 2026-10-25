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
        if (kind == ChartSeriesKind.Waterfall) variants.AddRange(new[] { "portable", "compact-portable", "portable-horizontal", "compact-portable-horizontal" });
        if (kind == ChartSeriesKind.StackedArea) variants.AddRange(new[] { "missing-zero", "compact-missing-zero", "missing-gap", "compact-missing-gap" });
        if (kind == ChartSeriesKind.RangeArea) variants.AddRange(new[] { "independent-boundaries", "compact-independent-boundaries" });
        if (kind == ChartSeriesKind.DifferenceArea) {
            variants.AddRange(new[] { "smooth-boundaries", "compact-smooth-boundaries" });
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
        if (kind is ChartSeriesKind.Pie or ChartSeriesKind.Donut) variants.AddRange(new[] { "variable-radius", "compact-variable-radius" });
        if (kind == ChartSeriesKind.Donut) variants.AddRange(new[] { "concentric", "compact-concentric", "automatic-rings", "compact-automatic-rings" });
        if (kind == ChartSeriesKind.Sunburst) variants.AddRange(new[] { "curved-labels", "compact-curved-labels" });
        if (kind == ChartSeriesKind.Treemap) variants.AddRange(new[] { "caption-policies", "compact-caption-policies" });
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
        if (kind == ChartSeriesKind.Scatter) { variants.Add("quadrants"); variants.Add("compact-quadrants"); variants.Add("pivot-quadrants"); variants.Add("compact-pivot-quadrants"); variants.Add("interior-quadrants"); variants.Add("compact-interior-quadrants"); }
        if (kind == ChartSeriesKind.Bubble) {
            variants.Add("plot-regions"); variants.Add("compact-plot-regions");
            variants.Add("custom-markers"); variants.Add("compact-custom-markers");
        }
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
        if (kind is ChartSeriesKind.Waterfall or ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot or ChartSeriesKind.Lollipop or ChartSeriesKind.Dumbbell) { variants.Add("horizontal"); variants.Add("compact-horizontal"); }
        if (kind == ChartSeriesKind.Dumbbell) { variants.Add("endpoint-markers"); variants.Add("compact-endpoint-markers"); }
        if (kind == ChartSeriesKind.Slope) variants.AddRange(new[] { "stages", "compact-stages", "horizontal-stages", "compact-horizontal-stages" });
        if (kind == ChartSeriesKind.Sankey) variants.AddRange(new[] { "source-colors", "compact-source-colors", "target-colors", "compact-target-colors", "quantitative-colors", "compact-quantitative-colors", "gradient-colors", "compact-gradient-colors" });
        if (kind == ChartSeriesKind.RangeBar) variants.AddRange(new[] { "endpoint-labels", "compact-endpoint-labels", "horizontal-endpoint-labels", "compact-horizontal-endpoint-labels" });
        if (kind == ChartSeriesKind.RangeBar) variants.AddRange(new[] { "independent-endpoint-labels", "compact-independent-endpoint-labels", "horizontal-independent-endpoint-labels", "compact-horizontal-independent-endpoint-labels" });
        if (kind == ChartSeriesKind.BoxPlot) variants.AddRange(new[] { "statistic-labels", "compact-statistic-labels", "horizontal-statistic-labels", "compact-horizontal-statistic-labels" });
        if (RelationshipOptionFamilies.Contains(kind)) {
            variants.Add("options");
            if (kind is ChartSeriesKind.Sankey or ChartSeriesKind.Sunburst) variants.Add("compact-options");
            if (kind == ChartSeriesKind.Sunburst) { variants.Add("authored-total"); variants.Add("compact-authored-total"); }
        }
        if (kind is ChartSeriesKind.TrendLine or ChartSeriesKind.Gauge) { variants.Add("precision"); variants.Add("compact-precision"); }
        if (kind == ChartSeriesKind.TrendLine) foreach (var model in new[] { "polynomial", "logarithmic", "exponential", "power", "confidence", "prediction" }) { variants.Add(model); variants.Add("compact-" + model); }
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
