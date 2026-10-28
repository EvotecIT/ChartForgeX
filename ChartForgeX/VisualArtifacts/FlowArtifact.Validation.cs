using System;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class FlowArtifact {
    internal void ValidateNative() {
        Defined(LayoutMode); Defined(Direction);
        foreach (var lane in Lanes) Defined(lane.Status);
        foreach (var step in Steps) { Defined(step.Kind); Defined(step.Status); }
        foreach (var edge in Connectors) { Defined(edge.Kind); Defined(edge.Direction); Defined(edge.Status); }
        static void Defined<T>(T value) where T : struct {
            if (!Enum.IsDefined(typeof(T), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "A native flow policy must be defined.");
        }
    }
}
