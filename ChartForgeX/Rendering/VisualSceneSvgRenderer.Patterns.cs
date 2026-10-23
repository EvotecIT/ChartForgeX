using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    // The same native brush and clip as raster export, with a stable tile instead of a width-dependent line count.
    private static bool WritePatternTile(SvgMarkupWriter writer, VisualScene scene, VisualSceneGroup group, string prefix, ref int index, VisualSvgOptions? options) {
        if (group.PathClip == null || !group.Metadata.TryGetValue("data-cfx-pattern-kind", out var kind)
            || !Enum.TryParse<ChartFillPattern>(kind, out var pattern) || index + 1 >= scene.Nodes.Count
            || !(scene.Nodes[index + 1] is VisualSceneLine brush)) return false;
        var spacing = Math.Max(4, double.Parse(group.Metadata["data-cfx-pattern-spacing"], CultureInfo.InvariantCulture));
        var id = prefix + "-pattern-tile-" + index.ToString(CultureInfo.InvariantCulture);
        writer.StartElement("defs").EndStartElement();
        writer.StartElement("pattern").Attribute("id", id).Attribute("patternUnits", "userSpaceOnUse")
            .Attribute("width", spacing).Attribute("height", spacing).EndStartElement();
        foreach (var line in ChartPatternLineGeometry.BuildUserSpace(pattern, -spacing, -spacing, spacing * 3, spacing * 3, 0, spacing, 0)) {
            writer.StartElement("line").Attribute("x1", line.X1).Attribute("y1", line.Y1).Attribute("x2", line.X2).Attribute("y2", line.Y2);
            Paint(writer, brush, prefix, index + 1, options: options); writer.EndEmptyElement();
        }
        writer.EndElement(); writer.EndElement();
        writer.StartElement("path").Attribute("d", PathData(group.PathClip)).Attribute("fill", "url(#" + id + ")").Attribute("stroke", "none").EndEmptyElement();
        while (index + 1 < scene.Nodes.Count && scene.Nodes[index + 1] is VisualSceneLine) index++;
        return true;
    }
}
