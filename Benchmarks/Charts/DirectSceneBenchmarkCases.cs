using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>Measures preparation separately from complete and already-prepared SVG/native-raster exports.</summary>
public static class DirectSceneBenchmarkCases {
    private static VisualRenderContext _context = null!;
    private static readonly VisualRenderOptions RasterOptions = new(scale: 1, supersampling: 2);

    /// <summary>Loads the same canonical theme file as the legacy lane outside timing.</summary>
    public static void Initialize(string tokenPath, string fontPath, string boldFontPath) {
        LegacySceneBenchmarkCases.Initialize(tokenPath, fontPath, boldFontPath);
        _context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(LegacySceneBenchmarkCases.Width, LegacySceneBenchmarkCases.Height)),
            VisualTheme.FromJson(File.ReadAllText(tokenPath)), VisualThemeMode.Light,
            new VisualFrame(LegacySceneBenchmarkCases.Title, "One prepared scene for SVG and native PNG"), FontSpec.FromFamily("CFX Proof Carlito"));
    }

    /// <summary>Uses the same legacy model factory; only the compilation/export path changes.</summary>
    public static object Create(string fixture) => LegacySceneBenchmarkCases.Create(fixture);

    /// <summary>Records the same source data independently of preparation.</summary>
    public static string SourceDigest(string fixture) => LegacySceneBenchmarkCases.SourceDigest(fixture);

    /// <summary>Compiles the scene once; export-only cases invoke this during setup.</summary>
    public static object Prepare(object model) => ((Chart)model).Prepare(_context);

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
            return prepared.Regions.Count;
        }
        return LegacySceneBenchmarkCases.Validate(result, operation, fixture);
    }
}
