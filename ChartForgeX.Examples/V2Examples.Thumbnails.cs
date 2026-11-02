using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

public static partial class V2Examples {
    private const int ThumbnailWidth = 400;
    private const int ThumbnailHeight = 280;
    private static readonly VisualTheme ThumbnailTheme = VisualTheme.Graphite().WithTypography(
        new VisualTypography(axisSize: 13.5, legendSize: 13.5, dataLabelSize: 13.5));

    // Previews are prepared at their display size. Dense diagram examples use a small,
    // truthful overview of the same notation; their complete model stays on the example page.
    private static void WriteThumbnail(string output, IVisualRenderable model, string id, string title, string family, VisualThemeMode mode, bool? legend) {
        IVisualRenderable previewModel = family switch {
            "sequence" => SimpleSequenceRecipe.Create(),
            "topology" => ThumbnailTopology(),
            "flow" => ThumbnailFlow(),
            "org-chart" => ThumbnailOrgChart(),
            "dashboard" => ThumbnailDashboard(mode),
            "state-timeline" when id.Contains("-availability-") => ThumbnailAvailability(mode),
            "chart-grid" => ChartGrid.Create().WithColumns(2).WithGap(12).WithPanelSize(184, 248)
                .Add(Chart.Create().WithXLabels("Mon", "Tue", "Wed").AddLine("Requests", new[] { new ChartPoint(1, 28), new ChartPoint(2, 42), new ChartPoint(3, 35) }))
                .Add(Chart.Create().WithXLabels("Mon", "Tue", "Wed").AddBar("Capacity", new[] { new ChartPoint(1, 52), new ChartPoint(2, 64), new ChartPoint(3, 58) })),
            _ => model
        };
        // Horizontal interval categories use the thumbnail's ordinary legend space.
        var horizontalIntervals = family == "range-bar" && model is Chart chart
            && chart.Options.RangeBar.Orientation == ChartOrientation.Horizontal;
        // The twelve-column state map needs this content width for complete 13.5 px codes and their hexagon clearance.
        var padding = family == "tile-map" ? 14 : 16;
        var prepared = previewModel.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(ThumbnailWidth, ThumbnailHeight), padding),
            ThumbnailTheme, mode, new VisualFrame("", "", showLegend: horizontalIntervals || family is "topology" or "sequence" or "flow" or "chart-grid" or "dashboard" ? false : legend),
            FontSpec.FromFamily(ProofFont)));
        var kind = family switch {
            "topology" => VisualArtifactKind.Topology, "flow" => VisualArtifactKind.Flow,
            "sequence" => VisualArtifactKind.Sequence, "chart-grid" => VisualArtifactKind.ChartGrid,
            _ => previewModel is TopologyChart ? VisualArtifactKind.Topology : previewModel is ChartGrid ? VisualArtifactKind.ChartGrid : VisualArtifactKind.Chart
        };
        var artifact = prepared.ToArtifact(id + "-thumbnail", kind);
        artifact.Accessibility.Name = title;
        if (family is "topology" or "flow" or "sequence" or "chart-grid" or "org-chart" or "dashboard")
            artifact.Accessibility.Description = "A compact overview. Open the example for the complete diagram, variants and source.";
        if (family == "state-timeline" && id.Contains("-availability-"))
            artifact.Accessibility.Description = "A compact overview of the first three targets. Open the example for all nine targets, variants and source.";
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".thumbnail.svg"), ExampleArtifactWriter.Svg(prepared, id + "-thumbnail", artifact.Accessibility));
        File.WriteAllBytes(Path.Combine(output, id + ".thumbnail.png"), prepared.ToPng());
    }

    private static Chart ThumbnailAvailability(VisualThemeMode mode) {
        var chart = AvailabilityLanesRecipe.Create(mode: mode);
        foreach (var series in chart.Series.Skip(3).ToArray()) chart.RemoveSeries(series);
        return chart;
    }

    private static TopologyChart ThumbnailOrgChart() {
        var chart = TopologyChart.Create().WithLegend(null);
        chart.DefaultRenderOptions = new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true };
        return chart.AddTeam("company", "Northwind Services", new[] {
            new TopologyTeamMember("avery", "Avery Quinn", "Managing director"),
            new TopologyTeamMember("jordan", "Jordan Vale", "Engineering") { ParentId = "avery" },
            new TopologyTeamMember("morgan", "Morgan Reed", "Operations") { ParentId = "avery" }
        }, new TopologyTeamOptions { IncludeTeamNode = false });
    }

    private static ChartGrid ThumbnailDashboard(VisualThemeMode mode) {
        var preview = ChartGrid.Create().WithColumns(2).WithGap(12).WithPanelSize(184, 248);
        foreach (var chart in OperationsDashboardRecipe.Create(mode: mode).Charts.Take(2)) {
            chart.Options.ShowDonutCenterLabel = false;
            preview.Add(chart.WithTitle("").WithSubtitle("").WithLegend(false));
        }
        return preview;
    }

    private static TopologyChart ThumbnailTopology() {
        var chart = TopologyChart.Create().WithId("service-overview");
        chart.DefaultRenderOptions = new TopologyRenderOptions { FitContentToViewport = true, WrapNodeLabels = true, IncludeLegend = false };
        chart.Nodes.Add(new TopologyNode { Id = "client", Label = "Client service", X = 0, Y = 0, Width = 138, Height = 66, ShowStatusBadge = false });
        chart.Nodes.Add(new TopologyNode { Id = "worker", Label = "Processing service", X = 224, Y = 0, Width = 138, Height = 66, ShowStatusBadge = false });
        chart.Edges.Add(new TopologyEdge { Id = "requests", SourceNodeId = "client", TargetNodeId = "worker", Label = "Requests",
            Direction = VisualLinkDirection.Forward, Routing = TopologyEdgeRouting.Straight });
        return chart;
    }

    private static FlowArtifact ThumbnailFlow() => FlowArtifact.Create("request-overview").WithSize(ThumbnailWidth, ThumbnailHeight, 16)
        .AddStep("received", "Received", FlowArtifactStepKind.Start).ConfigureStep("received", step => { step.Width = 128; step.Height = 64; })
        .AddStep("complete", "Complete", FlowArtifactStepKind.End).ConfigureStep("complete", step => { step.Width = 128; step.Height = 64; })
        .AddConnector("received", "complete", "Process");
}
