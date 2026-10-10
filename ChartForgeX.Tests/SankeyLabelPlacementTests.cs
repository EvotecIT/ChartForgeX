using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SankeyLabelPlacementTests {
    [Theory]
    [InlineData(ChartSankeyLabelPlacement.Left)]
    [InlineData(ChartSankeyLabelPlacement.Right)]
    [InlineData(ChartSankeyLabelPlacement.Center)]
    public void UniformPlacementMovesOnlyMeasuredCaptionsAndPreservesEveryFlowFact(ChartSankeyLabelPlacement placement) {
        var chart = Chain(); var original = Prepare(chart);
        chart.ConfigureSankey(options => { options.LabelPlacement = placement; options.EdgeLabelPlacement = null; });
        var prepared = Prepare(chart);
        Assert.Equal(original.Regions.Select(region => (region.Id, region.Role, region.Bounds, region.Label)),
            prepared.Regions.Select(region => (region.Id, region.Role, region.Bounds, region.Label)));
        var labels = Labels(prepared); Assert.Equal(3, labels.Length);
        foreach (var label in labels) {
            var bounds = Node(prepared, label.Id!.Substring("series-0-node-label-".Length));
            AssertSide(label, bounds, placement);
        }
        Assert.Equal(new[] { "first", "middle", "last" }, chart.Series[0].Nodes.Select(node => node.Id));
        Assert.Equal(new[] { 10d, 10d }, chart.Series[0].FlowLinks.Select(link => link.Value));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(ChartSankeyEdgeLabelPlacement.Inside)]
    [InlineData(ChartSankeyEdgeLabelPlacement.Outside)]
    public void EdgeOverridesKeepTheMiddleCenteredAndKeepOpposingLabelSlotsSeparate(ChartSankeyEdgeLabelPlacement edge) {
        var chart = Chain();
        chart.ConfigureSankey(options => { options.LabelPlacement = ChartSankeyLabelPlacement.Center; options.EdgeLabelPlacement = edge; });
        var prepared = Prepare(chart); var labels = Labels(prepared); Assert.Equal(3, labels.Length);
        AssertSide(labels.Single(label => label.Id!.EndsWith("-first", StringComparison.Ordinal)), Node(prepared, "first"),
            edge == ChartSankeyEdgeLabelPlacement.Inside ? ChartSankeyLabelPlacement.Right : ChartSankeyLabelPlacement.Left);
        AssertSide(labels.Single(label => label.Id!.EndsWith("-last", StringComparison.Ordinal)), Node(prepared, "last"),
            edge == ChartSankeyEdgeLabelPlacement.Inside ? ChartSankeyLabelPlacement.Left : ChartSankeyLabelPlacement.Right);
        AssertSide(labels.Single(label => label.Id!.EndsWith("-middle", StringComparison.Ordinal)), Node(prepared, "middle"), ChartSankeyLabelPlacement.Center);
        AssertSeparateBackdrops(prepared);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DenseInsideCaptionsShortenWithoutOverlapAndKeepFullSemanticsAndAuthoredTypography(bool dark) {
        var name = new string('W', 40);
        var chart = Chart.Create().WithSize(360, 260).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithPngFont(Font()).AddSankey("Flow", new[] { new ChartNode("first", name), new ChartNode("last", name) },
                new[] { new ChartFlowLink("flow", "first", "last", 100) })
            .ConfigureSankey(options => options.EdgeLabelPlacement = ChartSankeyEdgeLabelPlacement.Inside);
        var color = ChartColor.FromHex("#7356BD");
        chart.Series[0].ConfigurePointDataLabelStyle(0, style => { style.Color = color; style.FontSize = 13; style.Underline = true; });
        var prepared = Prepare(chart);
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "sankey-node" && region.Label == name + " 100"));
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "sankey.label-overflow");
        var first = Assert.Single(Labels(prepared), label => label.Id == "series-0-node-label-first");
        Assert.Equal(color, first.Color); Assert.Equal(13, first.Text.Size); Assert.True(first.Text.Style.Underline);
        AssertSeparateBackdrops(prepared);
    }

    [Fact]
    public void PreparedLabelsRemainDetachedAndInvalidEnumsDoNotMutateTheOptions() {
        var chart = Chain(); var prepared = Prepare(chart);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        chart.ConfigureSankey(options => { options.LabelPlacement = ChartSankeyLabelPlacement.Center; options.EdgeLabelPlacement = null; });
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Sankey.LabelPlacement = (ChartSankeyLabelPlacement)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Sankey.EdgeLabelPlacement = (ChartSankeyEdgeLabelPlacement)99);
        Assert.Equal(ChartSankeyLabelPlacement.Center, chart.Options.Sankey.LabelPlacement); Assert.Null(chart.Options.Sankey.EdgeLabelPlacement);
        Assert.Equal(ChartSankeyLabelPlacement.Right, new ChartSankeyOptions().LabelPlacement);
        Assert.Equal(ChartSankeyEdgeLabelPlacement.Outside, new ChartSankeyOptions().EdgeLabelPlacement);
    }

    private static void AssertSide(VisualSceneText label, ChartRect node, ChartSankeyLabelPlacement placement) {
        var expected = placement == ChartSankeyLabelPlacement.Left ? TextAlignment.Right : placement == ChartSankeyLabelPlacement.Center ? TextAlignment.Center : TextAlignment.Left;
        Assert.Equal(expected, label.Alignment);
        if (placement == ChartSankeyLabelPlacement.Left) Assert.True(label.X < node.Left);
        else if (placement == ChartSankeyLabelPlacement.Right) Assert.True(label.X > node.Right);
        else Assert.Equal(node.X + node.Width / 2, label.X, 8);
    }
    private static void AssertSeparateBackdrops(PreparedVisual prepared) {
        var boxes = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "sankey-label-backdrop").Select(node => node.Bounds).ToArray();
        Assert.NotEmpty(boxes);
        for (int i = 0; i < boxes.Length; i++) for (int j = i + 1; j < boxes.Length; j++)
            Assert.False(boxes[i].Left < boxes[j].Right && boxes[i].Right > boxes[j].Left && boxes[i].Top < boxes[j].Bottom && boxes[i].Bottom > boxes[j].Top);
    }
    private static Chart Chain() => Chart.Create().WithSize(900, 420).WithPngFont(Font()).AddSankey("Flow",
        new[] { new ChartNode("first", "First"), new ChartNode("middle", "Middle"), new ChartNode("last", "Last") },
        new[] { new ChartFlowLink("a", "first", "middle", 10), new ChartFlowLink("b", "middle", "last", 10) });
    private static string Font() => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Labels(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "sankey-node-label").ToArray();
    private static ChartRect Node(PreparedVisual prepared, string id) => prepared.Regions.Single(region => region.Id == "series-0-node-" + id).Bounds;
}
