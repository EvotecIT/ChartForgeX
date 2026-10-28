using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Implicit zero-height stack geometry uses a log baseline without accepting authored zero values.</summary>
public sealed class StackedAreaLogarithmicMissingDataTests {
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public void DerivedMissingContoursUseTheSameLogBaselineAsTheirLowerBoundary(bool negative, bool secondary, bool missingUpper) {
        var chart = Model(negative, secondary, ChartStackedAreaMissingDataPolicy.Zero);
        var index = missingUpper ? 1 : 0; chart.Series[index].Points.RemoveAt(1);
        var layout = ChartStackLayout.Create(chart, ChartBarCoordinateMap.Create(chart));
        var missing = layout.AreaVertices(index)!.Single(vertex => vertex.X == 1);
        Assert.Null(missing.SourcePoint); Assert.Equal(missing.Base, missing.End);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, $"stacked-area-log-zero-{negative}-{secondary}-{missingUpper}");
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".png", png);
        }
        Assert.NotEmpty(png); Assert.DoesNotContain("NaN", svg); Assert.DoesNotContain("Infinity", svg);
        var points = XDocument.Parse(svg).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(secondary ? 7 : 5, points.Length);
        Assert.DoesNotContain(points, point => (string?)point.Attribute("data-cfx-series") == index.ToString() && (string?)point.Attribute("data-cfx-x") == "1");
        Assert.All(prepared.Scene.Nodes.OfType<VisualScenePath>().SelectMany(path => path.Commands), command => {
            Assert.True(double.IsFinite(command.X)); Assert.True(double.IsFinite(command.Y));
        });
        var saved = prepared.ToSvg(); chart.Series[index].Points.Clear(); Assert.Equal(saved, prepared.ToSvg());
    }

    [Theory]
    [InlineData(false, ChartStackedAreaMissingDataPolicy.Independent)] [InlineData(true, ChartStackedAreaMissingDataPolicy.Independent)]
    [InlineData(false, ChartStackedAreaMissingDataPolicy.Gap)] [InlineData(true, ChartStackedAreaMissingDataPolicy.Gap)]
    public void ExistingIndependentAndGapPoliciesKeepValidLogarithmicSourceValues(bool negative, ChartStackedAreaMissingDataPolicy policy) {
        var chart = Model(negative, false, policy); chart.Series[0].Points.RemoveAt(1);
        var prepared = Prepare(chart); Assert.NotEmpty(prepared.ToSvg()); Assert.NotEmpty(prepared.ToPng());
        Assert.Equal(5, prepared.Regions.Count(region => region.Role == "point"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EmptyAlignedMemberHasNoInventedLogarithmicObservation(bool negative) {
        var chart = Model(negative, false, ChartStackedAreaMissingDataPolicy.Zero);
        chart.AddSeries(new ChartSeries(new ChartSeriesId("empty"), "Empty", ChartSeriesKind.StackedArea, Array.Empty<ChartPoint>())
            .WithStackedAreaMissingData(ChartStackedAreaMissingDataPolicy.Zero));
        var prepared = Prepare(chart); Assert.NotEmpty(prepared.ToSvg()); Assert.NotEmpty(prepared.ToPng());
        Assert.Equal(6, prepared.Regions.Count(region => region.Role == "point"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ActualZeroObservationsStillFailTheExistingLogDomainGuard(bool negative) {
        var chart = Model(negative, false, ChartStackedAreaMissingDataPolicy.Zero);
        chart.Series[0].Points[0] = new ChartPoint(0, 0);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    private static Chart Model(bool negative, bool secondary, ChartStackedAreaMissingDataPolicy policy) {
        var sign = negative ? -1 : 1;
        var chart = Chart.Create().WithSize(600, 400).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false);
        chart.AddSeries(new ChartSeries(new ChartSeriesId("lower"), "Lower", ChartSeriesKind.StackedArea,
            new[] { new ChartPoint(0, sign * 2), new ChartPoint(1, sign * 3), new ChartPoint(2, sign * 4) }).WithStackedAreaMissingData(policy));
        chart.AddSeries(new ChartSeries(new ChartSeriesId("upper"), "Upper", ChartSeriesKind.StackedArea,
            new[] { new ChartPoint(0, sign * 3), new ChartPoint(1, sign * 5), new ChartPoint(2, sign * 6) }).WithStackedAreaMissingData(policy));
        foreach (var series in chart.Series) { if (secondary) series.UseSecondaryValueAxis(); series.ConfigureMarkers(markers => markers.Enabled = false); }
        if (secondary) chart.AddLine("Primary context", new[] { new ChartPoint(0, 1), new ChartPoint(2, 2) });
        var axis = secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
        axis.WithLogarithmicScale(10, negative ? ChartLogarithmicDomain.Negative : ChartLogarithmicDomain.Positive)
            .WithBounds(negative ? -32 : 1, negative ? -1 : 32).WithReversal(negative == secondary);
        return chart;
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(600, 400)),
        frame: new VisualFrame(showLegend: false, showSurface: false, transparentBackground: true)));
}
