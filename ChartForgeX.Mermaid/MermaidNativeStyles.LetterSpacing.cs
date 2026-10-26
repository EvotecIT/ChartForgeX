using ChartForgeX.Topology;
using ChartForgeX.Typography;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidNativeStyles {
    /// <summary>Source lengths are computed once after presentation selected the actual native base size.</summary>
    internal static void ResolveLetterSpacing(TopologyChart chart) {
        var size = chart.Theme.Typography.DataLabelSize;
        foreach (var node in chart.Nodes) Apply(node.Metadata, node.TextStyle, size);
        foreach (var group in chart.Groups) Apply(group.Metadata, group.TextStyle, size);
        foreach (var edge in chart.Edges) Apply(edge.Metadata, edge.TextStyle, size);
    }
    internal static void ResolveLetterSpacing(ChartForgeX.VisualBlocks.BlockLayoutBlock block) {
        var size = ChartForgeX.VisualBlocks.VisualBlockRendering.BodyFontSize(block.Options.ResolvedTheme, 14);
        foreach (var item in block.Items) Apply(item.Metadata, item.TextStyle, size);
    }
    private static void Apply(System.Collections.Generic.Dictionary<string, string> metadata, TextStyleOverride? style, double fallbackSize) {
        if (style != null && metadata.TryGetValue("mermaid.style.letter-spacing", out var source)
            && TypographyCss.TryLetterSpacing(source, style.FontSize ?? fallbackSize, out var spacing)) style.LetterSpacing = spacing;
    }
}
