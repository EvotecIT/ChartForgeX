using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Svg;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    private static string NumericPartsPath(IReadOnlyList<ScenePart> parts) {
        var output = new SvgPathDataBuilder();
        foreach (var part in parts) output.Append(part.Path.Commands, part.Close);
        return output.Build();
    }
    private static IReadOnlyList<ScenePart> HorizontalCylinderParts(TopologyNode node) {
        var vertical = new TopologyNode { X = node.Y, Y = node.X, Width = node.Height, Height = node.Width, Shape = TopologyNodeShape.Cylinder };
        return SceneParts(vertical).Select(part => new ScenePart(part.Path.Transpose(), part.Close, part.Fill)).ToArray();
    }
}
