using ChartForgeX.Markup;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyCellContractTests {
    [Theory]
    [InlineData("groups", 120)]
    [InlineData("group-labels", 120)]
    [InlineData("wide-caption", 120)]
    [InlineData("resized", 120)]
    [InlineData("scenario", 120)]
    [InlineData("diagonal", 120)]
    [InlineData("view", 120)]
    [InlineData("hierarchy", 100)]
    [InlineData("priority", 120)]
    [InlineData("priority", 80)]
    [InlineData("width-reserve", 130)]
    [InlineData("failure", 120)]
    [InlineData("values", 120)]
    [InlineData("default-values", 120)]
    [InlineData("manual", 120)]
    [InlineData("group-ends", 120)]
    [InlineData("same-id", 120)]
    [InlineData("junctions", 120)]
    public void CellProjectionPreservesReadableStructureAndOperationalFacts(string contract, int width) {
        var source = contract switch {
            "hierarchy" => Hierarchy, "junctions" => Services, "manual" => Manual,
            "group-ends" => GroupEnds, _ => Network
        };
        var parsed = new MarkupArchitectureParser().Parse("```chartforgex architecture v1\n" + source + "\n```");
        Assert.False(parsed.HasErrors);
        var chart = contract == "width-reserve" ? TopologyCellLayoutTests.Example("ad-replication") : parsed.Document!;
        if (contract == "group-labels") chart = OfficeGroups();
        const string wideCaption = "東京東京東京東京東京東京";
        if (contract == "wide-caption") chart.Groups.Single(group => group.Id == "berlin").Label = wideCaption;
        if (contract is "diagonal" or "view") chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.ForceDirected)
            .AddAutoNode("a", "First").AddAutoNode("b", "Second");
        if (contract == "same-id") chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Layered)
            .AddAutoGroup("g", "Shell").AddAutoNode("g", "Authored node", status: TopologyHealthStatus.Warning)
            .AddAutoNode("b", "Outside").AddAutoNode("a", "Member", groupId: "g")
            .AddEdge("e", "g", "b", direction: ChartForgeX.Primitives.VisualLinkDirection.Forward);
        if (contract == "group-ends") {
            chart.AddEdge("region-outside", "region", "outside", "egress", direction: ChartForgeX.Primitives.VisualLinkDirection.Forward);
            chart.AddEdge("outside-other", "outside", "other", "ingress", direction: ChartForgeX.Primitives.VisualLinkDirection.Forward);
            chart.AddEdge("region-other", "region", "other", "link", direction: ChartForgeX.Primitives.VisualLinkDirection.Forward);
        }
        if (contract == "manual") {
            chart.LayoutMode = TopologyLayoutMode.Manual;
            for (var index = 0; index < chart.Nodes.Count; index++) { chart.Nodes[index].X = 100 + index * 300; chart.Nodes[index].Y = 100; }
            chart.Edges[0].SourcePort = TopologyEdgePort.Right; chart.Edges[0].TargetPort = TopologyEdgePort.Left;
            chart.Edges[0].Waypoints.Add(new ChartForgeX.Primitives.ChartPoint(300, 40));
        }
        var prepared = chart.Prepare(contract == "view" ? new TopologyRenderOptions { View = new TopologyView { NodeIds = { "a", "b" } } } : null);
        var projection = new TopologyCellLayoutOptions {
            MaximumColumns = width, CompactToFit = true, ReservedValueWidth = contract == "default-values" ? 0 : 10
        };
        var baseline = contract is "resized" or "scenario" ? TopologyCellLayout.Create(prepared, projection) : null;
        if (contract == "resized") prepared = prepared.WithOutputSize(300, 200);
        if (contract == "scenario") {
            chart.AddScenario("reply", "Reply", scenario => scenario.AddMessageStep(chart.Edges[0].Id, TopologyScenarioInteraction.Response, "Return", true));
            prepared = chart.PrepareScenarioStep("reply", 0);
        }
        var layout = TopologyCellLayout.Create(prepared, projection);
        foreach (var first in layout.Nodes) foreach (var second in layout.Nodes) {
            if (first.Id == second.Id) continue;
            Assert.True(first.Column + first.Width <= second.Column || second.Column + second.Width <= first.Column
                || first.Row + first.Height <= second.Row || second.Row + second.Height <= first.Row, first.Id + " overlaps " + second.Id);
        }
        foreach (var edge in layout.Edges) {
            var path = new List<TopologyCellPoint> { edge.Points[0] };
            for (var index = 1; index < edge.Points.Count; index++) {
                var first = edge.Points[index - 1]; var last = edge.Points[index];
                var count = Math.Abs(last.Column - first.Column) + Math.Abs(last.Row - first.Row);
                for (var step = 1; step <= count; step++) path.Add(new(first.Column + Math.Sign(last.Column - first.Column) * step,
                    first.Row + Math.Sign(last.Row - first.Row) * step));
            }
            foreach (var point in path) foreach (var node in layout.Nodes) {
                if (node.Id == edge.SourceNodeId || node.Id == edge.TargetNodeId) continue;
                Assert.False(node.Contains(point.Column, point.Row), edge.Id + " crosses " + node.Id);
            }
            foreach (var group in layout.Groups) {
                var boxes = layout.Nodes.Cast<TopologyCellBox>().Concat(layout.Groups).ToArray();
                var sourceNode = boxes.First(box => box.Id == edge.SourceNodeId); var targetNode = boxes.First(box => box.Id == edge.TargetNodeId);
                var crosses = sourceNode is TopologyCellGroup && group.Id == sourceNode.Id || targetNode is TopologyCellGroup && group.Id == targetNode.Id || group.Contains(sourceNode.Column, sourceNode.Row) != group.Contains(targetNode.Column, targetNode.Row);
                var border = path.Where(point => group.Contains(point.Column, point.Row) && (point.Column == group.Column
                    || point.Column == group.Column + group.Width - 1 || point.Row == group.Row || point.Row == group.Row + group.Height - 1)).ToArray();
                Assert.Equal(crosses ? 1 : 0, border.Length);
                Assert.DoesNotContain(border, point => point.Row == group.Row);
            }
        }
        var paint = new TopologyCellPaintOptions { IncludeLegend = false };
        var critical = chart.Edges.SingleOrDefault(edge => edge.Label == "1722 · 3 h");
        if (contract == "failure") { paint.FailedEdgeIds.Add(critical!.Id); paint.HighlightEdgeIds.Add(critical.Id); }
        if (contract is "values" or "default-values") paint.NodeValues["dc02"] = "lag 1m";
        var canvas = layout.Paint(paint);
        var cells = Enumerable.Range(0, canvas.Rows).SelectMany(row => Enumerable.Range(0, canvas.Columns).Select(column => canvas.Get(column, row))).ToArray();
        switch (contract) {
            case "view":
                var native = prepared.Analyze().Nodes.OrderBy(node => node.Bounds.Top).ToArray();
                Assert.True(native[0].Bounds.Bottom < native[1].Bounds.Top);
                var upper = layout.FindNode(native[0].Id)!; var lower = layout.FindNode(native[1].Id)!;
                Assert.True(upper.Row + upper.Height < lower.Row);
                break;
            case "resized":
            case "scenario":
                Assert.Equal(baseline!.Nodes.Select(node => (node.Id, node.Column, node.Row, node.Width, node.Height)),
                    layout.Nodes.Select(node => (node.Id, node.Column, node.Row, node.Width, node.Height)));
                Assert.Equal(baseline.Groups.Select(group => (group.Id, group.Column, group.Row, group.Width, group.Height)),
                    layout.Groups.Select(group => (group.Id, group.Column, group.Row, group.Width, group.Height)));
                if (contract == "scenario") Assert.Equal(ChartForgeX.Primitives.VisualLinkDirection.Backward,
                    layout.Edges.Single(edge => edge.Id == chart.Edges[0].Id).Direction);
                break;
            case "wide-caption":
                Assert.Contains(wideCaption, canvas.ToText(), StringComparison.Ordinal);
                break;
            case "group-labels":
                Assert.Contains("HTTPS", canvas.ToText(), StringComparison.Ordinal);
                break;
            case "groups":
                foreach (var group in layout.Groups) {
                    var members = layout.Nodes.Where(node => node.GroupId == group.Id).ToArray();
                    Assert.Equal(2, members.Min(node => node.Row) - group.Row);
                    for (var column = group.Column + 3; column < group.Column + 3 + group.Label.Length; column++)
                        Assert.Equal(TopologyCellRole.GroupLabel, canvas.Get(column, group.Row).Role);
                }
                break;
            case "hierarchy":
                foreach (var edge in layout.Edges) {
                    var parent = layout.FindNode(edge.SourceNodeId)!; var child = layout.FindNode(edge.TargetNodeId)!;
                    Assert.True(parent.Row + parent.Height < child.Row);
                    Assert.True(parent.Contains(edge.Points[0].Column, edge.Points[0].Row));
                    Assert.True(child.Contains(edge.Points[^1].Column, edge.Points[^1].Row));
                }
                Assert.True(layout.FindNode("dev")!.Row < layout.FindNode("app")!.Row);
                break;
            case "width-reserve":
                Assert.True(layout.Columns <= width, "Cell columns: " + layout.Columns);
                Assert.Contains("DC04", canvas.ToText(), StringComparison.Ordinal);
                break;
            case "priority":
                Assert.Contains("1722 · 3 h", canvas.ToText(), StringComparison.Ordinal);
                Assert.Contains("14 min", canvas.ToText(), StringComparison.Ordinal);
                break;
            case "failure":
                Assert.Contains(cells, cell => cell.ElementId == critical!.Id && cell.Role == TopologyCellRole.EdgeArrow && cell.Glyph == "✕");
                break;
            case "default-values":
            case "values":
                var node = layout.FindNode("dc02")!;
                var rows = canvas.ToText().Split('\n');
                Assert.Contains("Bridgehead", rows[node.Row + 2], StringComparison.Ordinal);
                Assert.Contains("lag 1m", rows[node.Row + 3], StringComparison.Ordinal);
                break;
            case "same-id":
                var same = Assert.Single(layout.Edges);
                var realNode = layout.FindNode("g")!;
                Assert.True(realNode.Contains(same.Points[0].Column, same.Points[0].Row));
                Assert.Contains(cells, cell => cell.ElementId == "e" && cell.Role == TopologyCellRole.EdgeArrow);
                break;
            case "manual":
                var authored = Assert.Single(layout.Edges);
                var first = layout.FindNode("first")!; var last = layout.FindNode("last")!;
                Assert.Equal(first.Column + first.Width - 1, authored.Points[0].Column);
                Assert.Equal(last.Column, authored.Points[^1].Column);
                Assert.True(authored.Points.Min(point => point.Row) < Math.Min(first.Row, last.Row));
                break;
            case "group-ends":
                Assert.Equal(3, layout.Edges.Count);
                foreach (var edge in layout.Edges) Assert.Contains(cells, cell => cell.ElementId == edge.Id && cell.Role == TopologyCellRole.EdgeArrow);
                break;
            case "junctions":
                Assert.Contains("requires", canvas.ToText(), StringComparison.Ordinal);
                for (var row = 0; row < canvas.Rows; row++) for (var column = 1; column + 1 < canvas.Columns; column++) {
                    if (canvas.Get(column, row).Role != TopologyCellRole.EdgeLabel) continue;
                    foreach (var neighbor in new[] { canvas.Get(column - 1, row), canvas.Get(column + 1, row) })
                        Assert.DoesNotContain(neighbor.Glyph, new[] { "┴", "┬", "┼", "├", "┤", "╋" });
                }
                break;
        }
    }

    private static TopologyChart OfficeGroups() => TopologyChart.Create().WithTitle("Small office")
        .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight).WithViewport(480, 240, 28)
        .AddAutoGroup("group-0", "Office LAN").AddAutoGroup("group-1", "DMZ").AddAutoGroup("group-2", "Cloud")
        .AddAutoNode("12 PCs", "12 PCs", TopologyNodeKind.Endpoint, groupId: "group-0")
        .AddAutoNode("File server", "File server", TopologyNodeKind.Storage, groupId: "group-0", subtitle: "FS01")
        .AddAutoNode("Printer", "Printer", TopologyNodeKind.Generic, groupId: "group-0")
        .AddAutoNode("Web server", "Web server", TopologyNodeKind.Server, groupId: "group-1", subtitle: "WEB01")
        .AddAutoNode("Microsoft 365", "Microsoft 365", TopologyNodeKind.Cloud, groupId: "group-2")
        .AddAutoNode("Backup", "Backup", TopologyNodeKind.Storage, groupId: "group-2", subtitle: "Offsite")
        .AddAutoNode("Firewall", "Firewall", TopologyNodeKind.Gateway, subtitle: "FW01")
        .AddEdge("link-0", "12 PCs", "Firewall", "HTTPS", direction: ChartForgeX.Primitives.VisualLinkDirection.Forward)
        .AddEdge("link-1", "File server", "Backup", "nightly", direction: ChartForgeX.Primitives.VisualLinkDirection.Forward)
        .AddEdge("link-2", "Firewall", "Web server", "443", direction: ChartForgeX.Primitives.VisualLinkDirection.Forward)
        .AddEdge("link-3", "Firewall", "Microsoft 365", "443", direction: ChartForgeX.Primitives.VisualLinkDirection.Forward);

    private const string Manual = """
        direction down
        service first "First"
        service last "Last"
        first -> last
        """;

    private const string GroupEnds = """
        direction right
        group region "Region"
          service first "First"
        end
        group other "Other"
          service last "Last"
        end
        service outside "Outside"
        """;

    private const string Network = """
        direction right
        group hq "Site: HQ"
          server dc01 "DC01" subtitle:"PDC emulator" status:healthy
          server dc02 "DC02" subtitle:"Bridgehead" status:healthy
        end
        group warsaw "Site: Warsaw"
          server dc03 "DC03" subtitle:"Bridgehead" status:warning
          server dc04 "DC04" subtitle:"Branch DC" status:healthy
        end
        group berlin "Site: Berlin"
          server dc05 "DC05" subtitle:"Bridgehead" status:critical
        end
        dc01 -> dc02 "1 min" status:healthy
        dc02 -> dc03 "14 min" status:warning
        dc03 -> dc04 "2 min" status:healthy
        dc02 -> dc05 "1722 · 3 h" status:critical
        """;

    private const string Services = """
        title "Why is the web site down? · WEB01"
        direction right
        service w3svc "W3SVC" subtitle:"World Wide Web" status:critical
        service was "WAS" subtitle:"Process activation" status:critical
        service http "HTTP" subtitle:"HTTP.sys" status:healthy
        service rpcss "RpcSs" subtitle:"RPC" status:healthy
        service appfabric "AppHostSvc" subtitle:"App host" status:healthy
        service sqlbrowser "SQLBrowser" subtitle:"stopped" status:unknown
        w3svc -> was "requires" status:critical
        w3svc -> http "requires"
        was -> rpcss "requires"
        was -> appfabric "requires" status:critical
        """;

    private const string Hierarchy = """
        direction down
        person cio "Anna" subtitle:"CIO"
        person ops "Marek" subtitle:"Operations"
        person dev "Julia" subtitle:"Development"
        person sd "Tom" subtitle:"Service desk"
        person net "Ola" subtitle:"Network"
        person app "Ben" subtitle:"Apps"
        cio -> ops
        cio -> dev
        ops -> sd
        ops -> net
        dev -> app
        """;
}
