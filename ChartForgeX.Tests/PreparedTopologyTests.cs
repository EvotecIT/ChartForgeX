using ChartForgeX.Topology;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedTopologyTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DecorativeReportsRetainAccessibilityAndUntitledHtmlHasAName(string? title) {
        var chart = TopologyChart.Create().AddAutoNode("a", "Service");
        chart.Title = title;
        chart.Accessibility.IsDecorative = true;
        chart.Accessibility.Language = "pl";
        var report = chart.PrepareReport();
        foreach (var page in report.Pages.Append(report.Overview)) {
            Assert.Contains("aria-hidden=\"true\"", page.ToSvg());
            Assert.True(page.ToInterchangeEnvelope().IsDecorative);
            Assert.Equal("pl", page.ToInterchangeEnvelope().Language);
        }
        string html = report.ToInteractiveHtmlPage();
        Assert.Contains("<title>Topology report</title>", html);
        Assert.Contains("<h1>Topology report</h1>", html);
    }

    [Fact]
    public void PreparedExportsAreDetachedFromSourceAndReturnedEnvelope() {
        var chart = TopologyChart.Create().WithViewport(600, 400).WithTheme(TopologyTheme.Light())
            .AddNode("a", "Original", 80, 100).AddNode("b", "Second", 320, 100)
            .AddEdge("ab", "a", "b");
        var prepared = chart.Prepare();
        var original = prepared.ToSvg();
        chart.Nodes[0].Label = "Changed";
        chart.Theme!.Foreground = "#ff0000";
        var envelope = prepared.ToInterchangeEnvelope();
        envelope.Nodes[0].Label = "External change";
        Assert.Equal(original, prepared.ToSvg());
        Assert.Equal("Original", prepared.ToInterchangeEnvelope().Nodes[0].Label);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public void ReportRetainsEveryNodeAndRelationshipInReadablePages(int count) {
        var chart = TopologyChart.Create().WithViewport(1200, 800).WithLayout(TopologyLayoutMode.Matrix);
        for (int i = 0; i < count; i++) chart.AddAutoNode("n" + i, "Service " + i);
        for (int i = 1; i < count; i++) chart.AddEdge("e" + i, "n" + ((i - 1) / 3), "n" + i);
        var report = chart.PrepareReport();
        Assert.Equal(count, report.Source.NodeCount);
        Assert.Equal(report.Pages.Count, report.Overview.NodeCount);
        Assert.Equal(count, report.NodePages.Count);
        Assert.Equal(count, report.Pages.Sum(page => page.NodeCount));
        Assert.Equal(count - 1, report.Pages.Sum(page => page.ToInterchangeEnvelope().Edges.Count) + report.CrossPageLinks.Count);
        Assert.All(report.Pages, page => Assert.Empty(page.Analyze().Collisions));
        Assert.All(report.CrossPageLinks, link => {
            Assert.Equal(report.NodePages[link.SourceNodeId], link.SourcePage);
            Assert.Equal(report.NodePages[link.TargetNodeId], link.TargetPage);
        });
        Assert.All(chart.Nodes, node => Assert.Null(node.DisplayMode));
    }

    [Fact]
    public void DenseRelationshipsAlsoLimitPageCapacity() {
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Matrix);
        for (int i = 0; i < 20; i++) chart.AddAutoNode("n" + i, "Node " + i);
        for (int i = 0; i < 20; i++) for (int j = i + 1; j < 20; j++) chart.AddEdge("e" + i + "-" + j, "n" + i, "n" + j);
        var report = chart.PrepareReport(new TopologyReportOptions { MaximumEdgesPerPage = 6 });
        Assert.All(report.Pages, page => Assert.InRange(page.ToInterchangeEnvelope().Edges.Count, 0, 6));
        Assert.Equal(190, report.CrossPageLinks.Count + report.Pages.Sum(page => page.ToInterchangeEnvelope().Edges.Count));
    }

    [Fact]
    public void HtmlReportEncodesUserTextOutsideInertTemplates() {
        const string label = "</template><script>alert(1)</script>";
        var report = TopologyChart.Create().WithTitle(label).AddAutoNode("node", label).PrepareReport();
        string html = report.ToInteractiveHtmlPage();
        Assert.DoesNotContain(label, html);
        Assert.Contains("&lt;/template&gt;", html);
        Assert.Contains("page-1", html);
    }

    [Fact]
    public void ReportNavigationUsesOriginalIdsWhenInterchangeRenamesThem() {
        string longId = new string('x', 300);
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Matrix)
            .AddAutoGroup("shared", "Group")
            .AddAutoNode("shared", "Shared node", groupId: "shared")
            .AddAutoNode(longId, "Long ID node")
            .AddEdge("edge", "shared", longId);
        var report = chart.PrepareReport(new TopologyReportOptions { MaximumNodesPerPage = 1 });
        Assert.Equal("Shared node", report.NodeLabels["shared"]);
        Assert.Equal("Long ID node", report.NodeLabels[longId]);
        Assert.DoesNotContain(report.Source.ToInterchangeEnvelope().Nodes, node => node.Id == "shared");
        Assert.Contains("Shared node → Long ID node", report.ToInteractiveHtmlPage());
        Assert.Equal("shared", Assert.Single(report.CrossPageLinks).SourceNodeId);
    }

    [Fact]
    public void ReadabilityExplainsFittingLossAndRejectsInvalidTargets() {
        var prepared = TopologyChart.Create().WithViewport(3000, 800).AddNode("a", "A", 80, 100).Prepare();
        Assert.True(prepared.AssessReadability(600, 400).NeedsDetailViews);
        Assert.Equal(0.2, prepared.AssessReadability(600, 400).FitScale, 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => prepared.AssessReadability(double.NaN, 400));
        Assert.Throws<ArgumentOutOfRangeException>(() => prepared.AssessReadability(600, 400, 2));
    }
}
