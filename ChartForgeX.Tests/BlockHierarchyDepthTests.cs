using ChartForgeX.VisualArtifacts;
using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BlockHierarchyDepthTests {
    internal const string Source = """
        block-beta
        columns 3
        block:services:2
          columns 2
          api["API"] worker("Worker")
          block:storage:2
            columns 2
            queue[["Queue"]] data[("Data")]
          end
        end
        client{{"Client"}}
        api --> client
        client --> data
        classDef default fill:#dbeafe,stroke:#1e3a8a,color:#172554
        classDef important fill:#fef3c7,stroke:#92400e,stroke-width:2,font-size:15px
        class api,worker important
        style api fill:#dcfce7
        """;

    [Fact]
    public void ParsedCompositesKeepSourceOrderLocalSpansAndNativeStyleCascade() {
        var result = new MermaidParser().ParseBlock(Source);
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == MermaidDiagnosticSeverity.Warning);
        var source = result.Document!;
        Assert.Equal(new[] { "services", "api", "worker", "storage", "queue", "data", "client" }, source.Items.Select(item => item.Id));
        Assert.Equal("services", source.Items[3].ParentId); Assert.Equal("storage", source.Items[4].ParentId);
        Assert.True(source.Items[0].IsComposite); Assert.Equal(2, source.Items[0].Columns); Assert.Equal(2, source.Items[3].ColumnSpan);
        Assert.Equal("#dcfce7", source.Items[1].Styles["fill"]); Assert.Equal("#92400e", source.Items[1].Styles["stroke"]);
        var block = source.ToBlockLayoutBlock();
        Assert.Equal(ChartColor.FromHex("#dcfce7"), block.Items[1].Fill); Assert.Equal(2d, block.Items[1].Stroke!.Width);
        Assert.Equal(15d, block.Items[1].TextStyle.FontSize); Assert.Equal("important", block.Items[1].Metadata["mermaid.classes"]);
        Assert.Equal(source.Items[1].Span.Line.ToString(CultureInfo.InvariantCulture), block.Items[1].Metadata["mermaid.source.line"]);
        Assert.Equal(2, source.Edges.Count); Assert.Equal("client", source.Edges[1].SourceId); Assert.Equal("data", source.Edges[1].TargetId);
    }

    [Theory]
    [InlineData(1100, false)]
    [InlineData(390, false)]
    [InlineData(1100, true)]
    [InlineData(390, true)]
    public void NestedCellsStayInsideRealParentSurfacesAndShareScriptFreeSvgPng(int width, bool dark) {
        var block = Parsed(width, dark); var svg = XDocument.Parse(block.ToSvg()); var image = block.RenderRgba();
        Save(block, "nested-" + width + "-" + dark);
        Assert.Equal(width, image.Width); Assert.Equal(620, image.Height);
        Assert.Equal(7, svg.Descendants().Count(node => Role(node) == "block-layout-node"));
        Assert.Equal(2, svg.Descendants().Count(node => Role(node) == "block-layout-edge"));
        var cells = Cells(svg);
        foreach (var cell in cells.Values) {
            var parentId = (string?)cell.Attribute("data-parent-id"); if (parentId == null) continue;
            var parent = cells[parentId];
            Assert.True(Number(cell, "data-x") > Number(parent, "data-x")); Assert.True(Number(cell, "data-y") > Number(parent, "data-y"));
            Assert.True(Number(cell, "data-x") + Number(cell, "data-width") < Number(parent, "data-x") + Number(parent, "data-width") + .002);
            Assert.True(Number(cell, "data-y") + Number(cell, "data-height") < Number(parent, "data-y") + Number(parent, "data-height") + .002);
        }
        var api = cells["api"];
        var px = (int)Math.Round(Number(api, "data-x") + Number(api, "data-width") / 2);
        var py = (int)Math.Round(Number(api, "data-y") + Number(api, "data-height") * .18);
        var offset = (py * image.Width + px) * 4;
        Assert.Equal(new byte[] { 220, 252, 231, 255 }, image.Pixels.Skip(offset).Take(4));
        var edges = svg.Descendants().Where(node => Role(node) == "block-layout-edge").ToArray();
        Assert.Contains(edges, edge => (string?)edge.Attribute("data-source-id") == "api" && (string?)edge.Attribute("data-target-id") == "client");
        Assert.Contains(edges, edge => (string?)edge.Attribute("data-source-id") == "client" && (string?)edge.Attribute("data-target-id") == "data");
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName is "script" or "foreignObject" or "image");
        Assert.DoesNotContain(svg.Descendants().Attributes(), attribute => attribute.Name.LocalName.StartsWith("on", StringComparison.Ordinal));
        Assert.All(svg.Descendants().Where(node => Role(node) == "block-layout-node-label"), node => Assert.True(Number(node, "font-size") > 0));
    }

    [Theory]
    [InlineData("a{Diamond}", BlockLayoutShape.Diamond)]
    [InlineData("a{{Hex}}", BlockLayoutShape.Hexagon)]
    [InlineData("a([Stadium])", BlockLayoutShape.Stadium)]
    [InlineData("a[[Sub]]", BlockLayoutShape.Subroutine)]
    [InlineData("a(((Double)))", BlockLayoutShape.DoubleCircle)]
    [InlineData("a[/Right/]", BlockLayoutShape.Parallelogram)]
    [InlineData("a[\\Left\\]", BlockLayoutShape.ParallelogramAlt)]
    [InlineData("a[/Trap\\]", BlockLayoutShape.Trapezoid)]
    [InlineData("a[\\Inverse/]", BlockLayoutShape.TrapezoidAlt)]
    [InlineData("a>Asym]", BlockLayoutShape.Asymmetric)]
    [InlineData("a<[Move]>(right,left)", BlockLayoutShape.Arrow)]
    [InlineData("a<[Move]>(up,down)", BlockLayoutShape.Arrow)]
    public void SupportedBlockShapeTokensUseNativeContoursAndPixels(string token, BlockLayoutShape expected) {
        var result = new MermaidParser().ParseBlock("block-beta\ncolumns 1\n" + token);
        Assert.False(result.HasErrors); var block = result.Document!.ToBlockLayoutBlock(new MermaidBlockRenderOptions { Width = 320, Height = 220, Title = "" });
        Assert.Equal(expected, block.Items[0].Shape);
        var svg = XDocument.Parse(block.ToSvg()); Assert.Single(svg.Descendants(), node => Role(node) == "block-layout-node");
        var image = block.RenderRgba(); Assert.Equal(320, image.Width); Assert.Equal(220, image.Height);
        Save(block, expected + "-" + (int)block.Items[0].ArrowDirections);
    }

    [Theory]
    [InlineData("block:parent\ncolumns 1\na:2\nend")]
    [InlineData("end\na")]
    [InlineData("block:parent\na")]
    [InlineData("a:25")]
    [InlineData("a\na")]
    [InlineData("a\nclass a missing")]
    [InlineData("a\nstyle missing fill:#fff")]
    [InlineData("a<[Move]>(diagonal)")]
    public void ContradictorySourceIsLocatedWithoutRenderedSubstitution(string body) {
        var result = new MermaidParser().ParseBlock("block-beta\n" + body);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, d => d.Severity == MermaidDiagnosticSeverity.Error && d.Span.Line >= 2);
        Assert.All(result.Diagnostics, d => Assert.True(d.Span.Line >= 1));
        var rendered = MermaidRenderer.Render("block-beta\n" + body);
        Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AncestorConnectorsTerminateOnTheFacingChildSurfaceRatherThanCrossingItsInterior(bool reverse) {
        var block = BlockLayoutBlock.Create().WithSize(640, 360).WithColumns(1).AddComposite("parent", columns: 1)
            .AddItem("child", "Child", "parent", shape: BlockLayoutShape.Circle).AddEdge(reverse ? "child" : "parent", reverse ? "parent" : "child");
        var svg = XDocument.Parse(block.ToSvg()); var cells = Cells(svg); var child = cells["child"];
        var edge = Assert.Single(svg.Descendants(), node => Role(node) == "block-layout-edge");
        var cx = Number(child, "data-x") + Number(child, "data-width") / 2; var cy = Number(child, "data-y") + Number(child, "data-height") / 2;
        var x = Number(edge, reverse ? "x1" : "x2"); var y = Number(edge, reverse ? "y1" : "y2");
        var ox = Number(edge, reverse ? "x2" : "x1"); var oy = Number(edge, reverse ? "y2" : "y1");
        var radius = Math.Min(Number(child, "data-width"), Number(child, "data-height")) / 2;
        Assert.InRange(Math.Abs(Math.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy))-radius), 0, .002);
        Assert.True((x-cx)*(ox-cx)+(y-cy)*(oy-cy)>0, "The child endpoint must face the actual opposite port outside its painted interior.");
    }

    [Theory]
    [InlineData(390, false)]
    [InlineData(1100, true)]
    public void NestedCircleConnectorsRetainTheAnalyticPaintedBoundary(int width, bool reverse) {
        var block = BlockLayoutBlock.Create().WithSize(width, 420).WithColumns(2)
            .AddComposite("group", columns: 1).AddItem("round", "Round", "group", shape: BlockLayoutShape.Circle)
            .AddItem("outside", "Outside", shape: BlockLayoutShape.Hexagon).AddEdge(reverse ? "outside" : "round", reverse ? "round" : "outside");
        var svg = XDocument.Parse(block.ToSvg()); var circle = Cells(svg)["round"];
        var edge = Assert.Single(svg.Descendants(), node => Role(node) == "block-layout-edge");
        var cx = Number(circle, "data-x") + Number(circle, "data-width") / 2; var cy = Number(circle, "data-y") + Number(circle, "data-height") / 2;
        var dx = Number(edge, reverse ? "x2" : "x1") - cx; var dy = Number(edge, reverse ? "y2" : "y1") - cy;
        var radius = Math.Min(Number(circle, "data-width"), Number(circle, "data-height")) / 2;
        Assert.InRange(Math.Abs(Math.Sqrt(dx * dx + dy * dy) - radius), 0, .002);
    }

    [Fact]
    public void ConnectorChainsKeepOfficialQuotedLabelsAndCapturedSolidThickDashedHiddenPaint() {
        const string source = "block-beta\ncolumns 4\na b c d\na -- \"Send\" --> b ==> c -.-> d\na ~~~ d\n";
        var parsed = new MermaidParser().ParseBlock(source); Assert.False(parsed.HasErrors);
        Assert.Equal(4, parsed.Document!.Edges.Count); Assert.Equal("Send", parsed.Document.Edges[0].Label);
        var block = parsed.Document.ToBlockLayoutBlock(); Assert.False(block.Edges[3].Visible);
        var svg = XDocument.Parse(block.ToSvg()); var edges = svg.Descendants().Where(node => Role(node) == "block-layout-edge").ToArray();
        Assert.Equal(3, edges.Length); Assert.Equal(3, Number(edges[1], "stroke-width")); Assert.Equal("4 3", (string?)edges[2].Attribute("stroke-dasharray"));
        Assert.Single(svg.Descendants(), node => Role(node) == "block-layout-connector-label" && node.Value == "Send");
    }

    [Fact]
    public void AutoColumnsAndForwardCrossBoundaryReferencesDoNotReorderDeclarations() {
        const string source = "block-beta\ncolumns auto\ninside --> outside\nblock:group\ncolumns auto\na:2 inside\nend\noutside\n";
        var result = new MermaidParser().ParseBlock(source); Assert.False(result.HasErrors);
        Assert.Equal(new[] { "group", "a", "inside", "outside" }, result.Document!.Items.Select(item => item.Id));
        var block = result.Document.ToBlockLayoutBlock(); Assert.True(block.AutoColumns); Assert.True(block.Items[0].AutoColumns);
        var cells = Cells(XDocument.Parse(block.ToSvg())); Assert.Equal(Number(cells["group"], "data-y"), Number(cells["outside"], "data-y"));
        Assert.Equal("group", block.Items.Single(item => item.Id == "inside").ParentId);
    }

    [Fact]
    public void MarkdownFenceUsesTheSameNestedSourceAndNativeArtifactOwner() {
        var result = new MermaidVisualMarkupParser().Parse("```mermaid {#nested width=\"900\" height=\"620\"}\n" + Source + "\n```");
        Assert.False(result.HasErrors); var artifact = Assert.Single(result.Artifacts);
        var model = Assert.IsType<BlockLayoutBlock>(artifact.Model);
        Assert.Equal("storage", model.Items.Single(item => item.Id == "data").ParentId);
        var svg = XDocument.Parse(artifact.ToSvg()); Assert.Equal(7, svg.Descendants().Count(node => Role(node) == "block-layout-node"));
        Assert.Equal(2, svg.Descendants().Count(node => Role(node) == "block-layout-edge"));
        Assert.Equal(900, RasterImageDecoder.Decode(artifact.ToPng()).Width);
    }

    [Fact]
    public void NestedBlocksAndClassExpansionConsumeTheExistingCumulativePolicy() {
        var options = new MermaidParseOptions(); options.ResourceLimits.MaximumElements = 12;
        var repeated = "block-beta\ncolumns 2\nclassDef default fill:#fff,stroke:#000,color:#111\nblock:parent\na b\nend\nc\n";
        var capped = new MermaidParser().Parse(repeated, options); Assert.True(capped.HasErrors);
        var nested = "block-beta\n" + string.Concat(Enumerable.Range(0, 65).Select(index => "block:g" + index + "\n"))
            + "a\n" + string.Concat(Enumerable.Repeat("end\n", 65));
        Assert.True(new MermaidParser().ParseBlock(nested).HasErrors);
    }

    [Fact]
    public void NativeHierarchyRejectsUnknownParentsCyclesAndLocalSpanOverflowBeforePaint() {
        var unknown = BlockLayoutBlock.Create().AddItem("a", "A", "missing");
        Assert.Throws<InvalidOperationException>(() => unknown.ToSvg());
        var cycle = BlockLayoutBlock.Create().AddComposite("a", parentId: "b").AddComposite("b", parentId: "a");
        Assert.Throws<InvalidOperationException>(() => cycle.RenderRgba());
        var overflow = BlockLayoutBlock.Create().AddComposite("parent", columns: 1).AddItem("a", "A", "parent", 2);
        Assert.Throws<InvalidOperationException>(() => overflow.ToSvg());
        var malformed = BlockLayoutBlock.Create().AddItem("a", "A"); malformed.Items[0].Columns = 2;
        Assert.Throws<InvalidOperationException>(() => malformed.ToSvg());
    }

    [Fact]
    public void UnsupportedCaptionDecorationIsRejectedRatherThanSilentlyPaintedAsPlainText() {
        var block = BlockLayoutBlock.Create().AddItem("a", "A"); block.Items[0].TextStyle.WithUnderline();
        Assert.Throws<NotSupportedException>(() => block.ToSvg());
    }

    [Fact]
    public void ItemCloneDetachesTypographyAndMetadataWhileRetainingImmutableStroke() {
        var item = new BlockLayoutItem("a", "A") { ParentId = "parent", Fill = ChartColor.FromHex("#cceeff"), Stroke = new ChartStrokeStyle(width: 2) };
        item.TextStyle.WithFontSize(18); item.Metadata["source"] = "original"; var copy = item.Clone();
        item.TextStyle.FontSize = 90; item.Metadata["source"] = "changed";
        Assert.Equal(18d, copy.TextStyle.FontSize); Assert.Equal("original", copy.Metadata["source"]); Assert.Same(item.Stroke, copy.Stroke); Assert.Equal("parent", copy.ParentId);
    }

    [Fact]
    public void EmptyParentAndOmittedOptionsKeepTheIncumbentFlatSvgAndPngBytes() {
        var first = BlockLayoutBlock.Create().WithSize(640, 320).WithColumns(2).AddItem("a", "A").AddItem("b", "B", shape: BlockLayoutShape.Circle).AddEdge("a", "b");
        var second = BlockLayoutBlock.Create().WithSize(640, 320).WithColumns(2).AddItem("a", "A", "").AddItem("b", "B", "", shape: BlockLayoutShape.Circle).AddEdge("a", "b");
        second.Items[0].TextStyle.FontSize = null;
        Assert.Equal(first.RenderSvg("same-default"), second.RenderSvg("same-default")); Assert.Equal(first.ToPng(), second.ToPng());
    }

    internal static BlockLayoutBlock Parsed(int width, bool dark) {
        var parsed = new MermaidParser().ParseBlock(Source); Assert.False(parsed.HasErrors);
        return parsed.Document!.ToBlockLayoutBlock(new MermaidBlockRenderOptions { Width = width, Height = 620, Title = "Source blocks" })
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
    }
    internal static Dictionary<string, XElement> Cells(XDocument document) => document.Descendants()
        .Where(node => Role(node) is "block-layout-composite" or "block-layout-item").ToDictionary(node => node.Attribute("data-block-id")!.Value, StringComparer.Ordinal);
    internal static string? Role(XElement node) => (string?)node.Attribute("data-cfx-role");
    internal static double Number(XElement node, string attribute) => double.Parse(node.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
    internal static void Save(BlockLayoutBlock block, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "block-depth-" + name + ".svg"), block.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, "block-depth-" + name + ".png"), block.ToPng());
    }
}
