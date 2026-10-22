using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyAuthoredPaintTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void AuthoredGroupPaintsSurviveThemeBindingsHighlightAndDetachedPreparation(VisualThemeMode mode) {
        var variable = Diagram(mode, true);
        var literal = Diagram(mode, false);
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeGroupStatusDots = false, DimmedOpacity = .5,
            SvgColorVariables = new SvgColorVariables().AddThemeToken(VisualThemeColorToken.Surface, "--theme-card") };
        options.HighlightNodeIds.Add("focus");
        var prepared = variable.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg());
        var surface = svg.Descendants().Single(item => (string?)item.Attribute("data-cfx-role") == "topology-group-surface");
        Assert.Contains("var(--group-fill,", (string?)surface.Attribute("fill"));
        Assert.Contains("var(--group-border,", (string?)surface.Attribute("stroke"));
        foreach (var role in new[] { "topology-group-label", "topology-group-subtitle" }) {
            var caption = Assert.Single(svg.Descendants(), item => (string?)item.Attribute("data-cfx-role") == role);
            var text = Assert.Single(caption.Descendants(), item => item.Name.LocalName == "text");
            Assert.Equal(role == "topology-group-label" ? "Domain" : "Native paints", text.Value);
            Assert.Contains("var(--group-text,", (string?)text.Attribute("fill"));
            var nativeText = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == role);
            Assert.Equal(64, nativeText.Color.A);
        }
        var native = prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>().Single(item => item.Role == "topology-group-surface");
        Assert.Equal(64, native.Fill!.Value.A);
        Assert.Equal(64, native.Stroke!.Value.A);
        var literalOptions = options.Clone(); literalOptions.SvgColorVariables = null;
        Assert.Equal(literal.ToPng(literalOptions), prepared.ToPng());
        var originalSvg = prepared.ToSvg(); var originalPng = prepared.ToPng();
        var group = variable.Groups[0];
        group.BackgroundColor = "#FF0000"; group.BorderColor = "#00FF00"; group.LabelColor = "#0000FF";
        Assert.Equal(originalSvg, prepared.ToSvg());
        Assert.Equal(originalPng, prepared.ToPng());
        var saved = Assert.Single(prepared.ToInterchangeEnvelope().Groups);
        Assert.Equal(("#a855f7", "var(--group-fill, #bae6fd80)", "var(--group-border, #07598580)", "var(--group-text, #082f4980)"),
            (saved.Color, saved.BackgroundColor, saved.BorderColor, saved.LabelColor));
        saved.BackgroundColor = "#000000";
        Assert.Equal("var(--group-fill, #bae6fd80)", Assert.Single(prepared.ToInterchangeEnvelope().Groups).BackgroundColor);
        var fitted = prepared.WithOutputSize(prepared.Width + 30, prepared.Height + 20);
        Assert.Equal("var(--group-fill, #bae6fd80)", Assert.Single(fitted.ToInterchangeEnvelope().Groups).BackgroundColor);
    }

    [Fact]
    public void CompartmentsDoNotPaintDuplicateSurfacesOrOverrideTheirContainersClearance() {
        var chart = TopologyChart.Create().WithViewport(660, 400, 20).WithLegend(null)
            .AddGroup("container", "Container", 10, 10, 610, 350)
            .AddGroup("region", "Region", 20, 60, 590, 290)
            .AddNode("a", "A", 60, 160, groupId: "region")
            .AddNode("b", "B", 380, 160, groupId: "region")
            .AddEdge("link", "a", "b", "Owns", routing: TopologyEdgeRouting.Straight);
        chart.Groups[0].BackgroundColor = "#bae6fd";
        var compartment = chart.Groups[1];
        compartment.ParentGroupId = "container";
        compartment.Presentation = TopologyGroupPresentation.Compartment;
        compartment.BackgroundColor = "#FF0000";
        var options = new TopologyRenderOptions { IncludeLegend = false, VisualStyle = TopologyVisualStyle.MonitoringDashboard,
            IncludeEdgeLabelBackplates = false };
        var prepared = chart.Prepare(options);
        var surfaces = prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>().Where(item => item.Role == "topology-group-surface").ToArray();
        Assert.Single(surfaces);
        var clearance = prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>().Single(item => item.Role == "topology-edge-label-clearance");
        Assert.Equal(ChartColor.Parse("#bae6fd").R, clearance.Fill!.Value.R);
        Assert.Equal(ChartColor.Parse("#bae6fd").G, clearance.Fill!.Value.G);
        Assert.Equal(ChartColor.Parse("#bae6fd").B, clearance.Fill!.Value.B);
        Assert.Equal("container", (string?)XDocument.Parse(prepared.ToSvg()).Descendants().Single(item => item.Attribute("data-clearance-group-id") != null).Attribute("data-clearance-group-id"));
    }

    [Fact]
    public void EdgeTextPaintIsIndependentOfLinePaintAcrossAllLabelRowsAndEndpoints() {
        var chart = TopologyChart.Create().WithViewport(780, 420, 20).WithLegend(null)
            .AddNode("a", "A", 60, 80).AddNode("b", "B", 500, 80)
            .AddNode("c", "C", 60, 270).AddNode("d", "D", 500, 270)
            .AddEdge("focus", "c", "d", "Focus", routing: TopologyEdgeRouting.Straight)
            .AddEdge("styled", "a", "b", "Primary", routing: TopologyEdgeRouting.Straight, color: "#123456");
        var edge = chart.Edges.Single(item => item.Id == "styled");
        edge.LabelColor = "var(--relationship-text, #7c2d1280)";
        edge.SecondaryLabel = "Secondary"; edge.TertiaryLabel = "Tertiary";
        edge.SourceLabel = "One"; edge.TargetLabel = "Many";
        var options = new TopologyRenderOptions { IncludeLegend = false, DimmedOpacity = .5 };
        options.HighlightEdgeIds.Add("focus");
        var prepared = chart.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg());
        var labels = svg.Descendants().Where(item => item.Name.LocalName == "text" && new[] { "Primary", "Secondary", "Tertiary", "One", "Many" }.Contains(item.Value)).ToArray();
        Assert.Equal(5, labels.Length);
        Assert.All(labels, label => Assert.Contains("var(--relationship-text,", (string?)label.Attribute("fill")));
        Assert.All(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(item => item.Text.Lines.Any(line => new[] { "Primary", "Secondary", "Tertiary", "One", "Many" }.Contains(line.Text))),
            item => Assert.Equal(64, item.Color.A));
        Assert.Equal(("#123456", "var(--relationship-text, #7c2d1280)"), (prepared.ToInterchangeEnvelope().Edges.Single(item => item.Id == "styled").Color,
            prepared.ToInterchangeEnvelope().Edges.Single(item => item.Id == "styled").LabelColor));
        edge.LabelColor = "#7c2d1280";
        Assert.Equal(chart.ToPng(options), prepared.ToPng());
    }

    private static TopologyChart Diagram(VisualThemeMode mode, bool variables) {
        string Paint(string name, string fallback) => variables ? "var(--" + name + ", " + fallback + ")" : fallback;
        var chart = TopologyChart.Create().WithTheme(VisualTheme.Graphite(), mode).WithViewport(780, 380, 20).WithLegend(null)
            .AddGroup("domain", "Domain", 20, 20, 520, 310, subtitle: "Native paints", color: "#a855f7")
            .AddNode("member", "Member", 80, 130, groupId: "domain")
            .AddNode("focus", "Focus", 590, 130);
        chart.Groups[0].BackgroundColor = Paint("group-fill", "#bae6fd80");
        chart.Groups[0].BorderColor = Paint("group-border", "#07598580");
        chart.Groups[0].LabelColor = Paint("group-text", "#082f4980");
        return chart;
    }
}
