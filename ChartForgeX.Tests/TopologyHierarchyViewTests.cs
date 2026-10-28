using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyHierarchyViewTests {
    [Fact]
    public void CollapseRetainsRawContributorsWithoutChangingBadgeOrSource() {
        var chart = Source(); var badge = chart.Nodes.Single(node => node.Id == "engineering"); badge.Badge = "Owner";
        badge.Details.Add(new TopologyNodeDetail { Label = "Authored", Value = "Retained" });
        var requested = new List<string> { "engineering", "platform" };
        var view = new TopologyHierarchyView(requested); requested.Clear();
        var prepared = chart.Prepare(Options(view)); var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(6, chart.Nodes.Count); Assert.Equal(4, prepared.NodeCount); Assert.Equal(6, prepared.SourceNodeCount);
        Assert.Equal(new[] { "engineering", "platform" }, view.CollapsedNodeIds);
        var collapsed = envelope.Nodes.Single(node => node.Id == "engineering");
        Assert.Equal("Owner", collapsed.Badge); Assert.Contains(collapsed.Details, detail => detail.Label == "Authored" && detail.Value == "Retained");
        Assert.Single(collapsed.Details, detail => detail.Extensions.TryGetValue("hierarchy.summary", out var value) && value == "descendants");
        Assert.Equal("2", collapsed.Extensions["hierarchy.hiddenCount"]);
        Assert.DoesNotContain(envelope.Nodes, node => node.Id is "platform" or "research");
        Assert.DoesNotContain(envelope.Edges, edge => edge.SourceId is "platform" or "research" || edge.TargetId is "platform" or "research");
        Assert.Equal("Owner", badge.Badge); Assert.Single(badge.Details); Assert.False(badge.Metadata.ContainsKey("hierarchy.collapsed"));
        Assert.Equal(6, envelope.Topology!.Hierarchy!.Source.Nodes.Count);
        Assert.Equal(2, envelope.Topology.Hierarchy.Source.Annotations.Count);
        Assert.Single(envelope.Topology.Hierarchy.Source.Scenarios);
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.TopToBottom)]
    [InlineData(TopologyLayoutDirection.LeftToRight)]
    [InlineData(TopologyLayoutDirection.BottomToTop)]
    [InlineData(TopologyLayoutDirection.RightToLeft)]
    public void ExplicitOrderUsesNativeDirectionAndRetainsParentOrder(TopologyLayoutDirection direction) {
        var chart = Source(); var original = chart.LayoutDirection;
        var view = new TopologyHierarchyView(nodeOrder: new[] { "sales", "engineering", "research", "platform" }, layoutDirection: direction);
        var prepared = chart.Prepare(Options(view)); var graph = prepared.ToInterchangeEnvelope();
        Assert.Equal(original, chart.LayoutDirection); Assert.Equal(direction, graph.Topology!.LayoutDirection);
        Assert.Equal(original, graph.Topology.Hierarchy!.Source.Topology!.LayoutDirection);
        var sales = graph.Nodes.Single(node => node.Id == "sales"); var engineering = graph.Nodes.Single(node => node.Id == "engineering");
        var horizontal = direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        Assert.True(horizontal ? sales.Y < engineering.Y : sales.X < engineering.X);
        var platform = graph.Nodes.Single(node => node.Id == "platform"); var research = graph.Nodes.Single(node => node.Id == "research");
        Assert.True(horizontal ? research.Y < platform.Y : research.X < platform.X);
        Assert.Equal(5, graph.Edges.Count); Assert.Equal(6, graph.Nodes.Count);
    }

    [Fact]
    public void EmptyRequestKeepsOrdinaryPaintAndOmittedWireBytes() {
        var chart = Source();
        var ordinary = chart.Prepare(Options(null)); var empty = chart.Prepare(Options(new TopologyHierarchyView()));
        Assert.Equal(ordinary.ToSvg(), empty.ToSvg()); Assert.Equal(ordinary.ToPng(), empty.ToPng());
        var noView = ordinary.ToInterchangeEnvelope(); Assert.Null(noView.Topology!.Hierarchy);
        Assert.DoesNotContain("\"hierarchy\"", noView.ToJson(), StringComparison.Ordinal);
        Assert.Equal(noView.ToJson(), VisualArtifactInterchangeEnvelope.FromJson(noView.ToJson()).ToJson());
        Assert.Equal(6, empty.SourceNodeCount);
    }

    [Fact]
    public void PortableSourceRestoresRawAutomaticStateAndDeepDetachedFacts() {
        var chart = Source(); var root = chart.Nodes[0]; var leaf = chart.Nodes.Single(node => node.Id == "research");
        root.X = 0; root.Y = 7; root.PreserveDisplayModeSize = true; root.Width = 177; root.Height = 71; root.CssClass = "authored-root";
        leaf.DisplayMode = null; leaf.ShowStatusBadge = true; leaf.Artwork = TopologyIconArtwork.Portrait(new ChartForgeX.Raster.RgbaImage(1, 1, new byte[] { 4, 5, 6, 255 }));
        var originalMedia = leaf.Artwork.ImageHref;
        leaf.Details.Add(new TopologyNodeDetail { Label = "Raw", Value = "Complete" }); leaf.Details[0].Metadata["fact"] = "original";
        var prepared = chart.Prepare(Options(new TopologyHierarchyView(new[] { "engineering" })));
        var frozen = prepared.ToInterchangeEnvelope().ToJson();
        chart.Nodes.Clear(); leaf.Details[0].Value = "changed"; leaf.Artwork.ImageHref = "changed"; chart.Scenarios[0].Steps.Clear(); chart.Annotations.Clear();
        Assert.Equal(frozen, prepared.ToInterchangeEnvelope().ToJson());
        var imported = VisualArtifactInterchangeEnvelope.FromJson(frozen); var restored = imported.ToTopologyHierarchySource();
        Assert.Equal(TopologyLayoutMode.Layered, restored.LayoutMode); Assert.Equal(6, restored.Nodes.Count); Assert.Equal(5, restored.Edges.Count);
        Assert.Equal(19, restored.Viewport.Padding); Assert.Equal(2, restored.Annotations.Count); Assert.Single(restored.Scenarios[0].Steps);
        var restoredRoot = restored.Nodes.Single(node => node.Id == "company"); Assert.True(restoredRoot.HasPositionOverride);
        Assert.Equal(0, restoredRoot.X); Assert.Equal(7, restoredRoot.Y); Assert.Equal(177, restoredRoot.Width); Assert.True(restoredRoot.PreserveDisplayModeSize);
        Assert.Equal("authored-root", restoredRoot.CssClass);
        var restoredLeaf = restored.Nodes.Single(node => node.Id == "research"); Assert.False(restoredLeaf.HasPositionOverride);
        Assert.False(restoredLeaf.PreserveDisplayModeSize); Assert.Null(restoredLeaf.DisplayMode); Assert.True(restoredLeaf.ShowStatusBadge);
        Assert.Equal("Complete", restoredLeaf.Details[0].Value); Assert.Equal("original", restoredLeaf.Details[0].Metadata["fact"]);
        Assert.Equal(originalMedia, restoredLeaf.Artwork!.ImageHref);
        var refill = restored.Prepare(Options(new TopologyHierarchyView())); Assert.Equal(6, refill.NodeCount);
        var projected = imported.ToTopologyChart(); Assert.Equal(TopologyLayoutMode.Manual, projected.LayoutMode); Assert.Equal(4, projected.Nodes.Count);
        restoredLeaf.Details[0].Value = "new edit"; Assert.Equal(frozen, imported.ToJson());
    }

    [Fact]
    public void PortableSourcePreservesCollidingGroupAndNodeIdentityAndRepeatedEdgeIdentity() {
        var chart = Source(); chart.Groups.Add(new TopologyGroup { Id = "company", Label = "Authored group" });
        chart.Nodes[0].GroupId = "company";
        chart.Edges[1].Id = chart.Edges[0].Id;
        chart.Scenarios[0].Steps.Clear(); chart.Scenarios[0].Steps.Add(new TopologyScenarioStep { Kind = TopologyScenarioStepKind.Edge, Id = chart.Edges[0].Id });
        var envelope = chart.Prepare(Options(new TopologyHierarchyView())).ToInterchangeEnvelope();
        var hierarchy = envelope.Topology!.Hierarchy!;
        Assert.NotEqual(hierarchy.SourceNodes[0].Id, hierarchy.SourceGroups[0].Id);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyHierarchySource();
        Assert.Equal("company", restored.Nodes[0].Id); Assert.Equal("company", restored.Groups[0].Id); Assert.Equal("company", restored.Nodes[0].GroupId);
        Assert.Equal(0, restored.Groups[0].Width); Assert.False(restored.Groups[0].HasPositionOverride);
        Assert.Equal(restored.Edges[0].Id, restored.Edges[1].Id); Assert.Single(restored.Scenarios[0].Steps);
        Assert.Equal(restored.Edges[0].Id, restored.Scenarios[0].Steps[0].Id);
        Assert.Equal("engineering", restored.Nodes.Single(node => node.Id == "platform").Metadata["hierarchy.parentId"]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 180)]
    [InlineData(280, 0)]
    public void AutomaticGroupDimensionsResolveOnlyOnTheDetachedVisibleLayout(double width, double height) {
        var chart = Source();
        var group = new TopologyGroup { Id = "authored-region", Label = "Authored region", Width = width, Height = height };
        chart.Groups.Add(group);
        foreach (var node in chart.Nodes) node.GroupId = group.Id;
        var strictRaw = new TopologyChartValidator().Validate(chart);
        if (width == 0) Assert.Contains(strictRaw.Errors, error => error.Code == "group-width");
        if (height == 0) Assert.Contains(strictRaw.Errors, error => error.Code == "group-height");
        var prepared = chart.Prepare(Options(new TopologyHierarchyView(new[] { "engineering" })));
        var envelope = prepared.ToInterchangeEnvelope(); var visibleGroup = Assert.Single(envelope.Groups);
        Assert.True(visibleGroup.Width > 0); Assert.True(visibleGroup.Height > 0);
        Assert.Equal(4, prepared.NodeCount); Assert.Equal(6, prepared.SourceNodeCount);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyHierarchySource();
        var rawGroup = Assert.Single(restored.Groups);
        Assert.Equal(width, rawGroup.Width); Assert.Equal(height, rawGroup.Height); Assert.False(rawGroup.HasPositionOverride);
        Assert.Equal(width, group.Width); Assert.Equal(height, group.Height); Assert.False(group.HasPositionOverride);
        Assert.Equal(6, restored.Nodes.Count); Assert.All(restored.Nodes, node => Assert.Equal(group.Id, node.GroupId));
        Assert.Equal(6, restored.Prepare(Options(new TopologyHierarchyView())).NodeCount);
        Assert.NotEmpty(prepared.ToSvg()); Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void AutomaticGroupAdmissionRetainsOtherRawValidationAndModelGuards() {
        var chart = Source(); chart.Groups.Add(new TopologyGroup { Id = "invalid-group", Label = "" });
        chart.Nodes[0].GroupId = "invalid-group";
        var failure = Assert.Throws<TopologyValidationException>(() => chart.Prepare(Options(new TopologyHierarchyView())));
        Assert.Contains(failure.Result.Errors, error => error.Code == "group-label-empty");
        Assert.Equal(6, chart.Nodes.Count); Assert.Equal(0, chart.Groups[0].Width); Assert.Equal(0, chart.Groups[0].Height);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TopologyGroup { Width = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new TopologyGroup { Height = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new TopologyGroup { Width = double.NaN });
        Assert.Throws<ArgumentOutOfRangeException>(() => new TopologyGroup { Height = double.PositiveInfinity });
    }

    [Fact]
    public void SourceBeginsAtExistingLevelFilterBoundaryWithoutInventingRemovedAncestors() {
        var chart = TopologyChart.Create().AddHierarchy(new[] { new TopologyHierarchyItem("outside", "Outside"),
            new TopologyHierarchyItem("retained", "Retained", "outside"), new TopologyHierarchyItem("child", "Child", "retained") },
            new TopologyHierarchyOptions { MinLevel = 1, IncludeAncestorContext = false });
        var prepared = chart.Prepare(Options(new TopologyHierarchyView(new[] { "retained" })));
        Assert.Equal(1, prepared.NodeCount); Assert.Equal(2, prepared.SourceNodeCount);
        var restored = prepared.ToInterchangeEnvelope().ToTopologyHierarchySource();
        Assert.Equal(2, restored.Nodes.Count); Assert.DoesNotContain(restored.Nodes, node => node.Id == "outside");
        Assert.Equal("outside", restored.Nodes.Single(node => node.Id == "retained").Metadata["hierarchy.parentId"]);
    }

    [Fact]
    public void MalformedRequestRejectsBeforeChangingSource() {
        var chart = Source(); var before = chart.Prepare().ToSvg();
        Assert.Throws<ArgumentException>(() => new TopologyHierarchyView(new[] { "engineering", "engineering" }));
        Assert.Throws<ArgumentException>(() => new TopologyHierarchyView(nodeOrder: new[] { " " }));
        Assert.Throws<ArgumentException>(() => chart.Prepare(Options(new TopologyHierarchyView(new[] { "absent" }))));
        Assert.Throws<ArgumentException>(() => chart.Prepare(Options(new TopologyHierarchyView(nodeOrder: new[] { "absent" }))));
        Assert.Equal(before, chart.Prepare().ToSvg()); Assert.Equal(6, chart.Nodes.Count);
        chart.Nodes[0].Metadata["hierarchy.parentId"] = "research";
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(Options(new TopologyHierarchyView())));
    }

    [Fact]
    public void NestedAndCyclicPortableSourcesRejectBeforeSnapshotOrSerialization() {
        var envelope = Source().Prepare(Options(new TopologyHierarchyView())).ToInterchangeEnvelope();
        var hierarchy = envelope.Topology!.Hierarchy!; hierarchy.Source = envelope;
        Assert.Throws<ArgumentException>(() => envelope.Validate()); Assert.Throws<ArgumentException>(() => envelope.ToJson());
        Assert.Throws<ArgumentException>(() => VisualArtifactInterchangeSnapshot.Capture(envelope));
        hierarchy.Source = Source().Prepare(Options(new TopologyHierarchyView())).ToInterchangeEnvelope();
        Assert.Throws<ArgumentException>(() => envelope.Validate()); Assert.Throws<ArgumentException>(() => envelope.ToJson());
    }

    [Fact]
    public void ParentAndRawSourceShareOnePortableValueBudget() {
        var envelope = Source().Prepare(Options(new TopologyHierarchyView())).ToInterchangeEnvelope();
        // Each graph is below the existing budget on its own; the retained pair must not reset it.
        var source = envelope.Topology!.Hierarchy!.Source;
        AddBudgetDetails(envelope.Nodes[0], 800); AddBudgetDetails(source.Nodes[0], 800);
        Assert.True(VisualArtifactInterchangeJson.CountValues(envelope) > VisualArtifactInterchangeEnvelope.MaximumJsonValues);
        Assert.Throws<ArgumentOutOfRangeException>(() => envelope.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => envelope.ToJson());
    }

    private static void AddBudgetDetails(VisualArtifactInterchangeNode node, int count) {
        for (var index = 0; index < count; index++) {
            var detail = new VisualArtifactInterchangeDetail { Label = "Budget", Value = "Retained" };
            for (var extension = 0; extension < 400; extension++) detail.Extensions.Add("k" + extension, "v"); node.Details.Add(detail);
        }
    }
    private static TopologyRenderOptions Options(TopologyHierarchyView? view) => new() { IncludeLegend = false, HierarchyView = view };
    private static TopologyChart Source() {
        var chart = TopologyChart.Create().WithId("organization-source").WithViewport(920, 620, 19).WithLegend(null).AddHierarchy(new[] {
            new TopologyHierarchyItem("company", "Organization"), new TopologyHierarchyItem("engineering", "Engineering", "company"),
            new TopologyHierarchyItem("sales", "Sales", "company"), new TopologyHierarchyItem("platform", "Platform", "engineering"),
            new TopologyHierarchyItem("research", "Research", "engineering"), new TopologyHierarchyItem("field", "Field", "sales")
        }, new TopologyHierarchyOptions { NodeDisplayMode = TopologyNodeDisplayMode.Card, LayoutPolicy = TopologyHierarchyLayoutPolicy.Standard });
        var scenario = new TopologyScenario { Id = "retained-path", Label = "Retained path" };
        scenario.Steps.Add(new TopologyScenarioStep { Kind = TopologyScenarioStepKind.Node, Id = "research" }); chart.Scenarios.Add(scenario);
        chart.Annotations.Add(new TopologyAnnotation("retained-note", "Complete source note", "research"));
        chart.Annotations.Add(new TopologyAnnotation("visible-note", "Visible source note", "sales")); return chart;
    }
}
