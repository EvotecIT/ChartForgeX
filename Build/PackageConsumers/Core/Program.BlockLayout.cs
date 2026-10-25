using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.VisualBlocks;

internal static partial class Program {
    private static void NativeBlockHierarchyContracts() {
        var block = BlockLayoutBlock.Create().WithSize(700, 460).WithColumns(3)
            .AddComposite("outer", "Outer", 2, 2).AddItem("a", "A", "outer", shape: BlockLayoutShape.Hexagon)
            .AddComposite("inner", "Inner", parentId: "outer", columns: 1).AddItem("b", "B", "inner", shape: BlockLayoutShape.Stadium)
            .AddItem("outside", "Outside", shape: BlockLayoutShape.Diamond).AddEdge("b", "outside", "Across");
        block.Items[1].Fill = ChartColor.FromHex("#cceeff"); block.Items[1].Stroke = new ChartStrokeStyle(width: 2);
        var detached = block.Items[1].Clone(); detached.Metadata["consumer"] = "detached";
        PackageAssertions.Require(!block.Items[1].Metadata.ContainsKey("consumer"), "Packed block item cloning retained mutable metadata aliases.");
        var svg = XDocument.Parse(block.ToSvg());
        PackageAssertions.Require(svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "block-layout-node") == 5,
            "Packed nested blocks lost a genuine source cell.");
        var edge = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "block-layout-edge");
        PackageAssertions.Require((string?)edge.Attribute("data-source-id") == "b" && (string?)edge.Attribute("data-target-id") == "outside",
            "Packed cross-boundary connector lost its source identity.");
        PackageAssertions.Png(block.ToPng());
    }
}
