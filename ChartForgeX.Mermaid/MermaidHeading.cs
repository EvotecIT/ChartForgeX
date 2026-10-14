using System;

namespace ChartForgeX.Mermaid;

/// <summary>Resolves the heading every Mermaid family shows above its native render.</summary>
internal static class MermaidHeading {
    /// <summary>
    /// Uses the caller's title, then the family's own title statement, then the frontmatter <c>title</c>
    /// that Mermaid accepts for every family, then the family name.
    /// </summary>
    internal static string Title(MermaidDocument document, string? optionTitle, string? sourceTitle, string fallback) {
        if (!string.IsNullOrWhiteSpace(optionTitle)) return optionTitle!;
        if (!string.IsNullOrWhiteSpace(sourceTitle)) return sourceTitle!;
        var frontMatterTitle = FrontMatterTitle(document.FrontMatter);
        return string.IsNullOrWhiteSpace(frontMatterTitle) ? fallback : frontMatterTitle!;
    }

    /// <summary>
    /// Mermaid draws no subtitle, so only a caller-supplied subtitle is shown. The header keyword line
    /// remains available as <c>mermaid.header</c> artifact metadata.
    /// </summary>
    internal static string Subtitle(string? optionSubtitle) => string.IsNullOrWhiteSpace(optionSubtitle) ? string.Empty : optionSubtitle!;

    /// <summary>Reads the top-level <c>title</c> scalar; nested keys such as configuration values are ignored.</summary>
    internal static string? FrontMatterTitle(string? frontMatter) => MermaidParser.ReadFrontMatterTitle(frontMatter);
}
