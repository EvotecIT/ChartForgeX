using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Owns Agentflow's finite shape contract independently from Flowchart's larger surface catalog.</summary>
internal static class MermaidAgentflowShapes {
    /// <summary>Maps the pinned family defaults, aliases and canonical names to existing native surfaces.</summary>
    internal static bool TryResolve(string shape, out TopologyNodeShape native) {
        switch (shape) {
            case "task": case "round": case "rect": case "squareRect": case "roundedRect":
            case "connector":
                native = TopologyNodeShape.Rounded; return true;
            case "collapsedGroup":
                native = TopologyNodeShape.CollapsedContainer; return true;
            case "tool": case "subroutine": case "subprocess": case "subproc": case "framed-rectangle":
                native = TopologyNodeShape.Subroutine; return true;
            case "input": case "lean-right":
                native = TopologyNodeShape.Parallelogram; return true;
            case "decision": case "diamond":
                native = TopologyNodeShape.Diamond; return true;
            case "refdoc": case "lin-doc": case "lined-document":
                native = TopologyNodeShape.LinedDocument; return true;
            case "action": case "hexagon": case "hex":
                native = TopologyNodeShape.Hexagon; return true;
            default:
                native = default; return false;
        }
    }

    /// <summary>Distinguishes shapes removed from Agentflow from unsupported native presentation extensions.</summary>
    internal static bool IsRemoved(string shape) => shape is
        "doc" or "stadium" or "terminal" or "circle" or "trapezoid" or "inv_trapezoid" or "inv-trapezoid"
        or "doublecircle" or "double-circle" or "typeDeclaration" or "procs" or "lean_left" or "lean-left"
        or "in-out" or "cylinder" or "ellipse" or "odd" or "tag-rect" or "tagged-rectangle" or "delay"
        or "half-rounded-rectangle" or "lin-rect" or "lined-rectangle" or "win-pane" or "window-pane"
        or "curv-trap" or "curved-trapezoid";
}
