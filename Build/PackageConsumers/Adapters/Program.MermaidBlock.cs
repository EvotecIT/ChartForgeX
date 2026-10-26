using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Mermaid;

internal static partial class Program {
    private static void NativeMermaidBlockContracts() {
        const string source = "block-beta\ncolumns 2\nblock:group\ncolumns 1\na{Decision}\nend\nb[(Store)]\na --> b\nclassDef selected fill:#bbf7d0,stroke:#166534,color:#14532d\nclass a selected\n";
        var parsed = new MermaidParser().ParseBlock(source);
        PackageAssertions.Require(!parsed.HasErrors && parsed.Document != null, "Packed nested Mermaid block source was rejected.");
        var block = parsed.Document!.ToBlockLayoutBlock(); var svg = XDocument.Parse(block.ToSvg());
        PackageAssertions.Require(block.Items.Single(item => item.Id == "a").ParentId == "group" &&
            svg.Descendants().Any(node => string.Equals((string?)node.Attribute("fill"), "#bbf7d0", StringComparison.OrdinalIgnoreCase)), "Packed block source lost hierarchy or native class paint.");
        PackageAssertions.Png(block.ToPng());
    }
}
