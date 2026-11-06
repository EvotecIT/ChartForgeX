using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private TopologyRegionLayout _regionLayout = null!;
    private TopologyRegionPaintBuilder _regionPainter = null!;
    private double _regionUnitWidth;
    private readonly List<ChartRect> _regionLabelBounds = new();
    private Dictionary<string, string> RegionValues() => new(StringComparer.Ordinal) {
        ["surface"] = _colors.ElevatedSurface.ToCss(), ["surface-sunken"] = _colors.Surface.ToCss(),
        ["border"] = _colors.Border.ToCss(), ["text"] = _colors.Foreground.ToCss(), ["text-muted"] = _colors.MutedForeground.ToCss()
    };
    private PreparedVisual CompileRegions(FlowArtifact? flow) {
        var validation = new TopologyChartValidator().ValidateScenarioReferences(_source);
        if (!validation.IsValid) throw new TopologyValidationException(validation);
        _chart = TopologyLayoutEngine.Clone(_source);
        _chart.Theme = _context.Theme; _chart.ThemeMode = _context.ThemeMode;
        _legend = _source.Legend == null ? null : TopologyLegend.Clone(_source.Legend);
        TopologyRegionPaintPlan plan;
        if (_resolvedLayout) {
            plan = _chart.PreparedRegionPaintPlan ?? throw new InvalidOperationException("The detached region preparation has no native paint plan.");
            InitializeRegionFacts();
        } else {
            _chart.PreparedRegionPaintPlan = null;
            if (_options.View != null) _chart = TopologyLayoutEngine.Prepare(_chart, _options.View, _options);
            _regionPainter = new TopologyRegionPaintBuilder(_builder);
            _regionUnitWidth = _context.Layout.Size.Width;
            _chart.Viewport.Width = _regionUnitWidth;
            var top = _options.IncludeTitle && !string.IsNullOrWhiteSpace(SourceTitle) ? 86 : 20;
            _regionLayout = new TopologyRegionLayout(_chart, _regionUnitWidth, top, text => _regionPainter.MeasureText(text, 12).Width);
            InitializeRegionFacts();
            var height = _regionLayout.Bottom + 44 + RegionLegendRows(_regionUnitWidth) * 28;
            var size = new VisualSize(_regionUnitWidth, height);
            _chart.Viewport = new TopologyViewport { Width = size.Width, Height = size.Height, Padding = 0 };
            using (_regionPainter.PushGroup(_chart.Id, "topology", new Dictionary<string, string> {
                ["class"] = "cfx-topo", ["data-chart-id"] = _chart.Id ?? "topology", ["data-layout-mode"] = "Regions",
                ["data-region-narrow"] = _regionLayout.Narrow ? "true" : "false"
            })) {
                if (top > 20) {
                    _regionPainter.Text(SourceTitle!, 20, 30, 18, _colors.Foreground, 600);
                    if (!string.IsNullOrWhiteSpace(SourceSubtitle)) _regionPainter.Text(RegionFit(SourceSubtitle!, _regionUnitWidth - 40, 12), 20, 52, 12, _colors.MutedForeground);
                }
                foreach (var group in _chart.Groups) BuildRegion(group);
                foreach (var edge in _chart.Edges.OrderByDescending(edge => edge.RoutingPriority).ThenBy(edge => edge.Id, StringComparer.Ordinal)) {
                    var source = _nodesById[edge.SourceNodeId]; var target = _nodesById[edge.TargetNodeId];
                    if (_regionLayout.Narrow && (edge.IsOverlay || source.GroupId != target.GroupId)) continue;
                    BuildRegionEdge(edge);
                }
                foreach (var node in _chart.Nodes) BuildRegionNode(node);
                if (_regionLayout.Narrow) {
                    foreach (var group in _chart.Groups) BuildRegionLinks(group);
                    foreach (var edge in _chart.Edges.Where(edge => edge.IsOverlay)) BuildRegionChip(edge);
                }
                BuildRegionLegend();
            }
            plan = _regionPainter.Freeze(size, _resolvedLabelBounds.ToDictionary(pair => pair.Key.Id, pair => pair.Value, StringComparer.Ordinal));
            _chart.PreparedRegionPaintPlan = plan;
            if (_naturalSize && !_options.FitContentToViewport) _context = new VisualRenderContext(new VisualLayoutOptions(size, _context.Layout.PaddingEdges), _context.Theme, _context.ThemeMode, _context.Frame, _context.Font);
        }
        _plot = _contentClip = new ChartRect(0, 0, _context.Layout.Size.Width, _context.Layout.Size.Height);
        _builder = new VisualSceneBuilder(_context.Layout.Size, _context.Font);
        _builder.Rect(_plot, _colors.Background);
        ResolveFit();
        plan.Paint(_builder, Point, _scale);
        foreach (var edge in _chart.Edges) if (plan.Labels.TryGetValue(edge.Id, out var bounds))
            _resolvedLabelBounds[edge] = Bounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        var accessibility = _source.Accessibility.Clone();
        accessibility.Name ??= RegionSummary();
        accessibility.Description ??= string.Join(". ", _chart.Nodes.Select(RegionTooltip));
        var semantics = SemanticSnapshot(accessibility);
        if (flow != null) semantics = VisualArtifactInterchangeMapping.FromPreparedFlow(flow, semantics);
        return new PreparedVisual(_builder.Build(), accessibility, semanticInterchange: semantics,
            svgOptions: new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(_options.IdScope), _options.SvgColorVariables, responsive: _options.UseResponsiveSvg));
    }
    private void InitializeRegionFacts() {
        _nodesById = _chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        _edgeRenderOrders = _chart.Edges.Select((edge, index) => (edge, index)).ToDictionary(pair => pair.edge, pair => pair.index);
        _highlight = TopologyHighlightState.From(_chart, _options);
        foreach (var edge in _chart.Edges) {
            var route = _chart.PreparedRegionRoutes != null && _chart.PreparedRegionRoutes.TryGetValue(edge.Id, out var points) ? points : Array.Empty<ChartPoint>();
            _routes[edge] = route; _paintRoutes[edge] = route;
        }
    }
    private string RegionSummary() => _chart.Groups.Count + " regions, " + _chart.Nodes.Count + " nodes, " + _chart.Edges.Count + " links: "
        + _chart.Edges.Count(edge => edge.Status == TopologyHealthStatus.Critical) + " failing, "
        + _chart.Edges.Count(edge => edge.Status == TopologyHealthStatus.Warning) + " slow";
    private static string RegionTooltip(TopologyNode node) => string.Join(" · ", new[] { node.Label, node.Subtitle,
        string.Join(", ", node.Badges.Select(badge => badge.Meaning)), node.Badges.Count > 2 ? "+" + (node.Badges.Count - 2) + " more badges" : null,
        node.Badge, node.StatusText ?? node.Status.ToString() }.Where(value => !string.IsNullOrWhiteSpace(value)));
    private static string RegionTone(TopologyHealthStatus status) => status switch {
        TopologyHealthStatus.Healthy => "tone-good", TopologyHealthStatus.Warning => "tone-warn", TopologyHealthStatus.Critical => "tone-bad", _ => "tone-neutral"
    };
    private static string RegionTone(TopologyTone tone, TopologyHealthStatus status) => tone == TopologyTone.Automatic ? RegionTone(status) : "tone-" + tone.ToString().ToLowerInvariant();
    private ChartColor RegionToneColor(TopologyTone tone, TopologyHealthStatus status) => tone switch {
        TopologyTone.Good => Status(TopologyHealthStatus.Healthy), TopologyTone.Warn => Status(TopologyHealthStatus.Warning),
        TopologyTone.Bad => Status(TopologyHealthStatus.Critical), TopologyTone.Info => _colors.Info,
        TopologyTone.Neutral => _colors.Status.Neutral.Fill, _ => status == TopologyHealthStatus.Disabled ? _colors.Status.Neutral.Fill : Status(status)
    };
    private static VisualThemeColorToken RegionColorToken(TopologyTone tone, TopologyHealthStatus status) => tone switch {
        TopologyTone.Info => VisualThemeColorToken.Info, TopologyTone.Good => VisualThemeColorToken.Positive,
        TopologyTone.Warn => VisualThemeColorToken.Warning, TopologyTone.Bad => VisualThemeColorToken.Negative,
        TopologyTone.Neutral => VisualThemeColorToken.Neutral, _ => status switch {
            TopologyHealthStatus.Healthy => VisualThemeColorToken.Positive, TopologyHealthStatus.Warning => VisualThemeColorToken.Warning,
            TopologyHealthStatus.Critical => VisualThemeColorToken.Negative, _ => VisualThemeColorToken.Neutral
        }
    };
    private string RegionFit(string value, double width, double size, int weight = 400) {
        if (_regionPainter.MeasureText(value, size, weight).Width <= width) return value;
        var length = value.Length;
        while (length > 0 && _regionPainter.MeasureText(value.Substring(0, length) + "…", size, weight).Width > width) length--;
        if (length > 0 && char.IsHighSurrogate(value[length - 1])) length--;
        return value.Substring(0, length) + "…";
    }
    private void BuildRegion(TopologyGroup group) {
        var values = RegionValues();
        values["x"] = Number(group.X); values["y"] = Number(group.Y); values["width"] = Number(group.Width); values["height"] = Number(group.Height);
        values["title-x"] = Number(group.X + 20); values["title-y"] = Number(group.Y + 28); values["subtitle-y"] = Number(group.Y + 46);
        var chipText = string.IsNullOrWhiteSpace(group.StatusText) ? "" : RegionFit(group.StatusText!, Math.Max(40, group.Width * .35 - 20), 11, 500);
        var chipWidth = chipText.Length == 0 ? 0 : _regionPainter.MeasureText(chipText, 11, 500).Width + 20;
        values["title"] = RegionFit(group.Label.ToUpperInvariant(), group.Width - 40 - (chipWidth > 0 ? chipWidth + 12 : 0), 12, 600);
        values["subtitle"] = RegionFit(group.Subtitle ?? "", group.Width - 40, 12);
        TopologyRegionTemplate.Draw(_regionPainter, "region", values, new Dictionary<string, string> { ["data-group-id"] = group.Id }, "topology-region", group.Id, () => {
            if (chipWidth <= 0) return;
            var x = group.X + group.Width - 20 - chipWidth;
            values["chip-x"] = Number(x); values["chip-y"] = Number(group.Y + 14); values["chip-width"] = Number(chipWidth);
            values["chip-label-x"] = Number(x + 10); values["chip-label-y"] = Number(group.Y + 28);
            values["chip-label"] = chipText; values["tone"] = RegionTone(group.Status); values["status"] = Status(group.Status).ToCss();
            values["status-token"] = RegionColorToken(TopologyTone.Automatic, group.Status).ToString();
            using (_regionPainter.PushTooltip(group.StatusText!)) TopologyRegionTemplate.Draw(_regionPainter, "region-status", values);
        });
    }
    private void BuildRegionNode(TopologyNode node) {
        var values = RegionValues(); var r = node.Width / 2; var cx = node.X + r; var cy = node.Y + r;
        var labelSize = r < 22 ? 11 : 13;
        values["cx"] = Number(cx); values["cy"] = Number(cy); values["r"] = Number(r); values["tone"] = RegionTone(node.Tone, node.Status);
        values["focus-r"] = Number(r + (r >= 26 ? 2.5 : 2) + 4);
        values["status"] = RegionToneColor(node.Tone, node.Status).ToCss(); values["ring-width"] = r >= 26 ? "5" : "4";
        values["status-token"] = RegionColorToken(node.Tone, node.Status).ToString();
        values["label-size"] = Number(labelSize); values["label-y"] = Number(cy + _regionPainter.TextAscent(labelSize, 700) / 2 - 1);
        values["label"] = RegionFit(node.Label, r * 2 - 8, labelSize, 700); values["sub-y"] = Number(cy + r + 16);
        values["sub"] = RegionFit(node.Subtitle ?? "", Math.Min(140, (_chart.Groups.First(group => group.Id == node.GroupId).Width - 40) / 2), 12);
        var metadata = NodeMetadata(node); metadata["data-node-x"] = Number(cx); metadata["data-node-y"] = Number(cy);
        metadata["data-region-tip"] = RegionTooltip(node); metadata["data-node-health-ring"] = "true";
        metadata["data-node-badge-meanings"] = string.Join(", ", node.Badges.Select(badge => badge.Meaning));
        metadata["data-region-generated-name"] = string.IsNullOrWhiteSpace(node.AccessibleName) ? "true" : "false";
        metadata["aria-label"] = string.IsNullOrWhiteSpace(node.AccessibleName) ? RegionTooltip(node) : node.AccessibleName!;
        var badges = node.Badges.Take(2).ToArray();
        using (_regionPainter.PushTooltip(RegionTooltip(node))) TopologyRegionTemplate.Draw(_regionPainter, "node", values, metadata, "topology-node", node.Id, () => {
            for (var index = 0; index < badges.Length; index++) {
                values["badge-tone"] = (badges[index].Informational ? _colors.Info : _colors.Warning).ToCss();
                values["badge-token"] = (badges[index].Informational ? VisualThemeColorToken.Info : VisualThemeColorToken.Warning).ToString();
                using (_regionPainter.PushTranslation(cx - (badges.Length * 14 + (badges.Length - 1) * 4) / 2d + index * 18, cy - r - (r >= 26 ? 2.5 : 2) - 14))
                    TopologyRegionTemplate.Draw(_regionPainter, TopologyRegionTemplate.BadgeSymbol(badges[index].Glyph), values);
            }
        });
    }
}
