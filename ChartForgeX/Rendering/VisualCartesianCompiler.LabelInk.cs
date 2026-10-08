using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    // Placement owns the chosen candidate. Ink for a contained alternative is selected only
    // after that choice, so outside labels keep their configured typography and foreground.
    private sealed class CartesianLabels : List<LabelPlacementRequest> {
        internal readonly Dictionary<LabelPlacementRequest, (ChartRect Bounds, TextStyle Style, SvgPaint? Paint)> ContainedInk = new();
        internal readonly HashSet<LabelPlacementRequest> Outlined = new();
    }
}
