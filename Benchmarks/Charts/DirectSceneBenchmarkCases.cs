using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>Measures preparation separately from complete and already-prepared SVG/native-raster exports.</summary>
public static class DirectSceneBenchmarkCases {
    private static VisualRenderContext _context = null!;
    private static VisualRenderContext _phase3Context = null!;
    private static VisualRenderContext _topologyContext = null!;
    private static readonly VisualRenderOptions RasterOptions = new(scale: 1, supersampling: 2);

    /// <summary>Loads the same canonical theme file as the legacy lane outside timing.</summary>
    public static void Initialize(string tokenPath, string fontPath, string boldFontPath) {
        LegacySceneBenchmarkCases.Initialize(tokenPath, fontPath, boldFontPath);
        var theme = VisualTheme.FromJson(File.ReadAllText(tokenPath), new VisualTypography("CFX Proof Carlito", 22, 13, 11, 12, 11));
        _context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(LegacySceneBenchmarkCases.Width, LegacySceneBenchmarkCases.Height), padding: 24),
            theme, VisualThemeMode.Light,
            new VisualFrame(LegacySceneBenchmarkCases.Title, "One prepared scene for SVG and native PNG", showLegend: true, legendPosition: ChartLegendPosition.TopLeft), FontSpec.FromFamily("CFX Proof Carlito"));
        _phase3Context = new VisualRenderContext(_context.Layout, theme, VisualThemeMode.Light, _context.Frame, _context.Font);
        _topologyContext = new VisualRenderContext(_context.Layout, theme, VisualThemeMode.Light,
            new VisualFrame(LegacySceneBenchmarkCases.Title, "One prepared scene for SVG and native PNG", showLegend: false, legendPosition: ChartLegendPosition.TopLeft), _context.Font);
    }

    /// <summary>Uses the same legacy model factory; only the compilation/export path changes.</summary>
    public static object Create(string fixture) => LegacySceneBenchmarkCases.Create(fixture);

    /// <summary>Records the same source data independently of preparation.</summary>
    public static string SourceDigest(string fixture) => LegacySceneBenchmarkCases.SourceDigest(fixture);

    /// <summary>Hashes detached compilation output or complete SVG/PNG/RGBA output outside timing.</summary>
    public static string OutputDigest(object result) => LegacySceneBenchmarkCases.OutputDigest(result is PreparedVisual prepared ? prepared.ToSvg() : result);

    /// <summary>Compiles the scene once; export-only cases invoke this during setup.</summary>
    public static object Prepare(object model) => ((IVisualRenderable)model).Prepare(model is ChartForgeX.Topology.TopologyChart ? _topologyContext
        : model is Chart chart && chart.Series.Count > 0 && chart.Series[0].Kind is not (ChartSeriesKind.Line or ChartSeriesKind.Donut or ChartSeriesKind.Bar or ChartSeriesKind.Scatter) ? _phase3Context : _context);

    /// <summary>Executes either full compile/export, compile alone, or export of an already prepared scene.</summary>
    public static object Execute(object model, string operation) {
        if (operation == "Compile") return Prepare(model);
        var prepared = model as PreparedVisual ?? (PreparedVisual)Prepare(model);
        return operation switch {
            "Svg" or "PreparedSvg" => prepared.ToSvg(),
            "Rgba" or "PreparedRgba" => prepared.ToRgba(RasterOptions),
            "Png" or "PreparedPng" => prepared.ToPng(RasterOptions),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    /// <summary>Validates complete outputs or semantic preparation without charging validation to the operation.</summary>
    public static long Validate(object result, string operation, string fixture) {
        if (result is PreparedVisual prepared) {
            LegacySceneBenchmarkCases.Validate(prepared.ToSvg(), "Svg", fixture);
            if (prepared.Regions.Count == 0) throw new InvalidOperationException("Compilation lost its semantic regions.");
            return prepared.Regions.Count;
        }
        return LegacySceneBenchmarkCases.Validate(result, operation, fixture);
    }
}
