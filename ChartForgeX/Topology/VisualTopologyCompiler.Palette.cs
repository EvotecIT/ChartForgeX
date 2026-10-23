using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private ChartColor PaletteColor(int index) => _colors.Palette[index % _colors.Palette.Count];

    private SvgPaint PalettePaint(int index, ChartColor result, double opacity = 1) {
        var paint = SvgPaint.Palette(PaletteColor(index), index % _colors.Palette.Count);
        return opacity == 1 ? paint : paint.WithOpacity(result, opacity);
    }

    private string? NodeAccentSource(TopologyNode node) => TopologyNodeAccent.Authored(node, TopologyRenderPrimitives.ResolveNodeIcon(node, _options), GlyphAccentSource(node));
    private bool NodeUsesPalette(TopologyNode node) => TopologyNodeAccent.UsesPalette(node, GlyphAccentSource(node));

    // Group, legend and detail glyphs are synthetic nodes and keep their own authored paint contract.
    private TopologyAccentSource GlyphAccentSource(TopologyNode node) => _nodesById != null && !string.IsNullOrEmpty(node.Id) &&
        _nodesById.TryGetValue(node.Id, out var actual) && ReferenceEquals(actual, node) ? _options.AccentSource : TopologyAccentSource.Auto;

    private ChartColor NodeAccent(TopologyNode node) {
        var authored = NodeAccentSource(node);
        if (!string.IsNullOrWhiteSpace(authored)) return Color(authored, Status(node.Status));
        return NodeUsesPalette(node) ? PaletteColor(node.PaletteIndex.GetValueOrDefault()) : Status(node.Status);
    }
}
