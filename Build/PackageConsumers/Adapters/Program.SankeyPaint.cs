using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void SankeySourcePaintContracts() {
        const string source = "%%{init: {\"sankey\":{\"linkColor\":\"gradient\",\"labelStyle\":\"outlined\",\"nodeColors\":{\"A].[B\":\"#2864B4\",\"Ready 東京\":\"#BB6428\"}}}}%%\nsankey-beta\nA].[B,Ready 東京,7";
        var options = new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject };
        var rendered = MermaidRenderer.Render(source, options); var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!rendered.HasErrors && rendered.Artifact != null && rendered.Diagnostics.Count == 0 && !fenced.HasErrors && fenced.Artifacts.Count == 1,
            "Packed Sankey paint became a warning or fallback artifact.");
        var chart = (Chart)rendered.Artifact!.Model!;
        PackageAssertions.Require(chart.Options.Sankey.FlowColorMode == ChartSankeyFlowColorMode.Gradient && chart.Options.Sankey.LabelStyle == ChartSankeyLabelStyle.Outlined
            && chart.Series[0].Nodes[0].Id == "A].[B" && chart.Series[0].PointColors[0] is { } capturedColor && capturedColor.Equals(ChartColor.FromHex("#2864B4")), "Packed source paint lost raw node identity.");
        PackageAssertions.Require(XDocument.Parse(rendered.Artifact.ToSvg()).Descendants().Any(node => node.Name.LocalName == "linearGradient")
            && rendered.Artifact.ToSvg() == fenced.Artifacts[0].ToSvg() && rendered.Artifact.ToPng().SequenceEqual(fenced.Artifacts[0].ToPng()),
            "Packed direct and fenced Sankey paint output differs.");
        Console.WriteLine("Packed Mermaid Sankey source colors, raw keys and native/fenced exports passed.");
    }
}
