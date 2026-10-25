using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static bool HasNativeSequenceNumber(string svg,string caption,string value) => XDocument.Parse(svg).Descendants().Any(group =>
        (string?)group.Attribute("data-cfx-role")=="sequence-message" && ((string?)group.Attribute("data-label"))?.StartsWith(caption,StringComparison.Ordinal)==true
        && group.Descendants().Any(number => (string?)number.Attribute("data-cfx-role")=="sequence-number" && number.Value==value));
    private static void SequenceMessagePresentationSourceContracts() {
        using var stream=typeof(Program).Assembly.GetManifestResourceStream("PackageProbe.sequence-message-presentation.mmd")??throw new InvalidOperationException("Missing message-presentation source.");using var reader=new StreamReader(stream);var source=reader.ReadToEnd();
        var result=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject});PackageAssertions.Require(result.Diagnostics.Count==0&&result.Artifact!=null,"Source number/alignment lowering failed.");
        var model=(SequenceArtifact)result.Artifact!.Model!;var prepared=model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1400,1000),24)));
        PackageAssertions.Require(model.Layout?.NumberPresentation==SequenceNumberPresentation.Badge&&prepared.Regions.Count(item=>item.Role=="sequence-number-badge")==3,"Source number occurrences changed.");PackageAssertions.Png(prepared.ToPng());
    }
}
