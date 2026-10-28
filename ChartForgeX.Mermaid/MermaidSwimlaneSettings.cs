using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Maps the pinned swimlane variant's flowchart spacing and explicit rank policy into native owners.</summary>
internal sealed class MermaidSwimlaneSettings {
    internal MermaidGraphSettings Graph { get; private set; } = null!;
    internal double? Padding => Graph.Padding;
    internal double? NodeSpacing => Graph.NodeSpacing;
    internal double? RankSpacing => Graph.RankSpacing;
    internal bool? UseMaxWidth => Graph.UseMaxWidth;
    internal bool? IgnoreCrossLaneEdges, AutomaticLaneOrdering, OptimizeRanksByCrossings;
    internal TopologyCrossingPaint? CrossingPaint;
    internal bool HasNativeSettings => Graph.HasNativeSettings || CrossingPaint != null || IgnoreCrossLaneEdges.HasValue
        || AutomaticLaneOrdering.HasValue || OptimizeRanksByCrossings.HasValue;

    internal static bool IsScalarPath(string path) => MermaidGraphSettings.IsScalarPath(path,"flowchart") || path is "swimlane.ignoreCrossLaneEdges"
        or "swimlane.automaticLaneOrdering" or "swimlane.optimizeRanksByCrossings" or "swimlane.lineHops";

    internal bool Handles(string path) {
        if (Graph.Handles(path)) return true;
        if (IsScalarPath(path)) return true;
        foreach (var root in new[] { "flowchart.diagramPadding", "flowchart.nodeSpacing", "flowchart.rankSpacing",
            "flowchart.useMaxWidth", "swimlane.ignoreCrossLaneEdges", "swimlane.automaticLaneOrdering",
            "swimlane.optimizeRanksByCrossings", "swimlane.lineHops" }) {
            if (path.StartsWith(root + ".", StringComparison.Ordinal)) return true;
        }
        return false;
    }

    internal static MermaidSwimlaneSettings Read(MermaidSourceConfiguration source,
        Action<MermaidConfigurationSetting, string> error, Action<MermaidConfigurationSetting, string> unsupported) {
        var result = new MermaidSwimlaneSettings {
            Graph = MermaidGraphSettings.Read(source,"flowchart",error), IgnoreCrossLaneEdges = Boolean("swimlane.ignoreCrossLaneEdges"),
            AutomaticLaneOrdering = Boolean("swimlane.automaticLaneOrdering"), OptimizeRanksByCrossings = Boolean("swimlane.optimizeRanksByCrossings")
        };
        var hops = source.Last("swimlane.lineHops");
        // The pinned source default is Arc, including an explicit null. Unsupported resolved curves
        // remain ordinary paint through the explicit generic SkipUnsupported policy.
        var style = TopologyCrossingPaintStyle.Arc; var enabled = true;
        var scalarNull = hops != null && !hops.IsStructured && !hops.IsString
            && (hops.Value == null || hops.Value == "~" || string.Equals(hops.Value, "null", StringComparison.OrdinalIgnoreCase));
        if (hops != null && hops.IsStructured) {
            error(hops, "Mermaid configuration 'swimlane.lineHops' must be false, null, true, 'arc' or 'gap'.");
            enabled = false;
        } else if (hops != null && !scalarNull) {
            if (!hops.IsString && bool.TryParse(hops.Value, out var value)) enabled = value;
            else if (hops.IsString && hops.Value is "arc" or "gap") style = hops.Value == "gap" ? TopologyCrossingPaintStyle.Gap : TopologyCrossingPaintStyle.Arc;
            else { error(hops, "Mermaid configuration 'swimlane.lineHops' must be false, null, true, 'arc' or 'gap'."); enabled = false; }
        }
        if (enabled) result.CrossingPaint = new TopologyCrossingPaint(style, 6, TopologyCrossingCurveHandling.SkipUnsupported);
        return result;

        bool? Boolean(string path) {
            var setting = source.Last(path);
            if (setting == null) return null;
            if (!setting.IsString && bool.TryParse(setting.Value, out var value)) return value;
            error(setting, "Mermaid configuration '" + path + "' must be a boolean.");
            return null;
        }
    }

    internal void Apply(TopologyChart chart, MermaidPresentationOptions? host) {
        Graph.Apply(chart,host);
        var render = chart.DefaultRenderOptions!;
        chart.CrossingPaint = CrossingPaint;
        if (IgnoreCrossLaneEdges.HasValue) render.SwimlaneIgnoreCrossLaneRankConstraints = IgnoreCrossLaneEdges;
        if (AutomaticLaneOrdering.HasValue) render.SwimlaneAutomaticLaneOrdering = AutomaticLaneOrdering;
        if (OptimizeRanksByCrossings.HasValue) render.LayeredOptimizeRanksByCrossings = OptimizeRanksByCrossings;
    }
}
