using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Renders an ordinary review workflow through the native source curve owner.</summary>
public static class FlowchartCurveRecipe {
    /// <summary>Creates the same source recipe used by the gallery and emitted consumer program.</summary>
    public static VisualArtifact Create(string family, string variant, VisualThemeMode mode, string? fontFamily = null) {
        if (variant is not ("compact" or "wide")) throw new ArgumentException("Unknown curve recipe size.", nameof(variant));
        var options = new MermaidRenderOptions {
            WarningPolicy = MermaidWarningPolicy.Reject,
            Flowchart = FamilyOptions(variant), Swimlane = FamilyOptions(variant),
            Presentation = new MermaidPresentationOptions { ThemeMode = mode, FontFamily = fontFamily, TopologySizing = MermaidTopologySizing.Natural }
        };
        var result = MermaidRenderer.Render(Source(family), options);
        if (result.HasErrors || result.Diagnostics.Count > 0 || result.Artifact == null)
            throw new InvalidOperationException("The native curve workflow must render without diagnostics.");
        return result.Artifact;
    }

    /// <summary>Gets the inert source shared by native, browser, and reference observations.</summary>
    public static string Source(string family) {
        if (family is not ("flowchart" or "swimlane")) throw new ArgumentException("Unknown curve recipe family.", nameof(family));
        return (family == "swimlane" ? "swimlane-beta" : "flowchart") + " LR\n" + Workflow;
    }

    private static MermaidFlowchartRenderOptions FamilyOptions(string variant) => new() { Width = variant == "compact" ? 640 : 1100, Height = 720 };
    private const string Workflow = "subgraph intake[Request intake]\ndirection TB\nReceive[Receive request] e@--> Review[Review request]\nReview --> Clarify[Clarify details]\nClarify --> Review\nend\nsubgraph delivery[Delivery]\ndirection TB\nApprove[Approve request] --> Publish[Publish result]\nPublish --> Archive[Archive receipt]\nend\nReview --> Approve\ne@{curve:bumpX}\n";
}
