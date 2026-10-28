using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    internal PreparedDiagramFont? PreparedFont { get; set; }
}

/// <summary>Admits authored native role requests into the same compiler-owned text capture.</summary>
internal static class TopologyPreparedFont {
    internal static bool HasMaterialTextPolicy(TopologyChart chart, FontSpec font) {
        foreach (var node in chart.Nodes) {
            if (Material(node.TextStyle) || Material(node.SubtitleTextStyle) || Material(node.CaptionTextStyle)) return true;
            foreach (var detail in node.Details) if (Material(detail.TextStyle)) return true;
        }
        foreach (var group in chart.Groups) if (Material(group.TextStyle)) return true;
        foreach (var edge in chart.Edges) if (Material(edge.TextStyle)) return true;
        return false;
        bool Material(TextStyleOverride? value) => value != null && (value.FontFamily != null || value.FontWeight != null
            || value.ColorPaletteIndex.HasValue || value.Variations != null || value.OpenTypeLanguageTag != null || value.TextCase.HasValue
            || !value.Resolve(new TextStyle { Font = font.Clone() }).Font.SameRequest(font));
    }
}
