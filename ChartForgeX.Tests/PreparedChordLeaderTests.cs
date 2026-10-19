using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Source-to-caption leaders keep compact chord drawings readable without losing their authored flow facts.</summary>
public sealed class PreparedChordLeaderTests {
    [Theory]
    [InlineData(390, VisualThemeMode.Light, false)]
    [InlineData(390, VisualThemeMode.Dark, false)]
    [InlineData(760, VisualThemeMode.Light, false)]
    [InlineData(760, VisualThemeMode.Dark, false)]
    [InlineData(390, VisualThemeMode.Light, true)]
    [InlineData(390, VisualThemeMode.Dark, true)]
    [InlineData(760, VisualThemeMode.Light, true)]
    [InlineData(760, VisualThemeMode.Dark, true)]
    public void ChordLeadersAvoidEveryOtherCaptionAndKeepTheirNativeSource(int width, VisualThemeMode mode, bool shortLabels) {
        var nodes = Enumerable.Range(0, 8).Select(index => new ChartNode("N" + index,
            shortLabels ? "R" + (index + 1) : "Regional processing and reconciliation " + (index + 1))).ToArray();
        var weights = new double[] { 1, 2, 3, 5, 8, 13, 21, 34 };
        var reverse = new double[] { 3, 2, 1, 4 };
        var flows = Enumerable.Range(0, 8).Select(index => new ChartFlowLink("R" + index, "N" + index, "N" + ((index + 1) % 8), weights[index])).ToList();
        for (var index = 0; index < reverse.Length; index++) flows.Add(new ChartFlowLink("B" + index, "N" + (index + 1), "N" + index, reverse[index]));
        flows.AddRange(new[] {
            new ChartFlowLink("P0", "N0", "N1", 2), new ChartFlowLink("P1", "N0", "N1", 7),
            new ChartFlowLink("S", "N3", "N3", 5), new ChartFlowLink("Z", "N7", "N2", 0)
        });
        var chart = Chart.Create().WithSize(width, 420).WithTheme(VisualTheme.Graphite(), mode)
            .WithTitle("Directed regional transfers").WithLegend(false).WithDataLabels()
            .AddChord("Regional transfers", nodes, flows);
        // Pin the same portable face that exposed the original compact crossing.
        chart.Options.PngFontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(context);
        var captions = NumericRadialAxisTextTests.Footprints(prepared.Scene).Where(item => item.Text.Role == "chord-node-label").ToArray();
        Assert.Equal(nodes.Length, captions.Length);
        foreach (var caption in captions) Assert.True(LabelPlacementService.Contains(new ChartRect(0, 0, width, 420), caption.Bounds));
        var leaders = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "chord-label-leader").ToArray();
        if (width == 390 && !shortLabels) {
            Assert.NotEmpty(leaders);
            var complete = Assert.Single(captions, caption => caption.Text.Id == "series-0-node-label-N4");
            Assert.Equal("Regional processing and reconciliation 5 17", complete.Text.Text.Lines.Single().Text);
        } else if (shortLabels) Assert.Empty(leaders);
        if (width == 760 && !shortLabels) {
            var restored = Assert.Single(captions, caption => caption.Text.Id == "series-0-node-label-N5");
            Assert.Equal("Regional processing and reconciliation 6 21", restored.Text.Text.Lines.Single().Text);
        }
        var renderedNodes = PreparedChordTests.Role(prepared, "chord-node");
        foreach (var leader in leaders) {
            var source = Assert.Single(renderedNodes, node => Same(Anchor(node, context.Theme.Spacing), leader.Start));
            var id = "series-0-node-label-" + (string?)source.Attribute("data-cfx-target-id");
            var own = Assert.Single(captions, caption => caption.Text.Id == id);
            Assert.True(Same(leader.End, new ChartPoint(Math.Max(own.Bounds.Left, Math.Min(own.Bounds.Right, leader.Start.X)),
                Math.Max(own.Bounds.Top, Math.Min(own.Bounds.Bottom, leader.Start.Y)))));
            var stroke = new LabelMarkShape(new[] { new List<ChartPoint> { leader.Start, leader.End } }, false, 1);
            foreach (var caption in captions.Where(caption => caption.Text.Id != id))
                Assert.False(stroke.Intersects(caption.Bounds), id + " leader crosses " + caption.Text.Id);
        }
        var links = PreparedChordTests.Role(prepared, "chord-link");
        Assert.Equal(nodes.Select(node => node.Id), renderedNodes.Select(node => (string?)node.Attribute("data-cfx-target-id")));
        Assert.Equal(flows.Select(flow => flow.Id), links.Select(link => (string?)link.Attribute("data-cfx-target-id")));
        Assert.Equal(flows.Select(flow => flow.Value), links.Select(link => Number(link, "value")));
        Assert.Equal(flows.Select(flow => flow.SourceId), links.Select(link => (string?)link.Attribute("data-cfx-source")));
        Assert.Equal(flows.Select(flow => flow.TargetId), links.Select(link => (string?)link.Attribute("data-cfx-target")));
        foreach (var node in nodes) {
            var semantic = renderedNodes.Single(element => (string?)element.Attribute("data-cfx-target-id") == node.Id);
            Assert.Equal(node.Label, (string?)semantic.Attribute("data-cfx-label"));
            Assert.Contains(prepared.Regions, region => region.Id == "series-0-node-" + node.Id && region.Label!.Contains(node.Label, StringComparison.Ordinal));
        }
        Assert.Equal(15, prepared.Scene.Nodes.Count(node => node.Role == "chord-ribbon"));
        Assert.Equal(15, prepared.Scene.Nodes.Count(node => node.Role == "chord-target-cue"));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Capture(prepared, width + "-" + mode + "-" + (shortLabels ? "short" : "long"));
    }

    private static double Number(XElement element, string name) => double.Parse(element.Attribute("data-cfx-" + name)!.Value, CultureInfo.InvariantCulture);
    private static ChartPoint Anchor(XElement node, double gap) {
        var angle = Number(node, "start-angle") + Number(node, "sweep") / 2;
        var radius = Number(node, "outer-radius") + gap;
        return new ChartPoint(Number(node, "center-x") + Math.Cos(angle) * radius, Number(node, "center-y") + Math.Sin(angle) * radius);
    }
    private static bool Same(ChartPoint first, ChartPoint second) => Math.Abs(first.X - second.X) < .001 && Math.Abs(first.Y - second.Y) < .001;
    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "chord-leader-" + name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, "chord-leader-" + name + ".png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}
