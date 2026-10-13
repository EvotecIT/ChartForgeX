using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects authored pair identities, supported coordinate policy and detached exports.</summary>
public sealed partial class DifferenceAreaTests {
    [Theory]
    [InlineData(ChartInterpolation.Linear)]
    [InlineData(ChartInterpolation.Step)]
    public void CrossingPairsKeepBothNamesValuesBreaksAndLogicalObservationIndices(ChartInterpolation interpolation) {
        var chart = Model(new(0, 8, 2), new(1, 2, 5), new(3, -4, -4, true));
        chart.Series[0].WithInterpolation(interpolation, interpolation == ChartInterpolation.Step ? ChartStepPosition.Middle : ChartStepPosition.End);
        chart.Series[0].ConfigureDifferenceArea(options => { options.FirstName = "Actual"; options.SecondName = "Forecast"; });
        chart.Series[0].WithPointColor(2, Red);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Series[0].WithPointColor(3, Red));
        var prepared = Prepare(chart);
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "point"));
        var facts = Group(prepared, "series-0-point-1");
        Assert.Equal("2", facts["data-cfx-first"]); Assert.Equal("5", facts["data-cfx-second"]);
        Assert.Equal("Actual", facts["data-cfx-label-first"]); Assert.Equal("Forecast", facts["data-cfx-label-second"]);
        Assert.Equal("2", facts["data-cfx-source-count"]);
        Assert.Equal("2", facts["data-cfx-source-0-index"]); Assert.Equal("3", facts["data-cfx-source-1-index"]);
        Assert.Contains("Actual: 2; Forecast: 5", facts["aria-label"]);
        Assert.Equal("true", Group(prepared, "series-0-point-2")["data-cfx-source-1-break"]);
        var markers = prepared.Scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role == "difference-marker").ToArray();
        Assert.Equal(2, markers.Length); Assert.All(markers, marker => Assert.Equal(Red, marker.Fill));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Series[0].WithInterpolation(ChartInterpolation.Linear);
        chart.Series[0].Points.Clear();
        chart.Series[0].DifferenceArea.FirstName = "Changed"; chart.Series[0].DifferenceArea.NegativeFill = Red;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData("incomplete")]
    [InlineData("unequal-x")]
    [InlineData("duplicate-x")]
    [InlineData("backtracking")]
    [InlineData("log-x")]
    [InlineData("log-y")]
    [InlineData("secondary-log-y")]
    [InlineData("sym-log-x")]
    [InlineData("smooth")]
    public void UnsupportedOrAmbiguousMutablePairsFailAtPreparation(string invalid) {
        var chart = Model(new(0, 8, 2), new(1, 2, 5)); var series = chart.Series[0];
        switch (invalid) {
            case "incomplete": series.Points.RemoveAt(3); break;
            case "unequal-x": series.Points[1] = new ChartPoint(.1, 2); break;
            case "duplicate-x": series.Points[2] = new ChartPoint(0, 2); series.Points[3] = new ChartPoint(0, 5); break;
            case "backtracking": series.Points[2] = new ChartPoint(-1, 2); series.Points[3] = new ChartPoint(-1, 5); break;
            case "log-x": chart.Options.XAxis.Scale = ChartScaleKind.Logarithmic; break;
            case "log-y": chart.Options.YAxis.Scale = ChartScaleKind.Logarithmic; break;
            case "secondary-log-y": series.UseSecondaryYAxis(); chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic; break;
            case "sym-log-x": chart.Options.XAxis.Scale = ChartScaleKind.SymmetricLogarithmic; break;
            case "smooth": series.Interpolation = ChartInterpolation.Smooth; break;
        }
        var error = Record.Exception(() => Prepare(chart));
        Assert.True(error is InvalidOperationException or NotSupportedException, error?.ToString());
    }

    [Fact]
    public void TypedFiniteInputAndFamilyOptionsKeepRangeIntervalValidationSeparate() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartDifferencePoint(0, double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartDifferencePoint(0, 1, double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddDifferenceArea("Empty", Array.Empty<ChartDifferencePoint>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRangeBand(0, 8, 2));
        var range = Chart.Create().AddRangeArea("Interval", new[] { new ChartRangeBand(0, 2, 8) });
        range.Series[0].Points[0] = new ChartPoint(0, 9);
        Assert.Throws<InvalidOperationException>(() => Prepare(range));
        var line = Chart.Create().AddLine("Line", new[] { new ChartPoint(0, 1) });
        line.Series[0].DifferenceArea.FirstName = "Actual";
        Assert.Throws<InvalidOperationException>(() => Prepare(line));
        var options = new ChartDifferenceAreaOptions();
        Assert.Throws<ArgumentException>(() => options.FirstName = " ");
        Assert.Throws<ArgumentOutOfRangeException>(() => options.NegativeFillOpacity = 1.1);
        var difference = Model(new ChartDifferencePoint(0, 8, 2));
        Assert.Throws<InvalidOperationException>(() => difference.Series[0].WithStackGroup("Pair"));
        Assert.Throws<InvalidOperationException>(() => difference.Series[0].WithNormalization());
        var date = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(date.ToOADate(), new ChartDifferencePoint(date.ToLocalTime(), 1, 2).X);
    }

    [Theory]
    [InlineData("wide", false)]
    [InlineData("compact", true)]
    [InlineData("dated-gaps", false)]
    [InlineData("compact-dated-gaps", true)]
    public void GalleryFactoriesUseRealPairedModelsAndIndependentDatedScales(string variant, bool compact) {
        var chart = V2GalleryModels.Create(ChartSeriesKind.DifferenceArea, variant);
        chart.WithSize(compact ? 360 : 800, compact ? 360 : 440);
        var series = Assert.Single(chart.Series, series => series.Kind == ChartSeriesKind.DifferenceArea);
        Assert.Equal("Actual", series.DifferenceArea.FirstName);
        var prepared = Prepare(chart);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "difference-positive");
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "difference-negative");
        if (variant.Contains("dated", StringComparison.Ordinal)) {
            Assert.Equal(ChartScaleKind.Time, chart.Options.XAxis.Scale);
            Assert.Equal(ChartAxisSide.Secondary, series.YAxis);
            Assert.Contains(series.Points, point => point.BreakBefore);
        }
    }

    private static readonly ChartColor Red = ChartColor.FromHex("#DF3040");
    private static readonly ChartColor Blue = ChartColor.FromHex("#305CDF");
    private static Chart Model(params ChartDifferencePoint[] pairs) {
        var chart = Chart.Create().WithSize(600, 420).WithHeader(false).WithAxes(false).WithGrid(false).WithLegend(false)
            .WithDataLabels(false).WithLineVisualStyle(ChartLineVisualStyle.Plain()).AddDifferenceArea("Comparison", pairs);
        chart.Series[0].WithStrokeWidth(1).ConfigureDifferenceArea(options => {
            options.PositiveFill = Red; options.NegativeFill = Blue;
            options.PositiveFillOpacity = .5; options.NegativeFillOpacity = .5;
        });
        return chart;
    }
    private static PreparedVisual Prepare(Chart chart) {
        var context = VisualExportRequest.ForChart(chart).Context;
        return chart.Prepare(new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode,
            new VisualFrame(showLegend: chart.Options.ShowLegend, showSurface: false, transparentBackground: true), context.Font));
    }
    private static IReadOnlyDictionary<string, string> Group(PreparedVisual prepared, string id) =>
        prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == id).Metadata;
}
