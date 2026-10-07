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
    private readonly VisualRenderContext _context;
    private readonly TopologyRenderOptions _options;
    private readonly VisualSceneBuilder _builder;
    private readonly VisualThemeColors _colors;
    private readonly Dictionary<string, IReadOnlyList<ChartPoint>> _routes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChartRect> _resolvedLabelBounds = new(StringComparer.Ordinal);
    private TopologyChart _chart = null!;
    private TopologyHighlightState _highlight = null!;
    private TopologyLegend? _legend;
    private IReadOnlyList<TopologyEdgeLabelLayout> _edgeLabels = Array.Empty<TopologyEdgeLabelLayout>();
    private ChartRect _plot;
    private double _scale = 1;
    private double _offsetX;
    private double _offsetY;

    internal VisualTopologyCompiler(TopologyChart source, VisualRenderContext context, TopologyRenderOptions options) {
        _source = source;
        _context = ResolveFrame(context, source, options);
        _options = options.CloneForRendering();
        _builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        _colors = context.Theme.Resolve(context.ThemeMode);
    }

    internal PreparedVisual Compile(FlowArtifact? flow = null) {
        DiagnoseExportOptions();
        var validator = new TopologyChartValidator();
        var references = validator.ValidateScenarioReferences(_source);
        if (!references.IsValid) throw new TopologyValidationException(references);
        var input = TopologyLayoutEngine.Clone(_source);
        var legend = _options.IncludeLegend ? TopologyLegend.Resolve(input, _options.LegendMode) : null;
        _legend = _context.Frame.ShowLegend ? legend : null;
        var entries = legend?.Items.Select((item, index) => new VisualLegendEntry(item.Label,
            Color(item.Color, item.Status.HasValue ? Status(item.Status.Value) : _colors.Accent), index.ToString(CultureInfo.InvariantCulture),
            marker: (builder, bounds) => BuildLegendMarker(item, bounds))).ToArray() ?? Array.Empty<VisualLegendEntry>();
        _plot = VisualFrameLayout.Build(_builder, _context, entries);
        if (_plot.Width <= 0 || _plot.Height <= 0) throw new InvalidOperationException("The common frame leaves no topology viewport.");
        input.Title = null; input.Subtitle = null; input.Legend = null;
        input.Viewport = new TopologyViewport { Width = _plot.Width, Height = _plot.Height, Padding = _context.Theme.Spacing };
        input.Theme = Theme();
        var layoutOptions = _options.CloneForRendering();
        layoutOptions.IncludeTitle = false; layoutOptions.IncludeLegend = false;
        _chart = TopologyLayoutEngine.Prepare(input, layoutOptions.View, layoutOptions, new TextMeasurementContext(_context.Font));
        if (layoutOptions.View != null) TopologyLayoutEngine.DetachOmittedSourceGroups(input, _chart);
        var validation = validator.Validate(_chart, validateScenarioReferences: false, layoutOptions);
        if (!validation.IsValid) throw new TopologyValidationException(validation);
        _options.TextMeasurement = _chart.TextMeasurement;
        // The common frame has already reserved the legend. Legacy geometry helpers must not
        // reserve a second legend inside the detached content viewport.
        _chart.Legend = null;
        _highlight = TopologyHighlightState.From(_chart, _options);
        var nodes = _chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in _chart.Edges) {
            var route = EdgePoints(_chart, edge, nodes);
            IReadOnlyList<ChartPoint> rendered = RenderedEdgeSamplePoints(_chart, edge, nodes, route, 64);
            if (ShouldRoundEdgeCorners(edge, rendered, _options)) rendered = RoundedOrthogonalRoutePoints(rendered, _options.EdgeCornerRadius);
            _routes.Add(edge.Id, rendered);
        }
        _edgeLabels = _options.IncludeEdgeLabels ? EdgeLabelLayouts(_chart, _options) : Array.Empty<TopologyEdgeLabelLayout>();
        ResolveFit();
        using (_builder.PushClip(_plot)) {
            BuildSurface(); BuildGroups();
            foreach (var edge in OrderedEdgesForRendering(_chart, _options)) BuildEdge(edge.Edge);
            BuildEdgeLabels();
            foreach (var node in _chart.Nodes) BuildNode(node);
            if (_options.IncludeLayoutDiagnosticOverlay) BuildDiagnostics();
        }
        var accessibility = _source.Accessibility.Clone();
        accessibility.Name ??= _context.Frame.Title ?? _source.Title ?? _source.Id;
        accessibility.Description ??= _context.Frame.Subtitle ?? _source.Subtitle;
        var semantics = SemanticSnapshot();
        if (flow != null) semantics = VisualArtifactInterchangeMapping.FromPreparedFlow(flow, semantics);
        var svgOptions = new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(_options.IdScope), _options.SvgColorVariables,
            _options.OpenLinksInNewTab ? VisualSvgLinkTarget.NewContext : VisualSvgLinkTarget.SameContext);
        return new PreparedVisual(_builder.Build(), accessibility, semanticInterchange: semantics, svgOptions: svgOptions);
    }

    private void DiagnoseExportOptions() {
        if (_options.Motion != null) throw new NotSupportedException("TopologyRenderOptions.Motion requires the Stories animation pipeline; Prepare produces a fixed static scene.");
        if (_options.EnableHtmlInteractions || _options.EnableHtmlViewportControls || _options.EnableHtmlExportControls || _options.EnableHtmlForceGraphControls || _options.EnableHtmlSynchronizedState || _options.EnableHtmlSelectionPanel || _options.EnableHtmlFullscreenControl || _options.EnableHtmlScenarioUrlState)
            _builder.AddDiagnostic(new VisualDiagnostic("topology.host-options", "HTML interaction and viewport options remain host metadata; a static prepared scene does not install their controls."));
    }

    private static VisualRenderContext ResolveFrame(VisualRenderContext context, TopologyChart source, TopologyRenderOptions options) {
        var resolved = VisualDiagramPrimitives.WithFrame(context, options.IncludeTitle ? source.Title : null, options.IncludeTitle ? source.Subtitle : null);
        if (options.HeaderStyle != TopologyHeaderStyle.CenterBanner) return resolved;
        var frame = resolved.Frame; var colors = resolved.Theme.Resolve(resolved.ThemeMode);
        TextStyle Center(double size, int weight, ChartColor color) {
            var font = resolved.Font; font.Weight = weight;
            return new TextStyle { Font = font, FontSize = size, Color = color, LineHeight = 1, Alignment = TextAlignment.Center };
        }
        return new VisualRenderContext(resolved.Layout, resolved.Theme, resolved.ThemeMode,
            new VisualFrame(frame.Title, frame.Subtitle, frame.ShowLegend, frame.LegendPosition, frame.ShowSurface, frame.TransparentBackground,
                frame.TitleStyle ?? Center(resolved.Theme.Typography.TitleSize, 600, colors.Foreground),
                frame.SubtitleStyle ?? Center(resolved.Theme.Typography.SubtitleSize, 400, colors.MutedForeground), frame.LegendStyle,
                frame.LegendMaximumRows, frame.LegendMaximumHeightFraction), resolved.Font);
    }

    private void ResolveFit() {
        var width = _chart.Viewport.Width; var height = _chart.Viewport.Height;
        _offsetX = _plot.X; _offsetY = _plot.Y;
        if (width <= _plot.Width + .001 && height <= _plot.Height + .001) return;
        if (!_options.FitContentToViewport)
            throw new NotSupportedException($"Prepared topology exceeds its fixed viewport: requires {width:0.##} x {height:0.##}, available {_plot.Width:0.##} x {_plot.Height:0.##}. Enlarge the common size or enable FitContentToViewport.");
        _scale = Math.Min(_plot.Width / width, _plot.Height / height);
        _offsetX += (_plot.Width - width * _scale) / 2;
        _offsetY += (_plot.Height - height * _scale) / 2;
        _builder.AddDiagnostic(new VisualDiagnostic("topology.content-fitted", "The complete topology, including text and native semantic geometry, was uniformly fitted into the common content viewport."));
    }

    private ChartPoint Point(ChartPoint p) => new(_offsetX + p.X * _scale, _offsetY + p.Y * _scale);
    private ChartRect Bounds(double x, double y, double width, double height) => new(_offsetX + x * _scale, _offsetY + y * _scale, width * _scale, height * _scale);
    private ChartColor Status(TopologyHealthStatus status) => status switch {
        TopologyHealthStatus.Healthy => _colors.Status.Pass.Fill, TopologyHealthStatus.Warning => _colors.Status.Medium.Fill,
        TopologyHealthStatus.Critical => _colors.Status.Critical.Fill, TopologyHealthStatus.Disabled => _colors.Status.Maintenance.Fill,
        _ => _colors.Status.Neutral.Fill
    };
    private static ChartColor Color(string? value, ChartColor fallback) => string.IsNullOrWhiteSpace(value) ? fallback : ChartColor.Parse(value!);
    private ChartColor Highlight(ChartColor color, bool highlighted) => color.WithOpacity(color.A / 255d * (highlighted || !_highlight.IsActive ? 1 : _highlight.DimmedOpacity));
    private TopologyTheme Theme() => new() {
        Background = _colors.Background.ToCss(), Foreground = _colors.Foreground.ToCss(), MutedForeground = _colors.MutedForeground.ToCss(),
        Card = _colors.Surface.ToCss(), Surface = _colors.Surface.ToCss(), Border = _colors.Border.ToCss(), Accent = _colors.Accent.ToCss(),
        Healthy = _colors.Status.Pass.Fill.ToCss(), Warning = _colors.Status.Medium.Fill.ToCss(), Critical = _colors.Status.Critical.Fill.ToCss(),
        Unknown = _colors.Status.Neutral.Fill.ToCss(), Disabled = _colors.Status.Maintenance.Fill.ToCss(), FontFamily = _context.Font.Family
    };
}
