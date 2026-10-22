using System.Xml.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Exercises the product-neutral compartment owner independently of any diagram language.</summary>
public sealed class TopologyCompartmentTests {
    [Fact]
    public void NativeCompartmentsPreserveIdentityAndSnapshotPresentationWithoutDuplicateSurfaces() {
        var chart = TopologyChart.Create().WithViewport(800, 500).WithLayout(TopologyLayoutMode.Layered)
            .AddAutoGroup("work", "Work")
            .AddAutoGroup("first", "First")
            .AddAutoGroup("second", "Second")
            .AddAutoNode("a", "First operation", groupId: "first")
            .AddAutoNode("b", "Second operation", groupId: "second");
        foreach (var group in chart.Groups.Where(group => group.Id != "work")) {
            group.ParentGroupId = "work";
            group.Presentation = TopologyGroupPresentation.Compartment;
        }
        var prepared = chart.Prepare();
        var svg = prepared.ToSvg();
        var xml = XDocument.Parse(svg);
        Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-group-surface");
        Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-compartment-separator");
        Assert.Equal(3, xml.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "topology-group"));
        var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(2, envelope.Groups.Count(group => group.Topology!.Presentation == TopologyGroupPresentation.Compartment));
        var json = envelope.ToJson();
        var detached = VisualArtifactInterchangeEnvelope.FromJson(json);
        Assert.Equal(json, detached.ToJson());
        Assert.All(detached.Groups.Where(group => group.Id != "work"), group => Assert.Equal("work", group.Topology!.ParentGroupId));
        chart.Groups[1].Presentation = TopologyGroupPresentation.Container;
        Assert.Equal(json, prepared.ToInterchangeEnvelope().ToJson());
        Assert.Equal(svg, prepared.ToSvg());
        Assert.All(chart.Groups, group => { Assert.Equal(0, group.Width); Assert.Equal(0, group.Height); });
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void CompartmentParentsAndMembershipHaveExplicitNativeValidation() {
        var chart = TopologyChart.Create().WithViewport(600, 400)
            .AddGroup("outside", "Outside", 0, 0, 300, 200)
            .AddGroup("part", "Part", 20, 30, 200, 140);
        chart.Groups[1].Presentation = TopologyGroupPresentation.Compartment;
        Assert.Contains(new TopologyChartValidator().Validate(chart).Errors, issue => issue.Code == "compartment-parent");
        chart.Groups[1].ParentGroupId = "outside";
        chart.AddNode("a", "Unpartitioned node", 50, 70, groupId: "outside");
        Assert.Contains(new TopologyChartValidator().Validate(chart).Errors, issue => issue.Code == "compartment-membership");
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Groups[1].Presentation = (TopologyGroupPresentation)99);
    }
}
