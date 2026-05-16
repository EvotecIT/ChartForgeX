using System.IO;
using ChartForgeX.Markup;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MarkupTopologyParsesFencedCommandDiagram() {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Tests", "Fixtures", "markup", "topology-service-map.md"));
        var result = new MarkupTopologyParser().Parse(source);

        Assert(!result.HasErrors, "Command topology markup should parse without errors: " + Diagnostics(result));
        Assert(result.Document != null, "Topology markup should produce a document.");
        Assert(result.Document!.Groups.Count == 2, "Topology markup should parse groups.");
        Assert(result.Document.Nodes.Count == 3, "Topology markup should parse nodes.");
        Assert(result.Document.Edges.Count == 2, "Topology markup should parse edges.");

        var chart = result.Document.ToTopologyChart();
        Assert(chart.LayoutMode == TopologyLayoutMode.Layered && chart.LayoutDirection == TopologyLayoutDirection.LeftToRight, "Topology markup should map compact layout aliases.");
        Assert(chart.ToSvg().Contains("data-cfx-role=\"topology\"", System.StringComparison.Ordinal), "Topology markup should render through the ChartForgeX SVG renderer.");
    }

    private static void MarkupTopologyParsesTableDiagramAndEmitsCSharp() {
        const string source = @"```chartforgex topology
title: ""Regional Directory Topology""
layout: densegrouped tb
groups:
| id | label | status | icon |
| -- | ----- | ------ | ---- |
| emea | EMEA | warning | microsoft-ad:site |
| amer | AMER | healthy | microsoft-ad:site |
nodes:
| id | label | group | kind | status | badge |
| -- | ----- | ----- | ---- | ------ | ----- |
| dc-emea | EMEA DC01 | emea | server | warning | GC |
| dc-amer | AMER DC01 | amer | server | healthy | GC |
edges:
| from | to | label | status | direction |
| ---- | -- | ----- | ------ | --------- |
| dc-emea | dc-amer | 92 ms | warning | bidirectional |
```";

        var result = new MarkupTopologyParser().Parse(source);

        Assert(!result.HasErrors, "Table topology markup should parse without errors: " + Diagnostics(result));
        var code = MarkupTopologyCSharpEmitter.Emit(result.Document!);
        Assert(code.Contains("TopologyChart.Create()", System.StringComparison.Ordinal), "C# emitter should create a topology chart.");
        Assert(code.Contains(".AddGroup(\"emea\", \"EMEA\", 0, 0, 260, 160, TopologyHealthStatus.Warning", System.StringComparison.Ordinal), "C# emitter should include parsed groups.");
        Assert(code.Contains(".WithNodeBadge(\"dc-emea\", \"GC\")", System.StringComparison.Ordinal), "C# emitter should include node badge helpers.");
    }

    private static void MarkupTopologyReportsMissingNodes() {
        var result = new MarkupTopologyParser().Parse("title \"Empty\"");

        Assert(result.HasErrors, "Topology markup without nodes should report a parser error.");
        Assert(Diagnostics(result).Contains("at least one node", System.StringComparison.Ordinal), "Missing-node diagnostic should be actionable.");
    }

    private static string Diagnostics<TDocument>(MarkupParseResult<TDocument> result) where TDocument : class =>
        string.Join("; ", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Severity + ":" + diagnostic.Message));
}
