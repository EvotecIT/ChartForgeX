using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    // The same native brush and clip as raster export, with a stable tile instead of a width-dependent line count.
    // Equal tiles (same pattern, spacing and brush paint) are written once and shared by every clip that uses them.
    private static bool WritePatternTile(SvgMarkupWriter writer, VisualScene scene, VisualSceneGroup group, string prefix, ref int index, VisualSvgOptions? options, PatternTileCache tiles) {
        if (group.PathClip == null || !group.Metadata.TryGetValue("data-cfx-pattern-kind", out var kind)
            || !Enum.TryParse<ChartFillPattern>(kind, out var pattern) || index + 1 >= scene.Nodes.Count
            || !(scene.Nodes[index + 1] is VisualSceneLine brush)) return false;
        var spacing = Math.Max(4, double.Parse(group.Metadata["data-cfx-pattern-spacing"], CultureInfo.InvariantCulture));
        var lines = tiles.Scratch();
        foreach (var line in ChartPatternLineGeometry.BuildUserSpace(pattern, -spacing, -spacing, spacing * 3, spacing * 3, 0, spacing, 0)) {
            lines.StartElement("line").Attribute("x1", line.X1).Attribute("y1", line.Y1).Attribute("x2", line.X2).Attribute("y2", line.Y2);
            Paint(lines, brush, prefix, index + 1, options: options); lines.EndEmptyElement();
        }
        var content = lines.Build();
        var key = spacing.ToString("R", CultureInfo.InvariantCulture) + "|" + content;
        if (!tiles.Ids.TryGetValue(key, out var id)) {
            id = prefix + "-pattern-tile-" + index.ToString(CultureInfo.InvariantCulture);
            tiles.Ids.Add(key, id);
            writer.StartElement("defs").EndStartElement();
            writer.StartElement("pattern").Attribute("id", id).Attribute("patternUnits", "userSpaceOnUse")
                .Attribute("width", spacing).Attribute("height", spacing).EndStartElement();
            writer.Raw(content);
            writer.EndElement(); writer.EndElement();
        }
        writer.StartElement("path").Attribute("d", PathData(group.PathClip)).Attribute("fill", "url(#" + id + ")").Attribute("stroke", "none").EndEmptyElement();
        while (index + 1 < scene.Nodes.Count && scene.Nodes[index + 1] is VisualSceneLine) index++;
        return true;
    }

    // One scratch writer renders each tile's lines; the rendered lines are the identity of the tile.
    private sealed class PatternTileCache {
        private readonly System.Text.StringBuilder _buffer = new(256);
        private readonly SvgMarkupWriter _writer;
        internal PatternTileCache() => _writer = new SvgMarkupWriter(_buffer);
        internal Dictionary<string, string> Ids { get; } = new(StringComparer.Ordinal);
        internal SvgMarkupWriter Scratch() { _buffer.Clear(); return _writer; }
    }
}
