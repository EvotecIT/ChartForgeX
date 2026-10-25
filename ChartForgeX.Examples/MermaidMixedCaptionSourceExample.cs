/// <summary>Thin ordinary Mermaid-package consumer of the maintained safe-caption source strings.</summary>
public static class MermaidMixedCaptionSourceExample {
    /// <summary>Returns source owned by this recipe; Flowchart nodes, Requirement text and Mindmap nodes use safe native emphasis.</summary>
    public static string Source(string family) => family switch {
        "flowchart" => "flowchart LR\nreview[\"`Review **carefully** and *record*\naccepted work`\"] --> ready[Ready]",
        "mindmap" => "mindmap\n  root[\"`Review **carefully** and *record*\naccepted work`\"]\n    ready[Ready]",
        "requirement" => "requirementDiagram\nrequirement review {\n id: R1\n text: \"Review **carefully**<br>and *record*<br>accepted work\"\n risk: low\n verifymethod: test\n}",
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };
    /// <summary>Creates one source-backed artifact through the canonical parser/converter and native renderer.</summary>
    public static VisualArtifact Create(string family, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, string? fontFamily = null) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var width = variant == "compact" ? 390 : 1100;
        var result = ChartForgeX.Mermaid.MermaidRenderer.Render(Source(family), new ChartForgeX.Mermaid.MermaidRenderOptions {
            Presentation = new ChartForgeX.Mermaid.MermaidPresentationOptions { FontFamily = fontFamily, ThemeMode = mode, TopologySizing = ChartForgeX.Mermaid.MermaidTopologySizing.Natural },
            Flowchart = new ChartForgeX.Mermaid.MermaidFlowchartRenderOptions { Width = width, Height = 420 },
            Requirement = new ChartForgeX.Mermaid.MermaidTopologyRenderOptions { Width = width, Height = 420 }, MindMap = new ChartForgeX.Mermaid.MermaidTopologyRenderOptions { Width = width, Height = 420 }
        });
        if (result.HasErrors || result.Artifact == null) throw new InvalidOperationException("The maintained mixed-caption source must render through its ordinary public API.");
        return result.Artifact;
    }
}
