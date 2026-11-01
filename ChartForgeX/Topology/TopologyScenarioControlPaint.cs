using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

/// <summary>Shares the accent ink between initial controls and subsequent route selections.</summary>
internal static class TopologyScenarioControlPaint {
    internal static string Ink(string? color) {
        var fallback = ChartColor.FromHex("#2563eb");
        if (!TopologySurfacePaintRules.TryColor(color, fallback, out var fill)) fill = fallback;
        return ChartColorMath.AccessibleTextOnBackground(fill).ToHex();
    }

    internal static string? Style(string? color) => string.IsNullOrWhiteSpace(color) ? null :
        "--cfx-topology-scenario-color:" + color!.Trim() + ";--cfx-topology-scenario-ink:" + Ink(color);
}
