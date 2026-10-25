using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StagePresentationOptionsTests {
    [Theory]
    [InlineData(false, ChartFunnelForm.StageBars, ChartOrientation.Vertical)]
    [InlineData(false, ChartFunnelForm.StageBars, ChartOrientation.Horizontal)]
    [InlineData(false, ChartFunnelForm.Cone, ChartOrientation.Vertical)]
    [InlineData(false, ChartFunnelForm.Cone, ChartOrientation.Horizontal)]
    [InlineData(true, ChartFunnelForm.StageBars, ChartOrientation.Vertical)]
    [InlineData(true, ChartFunnelForm.StageBars, ChartOrientation.Horizontal)]
    public void CombinedDefaultKeepsExactIncumbentExportsAndIgnoresSeparateOverrides(bool pyramid, ChartFunnelForm form, ChartOrientation orientation) {
        var chart = Create(pyramid, form, orientation);
        var first = Prepare(chart); var svg = first.ToSvg(); var png = first.ToPng();
        chart.ConfigureFunnel(options => {
            options.LabelContent = ChartStageLabelContent.Combined;
            options.CategoryLabel.Visible = false; options.ValueLabel.Placement = ChartDataLabelPlacement.Left;
            options.RetentionLabel.Style.FontSize = 28; options.DropOffLabel.Visible = false;
        }).ConfigurePyramid(options => {
            options.LabelContent = ChartStageLabelContent.Combined;
            options.CategoryLabel.Visible = false; options.ValueLabel.Placement = ChartDataLabelPlacement.Above;
        });
        var explicitDefault = Prepare(chart);
        Assert.Equal(svg, explicitDefault.ToSvg()); Assert.Equal(png, explicitDefault.ToPng());
        Assert.Equal(ChartStageLabelContent.Combined, chart.Options.Funnel.LabelContent);
        Assert.Equal(ChartStageLabelContent.Combined, chart.Options.Pyramid.LabelContent);
    }

    [Fact]
    public void ComponentTypographyIsCapturedAtomicallyAndPreparedExportsAreDetached() {
        var chart = Create(false, ChartFunnelForm.StageBars, ChartOrientation.Vertical);
        ChartValueLabelOptions? escaped = null;
        chart.ConfigureFunnel(options => { options.LabelContent = ChartStageLabelContent.Separate; escaped = options.CategoryLabel; options.CategoryLabel.Style.FontSize = 13; });
        escaped!.Visible = false; escaped.Style.FontSize = 60;
        Assert.True(chart.Options.Funnel.CategoryLabel.Visible); Assert.Equal(13, chart.Options.Funnel.CategoryLabel.Style.FontSize);
        var before = chart.Options.Funnel;
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ConfigureFunnel(options => { options.CategoryLabel.Visible = false; options.StageOutlineDashOffset = double.NaN; }));
        Assert.Same(before, chart.Options.Funnel); Assert.True(before.CategoryLabel.Visible);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.Funnel.ValueLabel.Visible = false; chart.Options.Funnel.CategoryLabel.Style.FontSize = 36;
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        chart = Create(true, ChartFunnelForm.StageBars, ChartOrientation.Vertical);
        chart.ConfigurePyramid(options => { escaped = options.ValueLabel; options.ValueLabel.Style.FontSize = 12; });
        escaped!.Style.FontSize = 72;
        Assert.Equal(12, chart.Options.Pyramid.ValueLabel.Style.FontSize);
    }

    [Fact]
    public void SelectorsAndConnectorPoliciesRejectInvalidValuesWithoutWideningOtherFamilies() {
        var chart = Create(false, ChartFunnelForm.Cone, ChartOrientation.Vertical);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Funnel.LabelContent = (ChartStageLabelContent)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Pyramid.LabelContent = (ChartStageLabelContent)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Pyramid.PartitionOutlineDashOffset = double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFunnelConnectorStyle(fillOpacity: -.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFunnelConnectorStyle(fillPattern: (ChartFillPattern)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFunnelConnectorStyle(strokeDashOffset: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Series[0].WithFunnelConnectorStyle(3, new()));
        var line = ChartSeries.CreateLine(new ChartSeriesId("line"), "Line", new[] { new ChartPoint(1, 1) });
        Assert.Throws<NotSupportedException>(() => line.WithFunnelConnectorStyle(0, new()));
        Assert.Throws<NotSupportedException>(() => line.WithPointStrokeColor(0, ChartColor.Black));
    }

    internal static Chart Create(bool pyramid, ChartFunnelForm form, ChartOrientation orientation, int width = 800, int height = 520) {
        var chart = Chart.Create().WithSize(width, height).WithHeader(false).WithLegend(false).WithDataLabels().WithXLabels("Intake", "Review", "Complete");
        var points = new[] { new ChartPoint(1, 100), new ChartPoint(2, 75), new ChartPoint(3, 25) };
        chart.AddSeries(new ChartSeries(new ChartSeriesId("stages"), "Stages", pyramid ? ChartSeriesKind.Pyramid : ChartSeriesKind.Funnel, points)
            .WithObservationIds(new[] { new ChartObservationId("intake"), new ChartObservationId("review"), new ChartObservationId("complete") }));
        chart.ConfigureFunnel(options => { options.Form = form; options.Orientation = orientation; });
        chart.ConfigurePyramid(options => options.Orientation = orientation);
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        return chart;
    }
    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
