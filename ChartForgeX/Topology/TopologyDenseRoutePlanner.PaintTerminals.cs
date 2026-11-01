using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    // Keep the searched plan immutable. Extend only its terminal legs for the public paint/hit route,
    // preserving their normal and fan offset instead of clamping them onto a different card side.
    internal static void AttachPaintTerminals(List<ChartPoint> route, bool source = true, bool target = true) {
        if (route.Count < 2) return;
        if (source) Extend(0, 1);
        if (target) Extend(route.Count - 1, route.Count - 2);
        void Extend(int index, int adjacent) {
            var point = route[index]; var next = route[adjacent];
            var dx = next.X - point.X; var dy = next.Y - point.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length > .000001) route[index] = new ChartPoint(point.X - dx / length * EndpointGap, point.Y - dy / length * EndpointGap);
        }
    }
}
