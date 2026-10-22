using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.Topology;

internal static partial class Program {
    private static void MermaidRequirementStyleContracts() {
        const string styled = "requirementDiagram\nrequirement req {\nid: 1\n}\nstyle req fill:#ff0000";
        var options = new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject };
        var strict = MermaidRenderer.Render(styled, options);
        var strictFence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + styled + "\n```");
        PackageAssertions.Require(!strict.HasErrors && strict.Diagnostics.Count == 0 && strict.Artifact?.Model is TopologyChart native
            && native.Nodes.Single(node => node.Id == "req").BackgroundColor == "#ff0000"
            && !strictFence.HasErrors && strictFence.Diagnostics.Count == 0 && strictFence.Artifacts.Count == 1,
            "Packed strict Requirement fill lost its native paint or Markdown application.");
        PackageAssertions.Require(XDocument.Parse(strict.Artifact!.ToSvg()).Descendants().Any(node =>
            string.Equals((string?)node.Attribute("fill"), "#ff0000", StringComparison.OrdinalIgnoreCase)),
            "Packed Requirement retained style metadata without painting the requested native fill.");
        PackageAssertions.Require(strict.Artifact.ToSvg() == strictFence.Artifacts[0].ToSvg()
            && strict.Artifact.ToPng().SequenceEqual(strictFence.Artifacts[0].ToPng()),
            "Packed direct and Markdown Requirement fill exports disagree.");
        PackageAssertions.Png(strict.Artifact.ToPng());

        // The shared safe-style owner diagnoses CSS filters instead of silently claiming a native effect.
        const string unsupported = "requirementDiagram\nrequirement req {\nid: 1\n}\nstyle req filter:blur(2px)";
        var rejected = MermaidRenderer.Render(unsupported, options);
        var rejectedFence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + unsupported + "\n```");
        PackageAssertions.Require(rejected.HasErrors && rejected.Artifact == null
            && rejected.Diagnostics.Any(item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement && item.Span.Line == 5)
            && rejectedFence.HasErrors && rejectedFence.Artifacts.Count == 0
            && rejectedFence.Diagnostics.Any(item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement && item.Line == 6),
            "Packed strict Requirement CSS filter rejection lost its shared diagnostic or source location.");
    }
}
