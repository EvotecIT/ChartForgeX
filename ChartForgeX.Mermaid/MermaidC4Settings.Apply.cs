using System;
using ChartForgeX.Topology;
using ChartForgeX.Typography;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidC4Settings {
    internal void Apply(TopologyChart chart, MermaidC4Document document, MermaidPresentationOptions? host) {
        if (!HasSettings) return;
        var geometry = _geometry.ContainsKey("width") || _geometry.ContainsKey("c4ShapePadding") || _wrap.HasValue;
        var grid = _geometry.ContainsKey("c4ShapeMargin") || _geometry.ContainsKey("diagramMarginX") || _geometry.ContainsKey("diagramMarginY")
            || _geometry.ContainsKey("nextLinePaddingX");
        if (grid) {
            var previous = chart.GridLayout;
            chart.LayoutMode = TopologyLayoutMode.GroupGrid;
            chart.GridLayout = new TopologyGridLayout(document.MaximumShapesPerRow ?? previous?.MaximumNodesPerRow ?? 4,
                document.MaximumBoundariesPerRow ?? previous?.MaximumGroupsPerRow ?? 2,
                Value("c4ShapeMargin", previous?.HorizontalSpacing ?? 28),
                _geometry.TryGetValue("c4ShapeMargin", out var margin) ? 2 * margin : previous?.VerticalSpacing ?? 28,
                Optional("diagramMarginX") ?? previous?.PaddingX, Optional("diagramMarginY") ?? previous?.PaddingY,
                Value("nextLinePaddingX", previous?.RowIndent ?? 0));
        }
        chart.DefaultRenderOptions ??= new TopologyRenderOptions();
        if (geometry) {
            chart.DefaultRenderOptions.AllowMultilineNodeLabels = true;
            chart.DefaultRenderOptions.WrapNodeLabels = _wrap ?? true;
            chart.DefaultRenderOptions.MaxNodeLabelLines = 32;
            chart.DefaultRenderOptions.MaxNodeSubtitleLines = 32;
        }
        for (var index = 0; index < chart.Nodes.Count; index++) {
            var node = chart.Nodes[index]; var element = document.Elements[index];
            if (_elements.TryGetValue(SourceType(element.Kind), out var style)) {
                if (style.Fill != null) { node.BackgroundColor = style.Fill; node.LabelColor = "#FFFFFF"; }
                if (style.Border != null) node.Color = style.Border;
                node.TextStyle = HostFont(style.Font, host);
            }
            if (geometry || node.TextStyle?.HasOverrides == true) {
                // The existing native shape-content owner measures complete source captions before routing.
                node.Shape ??= TopologyNodeShape.Rounded;
                node.FitLabelToContent = true;
                node.PreserveDisplayModeSize = true;
                if (_geometry.TryGetValue("width", out var width)) node.Width = width;
                if (_geometry.TryGetValue("c4ShapePadding", out var padding)) node.ContentPadding = padding / 2;
                if (geometry && (_wrap ?? true)) {
                    // This is the pinned shared C4 label helper's real allocation: width - 2*padding, minimum 32.
                    node.MaximumTextWidth = Math.Max(32, Value("width", 216) - 2 * Value("c4ShapePadding", 20));
                }
            }
        }
        if (_boundary != null) foreach (var group in chart.Groups) group.TextStyle = HostFont(_boundary, host);
        if (_message != null) foreach (var edge in chart.Edges) edge.TextStyle = HostFont(_message, host);
    }
    private double Value(string key, double fallback) => _geometry.TryGetValue(key, out var value) ? value : fallback;
    private double? Optional(string key) => _geometry.TryGetValue(key, out var value) ? value : (double?)null;
    private static TextStyleOverride? HostFont(TextStyleOverride? style, MermaidPresentationOptions? host) {
        if (style == null) return null;
        var copy = style.Clone();
        if (host?.FontFamily != null) copy.FontFamily = host.FontFamily;
        if (host?.FontSize != null) copy.FontSize = host.FontSize;
        return copy;
    }
    private static string SourceType(string kind) {
        var external = kind.EndsWith("_external", StringComparison.Ordinal);
        if (external) kind = kind.Substring(0, kind.Length - "_external".Length);
        if (kind.EndsWith("db", StringComparison.Ordinal)) kind = kind.Substring(0, kind.Length - 2) + "_db";
        else if (kind.EndsWith("queue", StringComparison.Ordinal)) kind = kind.Substring(0, kind.Length - 5) + "_queue";
        return (external ? "external_" : string.Empty) + kind;
    }
}
