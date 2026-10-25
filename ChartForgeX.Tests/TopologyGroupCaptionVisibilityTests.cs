using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyGroupCaptionVisibilityTests {
    private static TopologyChart Chart(TopologyLayoutMode mode, bool show) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(600, 400, 24).WithLayout(mode, TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("scope", "Line one\nLine two\nLine three", subtitle: "Further detail", iconId: "common:server")
            .AddAutoNode("node", "Visible node", groupId: "scope", width: 144, height: 64);
        chart.Groups[0].ShowLabel = show; chart.Groups[0].LayoutPolicy = TopologyGroupLayoutPolicy.VerticalStack;
        chart.Groups[0].LayoutDirection = TopologyLayoutDirection.LeftToRight;
        chart.Nodes[0].DisplayMode = TopologyNodeDisplayMode.Card; chart.Nodes[0].PreserveDisplayModeSize = true;
        return chart;
    }

    [Theory]
    [InlineData(TopologyLayoutMode.Layered)] [InlineData(TopologyLayoutMode.DenseGrouped)] [InlineData(TopologyLayoutMode.Swimlane)]
    public void NativeVisibilityReleasesMeasuredCaptionsButRetainsGlyphBodyContainmentAndSemanticIdentity(TopologyLayoutMode mode) {
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, AllowMultilineNodeLabels = true };
        var hidden = Chart(mode, false); var prepared = hidden.Prepare(options); var visible = Chart(mode, true).Prepare(options);
        Capture(prepared, "group-caption-hidden-" + mode); Capture(visible, "group-caption-visible-" + mode);
        var roles = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Select(text => text.Role);
        Assert.DoesNotContain("topology-group-label", roles); Assert.DoesNotContain("topology-group-subtitle", roles);
        var h = Assert.Single(prepared.Analyze().Groups).HeaderBounds!.Value;
        var v = Assert.Single(visible.Analyze().Groups).HeaderBounds!.Value; Assert.True(h.Height < v.Height);
        var envelope = prepared.ToInterchangeEnvelope(); var group = Assert.Single(envelope.Groups); var node = Assert.Single(envelope.Nodes);
        Assert.Equal(hidden.Groups[0].Label, group.Label); Assert.False(group.Topology!.ShowLabel); Assert.Equal("common:server", group.Topology!.IconId);
        Assert.True(node.Y!.Value >= group.Y!.Value && node.Y!.Value + node.Height!.Value <= group.Y!.Value + group.Height!.Value);
        var svg = prepared.ToSvg(); Assert.Contains("data-group-icon-shape=\"Server\"", svg);
        var json = envelope.ToJson(); hidden.Groups[0].ShowLabel = true; hidden.Groups[0].Label = "Changed";
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(json, prepared.ToInterchangeEnvelope().ToJson());
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); Assert.False(restored.Groups[0].ShowLabel);
        var restoredRoles = restored.Prepare(options).Visual.Scene.Nodes.OfType<VisualSceneText>().Select(text => text.Role);
        Assert.DoesNotContain("topology-group-label", restoredRoles); Assert.DoesNotContain("topology-group-subtitle", restoredRoles);
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void DefaultVisibleGroupsKeepTheExistingWireShapeAndOmittedFieldReadsAsTrue() {
        var chart = TopologyChart.Create().WithViewport(600, 400).WithLayout(TopologyLayoutMode.DenseGrouped).AddAutoGroup("scope", "Scope").AddAutoNode("node", "Node", groupId: "scope");
        var baseline = chart.Prepare(); var json = baseline.ToInterchangeEnvelope().ToJson();
        Assert.True(chart.Groups[0].ShowLabel); Assert.DoesNotContain("\"showLabel\"", json); Assert.DoesNotContain("data-group-show-label", baseline.ToSvg());
        var oldWire = VisualArtifactInterchangeEnvelope.FromJson(json); Assert.True(oldWire.Groups[0].Topology!.ShowLabel);
        chart.Groups[0].ShowLabel = true; Assert.Equal(baseline.ToSvg(), chart.Prepare().ToSvg()); Assert.Equal(json, chart.Prepare().ToInterchangeEnvelope().ToJson());
    }

    [Fact]
    public void GeographicCalloutCaptionPaintAlsoHonorsTheGroupFlagWhileStatusFactsRemain() {
        var chart = TopologyChart.Create().WithViewport(900, 500).WithLayout(TopologyLayoutMode.Geographic)
            .AddAutoGroup("scope", "Hidden scope", subtitle: "Hidden detail").AddAutoNode("node", "Node", groupId: "scope");
        chart.Groups[0].Longitude = 10; chart.Groups[0].Latitude = 52; chart.Groups[0].ShowLabel = false;
        chart.Nodes[0].Longitude = 10; chart.Nodes[0].Latitude = 52;
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeGeographicCallouts = true, IncludeLegend = false });
        Capture(prepared, "group-caption-hidden-geographic");
        var roles = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Select(text => text.Role).ToArray();
        Assert.DoesNotContain("topology-callout-title", roles); Assert.DoesNotContain("topology-callout-subtitle", roles);
        Assert.Contains("topology-callout-count", roles); Assert.Equal("Hidden scope", prepared.ToInterchangeEnvelope().Groups[0].Label);
    }

    private static void Capture(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), prepared.ToPng()); File.WriteAllText(Path.Combine(output, name + ".native.json"), prepared.ToInterchangeEnvelope().ToJson());
    }
}
