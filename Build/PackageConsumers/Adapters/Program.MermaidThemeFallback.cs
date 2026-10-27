using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void NativeMermaidThemeFallbackContracts() {
        const string source = "railroad-abnf-beta\nvalue = %s\"Exact\" ;";
        var document = new MermaidParser().Parse(source).Document!;
        document.Theme = "dark";
        var expected = document.ToVisualArtifact(FixtureOptions()).ToPng();
        var result = new MermaidVisualMarkupParser(FixtureOptions(), VisualThemeMode.Dark)
            .Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!result.HasErrors && result.Artifacts.Count == 1,
            "Packed Mermaid theme fallback rejected valid fenced source.");
        var artifact = result.Artifacts.Single();
        PackageAssertions.Require(!artifact.Metadata.ContainsKey("mermaid.config.theme") && expected.SequenceEqual(artifact.ToPng()),
            "Packed Mermaid theme fallback lost captured native paint or claimed an authored palette.");
        var authored = "---\nconfig:\n  theme: default\n---\n" + source;
        var explicitResult = new MermaidVisualMarkupParser(FixtureOptions(), VisualThemeMode.Dark)
            .Parse("```mermaid\n" + authored + "\n```");
        PackageAssertions.Require(!explicitResult.HasErrors && explicitResult.Artifacts.Single().Metadata["mermaid.config.theme"] == "default"
            && RenderFixture(authored).Artifact!.ToPng().SequenceEqual(explicitResult.Artifacts.Single().ToPng()),
            "Packed Mermaid page fallback replaced an authored native palette.");
    }
}
