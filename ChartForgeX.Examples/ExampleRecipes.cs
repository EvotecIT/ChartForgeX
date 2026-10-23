using System.Collections.ObjectModel;

/// <summary>Compiled example factories and their native source files. This catalog is development tooling only.</summary>
public static partial class ExampleRecipes {
    private static readonly IReadOnlyDictionary<ChartSeriesKind, ExampleRecipe> Families = new ReadOnlyDictionary<ChartSeriesKind, ExampleRecipe>(new Dictionary<ChartSeriesKind, ExampleRecipe> {
        [ChartSeriesKind.Line] = new(nameof(LineRecipe), LineRecipe.Create),
        [ChartSeriesKind.StepLine] = new(nameof(StepLineRecipe), StepLineRecipe.Create),
        [ChartSeriesKind.Area] = new(nameof(AreaRecipe), AreaRecipe.Create),
        [ChartSeriesKind.StepArea] = new(nameof(StepAreaRecipe), StepAreaRecipe.Create),
        [ChartSeriesKind.Scatter] = new(nameof(ScatterRecipe), ScatterRecipe.Create),
        [ChartSeriesKind.Bubble] = new(nameof(BubbleRecipe), BubbleRecipe.Create),
        [ChartSeriesKind.ErrorBar] = new(nameof(ErrorBarRecipe), ErrorBarRecipe.Create),
        [ChartSeriesKind.Candlestick] = new(nameof(CandlestickRecipe), CandlestickRecipe.Create),
        [ChartSeriesKind.RangeBand] = new(nameof(RangeBandRecipe), RangeBandRecipe.Create),
        [ChartSeriesKind.RangeArea] = new(nameof(RangeAreaRecipe), RangeAreaRecipe.Create),
        [ChartSeriesKind.Bar] = new(nameof(BarRecipe), BarRecipe.Create),
        [ChartSeriesKind.Lollipop] = new(nameof(LollipopRecipe), LollipopRecipe.Create),
        [ChartSeriesKind.Dumbbell] = new(nameof(DumbbellRecipe), DumbbellRecipe.Create),
        [ChartSeriesKind.RangeBar] = new(nameof(RangeBarRecipe), RangeBarRecipe.Create),
        [ChartSeriesKind.BoxPlot] = new(nameof(BoxPlotRecipe), BoxPlotRecipe.Create),
        [ChartSeriesKind.HorizontalBar] = new(nameof(HorizontalBarRecipe), HorizontalBarRecipe.Create),
        [ChartSeriesKind.Heatmap] = new(nameof(HeatmapRecipe), HeatmapRecipe.Create),
        [ChartSeriesKind.HexbinHeatmap] = new(nameof(HexbinHeatmapRecipe), HexbinHeatmapRecipe.Create),
        [ChartSeriesKind.CalendarHeatmap] = new(nameof(CalendarHeatmapRecipe), CalendarHeatmapRecipe.Create),
        [ChartSeriesKind.DottedMap] = new(nameof(DottedMapRecipe), DottedMapRecipe.Create),
        [ChartSeriesKind.TileMap] = new(nameof(TileMapRecipe), TileMapRecipe.Create),
        [ChartSeriesKind.RegionMap] = new(nameof(RegionMapRecipe), RegionMapRecipe.Create),
        [ChartSeriesKind.Gauge] = new(nameof(GaugeRecipe), GaugeRecipe.Create),
        [ChartSeriesKind.Circle] = new(nameof(CircleRecipe), CircleRecipe.Create),
        [ChartSeriesKind.ProgressRing] = new(nameof(ProgressRingRecipe), ProgressRingRecipe.Create),
        [ChartSeriesKind.LayeredRadial] = new(nameof(LayeredRadialRecipe), LayeredRadialRecipe.Create),
        [ChartSeriesKind.Bullet] = new(nameof(BulletRecipe), BulletRecipe.Create),
        [ChartSeriesKind.Waterfall] = new(nameof(WaterfallRecipe), WaterfallRecipe.Create),
        [ChartSeriesKind.Radar] = new(nameof(RadarRecipe), RadarRecipe.Create),
        [ChartSeriesKind.Funnel] = new(nameof(FunnelRecipe), FunnelRecipe.Create),
        [ChartSeriesKind.Timeline] = new(nameof(TimelineRecipe), TimelineRecipe.Create),
        [ChartSeriesKind.Gantt] = new(nameof(GanttRecipe), GanttRecipe.Create),
        [ChartSeriesKind.Sankey] = new(nameof(SankeyRecipe), SankeyRecipe.Create),
        [ChartSeriesKind.Tree] = new(nameof(TreeRecipe), TreeRecipe.Create),
        [ChartSeriesKind.Sunburst] = new(nameof(SunburstRecipe), SunburstRecipe.Create),
        [ChartSeriesKind.Pie] = new(nameof(PieRecipe), PieRecipe.Create),
        [ChartSeriesKind.Donut] = new(nameof(DonutRecipe), DonutRecipe.Create),
        [ChartSeriesKind.StackedArea] = new(nameof(StackedAreaRecipe), StackedAreaRecipe.Create),
        [ChartSeriesKind.Slope] = new(nameof(SlopeRecipe), SlopeRecipe.Create),
        [ChartSeriesKind.Treemap] = new(nameof(TreemapRecipe), TreemapRecipe.Create),
        [ChartSeriesKind.Pictorial] = new(nameof(PictorialRecipe), PictorialRecipe.Create),
        [ChartSeriesKind.ProgressBar] = new(nameof(ProgressBarRecipe), ProgressBarRecipe.Create),
        [ChartSeriesKind.WordCloud] = new(nameof(WordCloudRecipe), WordCloudRecipe.Create),
        [ChartSeriesKind.Ohlc] = new(nameof(OhlcRecipe), OhlcRecipe.Create),
        [ChartSeriesKind.PolarArea] = new(nameof(PolarAreaRecipe), PolarAreaRecipe.Create),
        [ChartSeriesKind.TrendLine] = new(nameof(TrendLineRecipe), TrendLineRecipe.Create),
        [ChartSeriesKind.Polar] = new(nameof(PolarRecipe), PolarRecipe.Create),
        [ChartSeriesKind.StateTimeline] = new(nameof(StateTimelineRecipe), StateTimelineRecipe.Create),
        [ChartSeriesKind.GanttLane] = new(nameof(GanttLaneRecipe), GanttLaneRecipe.Create),
        [ChartSeriesKind.Pyramid] = new(nameof(PyramidRecipe), PyramidRecipe.Create),
        [ChartSeriesKind.RadialBar] = new(nameof(RadialBarRecipe), RadialBarRecipe.Create),
        [ChartSeriesKind.RadialColumn] = new(nameof(RadialColumnRecipe), RadialColumnRecipe.Create),
        [ChartSeriesKind.Chord] = new(nameof(ChordRecipe), ChordRecipe.Create),
        [ChartSeriesKind.DifferenceArea] = new(nameof(DifferenceAreaRecipe), DifferenceAreaRecipe.Create),
    });
    private static readonly IReadOnlyDictionary<string, ExampleRecipe> NamedRecipes = new ReadOnlyDictionary<string, ExampleRecipe>(new Dictionary<string, ExampleRecipe>(StringComparer.Ordinal) {
        ["license-cost"] = new(nameof(LicenseCostRecipe), LicenseCostRecipe.Create),
        ["dashboard-premium-trend"] = new(nameof(DashboardPremiumTrendRecipe), DashboardPremiumTrendRecipe.Create),
        ["control-coverage-heatmap"] = new(nameof(ControlCoverageRecipe), ControlCoverageRecipe.Create),
        ["travel-dotted-map"] = new(nameof(TravelDottedMapRecipe), TravelDottedMapRecipe.Create),
        ["histogram"] = new(nameof(HistogramRecipe), HistogramRecipe.Create),
        ["chart-grid"] = new(nameof(ChartGridRecipe), ChartGridRecipe.Create),
        ["topology"] = new(nameof(TopologyRecipe), TopologyRecipe.Create),
        ["flow"] = new(nameof(FlowRecipe), FlowRecipe.Create),
        ["sequence"] = new(nameof(SequenceRecipe), SequenceRecipe.Create),
        ["simple-topology"] = new(nameof(SimpleTopologyRecipe), SimpleTopologyRecipe.Create),
        ["simple-sequence"] = new(nameof(SimpleSequenceRecipe), SimpleSequenceRecipe.Create),
        ["equal-pie"] = new(nameof(EqualPieRecipe), EqualPieRecipe.Create),
        ["equal-donut"] = new(nameof(EqualDonutRecipe), EqualDonutRecipe.Create),
        ["mixed-radial"] = new(nameof(MixedRadialRecipe), (variant, mode) => MixedRadialRecipe.Create(variant, mode)),
        ["radial-bar-scales"] = new(nameof(RadialBarScalesRecipe), RadialBarScalesRecipe.Create),
        ["comparison"] = new(nameof(ComparisonRecipe), ComparisonRecipe.Create),
        ["supported-combinations"] = new(nameof(SupportedCombinationRecipe), SupportedCombinationRecipe.Create),
        ["findings"] = new(nameof(FindingsRecipe), FindingsRecipe.Create),
        ["line-viewport"] = new(nameof(LineViewportRecipe), LineViewportRecipe.Create),
        ["step-line-viewport"] = new(nameof(StepLineViewportRecipe), StepLineViewportRecipe.Create),
        ["keyed-point-changes"] = new(nameof(KeyedPointChangesRecipe), KeyedPointChangesRecipe.Create),
        ["financial-layers"] = new(nameof(FinancialLayersRecipe), FinancialLayersRecipe.Create),
        ["geographic-datasets"] = new(nameof(GeographicDatasetsRecipe), GeographicDatasetsRecipe.Create),
        ["source-drawings"] = new(nameof(SourceDrawingsRecipe), SourceDrawingsRecipe.Create)
    });

    private static readonly IReadOnlyDictionary<(ChartSeriesKind Kind, string Variant), ExampleRecipe> Options = OptionRecipes();

    /// <summary>Gets the executable recipe for the selected chart family and option variant.</summary>
    public static ExampleRecipe ForKind(ChartSeriesKind kind, string variant = "wide") {
        var option = variant.StartsWith("compact-", StringComparison.Ordinal) ? variant.Substring(8) : variant;
        // These compact fixtures deliberately retain the ordinary numeric model.
        if (variant == "compact-options" && kind is ChartSeriesKind.Heatmap or ChartSeriesKind.TileMap) option = "wide";
        if (Options.TryGetValue((kind, option), out var advanced)) return advanced;
        return Families.TryGetValue(kind, out var recipe) ? recipe : throw new ArgumentOutOfRangeException(nameof(kind));
    }

    /// <summary>Gets an executable diagram, composition or explicit comparison recipe.</summary>
    public static ExampleRecipe Named(string name) => NamedRecipes.TryGetValue(name, out var recipe)
        ? recipe : throw new ArgumentOutOfRangeException(nameof(name));

    /// <summary>Gets the ordinary chart-family recipes for compile and execution qualification.</summary>
    public static IEnumerable<KeyValuePair<ChartSeriesKind, ExampleRecipe>> ChartFamilies => Families;

    /// <summary>Gets diagram, composition and comparison recipes for compile and execution qualification.</summary>
    public static IEnumerable<KeyValuePair<string, ExampleRecipe>> OtherRecipes => NamedRecipes;

    /// <summary>Gets advanced option selections for standalone execution qualification.</summary>
    public static IEnumerable<KeyValuePair<(ChartSeriesKind Kind, string Variant), ExampleRecipe>> OptionVariants => Options;

    private static IReadOnlyDictionary<(ChartSeriesKind, string), ExampleRecipe> OptionRecipes() {
        var recipes = new Dictionary<(ChartSeriesKind, string), ExampleRecipe>();
        void Add(ChartSeriesKind kind, string factory, Func<string, VisualThemeMode, IVisualRenderable> create, params string[] variants) {
            var recipe = new ExampleRecipe(factory, create);
            foreach (var variant in variants) recipes.Add((kind, variant), recipe);
        }
        Add(ChartSeriesKind.Donut, nameof(ConcentricPieRecipe), ConcentricPieRecipe.Create, "concentric");
        Add(ChartSeriesKind.TrendLine, nameof(TrendLineModelRecipe), TrendLineModelRecipe.Create, "polynomial", "logarithmic", "exponential", "power", "confidence", "prediction");
        Add(ChartSeriesKind.Sankey, nameof(SankeyOptionsRecipe), SankeyOptionsRecipe.Create, "options", "source-colors", "target-colors", "quantitative-colors");
        Add(ChartSeriesKind.Lollipop, nameof(LollipopHorizontalRecipe), LollipopHorizontalRecipe.Create, "horizontal");
        Add(ChartSeriesKind.Dumbbell, nameof(DumbbellEndpointRecipe), DumbbellEndpointRecipe.Create, "horizontal", "endpoint-markers");
        Add(ChartSeriesKind.Chord, nameof(ChordRecipe), ChordRecipe.Create, "color-dimension");
        Add(ChartSeriesKind.Scatter, nameof(ScatterMarkerShapesRecipe), ScatterMarkerShapesRecipe.Create, "options");
        Add(ChartSeriesKind.Scatter, nameof(ScatterColorScaleRecipe), ScatterColorScaleRecipe.Create, "color-scale");
        Add(ChartSeriesKind.Scatter, nameof(ScatterPositionedColorScaleRecipe), ScatterPositionedColorScaleRecipe.Create, "positioned-color-scale");
        Add(ChartSeriesKind.Scatter, nameof(ScatterColorDimensionRecipe), ScatterColorDimensionRecipe.Create, "color-dimension", "gradient-color-legend", "swatch-color-legend");
        Add(ChartSeriesKind.Scatter, nameof(ScatterQuadrantsRecipe), ScatterQuadrantsRecipe.Create, "quadrants");
        Add(ChartSeriesKind.Scatter, nameof(ScatterPartialColorRangeRecipe), ScatterPartialColorRangeRecipe.Create, "partial-color-range");
        Add(ChartSeriesKind.Bubble, nameof(BubbleMarkerOptionsRecipe), BubbleMarkerOptionsRecipe.Create, "options", "custom-markers");
        Add(ChartSeriesKind.Bubble, nameof(BubbleColorScaleRecipe), BubbleColorScaleRecipe.Create, "color-scale");
        Add(ChartSeriesKind.Bubble, nameof(BubbleColorDimensionRecipe), BubbleColorDimensionRecipe.Create, "color-dimension");
        Add(ChartSeriesKind.Bubble, nameof(BubblePlotRegionsRecipe), BubblePlotRegionsRecipe.Create, "plot-regions");
        Add(ChartSeriesKind.Heatmap, nameof(HeatmapCategoryRecipe), HeatmapCategoryRecipe.Create, "options");
        Add(ChartSeriesKind.Heatmap, nameof(HeatmapColorScaleRecipe), HeatmapColorScaleRecipe.Create, "color-scale");
        Add(ChartSeriesKind.Heatmap, nameof(HeatmapPositionedColorScaleRecipe), HeatmapPositionedColorScaleRecipe.Create, "positioned-color-scale");
        Add(ChartSeriesKind.Heatmap, nameof(HeatmapColorDimensionRecipe), HeatmapColorDimensionRecipe.Create, "color-dimension", "gradient-color-legend", "swatch-color-legend");
        Add(ChartSeriesKind.HexbinHeatmap, nameof(HexbinHeatmapColorScaleRecipe), HexbinHeatmapColorScaleRecipe.Create, "color-scale");
        Add(ChartSeriesKind.CalendarHeatmap, nameof(CalendarHeatmapColorScaleRecipe), CalendarHeatmapColorScaleRecipe.Create, "color-scale");
        Add(ChartSeriesKind.CalendarHeatmap, nameof(CalendarHeatmapColorDimensionRecipe), CalendarHeatmapColorDimensionRecipe.Create, "color-dimension");
        Add(ChartSeriesKind.RegionMap, nameof(RegionMapRecipe), RegionMapRecipe.Create, "color-dimension");
        Add(ChartSeriesKind.TileMap, nameof(TileMapOptionsRecipe), TileMapOptionsRecipe.Create, "options");
        Add(ChartSeriesKind.TileMap, nameof(TileMapPositionedColorScaleRecipe), TileMapPositionedColorScaleRecipe.Create, "positioned-color-scale");
        Add(ChartSeriesKind.TileMap, nameof(TileMapColorDimensionRecipe), TileMapColorDimensionRecipe.Create, "color-dimension", "gradient-color-legend", "swatch-color-legend");
        Add(ChartSeriesKind.TileMap, nameof(TileMapPartialColorRangeRecipe), TileMapPartialColorRangeRecipe.Create, "partial-color-range");
        Add(ChartSeriesKind.Treemap, nameof(TreemapColorDimensionRecipe), TreemapColorDimensionRecipe.Create, "color-dimension");
        return new ReadOnlyDictionary<(ChartSeriesKind, string), ExampleRecipe>(recipes);
    }
}

/// <summary>A strongly typed factory paired with exactly one compiled native source file.</summary>
public sealed class ExampleRecipe {
    private readonly Func<string, VisualThemeMode, IVisualRenderable> _create;
    internal ExampleRecipe(string factory, Func<string, VisualThemeMode, IVisualRenderable> create) { Factory = factory; _create = create; }
    /// <summary>Gets the native factory class used by the downloaded program.</summary>
    public string Factory { get; }
    /// <summary>Creates the same model that the gallery and standalone program render.</summary>
    public IVisualRenderable Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) => _create(variant, mode);
}
