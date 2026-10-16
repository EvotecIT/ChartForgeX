using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyOptionalCaptionFieldsTests {
    public static IEnumerable<object[]> BlankFields() {
        foreach (var field in new[] { "icon", "symbol", "subtitle" })
            foreach (var value in new[] { "", " \t" })
                foreach (var shaped in new[] { false, true }) yield return new object[] { field, value, shaped };
    }

    [Theory]
    [MemberData(nameof(BlankFields))]
    public void BlankOptionalFieldsPreserveLabelOnlyContentFitting(string field, string value, bool shaped) {
        const string label = "Page the service owner";
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(700, 250)),
            VisualTheme.Graphite().WithTypography(new VisualTypography(dataLabelSize: 18)),
            frame: new VisualFrame(showLegend: false), font: FontSpec.FromFamily("CFX Unresolvable Optional Caption Font"));
        var options = new TopologyRenderOptions { IncludeLegend = false, MaxNodeLabelLines = 1 };
        TopologyChart Chart(bool blank) {
            var chart = TopologyChart.Create().WithViewport(700, 250);
            chart.Nodes.Add(new TopologyNode {
                Id = "owner", Label = label, X = 30, Y = 30, Width = 70, Height = 24,
                Shape = shaped ? TopologyNodeShape.Rectangle : null,
                FitLabelToContent = true, PreserveDisplayModeSize = true, ShowStatusBadge = false,
                IconId = blank && field == "icon" ? value : null,
                Symbol = blank && field == "symbol" ? value : null,
                Subtitle = blank && field == "subtitle" ? value : null
            });
            return chart;
        }
        var source = Chart(true);
        var visual = source.Prepare(context, options);
        var control = Chart(false).Prepare(context, options);
        Capture(visual, field + "-" + (value.Length == 0 ? "empty" : "whitespace") + "-" + (shaped ? "shape" : "card"));
        var text = Assert.Single(visual.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-node-label");
        Assert.Equal(label, Assert.Single(text.Text.Lines).Text);
        Assert.Equal(18, text.Text.Size);
        Assert.DoesNotContain(visual.Diagnostics, diagnostic => diagnostic.Code.StartsWith("topology.label-", StringComparison.Ordinal));
        Assert.DoesNotContain(visual.Scene.Nodes, node => node.Role == "topology-node-subtitle" || node.Role == "topology-node-symbol");
        var fitted = Assert.Single(visual.ToArtifact().ToInterchangeEnvelope().Nodes);
        var expected = Assert.Single(control.ToArtifact().ToInterchangeEnvelope().Nodes);
        Assert.True(fitted.Width > 70);
        Assert.Equal(expected.X, fitted.X); Assert.Equal(expected.Y, fitted.Y);
        Assert.Equal(expected.Width, fitted.Width); Assert.Equal(expected.Height, fitted.Height);
        Assert.Equal(70, source.Nodes[0].Width); Assert.Equal(24, source.Nodes[0].Height);
        var svg = XDocument.Parse(visual.ToSvg());
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == label);
        Assert.Equal(control.ToPng(), visual.ToPng());
        var image = PngReader.Decode(visual.ToPng());
        Assert.Equal(700, image.Width); Assert.Equal(250, image.Height);
        // Strict icon resolution must use the same absence rule as fitting and painting.
        options.RequireResolvedIcons = true;
        Assert.Equal(visual.ToPng(), source.Prepare(context, options).ToPng());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t")]
    public void StrictIconResolutionTreatsBlankNodeGroupAndLegendIconsAsAbsent(string blank) {
        TopologyChart Chart(string? icon) {
            var chart = TopologyChart.Create().WithViewport(700, 350);
            chart.Groups.Add(new TopologyGroup { Id = "service", Label = "Service", X = 30, Y = 30, Width = 500, Height = 180, IconId = icon });
            chart.Nodes.Add(new TopologyNode { Id = "owner", Label = "Owner", GroupId = "service", X = 70, Y = 100, Width = 130, Height = 50, IconId = icon });
            chart.Legend = TopologyLegend.Create().AddNodeKind("Owner", TopologyNodeKind.Generic, iconId: icon);
            return chart;
        }
        var options = new TopologyRenderOptions { RequireResolvedIcons = true };
        var control = Chart(null).Prepare(options);
        var actual = Chart(blank).Prepare(options);
        Capture(actual.Visual, "strict-icons-" + (blank.Length == 0 ? "empty" : "whitespace"));
        var svg = XDocument.Parse(actual.ToSvg());
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == "Owner");
        Assert.Equal(control.ToPng(), actual.ToPng());
    }

    private static void Capture(PreparedVisual visual, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "optional-caption-" + name + ".svg"), visual.ToSvg());
        File.WriteAllBytes(Path.Combine(output, "optional-caption-" + name + ".native.png"), visual.ToPng());
    }
}
