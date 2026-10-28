/// <summary>Consumes canonical UseCase source through the ordinary Mermaid package.</summary>
public static class MermaidUseCaseJsonSourceExample {
    /// <summary>Parses the supplied complete source and renders its linked native table vertex.</summary>
    public static VisualArtifact Create(string source, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, string? fontFamily = null) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var compact = variant == "compact"; if (compact) source = source.Replace("direction LR", "direction TB", StringComparison.Ordinal);
        var result = ChartForgeX.Mermaid.MermaidRenderer.Render(source, new ChartForgeX.Mermaid.MermaidRenderOptions {
            WarningPolicy = ChartForgeX.Mermaid.MermaidWarningPolicy.Reject,
            UseCase = new ChartForgeX.Mermaid.MermaidFlowchartRenderOptions { Width = compact ? 390 : 1000, Height = compact ? 1000 : 680,
                Presentation = new ChartForgeX.Mermaid.MermaidPresentationOptions { FontFamily = fontFamily, ThemeMode = mode,
                    TopologySizing = ChartForgeX.Mermaid.MermaidTopologySizing.FixedViewport } } });
        if (result.HasErrors || result.Artifact == null) throw new InvalidOperationException("The complete UseCase JSON source must lower through its canonical native table owner.");
        // This example explicitly chooses complete participant captions through the existing native per-node policy.
        var topology = (ChartForgeX.Topology.TopologyChart)result.Artifact.Model!;
        topology.DefaultRenderOptions ??= new ChartForgeX.Topology.TopologyRenderOptions();
        topology.DefaultRenderOptions.WrapNodeLabels = true;
        foreach (var node in topology.Nodes) if (node.Shape == ChartForgeX.Topology.TopologyNodeShape.Actor)
            node.MaximumLabelCharacters = node.Label.Length;
        return result.Artifact;
    }
}
