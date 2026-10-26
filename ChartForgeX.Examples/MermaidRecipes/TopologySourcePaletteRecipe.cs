using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Uses the source language's default palette for an ordinary review workflow.</summary>
public static class TopologySourcePaletteRecipe {
    /// <summary>Creates the graph shared by the gallery and emitted standalone consumer.</summary>
    public static VisualArtifact Create(string family, string variant, VisualThemeMode mode, string? fontFamily = null) {
        if (variant is not ("compact" or "wide")) throw new ArgumentException("Unknown palette recipe size.", nameof(variant));
        var width = variant == "compact" ? 640 : 1000;
        var result = MermaidRenderer.Render(Source(family), new MermaidRenderOptions {
            WarningPolicy = MermaidWarningPolicy.Reject,
            Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 440 },
            Swimlane = new MermaidFlowchartRenderOptions { Width = width, Height = 440 },
            Agentflow = new MermaidTopologyRenderOptions { Width = width, Height = 440 },
            Presentation = new MermaidPresentationOptions { ThemeMode = mode, FontFamily = fontFamily, TopologySizing = MermaidTopologySizing.Natural }
        });
        if (result.HasErrors || result.Diagnostics.Count != 0 || result.Artifact == null) throw new InvalidOperationException("The source palette workflow must render without diagnostics.");
        return result.Artifact;
    }
    /// <summary>Gets literal source without an invented authored theme declaration.</summary>
    public static string Source(string family) {
        var header = family == "flowchart" ? "flowchart LR\nsubgraph intake[Request intake]" : family == "swimlane" ? "swimlane-beta LR\nsubgraph intake[Request intake]"
            : family == "agentflow" ? "agentflow-beta LR\nflow intake[Request intake]" : throw new ArgumentException("Unknown palette source family.", nameof(family));
        return header + "\nReceive[Receive request] -->|Review| Approve[Approve request]\nend\nApprove -->|Publish| Publish[Publish result]";
    }
}
