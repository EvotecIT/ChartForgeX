using System;
using System.Collections.Generic;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Shows finite caller-owned images and native catalog glyphs in an ordinary review workflow.</summary>
public static class FlowchartAssetRecipe {
    /// <summary>Creates the same source recipe used by native, emitted and browser consumers.</summary>
    public static VisualArtifact Create(string variant, VisualThemeMode mode, string? fontFamily = null) {
        if (variant is not ("compact" or "wide")) throw new ArgumentException("Unknown asset recipe size.", nameof(variant));
        var options = new MermaidRenderOptions {
            WarningPolicy = MermaidWarningPolicy.Reject,
            Flowchart = new MermaidFlowchartRenderOptions { Width = variant == "compact" ? 640 : 1100, Height = 720,
                ImageBindings = new Dictionary<string,RgbaImage> { ["review-team"] = Image() } },
            Presentation = new MermaidPresentationOptions { FontFamily = fontFamily, ThemeMode = mode, TopologySizing = MermaidTopologySizing.Natural }
        };
        var result = MermaidRenderer.Render(Source(), options);
        if (result.HasErrors || result.Diagnostics.Count != 0 || result.Artifact == null) throw new InvalidOperationException("The finite asset workflow must render without diagnostics.");
        return result.Artifact;
    }
    /// <summary>Gets the inert source shared by compiled and emitted programs.</summary>
    public static string Source() => "flowchart LR\nsubgraph intake[Request intake]\nReceive[Receive request] --> Review@{img:'review-team',label:'Review group',pos:'t',h:40,constraint:'on'}\nReview --> Policy@{icon:'common:server',label:'Policy service',form:'rounded'}\nend\nsubgraph delivery[Delivery]\nPublish@{icon:'cloud:cloud',label:'Publish result',form:'circle'} --> Archive[Archive receipt]\nend\nPolicy --> Publish\nstyle Review fill:#fef2f2,stroke:#475569,color:#1e293b\nstyle Policy fill:#eef2ff,stroke:#475569,color:#1e293b\nstyle Publish fill:#ecfdf5,stroke:#475569,color:#1e293b\n";
    /// <summary>Creates a small synthetic caller image with opaque and translucent pixels, without an external asset.</summary>
    public static RgbaImage Image() => new(4,2,new byte[] { 37,99,235,255,14,165,233,255,20,184,166,255,34,197,94,255,
        37,99,235,128,14,165,233,192,20,184,166,255,34,197,94,255 });
}
