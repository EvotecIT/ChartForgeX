using System;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Source configuration lowers through the native crossing policy and current interpolation owner.</summary>
public static class SwimlaneCrossingPaintRecipe {
    /// <summary>Returns the complete source for default Basis, disabled paint or explicit linear Arc/Gap.</summary>
    public static string Source(string form = "basis") {
        if (form is not ("basis" or "false" or "arc" or "gap")) throw new ArgumentOutOfRangeException(nameof(form));
        var settings = form == "basis" ? "" : "%%{init: {\"swimlane\":{\"lineHops\":" + (form == "false" ? "false" : "\"" + form + "\"") + "},\"flowchart\":{\"curve\":\"linear\"}}}%%\n";
        return settings + "swimlane-beta LR\n subgraph Inbound\n a[Accept]\n b[Build]\n end\n subgraph Outbound\n c[Check]\n d[Deliver]\n end\n a --> d\n b --> c\n";
    }
    /// <summary>Creates the source-backed artifact through the normal Mermaid package.</summary>
    public static VisualArtifact Create(string form = "basis", string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var rendered = MermaidRenderer.Render(Source(form), new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = mode },
            Swimlane = new MermaidFlowchartRenderOptions { Width = variant == "compact" ? 390 : 760, Height = 520 }
        });
        return rendered.Artifact ?? throw new InvalidOperationException("The supported source must produce a native artifact.");
    }
}
