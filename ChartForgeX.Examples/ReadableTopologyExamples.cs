using ChartForgeX.Topology;
using ChartForgeX.Interactivity.Html;

internal static class ReadableTopologyExamples {
    public static void Write(string output) {
        var chart = TopologyChart.Create().WithId("readable-service-report").WithTitle("Service inventory")
            .WithSubtitle("Readable detail pages with an explicit cross-page relationship index")
            .WithViewport(1200, 800).WithLayout(TopologyLayoutMode.Layered);
        for (int i = 0; i < 100; i++) chart.AddAutoNode("service-" + i, "Service " + i, subtitle: "Owner: Platform team");
        for (int i = 1; i < 100; i++) chart.AddEdge("link-" + i, "service-" + ((i - 1) / 3), "service-" + i);
        var report = chart.PrepareReport();
        File.WriteAllText(Path.Combine(output, "readable-topology-report.html"), report.ToInteractiveHtmlPage());
        File.WriteAllText(Path.Combine(output, "readable-topology-overview.svg"), report.Overview.ToSvg());
        for (int i = 0; i < report.Pages.Count; i++)
            File.WriteAllText(Path.Combine(output, "readable-topology-page-" + (i + 1) + ".svg"), report.Pages[i].ToSvg());
        var page = report.Pages[0];
        File.WriteAllText(Path.Combine(output, "readable-topology-detail.svg"), page.ToSvg());
        File.WriteAllBytes(Path.Combine(output, "readable-topology-detail.png"), page.ToPng());
        File.WriteAllText(Path.Combine(output, "readable-topology-detail.json"), page.ToInterchangeEnvelope().ToJson());
        File.WriteAllLines(Path.Combine(output, "readable-topology-index.txt"),
            report.NodePages.Select(pair => pair.Key + ": page " + pair.Value)
                .Concat(report.CrossPageLinks.Select(link => link.EdgeId + ": page " + link.SourcePage + " -> page " + link.TargetPage)));
    }
}
