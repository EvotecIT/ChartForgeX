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
    private readonly VisualFrame _frameRequest;
    private readonly TopologyRenderOptions _options;
    private VisualSceneBuilder _builder;
    private readonly VisualThemeColors _colors;
    private readonly TopologyPaint _svgTheme;
    private readonly bool _resolvedLayout;
    private readonly bool _naturalSize;
    private readonly Dictionary<TopologyEdge, IReadOnlyList<ChartPoint>> _routes = new();
    private readonly Dictionary<TopologyEdge, IReadOnlyList<ChartPoint>> _paintRoutes = new();
    private readonly Dictionary<TopologyEdge, (TopologyEdge Owner, IReadOnlyList<ChartPoint> Tail)> _trunks = new();
    private readonly Dictionary<TopologyEdge, Dictionary<string, string>> _edgeMetadata = new();
    private Dictionary<string, TopologyNode> _nodesById = null!;
    private IReadOnlyDictionary<TopologyEdge, int> _edgeRenderOrders = null!;
    private readonly Dictionary<TopologyEdge, ChartRect> _resolvedLabelBounds = new();
    private readonly Dictionary<string, IReadOnlyList<ChartRect>> _groupHeaderParts = new(StringComparer.Ordinal);
    private TopologyChart _chart = null!;
    private TopologyHighlightState _highlight = null!;
    private TopologyLegend? _legend;
    private IReadOnlyList<TopologyEdgeLabelLayout> _edgeLabels = Array.Empty<TopologyEdgeLabelLayout>();
    private ChartRect _plot;
    private ChartRect _contentClip;
    private double _scale = 1;
    private double _offsetX;
    private double _offsetY;

    internal VisualTopologyCompiler(TopologyChart source, VisualRenderContext context, TopologyRenderOptions options, bool resolvedLayout = false, bool naturalSize = false) {
        context = PreparedDiagramFont.ScopeContext(context, TopologyPreparedFont.HasMaterialTextPolicy(source, context.Font));
        _frameRequest = context.Frame.RequestValues(retainOwnedFonts: true);
        _source = source;
        _svgTheme = new TopologyPaint(context.Theme, context.ThemeMode, options.SvgColorVariables?.Clone());
        _context = ResolveFrame(context, source, options);
        _options = options.CloneForRendering();
        InitializeOptionsMetrics(context, _options);
        if (!Enum.IsDefined(typeof(TextMeasurementMode), _options.TextMeasurementMode)) throw new ArgumentOutOfRangeException(nameof(options.TextMeasurementMode));
        _builder = new VisualSceneBuilder(context.Layout.Size, context.Font, context.NativeTextCapture);
        _colors = context.Theme.Resolve(context.ThemeMode);
        _resolvedLayout = resolvedLayout;
        _naturalSize = naturalSize;
    }

    internal PreparedVisual Compile(FlowArtifact? flow = null) {
        var validator = new TopologyChartValidator();
        var interpolation = validator.ValidateInterpolationInputs(_source);
        if (!interpolation.IsValid) throw new TopologyValidationException(interpolation);
        var paints=validator.ValidateSurfacePaintInputs(_source);
        if(!paints.IsValid)throw new TopologyValidationException(paints);
        ValidateSurfacePaintHost();
        var external = validator.ValidateExternalPresentationInputs(_source, _options);
        if (!external.IsValid) throw new TopologyValidationException(external);
        if (_options.EnableHtmlInteractions && _options.EnableHtmlForceGraphControls && _source.Nodes.Any(TopologyExternalPresentationRules.HasPolicy))
            throw new NotSupportedException("Explicit native external presentation cannot be rewritten by live force graph controls.");
        var references = validator.ValidateScenarioReferences(_source);
        if (!references.IsValid) throw new TopologyValidationException(references);
        var input = TopologyLayoutEngine.Clone(_source);
        TopologyPreparedNodeBasis.ApplyLifecycle(input);
        TopologyPreparedEdgePlan.ApplyLifecycle(input);
        TopologyPreparedFramePadding.ApplyLifecycle(input, _options);
        var legend = _options.IncludeLegend ? TopologyLegend.Resolve(input, _options.LegendMode) : null;
        _legend = _context.Frame.ShowLegend ? legend : null;
        _context = ResolveLegendFrame(_context, _legend);
        TopologyPreparedFramePadding.ApplyAnnotationLifecycle(input, _options, _context);
        var entries = legend?.Items.Select((item, index) => new VisualLegendEntry(item.Label,
            Color(item.Color, item.Status.HasValue ? Status(item.Status.Value) : _colors.Accent), index.ToString(CultureInfo.InvariantCulture),
            marker: (builder, bounds, _) => BuildLegendMarker(item, bounds))).ToArray() ?? Array.Empty<VisualLegendEntry>();
        _plot = VisualFrameLayout.Build(_builder, _context, entries, FramePaints());
        if (_plot.Height <= 0) ReserveNaturalFrame(entries);
        if (_plot.Width <= 0 || _plot.Height <= 0) throw new InvalidOperationException("The common frame leaves no topology viewport.");
        input.Title = null; input.Subtitle = null; input.Legend = null;
        // Canonical topology coordinates already contain their padding. Give the layout that
        // domain once, then subtract its origin when placing it in the shared content frame.
        // Typed prepared imports already use the complete scene domain, including that frame.
        if (!_resolvedLayout && !PreservePreparedSceneCoordinates) input.Viewport = new TopologyViewport {
            Width = _plot.Width + _context.Layout.Padding * 2, Height = _plot.Height + _context.Layout.Padding * 2,
            Padding = _context.Layout.Padding
        };
        input.Theme = _context.Theme; input.ThemeMode = _context.ThemeMode;
        // Typed scene imports already carry fitted node bounds. Manual output may
        // transform that scene uniformly, but must not rerun authoring-time sizing.
        if (!_resolvedLayout && (!input.UsesPreparedSceneCoordinates || input.LayoutMode != TopologyLayoutMode.Manual))
            FitNodeLabels(input);
        var layoutOptions = _options.CloneForRendering();
        layoutOptions.IncludeTitle = false; layoutOptions.IncludeLegend = false;
        // A view selects data and identity here. Its headings were already measured by the
        // common frame; the detached layout copy must not reserve them a second time.
        if (layoutOptions.View != null) { layoutOptions.View.Title = null; layoutOptions.View.Subtitle = null; }
        _chart = _resolvedLayout ? input : TopologyLayoutEngine.Prepare(input, layoutOptions.View, layoutOptions, new TextMeasurementContext(_context.Font, _options.TextMeasurementMode), hierarchySource: _source);
        if (layoutOptions.View != null) TopologyLayoutEngine.DetachOmittedSourceGroups(input, _chart);
        var validation = validator.Validate(_chart, validateScenarioReferences: false, layoutOptions);
        if (!validation.IsValid) throw new TopologyValidationException(validation);
        _options.TextMeasurement = _chart.TextMeasurement;
        ValidateExternalGlyphSlots();
        // The common frame has already reserved the legend. Legacy geometry helpers must not
        // reserve a second legend inside the detached content viewport.
        _chart.Title = null; _chart.Subtitle = null; _chart.Legend = null;
        PrepareAnnotations();
        PreparePathDecorations();
        DiagnoseExportOptions();
        _highlight = TopologyHighlightState.From(_chart, _options);
        var nodes = _nodesById = TopologyGroupEndpoints.Resolve(_chart);
        _edgeRenderOrders = EdgeRenderOrderMap(_chart, _options);
        using var numericRouteScope = _chart.Edges.Any(edge => edge.Interpolation.HasValue || edge.PreparedEdgePlan?.Geometry != null) ? new TopologyRenderRouteCache(_chart) : null;
        foreach (var edge in _chart.Edges) {
            if (SeedCapturedRoute(edge)) continue;
            var route = EdgePoints(_chart, edge, nodes);
            var paintRoute = TopologyDenseRoutePlanner.PaintPoints(_chart, edge, route, includeOwner: true);
            var trunk = TopologyDenseRoutePlanner.SharedTrunk(_chart, edge);
            if (_options.FitContentToViewport && edge.PathGeometry == null) {
                TopologyRenderPrimitives.AttachEndpointsToSurfaces(edge, nodes[edge.SourceNodeId], nodes[edge.TargetNodeId], route);
                if (paintRoute.Count > 1) {
                    TopologyRenderPrimitives.AttachEndpoint(nodes[edge.SourceNodeId], edge.SourcePort, edge.SourcePortId, paintRoute, 0, 1);
                    if (trunk.Owner == null)
                        TopologyRenderPrimitives.AttachEndpoint(nodes[edge.TargetNodeId], edge.TargetPort, edge.TargetPortId, paintRoute, paintRoute.Count - 1, paintRoute.Count - 2);
                    if (ReferenceEquals(trunk.Owner, edge) && trunk.Tail is { Count: > 1 } tail)
                        TopologyRenderPrimitives.AttachEndpoint(nodes[edge.TargetNodeId], edge.TargetPort, edge.TargetPortId, tail, tail.Count - 1, tail.Count - 2);
                }
            }
            _routes.Add(edge, edge.Interpolation.HasValue ? route.ToArray() : SampleRoute(route));
            _paintRoutes.Add(edge, edge.Interpolation.HasValue ? paintRoute.ToArray() : SampleRoute(paintRoute));
            if (trunk.Owner != null && trunk.Tail != null) _trunks.Add(edge, (trunk.Owner, edge.Interpolation.HasValue ? trunk.Tail.ToArray() : SampleRoute(trunk.Tail)));

            IReadOnlyList<ChartPoint> SampleRoute(List<ChartPoint> points) {
                return TopologyResolvedRouteSamples.Sample(_chart, _options, edge, nodes, points);
            }
        }
        PrepareInterpolatedRoutes();
        PrepareCrossingPaintPaths();
        _edgeLabels = _options.IncludeEdgeLabels ? EdgeLabelLayouts(_chart, _options) : Array.Empty<TopologyEdgeLabelLayout>();
        ResolveNaturalSize(entries);
        ResolveFit();
        ResolveExplicitLabelBaselines();
        EmitCrossingPaintDiagnostics();
        using (_builder.PushGroup(_source.Id, "topology", RootMetadata()))
        using (_builder.PushClip(_contentClip)) {
            BuildSurface(); BuildGroups();
            BuildPathDecorations();
            _chart.PreparedGroupHeaderParts = CaptureGroupHeaderParts();
            foreach (var edge in OrderedEdgesForRendering(_chart, _options)) BuildEdge(edge.Edge);
            BuildEdgeLabels();
            foreach (var node in _chart.Nodes) BuildNode(node);
            BuildAnnotations();
            if (_options.IncludeLayoutDiagnosticOverlay) BuildDiagnostics();
        }
        var accessibility = _source.Accessibility.Clone();
        accessibility.Name ??= HeadingOrSource(_context.Frame.Title, SourceTitle) ?? _source.Labels.UntitledTopology;
        if (accessibility.Description == null) {
            // A host's language formatter describes the resolved facts independently of its presentation subtitle.
            var description = _source.Labels.AccessibleTextFormatter == null
                ? HeadingOrSource(_context.Frame.Subtitle, SourceSubtitle) : null;
            accessibility.Description = description ?? VisualArtifactInterchangeMapping.BoundedGeneratedText(
                _source.Labels.Describe(HeadingOrSource(_context.Frame.Title, SourceTitle), _chart.Groups.Count, _chart.Nodes.Count, _chart.Edges.Count), string.Empty);
        }
        var semantics = SemanticSnapshot(accessibility);
        if (flow != null) semantics = VisualArtifactInterchangeMapping.FromPreparedFlow(flow, semantics);
        var svgOptions = new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(_options.IdScope), _options.SvgColorVariables,
            _options.OpenLinksInNewTab ? VisualSvgLinkTarget.NewContext : VisualSvgLinkTarget.SameContext, responsive: _options.UseResponsiveSvg);
        return new PreparedVisual(_builder.Build(), accessibility, semanticInterchange: semantics, svgOptions: svgOptions, topologyHierarchyPreparation: _chart.HierarchyPreparation)
            .WithCapturedText(_context.Font.NativeTextCapture?.Freeze());
    }

    private void DiagnoseExportOptions() {
        if (_options.EnableHtmlInteractions || _options.EnableHtmlViewportControls || _options.EnableHtmlExportControls || _options.EnableHtmlForceGraphControls || _options.EnableHtmlSynchronizedState || _options.EnableHtmlSelectionPanel || _options.EnableHtmlFullscreenControl || _options.EnableHtmlScenarioUrlState)
            _builder.AddDiagnostic(new VisualDiagnostic("topology.host-options", "HTML interaction and viewport options remain host metadata; a static prepared scene does not install their controls."));
    }

    internal static VisualRenderContext ResolveFrame(VisualRenderContext context, TopologyChart source, TopologyRenderOptions options) {
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
                frame.LegendMaximumRows, frame.LegendMaximumHeightFraction, frame.ShowCard, legendTitle: frame.LegendTitle), resolved.Font).WithNativeTextCapture(resolved.NativeTextCapture);
    }

    internal static VisualRenderContext ResolveLegendFrame(VisualRenderContext context, TopologyLegend? legend) =>
        context.Frame.LegendTitle == null && legend?.Title != null
            ? new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, context.Frame.WithLegendTitle(legend.Title), context.Font).WithNativeTextCapture(context.NativeTextCapture)
            : context;

    private ChartPoint Point(ChartPoint p) => new(_offsetX + p.X * _scale, _offsetY + p.Y * _scale);
    private static string? HeadingOrSource(string? heading, string? source) => string.IsNullOrWhiteSpace(heading) ? source : heading;
    private ChartRect Bounds(double x, double y, double width, double height) => new(_offsetX + x * _scale, _offsetY + y * _scale, width * _scale, height * _scale);
    private ChartColor Status(TopologyHealthStatus status) => status switch {
        TopologyHealthStatus.Healthy => _colors.Status.Pass.Fill, TopologyHealthStatus.Warning => _colors.Status.Medium.Fill,
        TopologyHealthStatus.Critical => _colors.Status.Critical.Fill, TopologyHealthStatus.Disabled => _colors.Status.Maintenance.Fill,
        _ => _colors.Status.Neutral.Fill
    };
    private static ChartColor Color(string? value, ChartColor fallback) =>
        TopologySurfacePaintRules.TryColor(value, fallback, out var resolved) ? resolved : ChartColor.Parse(value!);
    private ChartColor Highlight(ChartColor color, bool highlighted) => color.WithOpacity(color.A / 255d * (highlighted || !_highlight.IsActive ? 1 : _highlight.DimmedOpacity));
    private TopologyPaint Theme() => new(_context.Theme, _context.ThemeMode);
}
