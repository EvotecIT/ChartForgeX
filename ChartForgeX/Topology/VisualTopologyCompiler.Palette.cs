using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private ChartColor PaletteColor(int index) => _colors.Palette[index % _colors.Palette.Count];

    private SvgPaint PalettePaint(int index, ChartColor result, double opacity = 1) {
        var paint = SvgPaint.Palette(PaletteColor(index), index % _colors.Palette.Count);
        return opacity == 1 ? paint : paint.WithOpacity(result, opacity);
    }

    private ChartColor NodeAccent(TopologyNode node) => Color(node.Color ?? TopologyRenderPrimitives.ResolveNodeIcon(node, _options)?.Color,
        node.PaletteIndex.HasValue ? PaletteColor(node.PaletteIndex.Value) : Status(node.Status));
}
