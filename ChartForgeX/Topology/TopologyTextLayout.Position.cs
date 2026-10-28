using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal sealed partial class TopologyTextLayout {
    // Painting and explicit-baseline placement use the same measured row allocation.
    internal double PositionedTop(double y, double allocationHeight, bool verticallyCentered) =>
        y + (verticallyCentered ? Math.Max(0, allocationHeight - Height) / 2 : 0);
    internal double FirstBaseline(double y, double allocationHeight, bool verticallyCentered, double ascent) =>
        PositionedTop(y, allocationHeight, verticallyCentered) + ascent;
    internal static ChartRect PositionEnvelope(TopologyLabelPosition? position, double x, double y, double width, double height) {
        var top = y - height / 2; var bottom = y + height / 2;
        if (position != null && position.VerticalAnchor == TopologyLabelVerticalAnchor.TextBaseline) {
            top = Math.Min(top, position.Point.Y - height); bottom = Math.Max(bottom, position.Point.Y + height);
        }
        return new ChartRect(x - width / 2, top, width, bottom - top);
    }

}
