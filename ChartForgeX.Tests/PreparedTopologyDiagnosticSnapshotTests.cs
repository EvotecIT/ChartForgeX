using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedTopologyDiagnosticSnapshotTests {
    [Theory]
    [InlineData(TextMeasurementMode.PortableEstimate)]
    [InlineData(TextMeasurementMode.InstalledFonts)]
    public void DetachedAnalysisRetainsDenseRoutingAndMeasurementWithoutReservingTheFrameAgain(TextMeasurementMode mode) {
        var chart = DenseRouteFixture.Small();
        chart.Accessibility.Language = "pl";
        var title = chart.Title;
        var options = DenseRouteFixture.Options();
        options.TextMeasurementMode = mode;
        var prepared = chart.Prepare(options);
        var svg = prepared.ToSvg();
        var before = prepared.Analyze();
        Assert.All(before.Edges, edge => Assert.Equal(TopologyEdgeRouter.PlannedCorridor, edge.Corridor));
        Assert.Equal(title, prepared.Title);
        Assert.Equal("pl", prepared.Language);
        Assert.Equal(title, prepared.ToInterchangeEnvelope().Title);

        chart.Title = "Changed source";
        chart.Nodes.Clear(); chart.Edges.Clear();
        options.ReadableDenseLayout = false;
        options.TextMeasurementMode = mode == TextMeasurementMode.PortableEstimate ? TextMeasurementMode.InstalledFonts : TextMeasurementMode.PortableEstimate;
        TopologyDenseRoutePlanner.ClearPlanCache();
        var after = prepared.Analyze();
        Assert.Equal(before.Edges.Select(edge => edge.Corridor), after.Edges.Select(edge => edge.Corridor));
        Assert.Equal(before.Edges.SelectMany(edge => edge.Points), after.Edges.SelectMany(edge => edge.Points));
        Assert.Equal(before.Nodes.Select(node => node.CaptionBounds), after.Nodes.Select(node => node.CaptionBounds));
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(title, prepared.Title);
    }
}
