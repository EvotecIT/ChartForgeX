using ChartForgeX.Topology;
using ChartForgeX.Interactivity.Html;
using Xunit;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

public sealed class PreparedTopologyTests {
    private static string[][] ReportRecords(string html, string id) {
        string marker = "<script type=\"application/json\" id=\"" + id + "\">";
        int start = html.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        int end = html.IndexOf("</script>", start, StringComparison.Ordinal);
        return System.Text.Json.JsonSerializer.Deserialize<string[][]>(html.Substring(start, end - start))!;
    }

    [Fact]
    public void ReportRetainsDeclaredGroupOrderAndNodeOrderWithinGroups() {
        var chart = TopologyChart.Create().AddAutoGroup("z-first", "First group").AddAutoGroup("a-second", "Second group")
            .AddAutoNode("second", "Second", groupId: "a-second")
            .AddAutoNode("first-b", "B", groupId: "z-first")
            .AddAutoNode("ungrouped", "Ungrouped")
            .AddAutoNode("first-a", "A", groupId: "z-first");
        var report = chart.PrepareReport(new TopologyReportOptions { MaximumNodesPerPage = 1 });
        Assert.Equal(new[] { "ungrouped", "first-b", "first-a", "second" }, report.Pages.Select(page => Assert.Single(page.ToInterchangeEnvelope().Nodes).Id));
    }

    [Fact]
    public void ReportSearchRecordsFollowStableDetailPageOrder() {
        var chart = TopologyChart.Create().AddAutoGroup("z-first", "First group").AddAutoGroup("a-second", "Second group");
        for (var index = 0; index < 22; index++) {
            string groupId = index % 2 == 0 ? "a-second" : "z-first";
            chart.AddAutoNode("node-" + index.ToString("D2"), "Node " + index, groupId: groupId);
        }
        chart.AddAutoNode("ungrouped", "Ungrouped");

        var report = chart.PrepareReport(new TopologyReportOptions { MaximumNodesPerPage = 1 });
        string[][] records = ReportRecords(report.ToInteractiveHtmlPage(), "objects");

        Assert.Equal(report.Pages.Select(page => Assert.Single(page.ToInterchangeEnvelope().Nodes).Id), records.Select(record => record[5]));
    }

    [Fact]
    public void ReportNavigationDataRoundTripsHtmlSensitiveLabels() {
        const string label = "</script><script>alert(1)</script>\n\"&";
        string html = TopologyChart.Create().AddAutoNode("node", label).PrepareReport().ToInteractiveHtmlPage();
        Assert.Equal(label + " — node", Assert.Single(ReportRecords(html, "objects"))[1]);
        Assert.DoesNotContain(label, html);
    }

    [Fact]
    public void ReportSearchKeepsStableIdsIndependentOfTheLabelLocale() {
        var chart = TopologyChart.Create().AddAutoNode("SERVICE-ID", "HİZMET");
        chart.Accessibility.Language = "tr";

        string html = chart.PrepareReport().ToInteractiveHtmlPage();
        var record = Assert.Single(ReportRecords(html, "objects"));

        Assert.Equal("tr", record[3]);
        Assert.Equal("HİZMET", record[4]);
        Assert.Equal("SERVICE-ID", record[5]);
        Assert.Contains("normalize(entry[4], entry[3])", html);
        Assert.Contains("normalizeIdentifier(entry[5])", html);
    }

    [Fact]
    public void OverviewCardsFollowNumericPageOrder() {
        var chart = TopologyChart.Create();
        for (int i = 0; i < 12; i++) chart.AddAutoNode("n" + i, "Node " + i);
        var overview = chart.PrepareReport(new TopologyReportOptions { MaximumNodesPerPage = 1 }).Overview.ToInterchangeEnvelope();
        var labels = overview.Nodes.OrderBy(node => node.Y).ThenBy(node => node.X).Select(node => node.Label);
        Assert.Equal(Enumerable.Range(1, 12).Select(number => "Page " + number), labels);
    }

    [Fact]
    public void ReportPreservesAndSnapshotsCustomIconCatalog() {
        var icon = new TopologyIconDefinition("vendor", "service", "Service", TopologyNodeKind.Service)
            .WithArtwork(TopologyIconArtwork.InlineSvg("<path d='M0 0h24v24H0z'/>", "0 0 24 24"));
        var catalog = new TopologyIconCatalog().AddPack(new TopologyIconPack("vendor", "Vendor").AddIcon(icon));
        var chart = TopologyChart.Create().AddIconNode("a", "Service", "vendor:service", 100, 100, catalog: catalog);
        var report = chart.PrepareReport(new TopologyReportOptions { IconCatalog = catalog, RequireResolvedIcons = true });
        string svg = report.Pages[0].ToSvg();
        icon.Artwork!.SvgBody = "<circle cx='12' cy='12' r='4'/>";
        catalog.RemovePack("vendor");
        Assert.Contains("M0 0h24v24H0z", svg);
        Assert.Equal(svg, report.Pages[0].ToSvg());
        Assert.NotEmpty(report.Pages[0].ToPng());
        Assert.Throws<TopologyValidationException>(() => chart.PrepareReport(new TopologyReportOptions { RequireResolvedIcons = true }));
    }

    [Fact]
    public void ReportSmallRequestedGapStillRespectsPageWidth() {
        var chart = TopologyChart.Create();
        for (int i = 0; i < 4; i++) chart.AddAutoNode("n" + i, "Node " + i);
        var report = chart.PrepareReport(new TopologyReportOptions { Gap = 1, PageWidth = 1043 });
        Assert.All(report.Pages, page => Assert.InRange(page.Width, 0, 1043));
        Assert.Equal(4, report.Pages.Sum(page => page.NodeCount));
    }

    [Fact]
    public void ViewDoesNotHideInvalidSourceGroupReferences() {
        var chart = TopologyChart.Create().AddAutoNode("a", "A");
        chart.Nodes[0].GroupId = "missing";
        Assert.Throws<TopologyValidationException>(() => chart.Prepare(new TopologyRenderOptions { View = new TopologyView() }));
    }

    [Fact]
    public void ReportPaginationReservesRenderedHeader() {
        var chart = TopologyChart.Create();
        for (int i = 0; i < 12; i++) chart.AddAutoNode("n" + i, "Node " + i);
        var report = chart.PrepareReport(new TopologyReportOptions { MinimumNodeHeight = 200 });
        Assert.All(report.Pages, page => Assert.InRange(page.Height, 0, 800));
        Assert.Equal(12, report.Pages.Sum(page => page.NodeCount));
    }

    [Fact]
    public void OverviewCombinesBothEndpointOrders() {
        var chart = TopologyChart.Create().AddAutoNode("a", "A").AddAutoNode("b", "B")
            .AddEdge("ab", "a", "b").AddEdge("ba", "b", "a");
        var report = chart.PrepareReport(new TopologyReportOptions { MaximumNodesPerPage = 1 });
        var edge = Assert.Single(report.Overview.ToInterchangeEnvelope().Edges);
        Assert.Equal("2 relationships", edge.Label);
        Assert.Equal(2, report.CrossPageLinks.Count);
    }

    [Theory]
    [InlineData(VisualLinkDirection.None, " — ")]
    [InlineData(VisualLinkDirection.Forward, " → ")]
    [InlineData(VisualLinkDirection.Backward, " ← ")]
    [InlineData(VisualLinkDirection.Bidirectional, " ↔ ")]
    public void CrossPageNavigationRetainsDirection(VisualLinkDirection direction, string separator) {
        var chart = TopologyChart.Create().AddAutoNode("a", "Source").AddAutoNode("b", "Target")
            .AddEdge("ab", "a", "b", direction: direction);
        var report = chart.PrepareReport(new TopologyReportOptions { MaximumNodesPerPage = 1 });
        Assert.Equal(direction, Assert.Single(report.CrossPageLinks).Direction);
        Assert.Contains("Source" + separator + "Target", report.ToInteractiveHtmlPage());
    }

    [Fact]
    public void ReportInterchangeRetainsSourceIdWhenProjectionBoundsTheNodeId() {
        string sourceId = new string('n', 720);
        var chart = TopologyChart.Create().AddAutoNode(sourceId, "Bounded identifier");
        var report = chart.PrepareReport();

        var node = Assert.Single(report.Pages).ToInterchangeEnvelope().Nodes.Single();

        Assert.NotEqual(sourceId, node.Id);
        Assert.Equal(sourceId, node.Extensions["chartforgex.sourceId"]);
        Assert.Equal(1, report.NodePages[sourceId]);
    }

    [Fact]
    public void ReportInterchangeOmitsSourceIdWhenItExceedsTheInterchangeTextBudget() {
        string sourceId = new string('n', 65_537);
        var report = TopologyChart.Create().AddAutoNode(sourceId, "Oversized identifier").PrepareReport();

        var node = Assert.Single(report.Pages).ToInterchangeEnvelope().Nodes.Single();

        Assert.NotEqual(sourceId, node.Id);
        Assert.DoesNotContain("chartforgex.sourceId", node.Extensions.Keys);
        Assert.Equal(1, report.NodePages[sourceId]);
    }

    [Fact]
    public void InterchangeRetainsOnlySourceIdsThatFitTheAggregateJsonBudget() {
        var chart = TopologyChart.Create();
        for (var index = 0; index < 132; index++) {
            string prefix = index.ToString("D3") + "-";
            chart.AddAutoNode(prefix + new string('n', 65_536 - prefix.Length), "Node " + index);
        }

        VisualArtifactInterchangeEnvelope envelope = chart.ToVisualArtifact().ToInterchangeEnvelope();
        string json = envelope.ToJson();
        int retainedSourceIds = envelope.Nodes.Count(node => node.Extensions.ContainsKey("chartforgex.sourceId"));

        Assert.InRange(retainedSourceIds, 1, chart.Nodes.Count - 1);
        Assert.True(json.Length <= VisualArtifactInterchangeEnvelope.MaximumJsonCharacters);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(json) <= VisualArtifactInterchangeEnvelope.MaximumJsonUtf8Bytes);
    }

    [Fact]
    public void ReportInterchangeRelocatesCollidingSourceIdMetadata() {
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Manual)
            .AddGroup("shared", "Group", 0, 0, 400, 200)
            .AddNode("shared", "Bounded identifier", 30, 70, groupId: "shared");
        chart.Nodes[0].Metadata["chartforgex.sourceId"] = "caller-owned";

        var node = chart.ToVisualArtifact().ToInterchangeEnvelope().Nodes.Single();

        Assert.Equal("shared", node.Extensions["chartforgex.sourceId"]);
        Assert.Contains("caller-owned", node.Extensions.Values);
        Assert.Equal(2, node.Extensions.Count);
    }

    [Fact]
    public void ReportRelocatesCallerMetadataThatUsesTheGeneratedGroupKey() {
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Manual)
            .AddGroup("group", "Group", 0, 0, 500, 260)
            .AddNode("node", "Node", 40, 90, groupId: "group");
        chart.Nodes[0].Metadata["report.sourceGroupId"] = "caller-owned";
        chart.Nodes[0].Metadata["report.sourceGroupId.source"] = "caller-owned-existing";

        var extensions = chart.PrepareReport().Pages.Single().ToInterchangeEnvelope().Nodes.Single().Extensions;

        Assert.Equal("group", extensions["report.sourceGroupId"]);
        Assert.Equal("caller-owned-existing", extensions["report.sourceGroupId.source"]);
        Assert.Contains("caller-owned", extensions.Values);
        Assert.Equal(3, extensions.Count);
    }

    [Fact]
    public void ReportOmitsGeneratedGroupMetadataWhenTheExtensionBagIsFull() {
        var chart = TopologyChart.Create().AddAutoGroup("group", "Group").AddAutoNode("node", "Node", groupId: "group");
        for (var index = 0; index < 543; index++) chart.Nodes[0].Metadata["metadata-" + index] = "value";
        chart.Nodes[0].Metadata["report.sourceGroupId"] = "caller-owned";

        var extensions = chart.PrepareReport().Pages.Single().ToInterchangeEnvelope().Nodes.Single().Extensions;

        Assert.Equal(544, extensions.Count);
        Assert.Equal("caller-owned", extensions["report.sourceGroupId"]);
    }

    [Fact]
    public void ReportOmitsGeneratedGroupMetadataWhenTheGroupIdExceedsTheTextBudget() {
        string groupId = new string('g', 65_537);
        var chart = TopologyChart.Create().AddAutoGroup(groupId, "Group").AddAutoNode("node", "Node", groupId: groupId);

        var extensions = chart.PrepareReport().Pages.Single().ToInterchangeEnvelope().Nodes.Single().Extensions;

        Assert.DoesNotContain("report.sourceGroupId", extensions.Keys);
    }

    [Fact]
    public void ReportPageTitlesRetainTheirSuffixWithinTheInterchangeTextBudget() {
        string title = new string('t', 65_536);
        var report = TopologyChart.Create().WithTitle(title).AddAutoNode("node", "Node").PrepareReport();

        VisualArtifactInterchangeEnvelope page = Assert.Single(report.Pages).ToInterchangeEnvelope();

        Assert.Equal(65_536, page.Title.Length);
        Assert.EndsWith(" — 1", page.Title);
        Assert.NotEmpty(page.ToJson());
        Assert.Equal(title, report.Overview.ToInterchangeEnvelope().Title);
    }

    [Fact]
    public void ReportHtmlDoesNotApplyInterchangeMetricBudgets() {
        var chart = TopologyChart.Create().AddAutoNode("a", "Source");
        for (int i = 0; i < 1025; i++) chart.Nodes[0].Metrics.Add("metric" + i, "1");
        var report = chart.PrepareReport();
        Assert.Contains("1 objects", report.ToInteractiveHtmlPage());
    }

    [Fact]
    public void ReportCardsReserveMultilineHeadersAndDetails() {
        var chart = TopologyChart.Create().AddAutoNode("a", "First line\nSecond line", subtitle: "First subtitle\nSecond subtitle");
        chart.Nodes[0].Details.Add(new TopologyNodeDetail { Label = "Owner", Value = "Operations" });
        var report = chart.PrepareReport();
        var node = report.Pages[0].ToInterchangeEnvelope().Nodes[0];
        Assert.True(node.Height >= 102, "Both header lines and the detail row must fit inside the card.");
    }

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
        Assert.Contains("<h1 lang=\"pl\">Topology report</h1>", html);
        Assert.Equal("Topology report", report.Overview.Title);
        Assert.Equal("Topology report — 1", report.Pages[0].Title);
    }

    [Fact]
    public void ReportHtmlRetainsUserLanguageAndEnglishNavigation() {
        var chart = TopologyChart.Create().WithTitle("Usługi")
            .AddAutoNode("a", "Źródło").AddAutoNode("b", "Cel").AddEdge("ab", "a", "b");
        chart.Accessibility.Language = "pl";
        var report = chart.PrepareReport(new TopologyReportOptions { MaximumNodesPerPage = 1 });
        chart.Accessibility.Language = "de";
        string html = report.ToInteractiveHtmlPage();
        Assert.Equal("pl", report.Source.Language);
        Assert.Contains("<html lang=\"pl\">", html);
        Assert.Contains("<main lang=\"en\">", html);
        Assert.Contains("<h1 lang=\"pl\">", html);
        var objects = ReportRecords(html, "objects");
        Assert.All(objects, record => Assert.Equal("pl", record[3]));
        var link = Assert.Single(ReportRecords(html, "links-1"));
        Assert.Equal("pl", link[3]);
        Assert.Equal(" · page 2", link[2]);
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
    public void HtmlReportEncodesUserTextAndStoresInactivePagesAsText() {
        const string label = "</template><script>alert(1)</script>";
        var report = TopologyChart.Create().WithTitle(label).AddAutoNode("node", label).PrepareReport();
        string html = report.ToInteractiveHtmlPage();
        Assert.DoesNotContain(label, html);
        Assert.Contains("&lt;/template&gt;", html);
        Assert.Contains("page-1", html);
        Assert.DoesNotContain("<template", html);
        Assert.DoesNotContain("<svg", html);
        string marker = "<script type=\"application/json\" id=\"page-1\">";
        int start = html.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        int end = html.IndexOf("</script>", start, StringComparison.Ordinal);
        string page = System.Text.Json.JsonSerializer.Deserialize<string>(html.Substring(start, end - start))!;
        Assert.Contains("<svg", page);
        Assert.Contains("&lt;/template&gt;", page);
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
        Assert.Contains("Shared node — Long ID node", report.ToInteractiveHtmlPage());
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

    [Fact]
    public void ReadabilityIgnoresHiddenAnchorsOutsideTheVisibleViewport() {
        var chart = TopologyChart.Create().WithViewport(600, 400).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("visible", "Visible", 80, 140)
            .AddNode("anchor", "Anchor", 900, 900)
            .WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);

        var readability = chart.Prepare().AssessReadability(600, 400);

        Assert.Equal(0, readability.OutOfBoundsNodeCount);
        Assert.Equal(0, readability.NodeCollisionCount);
        Assert.False(readability.NeedsDetailViews);
    }
}
