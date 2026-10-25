using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

internal static partial class Program {
    private static void IshikawaNaturalCauseContracts() {
        const string source = "%%{init:{'ishikawa':{'diagramPadding':24,'useMaxWidth':false},'themeVariables':{'mainBkg':'#E2E8F0','lineColor':'#7C3AED','textColor':'#182235'}}}%%\nishikawa-beta\nDelayed release\n  People\n    Shared ownership\n      Missing reviewer availability\n  Process\n    Change review\n      Incomplete acceptance evidence";
        var direct = MermaidRenderer.Render(source); PackageAssertions.Require(!direct.HasErrors && direct.Artifact != null, "Packed Ishikawa source failed.");
        var artifact = direct.Artifact!; var block = (FishboneDiagramBlock)artifact.Model!; var size = block.MeasureNaturalSize();
        PackageAssertions.Require(block.Options.Size.Width >= size.Width && block.Options.Size.Height >= size.Height, "Packed Fishbone natural frame was lost.");
        var svg = artifact.ToSvg(); var groups = XDocument.Parse(svg).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "fishbone-cause").ToArray();
        PackageAssertions.Require(groups.Length == 6 && (string?)groups[2].Attribute("data-parent-id") == "cause-0.0", "Packed Ishikawa lost actual nested parent identity.");
        PackageAssertions.Require(groups[2].Descendants().Any(e => e.Name.LocalName == "title" && e.Value == "Missing reviewer availability"), "Packed Ishikawa lost its complete caption.");
        PackageAssertions.Png(artifact.ToPng()); PackageAssertions.Require(artifact.HtmlSizing == VisualArtifactHtmlSizing.PreserveSize, "Packed Ishikawa changed native host sizing.");
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fenced.HasErrors && fenced.Artifacts.Single().ToSvg() == svg, "Packed Ishikawa direct/fence geometry differed.");
        var native = FishboneDiagramBlock.Create("Outcome").WithTheme(VisualTheme.Graphite()); native.AddCause("Cause").AddChild("Nested").AddChild("Deep fact");
        native.WithNaturalSize(); PackageAssertions.Png(native.ToPng()); PackageAssertions.Require(native.ToSvg().Contains("Deep fact", StringComparison.Ordinal), "Packed native cause tree failed.");
        var portable = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        PackageAssertions.Require(portable.Width == block.Options.Size.Width && portable.Extensions["mermaid.nodes"] == "7", "Packed Ishikawa envelope dimensions or source count changed.");
    }
}
