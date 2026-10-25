using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Requirement role colors rendered through native topology, SVG and PNG.</summary>
public static class RequirementThemeRecipe {
    /// <summary>Creates the same finite native drawing used by the complete source program.</summary>
    public static VisualArtifact Create(string variant, VisualThemeMode mode, string? fontFamily = null) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        var result = MermaidRenderer.Render(Source(mode), new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = mode, FontFamily = fontFamily },
            Requirement = new MermaidTopologyRenderOptions { Width = variant == "compact" ? 390 : 1100, Height = 640 }
        });
        if (result.HasErrors || result.Diagnostics.Count != 0 || result.Artifact == null)
            throw new InvalidOperationException("The native requirement recipe must render without diagnostics.");
        return result.Artifact;
    }

    /// <summary>Returns literal native role settings and requirements; it contains no external assets.</summary>
    public static string Source(VisualThemeMode mode) {
        var dark = mode == VisualThemeMode.Dark;
        return "%%{init:{\"themeVariables\":{\"requirementBackground\":\"" + (dark ? "#382535" : "#F5DBE4")
            + "\",\"requirementBorderColor\":\"" + (dark ? "#E986BC" : "#B71D62")
            + "\",\"requirementTextColor\":\"" + (dark ? "#F9EAF5" : "#28132B")
            + "\",\"relationColor\":\"" + (dark ? "#C4B5FD" : "#2F4B7C")
            + "\",\"relationLabelColor\":\"" + (dark ? "#F9D994" : "#0D5325")
            + "\",\"relationLabelBackground\":\"" + (dark ? "#3B2755" : "#E7F2E9") + "\"}}}%%\n"
            + "requirementDiagram\ndirection TB\nrequirement review {\nid: R1\ntext: Review work\nrisk: low\nverifymethod: test\n}\n"
            + "element record {\ntype: document\ndocref: Review record\n}\nrecord - satisfies -> review";
    }
}
