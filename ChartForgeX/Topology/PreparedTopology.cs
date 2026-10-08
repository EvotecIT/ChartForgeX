using System;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.Accessibility;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

/// <summary>
/// A detached layout snapshot shared by SVG, PNG, diagnostics, and native-document interchange.
/// Changes to the source chart, options, artwork or font resources do not change the compiled scene.
/// </summary>
public sealed class PreparedTopology {
    private readonly TopologyChart _chart;
    private readonly TopologyRenderOptions _options;
    private readonly double _requestedWidth;
    private readonly double _requestedHeight;
    private readonly PreparedVisual _visual;
    private readonly VisualRenderContext _context;
    private readonly VisualRenderOptions _rasterOptions;
    private readonly string? _title;
    private readonly string? _subtitle;
    private readonly TopologyLegend? _legend;
    private readonly TopologyTheme? _svgTheme;
    private readonly Lazy<ResolvedTopologyGeometry> _geometry;
    private readonly Func<TopologyLayoutDiagnosticReport> _diagnostics;
    internal ResolvedTopologyGeometry Geometry => _geometry.Value;
    internal VisualRenderOptions RasterOptions => _rasterOptions;

    internal PreparedTopology(VisualTopologyCompiler compiler, PreparedVisual visual, VisualRenderOptions rasterOptions) {
        _chart = compiler.LayoutSnapshot(); _options = compiler.OptionsSnapshot();
        _visual = visual; _geometry = compiler.GeometrySnapshot(_chart, _options);
        _diagnostics = compiler.DiagnosticsSnapshot(_chart, _chart.RenderOptions ?? _options);
        _context = compiler.Context; _rasterOptions = rasterOptions;
        _title = compiler.SourceTitle; _subtitle = compiler.SourceSubtitle;
        _legend = compiler.FrameLegend;
        _svgTheme = compiler.SvgTheme;
        _requestedWidth = visual.Size.Width; _requestedHeight = visual.Size.Height;
    }

    /// <summary>Gets the prepared geometry width in pixels, before optional output fitting.</summary>
    public double Width => _visual.Size.Width;
    /// <summary>Gets the prepared geometry height in pixels, before optional output fitting.</summary>
    public double Height => _visual.Size.Height;
    /// <summary>Gets the number of nodes retained by the selected view.</summary>
    public int NodeCount => _chart.Nodes.Count;
    /// <summary>Gets the number of retained relationships.</summary>
    public int EdgeCount => _chart.Edges.Count;
    /// <summary>Gets the source title without requiring interchange serialization.</summary>
    public string? Title => _title;
    /// <summary>Gets the source language for accessible labels and surrounding document content.</summary>
    public string? Language => _chart.Accessibility.Language;

    /// <summary>Fits an existing validated layout to an artifact's output size without using that size as layout input.</summary>
    internal PreparedTopology WithOutputSize(double width, double height) {
        if (width == _requestedWidth && height == _requestedHeight) return this;
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), _context.Layout.PaddingEdges),
            _context.Theme, _context.ThemeMode, _context.Frame, _context.Font);
        var source = TopologyLayoutEngine.Clone(_chart);
        source.Title = _title; source.Subtitle = _subtitle;
        source.Legend = _legend == null ? null : TopologyLegend.Clone(_legend);
        var compiler = new VisualTopologyCompiler(source, context, _options, resolvedLayout: true, svgTheme: _svgTheme);
        return new PreparedTopology(compiler, compiler.Compile(), _rasterOptions);
    }

    /// <summary>Renders the prepared geometry without running layout again.</summary>
    public string ToSvg() => _visual.ToSvg();
    internal string ToSvg(VisualAccessibility accessibility) => _visual.ToSvg(accessibility);

    /// <summary>Renders the same prepared geometry through the dependency-free raster renderer.</summary>
    public byte[] ToPng() => _visual.ToPng(_rasterOptions);

    internal PreparedVisual Visual => _visual;
    internal RgbaImage ToRgba() => _visual.ToRgba(_rasterOptions);

    /// <summary>Returns a detached semantic envelope with the positions used by the renderers.</summary>
    public VisualArtifactInterchangeEnvelope ToInterchangeEnvelope() => _visual.SemanticInterchange!;

    /// <summary>Measures the prepared geometry in output coordinates without running layout again. The returned report is detached.</summary>
    public TopologyLayoutDiagnosticReport Analyze() => _diagnostics();

    /// <summary>Evaluates collisions, viewport expansion, and readability at a target display size.</summary>
    public TopologyReadabilityReport AssessReadability(double targetWidth, double targetHeight, double minimumScale = 0.65) =>
        TopologyReadabilityReport.Create(Analyze(), _chart, _options, targetWidth, targetHeight, minimumScale);
}

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Validates and prepares a detached layout once for multiple exports and diagnostics.
    /// Use <see cref="PreparedTopology.AssessReadability"/> before fitting a dense layout into a small output.
    /// </summary>
    public static PreparedTopology Prepare(this TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var effective = chart.ResolveRenderOptions(options).CloneForRendering();
        var request = VisualExportRequest.ForTopology(chart, effective);
        var compiler = new VisualTopologyCompiler(chart, request.Context, effective, naturalSize: true, svgTheme: chart.Theme);
        var visual = compiler.Compile();
        return new PreparedTopology(compiler, visual, request.RasterOptions);
    }
}
