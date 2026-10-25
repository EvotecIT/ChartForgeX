using System;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Ordinary Mermaid-package recipes for native mixed captions in graph headers, details and relationships.</summary>
public static class MermaidRemainingCaptionRolesExample {
    /// <summary>Returns complete editable source; identities remain independent of their formatted captions.</summary>
    public static string Source(string family) => family switch {
        "class" => "classDiagram\ndirection TB\nnamespace Domain[\"`Review **scope**`\"] {\nclass A[\"`Review **carefully**`\"]\nclass B[Ready]\nA : `+record *work*`\nA --> B : `Accept **work**`\n}",
        "er" => "erDiagram\ndirection TB\nsubgraph Domain[\"Review **scope**\"]\nA[\"Review **carefully**\"] {\nstring id PK \"record *work*\"\n}\nB[Ready]\nA ||--o{ B : \"Accept **work**\"\nend",
        "flowchart" => "flowchart TB\nsubgraph Domain[\"`Review **scope**`\"]\nA[Review] -->|\"`Accept **work** and *record*`\"| B[Ready]\nend",
        "requirement" => "requirementDiagram\ndirection TB\nrequirement \"Review **carefully**\" {\nid: R1\ntext: \"Accept *work*\"\nrisk: low\nverifymethod: test\n}\nelement \"Record *work*\" {\ntype: document\n}\n\"Record *work*\" - satisfies -> \"Review **carefully**\"",
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };

    /// <summary>Creates one source-backed native artifact with compact/wide geometry and paired themes.</summary>
    public static VisualArtifact Create(string family, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, string? fontFamily = null) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        var width = variant == "compact" ? 390 : 1100;
        var topology = new MermaidTopologyRenderOptions { Width = width, Height = 620 };
        var result = MermaidRenderer.Render(Source(family), new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { FontFamily = fontFamily, ThemeMode = mode, TopologySizing = MermaidTopologySizing.Natural },
            Class = topology, EntityRelationship = topology, Requirement = topology,
            Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 620 }
        });
        if (result.HasErrors || result.Artifact == null) throw new InvalidOperationException("The maintained remaining-caption recipe must render through its public source API.");
        return result.Artifact;
    }
}
