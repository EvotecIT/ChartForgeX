using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

internal static partial class Program {
    private static void WardleyOverlayContracts() {
        const string source = "wardley-beta\ncomponent API [0.6,0.3] (outsource) inertia\npipeline API {\ncomponent Small [0.2] label [-8,16]\ncomponent Scale [0.5] label [-8,30]\n}\nScale -> API\nannotations [0.2,0.1]\nannotation 1, [0.8,0.5] \"Keep the contract visible\"";
        var direct = MermaidRenderer.Render(source); PackageAssertions.Require(!direct.HasErrors && direct.Artifact != null, "Packed Wardley source failed.");
        var artifact = direct.Artifact!; var block = (WardleyMapBlock)artifact.Model!; var svg = artifact.ToSvg(); var xml = XDocument.Parse(svg);
        PackageAssertions.Require(block.Links.Single().FromId == "API_Scale", "Packed Wardley child endpoint identity changed.");
        PackageAssertions.Require(xml.Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == "wardley-annotation-box") == 1, "Packed Wardley annotation box was not painted.");
        PackageAssertions.Require(xml.Descendants().Any(e => (string?)e.Attribute("data-cfx-role") == "wardley-sourcing-outsource"), "Packed Wardley sourcing paint was not emitted.");
        PackageAssertions.Png(artifact.ToPng());
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fenced.HasErrors && fenced.Artifacts.Single().ToSvg() == svg, "Packed Wardley direct/fence output differs.");
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        PackageAssertions.Require(envelope.Extensions["mermaid.annotation-box"] == "true" && envelope.Extensions["mermaid.annotations"] == "1", "Packed Wardley common envelope lost its retained facts.");
        var map = WardleyMapBlock.Create().WithSize(600, 400).WithAnnotationsBox(.2, .1).WithStyle(new WardleyMapStyle(annotationFill: ChartColor.Parse("#D1FAE5")));
        map.AddNode("api", "API", .6, .3); map.AddAnnotation(1, "Keep the contract visible", .8, .5);
        PackageAssertions.Png(map.ToPng()); PackageAssertions.Require(map.ToSvg().Contains("Keep the contract visible", StringComparison.Ordinal), "Packed native Wardley caption failed.");
    }
}
