using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SankeyVisualDefaultsTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void DefaultNodesAreNarrowNeutralBarsWhileRibbonsRetainSourceColors(VisualThemeMode mode) {
        var chart = Flow();
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(720, 420)), themeMode: mode,
            frame: new VisualFrame(showLegend: false));
        var colors = context.Theme.Resolve(mode);
        var prepared = chart.Prepare(context);
        var nodes = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "sankey-node-mark").ToArray();
        Assert.Equal(3, nodes.Length);
        Assert.All(nodes, node => { Assert.Equal(10, node.Bounds.Width); Assert.Equal(colors.Status.Neutral.Fill, node.Fill); });
        var ribbons = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "sankey-ribbon").ToArray();
        Assert.Equal(ChartColorMath.WithOpacity(colors.Palette[0], .35), ribbons[0].Fill);
        Assert.Equal(ChartColorMath.WithOpacity(colors.Palette[2 % colors.Palette.Count], .35), ribbons[1].Fill);
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "sankey-link"));
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "sankey-node"));
    }

    [Fact]
    public void AuthoredNodeColorsAndStatesRemainIndependentOfNeutralDefaults() {
        var chart = Flow();
        var authored = ChartColor.FromHex("#7D3F98");
        chart.Series[0].WithPointColor(0, authored);
        chart.Series[0].WithNodeState(chart.Series[0].Nodes[1].Id, ChartSeriesState.Warning);
        var request = VisualExportRequest.ForChart(chart);
        var prepared = chart.Prepare(request.Context);
        var nodes = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "sankey-node-mark").ToArray();
        Assert.Equal(authored, nodes[0].Fill);
        Assert.Equal(request.Context.Theme.Resolve(request.Context.ThemeMode).Status.Medium.Fill, nodes[1].Fill);
        var ribbons = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "sankey-ribbon").ToArray();
        Assert.Equal(ChartColorMath.WithOpacity(authored, .35), ribbons[0].Fill);
        chart.Series[0].Color = authored;
        Assert.All(chart.Prepare(request.Context).Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "sankey-node-mark"),
            node => Assert.Equal(authored, node.Fill));
    }

    [Fact]
    public void LabeledFlowOmitsTheRedundantSeriesLegendUnlessExplicitlyRequested() {
        var chart = Flow();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "legend-entry");
        Assert.Equal(3, prepared.Scene.Nodes.Count(node => node.Role == "sankey-node-label"));
        chart.WithLegend(true);
        prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Single(prepared.Scene.Nodes, node => node.Role == "legend-entry");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompactNodeCaptionsWrapNamesAndValuesWithoutDiscardingAuthoredTextStyle(bool dark) {
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font), "The existing Carlito example fixture must be available.");
        var chart = Chart.Create().WithSize(360, 360).WithPngFont(font)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Request routing").WithSubtitle("Observed weighted flows")
            .AddSankey("Requests", new[] { new ChartNode("Received", "Received"), new ChartNode("Automatic", "Automatic"), new ChartNode("Manual", "Manual"), new ChartNode("Completed", "Completed"), new ChartNode("Review", "Review") }, new[] {
                new ChartFlowLink("flow-1", "Received", "Automatic", 72), new ChartFlowLink("flow-2", "Received", "Manual", 28),
                new ChartFlowLink("flow-3", "Automatic", "Completed", 65), new ChartFlowLink("flow-4", "Automatic", "Review", 7),
                new ChartFlowLink("flow-5", "Manual", "Completed", 20), new ChartFlowLink("flow-6", "Manual", "Review", 8)
            });
        var authored = ChartColor.FromHex("#7D3F98");
        chart.Series[0].WithPointDataLabelStyle(0, style => { style.Color = authored; style.FontSize = 13; style.Underline = true; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "sankey-node-label").ToArray();
        Assert.Equal(new[] { "Received 100", "Automatic 72", "Manual 28", "Completed 85", "Review 15" },
            labels.OrderBy(label => Array.FindIndex(chart.Series[0].Nodes.ToArray(), node => label.Id == "series-0-node-label-" + node.Id)).Select(label => string.Join(" ", label.Text.Lines.Select(line => line.Text))));
        var source = Assert.Single(labels, label => label.Id == "series-0-node-label-Received");
        Assert.Equal(2, source.Text.Lines.Count); Assert.Equal(13, source.Text.Size); Assert.Equal(authored, source.Color);
        Assert.True(source.Text.Style.Underline); Assert.Equal(TextAlignment.Right, source.Alignment);
        Assert.All(labels.Where(label => label != source), label => { Assert.Equal(12, label.Text.Size); Assert.Equal(TextAlignment.Left, label.Alignment); });
        Assert.Equal(5, prepared.Regions.Count(region => region.Role == "sankey-node"));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "sankey.label-overflow");
        var backgrounds = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(rect => rect.Role == "sankey-label-backdrop").ToArray();
        Assert.Equal(labels.Length, backgrounds.Length);
        Assert.All(backgrounds, background => {
            Assert.InRange(background.Bounds.Left, chart.Options.Padding.Left, prepared.Size.Width - chart.Options.Padding.Right);
            Assert.InRange(background.Bounds.Right, chart.Options.Padding.Left, prepared.Size.Width - chart.Options.Padding.Right);
        });
    }

    [Fact]
    public void UnbreakableDenseCaptionsRetainCompleteNodeSemanticsAndReportTheirFittingLimit() {
        var name = new string('W', 60);
        var chart = Chart.Create().WithSize(360, 240).AddSankey("Requests", new[] { new ChartNode(name, name), new ChartNode("Completed", "Completed") }, new[] { new ChartFlowLink("flow-7", name, "Completed", 100) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Contains(prepared.Regions, region => region.Role == "sankey-node" && region.Label == name + " 100");
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "sankey.label-overflow");
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "sankey-node-label"),
            text => Assert.Equal(12, text.Text.Size));
    }

    private static Chart Flow() => Chart.Create().AddSankey("Requests", new[] { new ChartNode("Automatic", "Automatic"), new ChartNode("Completed", "Completed"), new ChartNode("Manual", "Manual") }, new[] {
        new ChartFlowLink("flow-8", "Automatic", "Completed", 70), new ChartFlowLink("flow-9", "Manual", "Completed", 30)
    });
}
