using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>Owns one resolved diagram observation for scene export and native semantic projection.</summary>
internal sealed partial class VisualTopologyCompiler {
    private readonly TopologyChart _source;
    private VisualRenderContext _context;
    private readonly TopologyRenderOptions _options;
    private VisualSceneBuilder _builder;
    private readonly VisualThemeColors _colors;
    private readonly bool _resolvedLayout;
    private readonly bool _naturalSize;
    private readonly Dictionary<TopologyEdge, IReadOnlyList<ChartPoint>> _routes = new();
    private readonly Dictionary<TopologyEdge, IReadOnlyList<ChartPoint>> _paintRoutes = new();
    private readonly Dictionary<TopologyEdge, (TopologyEdge Owner, IReadOnlyList<ChartPoint> Tail)> _trunks = new();
    private readonly Dictionary<TopologyEdge, Dictionary<string, string>> _edgeMetadata = new();
    private Dictionary<string, TopologyNode> _nodesById = null!;
    private IReadOnlyDictionary<TopologyEdge, int> _edgeRenderOrders = null!;
    private readonly Dictionary<TopologyEdge, ChartRect> _resolvedLabelBounds = new();
    private TopologyChart _chart = null!;
    private TopologyHighlightState _highlight = null!;
    private TopologyLegend? _legend;
    private IReadOnlyList<TopologyEdgeLabelLayout> _edgeLabels = Array.Empty<TopologyEdgeLabelLayout>();
    private ChartRect _plot;
    private double _scale = 1;
    private double _offsetX;
    private double _offsetY;

    internal VisualTopologyCompiler(TopologyChart source, VisualRenderContext context, TopologyRenderOptions options, bool resolvedLayout = false, bool naturalSize = false) {
        _source = source;
        _context = ResolveFrame(context, source, options);
        _options = options.CloneForRendering();
        _options.ResolvedIconLabelFontSize = context.Theme.Typography.DataLabelSize * (10.5 / 11);
        _options.ResolvedEdgeLabelScale = context.Theme.Typography.DataLabelSize / 11;
        var badgeFace = new VisualSceneTextFace(context.Font, 600);
        var badgeFontSize = context.Theme.Typography.DataLabelSize * .75;
        _options.ResolvedBadgeTextWidth = value => badgeFace.Prepare(value, badgeFontSize).Metrics.Width;
        if (!Enum.IsDefined(typeof(TextMeasurementMode), _options.TextMeasurementMode)) throw new ArgumentOutOfRangeException(nameof(options.TextMeasurementMode));
        _builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        _colors = context.Theme.Resolve(context.ThemeMode);
        _resolvedLayout = resolvedLayout;
        _naturalSize = naturalSize;
    }

    internal PreparedVisual Compile(FlowArtifact? flow = null) {
        var validator = new TopologyChartValidator();
        var references = validator.ValidateScenarioReferences(_source);
        if (!references.IsValid) throw new TopologyValidationException(references);
        var input = TopologyLayoutEngine.Clone(_source);
        var legend = _options.IncludeLegend ? TopologyLegend.Resolve(input, _options.LegendMode) : null;
        _legend = _context.Frame.ShowLegend ? legend : null;
        var frame = _context.Frame;
        if (frame.LegendTitle == null && _legend?.Title != null) {
            _context = new VisualRenderContext(_context.Layout, _context.Theme, _context.ThemeMode,
                frame.WithLegendTitle(_legend.Title), _context.Font);
        }
        var entries = legend?.Items.Select((item, index) => new VisualLegendEntry(item.Label,
            Color(item.Color, item.Status.HasValue ? Status(item.Status.Value) : _colors.Accent), index.ToString(CultureInfo.InvariantCulture),
            marker: (builder, bounds) => BuildLegendMarker(item, bounds))).ToArray() ?? Array.Empty<VisualLegendEntry>();
        _plot = VisualFrameLayout.Build(_builder, _context, entries);
        if (_plot.Height <= 0) ReserveNaturalFrame(entries);
        if (_plot.Width <= 0 || _plot.Height <= 0) throw new InvalidOperationException("The common frame leaves no topology viewport.");
        input.Title = null; input.Subtitle = null; input.Legend = null;
        // Canonical topology coordinates already contain their padding. Give the layout that
        // domain once, then subtract its origin when placing it in the shared content frame.
        if (!_resolvedLayout) input.Viewport = new TopologyViewport {
            Width = _plot.Width + _context.Layout.Padding * 2, Height = _plot.Height + _context.Layout.Padding * 2,
            Padding = _context.Layout.Padding
        };
        input.Theme = Theme();
        var layoutOptions = _options.CloneForRendering();
        layoutOptions.IncludeTitle = false; layoutOptions.IncludeLegend = false;
        // A view selects data and identity here. Its headings were already measured by the
        // common frame; the detached layout copy must not reserve them a second time.
        if (layoutOptions.View != null) { layoutOptions.View.Title = null; layoutOptions.View.Subtitle = null; }
        _chart = _resolvedLayout ? input : TopologyLayoutEngine.Prepare(input, layoutOptions.View, layoutOptions, new TextMeasurementContext(_context.Font, _options.TextMeasurementMode));
        if (layoutOptions.View != null) TopologyLayoutEngine.DetachOmittedSourceGroups(input, _chart);
        var validation = validator.Validate(_chart, validateScenarioReferences: false, layoutOptions);
        if (!validation.IsValid) throw new TopologyValidationException(validation);
        _options.TextMeasurement = _chart.TextMeasurement;
        // The common frame has already reserved the legend. Legacy geometry helpers must not
        // reserve a second legend inside the detached content viewport.
        _chart.Title = null; _chart.Subtitle = null; _chart.Legend = null;
        ResolveNaturalSize(entries);
        DiagnoseExportOptions();
        _highlight = TopologyHighlightState.From(_chart, _options);
        var nodes = _nodesById = _chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        _edgeRenderOrders = EdgeRenderOrderMap(_chart, _options);
        foreach (var edge in _chart.Edges) {
            var route = EdgePoints(_chart, edge, nodes);
            _routes.Add(edge, SampleRoute(route));
            _paintRoutes.Add(edge, SampleRoute(TopologyDenseRoutePlanner.PaintPoints(_chart, edge, route, includeOwner: true)));
            var trunk = TopologyDenseRoutePlanner.SharedTrunk(_chart, edge);
            if (trunk.Owner != null && trunk.Tail != null) _trunks.Add(edge, (trunk.Owner, SampleRoute(trunk.Tail)));

            IReadOnlyList<ChartPoint> SampleRoute(List<ChartPoint> points) {
                return TopologyResolvedRouteSamples.Sample(_chart, _options, edge, nodes, points);
            }
        }
        _edgeLabels = _options.IncludeEdgeLabels ? EdgeLabelLayouts(_chart, _options) : Array.Empty<TopologyEdgeLabelLayout>();
        ResolveFit();
        using (_builder.PushGroup(_source.Id, "topology", RootMetadata()))
        using (_builder.PushClip(_plot)) {
            BuildSurface(); BuildGroups();
            foreach (var edge in OrderedEdgesForRendering(_chart, _options)) BuildEdge(edge.Edge);
            BuildEdgeLabels();
            foreach (var node in _chart.Nodes) BuildNode(node);
            if (_options.IncludeLayoutDiagnosticOverlay) BuildDiagnostics();
        }
        var accessibility = _source.Accessibility.Clone();
        accessibility.Name ??= HeadingOrSource(_context.Frame.Title, SourceTitle) ?? _source.Labels.UntitledTopology;
        accessibility.Description ??= HeadingOrSource(_context.Frame.Subtitle, SourceSubtitle)
            ?? VisualArtifactInterchangeMapping.BoundedGeneratedText(_source.Labels.Describe(HeadingOrSource(_context.Frame.Title, SourceTitle), _chart.Groups.Count, _chart.Nodes.Count, _chart.Edges.Count), string.Empty);
        var semantics = SemanticSnapshot(accessibility);
        if (flow != null) semantics = VisualArtifactInterchangeMapping.FromPreparedFlow(flow, semantics);
        var svgOptions = new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(_options.IdScope), _options.SvgColorVariables,
            _options.OpenLinksInNewTab ? VisualSvgLinkTarget.NewContext : VisualSvgLinkTarget.SameContext, responsive: _options.UseResponsiveSvg);
        return new PreparedVisual(_builder.Build(), accessibility, semanticInterchange: semantics, svgOptions: svgOptions);
    }

    private void DiagnoseExportOptions() {
        if (_options.EnableHtmlInteractions || _options.EnableHtmlViewportControls || _options.EnableHtmlExportControls || _options.EnableHtmlForceGraphControls || _options.EnableHtmlSynchronizedState || _options.EnableHtmlSelectionPanel || _options.EnableHtmlFullscreenControl || _options.EnableHtmlScenarioUrlState)
            _builder.AddDiagnostic(new VisualDiagnostic("topology.host-options", "HTML interaction and viewport options remain host metadata; a static prepared scene does not install their controls."));
    }

    private static VisualRenderContext ResolveFrame(VisualRenderContext context, TopologyChart source, TopologyRenderOptions options) {
        var resolved = VisualDiagramPrimitives.WithFrame(context,
            options.IncludeTitle ? HeadingOrSource(options.View?.Title, source.Title) : null,
            options.IncludeTitle ? HeadingOrSource(options.View?.Subtitle, source.Subtitle) : null);
        if (options.HeaderStyle != TopologyHeaderStyle.CenterBanner) return resolved;
        var frame = resolved.Frame; var colors = resolved.Theme.Resolve(resolved.ThemeMode);
        TextStyle Center(double size, int weight, ChartColor color) {
            var font = resolved.Font; font.Weight = weight;
            return new TextStyle { Font = font, FontSize = size, Color = color, LineHeight = 1, Alignment = TextAlignment.Center };
        }
        return new VisualRenderContext(resolved.Layout, resolved.Theme, resolved.ThemeMode,
            new VisualFrame(frame.Title, frame.Subtitle, frame.ShowLegend, frame.LegendPosition, frame.ShowSurface, frame.TransparentBackground,
                frame.TitleStyle ?? Center(resolved.Theme.Typography.TitleSize, 700, colors.Foreground),
                frame.SubtitleStyle ?? Center(resolved.Theme.Typography.SubtitleSize, 400, colors.MutedForeground), frame.LegendStyle,
                frame.LegendMaximumRows, frame.LegendMaximumHeightFraction, frame.ShowCard, legendTitle: frame.LegendTitle), resolved.Font);
    }

    private ChartPoint Point(ChartPoint p) => new(_offsetX + p.X * _scale, _offsetY + p.Y * _scale);
    private static string? HeadingOrSource(string? heading, string? source) => string.IsNullOrWhiteSpace(heading) ? source : heading;
    private ChartRect Bounds(double x, double y, double width, double height) => new(_offsetX + x * _scale, _offsetY + y * _scale, width * _scale, height * _scale);
    private ChartColor Status(TopologyHealthStatus status) => status switch {
        TopologyHealthStatus.Healthy => _colors.Status.Pass.Fill, TopologyHealthStatus.Warning => _colors.Status.Medium.Fill,
        TopologyHealthStatus.Critical => _colors.Status.Critical.Fill, TopologyHealthStatus.Disabled => _colors.Status.Maintenance.Fill,
        _ => _colors.Status.Neutral.Fill
    };
    private static ChartColor Color(string? value, ChartColor fallback) => string.IsNullOrWhiteSpace(value) ? fallback
        : ChartForgeX.SvgRaster.SvgRasterColor.TryParse(value, out var parsed) ? parsed
        : SvgPaint.TryCssVariable(value, fallback, out var resolved, out _) ? resolved : ChartColor.Parse(value!);
    private ChartColor Highlight(ChartColor color, bool highlighted) => color.WithOpacity(color.A / 255d * (highlighted || !_highlight.IsActive ? 1 : _highlight.DimmedOpacity));
    private static string ThemeToken(ChartColor color) => color.A == 255 ? color.ToHex() : color.ToHexRgba();
    private TopologyTheme Theme() => new() {
        Background = ThemeToken(_colors.Background), Foreground = ThemeToken(_colors.Foreground), MutedForeground = ThemeToken(_colors.MutedForeground),
        Card = ThemeToken(_colors.Surface), Surface = ThemeToken(_colors.Surface), Border = ThemeToken(_colors.Border), Accent = ThemeToken(_colors.Accent),
        Healthy = ThemeToken(_colors.Status.Pass.Fill), Warning = ThemeToken(_colors.Status.Medium.Fill), Critical = ThemeToken(_colors.Status.Critical.Fill),
        Unknown = ThemeToken(_colors.Status.Neutral.Fill), Disabled = ThemeToken(_colors.Status.Maintenance.Fill), FontFamily = _context.Font.Family
    };
}
