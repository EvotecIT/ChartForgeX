namespace ChartForgeX.Rendering;

/// <summary>One detached original caller request, before model headings and legend facts are resolved.</summary>
internal static class PreparedDiagramFrame {
    internal static VisualFrame? Capture(VisualFrame request, bool includeLegend, bool transparent) =>
        !transparent && request.SameRequest(new VisualFrame()) || request.SameRequest(new VisualFrame(showLegend: includeLegend, transparentBackground: transparent))
            ? null : request.RequestValues(retainOwnedFonts: true);
}
