using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class CartesianFillObservationBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.Area, false)]
    [InlineData(ChartSeriesKind.StepArea, false)]
    [InlineData(ChartSeriesKind.StackedArea, false)]
    [InlineData(ChartSeriesKind.Area, true)]
    [InlineData(ChartSeriesKind.StepArea, true)]
    [InlineData(ChartSeriesKind.StackedArea, true)]
    public void DisconnectedScalarObservationCannotBorrowAnotherSegmentsFillWitness(ChartSeriesKind kind, bool terminal) {
        var html = BrokenScalarChart(kind, terminal).ToInteractiveHtmlFragmentWithoutAssets();
        var match = Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]+)\"");
        Assert.True(match.Success);
        using var metadata = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
        var regions = metadata.RootElement.GetProperty("regions").EnumerateArray();
        var isolated = Assert.Single(regions, item => item.GetProperty("id").GetString() == "series-0-point-4");
        Assert.False(isolated.TryGetProperty("fillInteriors", out var witnesses) && witnesses.GetArrayLength() > 0);
        var painted = Assert.Single(regions, item => item.GetProperty("id").GetString() == "series-0-point-0");
        Assert.NotEmpty(painted.GetProperty("fillInteriors").EnumerateArray());
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Area, false, false)]
    [InlineData(ChartSeriesKind.StepArea, false, false)]
    [InlineData(ChartSeriesKind.StackedArea, false, false)]
    [InlineData(ChartSeriesKind.Area, true, false)]
    [InlineData(ChartSeriesKind.StepArea, true, false)]
    [InlineData(ChartSeriesKind.StackedArea, true, false)]
    [InlineData(ChartSeriesKind.Area, false, true)]
    [InlineData(ChartSeriesKind.StepArea, true, true)]
    [InlineData(ChartSeriesKind.StackedArea, false, true)]
    public async Task DisconnectedScalarObservationCannotBorrowAnotherSegmentsNativeFill(ChartSeriesKind kind, bool terminal, bool patternOnly) {
        if (!Enabled) return;
        var chart = BrokenScalarChart(kind, terminal);
        if (patternOnly) chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
        await AssertAvailabilityAsync(chart, 4, false,
            $"scalar-disconnected-{kind}-{terminal}-{patternOnly}", patternOnly, captureScalarInk: true);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Area)]
    [InlineData(ChartSeriesKind.StepArea)]
    [InlineData(ChartSeriesKind.StackedArea)]
    public async Task PaintedObservationAfterABreakKeepsItsOwnSegment(ChartSeriesKind kind) {
        if (!Enabled) return;
        await AssertAvailabilityAsync(BrokenScalarChart(kind, terminal: true), 5, true,
            $"scalar-disconnected-painted-{kind}", expectedReadout: "2");
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.RangeArea, false)]
    [InlineData(ChartSeriesKind.RangeBand, false)]
    [InlineData(ChartSeriesKind.DifferenceArea, false)]
    [InlineData(ChartSeriesKind.RangeArea, true)]
    [InlineData(ChartSeriesKind.RangeBand, true)]
    [InlineData(ChartSeriesKind.DifferenceArea, true)]
    public async Task DisconnectedPairedObservationCannotBorrowAnotherSegmentsNativeFill(ChartSeriesKind kind, bool patternOnly) {
        if (!Enabled) return;
        var chart = BrokenPairedChart(kind);
        if (patternOnly) chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
        await AssertAvailabilityAsync(chart, 4, false, $"paired-disconnected-{kind}-{patternOnly}", patternOnly);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.RangeArea)]
    [InlineData(ChartSeriesKind.RangeBand)]
    [InlineData(ChartSeriesKind.DifferenceArea)]
    public async Task PaintedPairedObservationAfterABreakKeepsItsOwnSegment(ChartSeriesKind kind) {
        if (!Enabled) return;
        await AssertAvailabilityAsync(BrokenPairedChart(kind), 5, true,
            $"paired-disconnected-painted-{kind}", expectedReadout: "2");
    }

    private static Chart BrokenPairedChart(ChartSeriesKind kind) {
        var ranges = new[] {
            new ChartRangeBand(0, 6, 8), new ChartRangeBand(.05, 6, 8), new ChartRangeBand(.1, 1, 2),
            new ChartRangeBand(2, 1, 2), new ChartRangeBand(.025, 6, 8), new ChartRangeBand(1, 1, 2), new ChartRangeBand(2, 1, 2)
        };
        var chart = kind == ChartSeriesKind.DifferenceArea
            ? Frame().AddDifferenceArea("Observed", ranges.Select(range => new ChartDifferencePoint(range.X, range.Upper, range.Lower)))
            : Range(kind, ranges);
        if (kind == ChartSeriesKind.DifferenceArea) {
            var point = chart.Series[0].DifferencePoints[4];
            chart.Series[0].UpdateDifferencePoint(4, new ChartDifferencePoint(point.X, point.First, point.Second, true));
        } else {
            var point = chart.Series[0].Ranges[4];
            chart.Series[0].UpdateRange(4, new ChartRangeBand(point.X, point.Lower, point.Upper, true));
        }
        Configure(chart, ChartStepPosition.Start);
        if (kind == ChartSeriesKind.DifferenceArea) chart.Series[0].WithColor(ChartColor.Transparent).ConfigureDifferenceArea(options => {
            options.PositiveFill = ChartColor.FromHex("#DF3040"); options.NegativeFill = ChartColor.FromHex("#305CDF");
            options.PositiveFillOpacity = .8; options.NegativeFillOpacity = .8;
        });
        return chart;
    }

    private static Chart BrokenScalarChart(ChartSeriesKind kind, bool terminal) {
        var points = new List<ChartPoint> {
            new(0, 8), new(.05, 8), new(.1, 2), new(2, 2), new(.025, 8, true)
        };
        if (terminal) { points.Add(new ChartPoint(1, 2)); points.Add(new ChartPoint(2, 2)); }
        var chart = Frame();
        if (kind == ChartSeriesKind.StepArea) chart.AddStepArea("Observed", points);
        else if (kind == ChartSeriesKind.StackedArea) chart.AddStackedArea("Observed", points);
        else chart.AddArea("Observed", points);
        Configure(chart, ChartStepPosition.Start);
        if (!terminal && kind != ChartSeriesKind.StepArea) chart.Series[0].WithInterpolation(ChartInterpolation.Linear);
        return chart;
    }
}
