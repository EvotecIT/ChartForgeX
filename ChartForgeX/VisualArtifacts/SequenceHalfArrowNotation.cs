namespace ChartForgeX.VisualArtifacts;

/// <summary>The incumbent finite half-arrow token interpretation shared by terminal paint and measured number placement.</summary>
internal static class SequenceHalfArrowNotation {
    internal static bool TryRead(string notation, out bool top, out bool solid, out bool reverse) {
        if (TryReadCore(notation,out top,out solid,out reverse)) return true;
        return notation.Length > 1 && (notation[notation.Length-1]=='+'||notation[notation.Length-1]=='-')
            && TryReadCore(notation.Substring(0,notation.Length-1),out top,out solid,out reverse);
    }
    private static bool TryReadCore(string notation, out bool top, out bool solid, out bool reverse) {
        top = solid = reverse = false;
        switch (notation) {
            case "-|\\": case "--|\\": top = true; solid = true; reverse = false; break;
            case "-|/": case "--|/": top = false; solid = true; reverse = false; break;
            case "-\\\\": case "--\\\\": top = true; solid = false; reverse = false; break;
            case "-//": case "--//": top = false; solid = false; reverse = false; break;
            case "/|-": case "/|--": top = true; solid = true; reverse = true; break;
            case "\\|-": case "\\|--": top = false; solid = true; reverse = true; break;
            case "//-": case "//--": top = true; solid = false; reverse = true; break;
            case "\\\\-": case "\\\\--": top = false; solid = false; reverse = true; break;
            default: return false;
        }
        return true;
    }
}
