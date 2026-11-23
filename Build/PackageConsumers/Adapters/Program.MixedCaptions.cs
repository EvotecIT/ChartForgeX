using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void NativeMixedCaptionContracts() {
        const string source = "flowchart LR\nA[\"`Normal **bold** and *italic*`\"]";
        var result = RenderFixture(source);
        PackageAssertions.Require(!result.HasErrors && result.Diagnostics.Count == 0 && result.Artifact != null, "Packed safe Markdown source did not render natively.");
        var artifact = result.Artifact!; var model = (TopologyChart)artifact.Model!; var content = model.Nodes[0].LabelContent;
        PackageAssertions.Require(content.PlainText == "Normal bold and italic" && content.Runs.Any(run => run.Emphasis == TextInlineEmphasis.Bold), "Packed native source lost its editable runs or plain projection.");
        var svg = XDocument.Parse(artifact.ToSvg()); var ids = svg.Descendants().Attributes("id").Select(id => id.Value).ToArray();
        PackageAssertions.Require(svg.Descendants().Any(node => node.Attribute("data-cfx-native-text-resources") != null)
            && svg.Descendants().Where(node => node.Name.LocalName == "use").All(node => ids.Contains(((string)node.Attribute("href")!).Substring(1))), "Packed caption SVG lost permanent glyph resources.");
        PackageAssertions.Png(artifact.ToPng()); var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).ToTopologyChart();
        PackageAssertions.Require(restored.Nodes[0].LabelContent.Runs.Select(run => run.Emphasis).SequenceEqual(content.Runs.Select(run => run.Emphasis)), "Packed editable caption runs did not survive portable restoration.");
    }
}
