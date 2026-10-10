using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the readable scalar hierarchy and independent scale rows of shallow linear gauges.</summary>
public sealed class V2LinearGaugeLayoutTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void FramedLinearGaugeMeasuresItsSummaryAboveThePointerAndKeepsScaleRowsSeparate(bool dark, bool carlito) {
        var chart = Chart.Create().WithSize(596, 230).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Linear readiness").WithSubtitle("Explicit target and bands").AddLinearGauge("Readiness", 87)
            .ConfigureGauge(options => {
                options.Target = 90;
                options.Bands.Add(new ChartGaugeBand(0, 60, ChartSeriesState.Danger));
                options.Bands.Add(new ChartGaugeBand(60, 80, ChartSeriesState.Warning));
                options.Bands.Add(new ChartGaugeBand(80, 100, ChartSeriesState.Quiet));
            });
        if (carlito) {
            var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
            Assert.True(File.Exists(font), "The gallery's resolved Carlito face must be available.");
            chart.WithPngFont(font);
        }
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(context);
        var value = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "gauge-label");
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "gauge-title");
        Assert.Equal("87", Assert.Single(value.Text.Lines).Text);
        Assert.Equal(context.Theme.Typography.ScalarValueSize, value.Text.Size);
        Assert.Equal("Readiness", Assert.Single(caption.Text.Lines).Text);
        Assert.Equal(context.Theme.Resolve(context.ThemeMode).MutedForeground, caption.Color);
        var valueBounds = Assert.Single(prepared.Regions, region => region.Role == "gauge-label").Bounds;
        var captionBounds = Assert.Single(prepared.Regions, region => region.Role == "gauge-title").Bounds;
        var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "gauge-track");
        var target = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "gauge-target");
        var axis = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "gauge-axis");
        Assert.True(value.Text.Metrics.Height <= valueBounds.Height + .000001);
        Assert.True(valueBounds.Right <= captionBounds.Left);
        Assert.True(valueBounds.Bottom <= track.Bounds.Top - track.Bounds.Height / 2);
        Assert.True(captionBounds.Bottom <= track.Bounds.Top - track.Bounds.Height / 2);
        Assert.True(target.End.Y < axis.Start.Y);
        Assert.Equal(5, prepared.Scene.Nodes.Count(node => node.Role == "gauge-tick"));
        Assert.Equal("90", Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "gauge-target-label").Text.Lines[0].Text);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
        Assert.Contains("data-cfx-role=\"gauge-label\"", prepared.ToSvg(), StringComparison.Ordinal);
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void HeightConstrainedLinearGaugeRetainsItsMeasurementInsteadOfPaintingAMicroscopicDefault() {
        var chart = Chart.Create().AddLinearGauge("Readiness", 87).ConfigureGauge(options => options.Target = 90);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(180, 58), 0));
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        VisualGaugeCompiler.Build(chart, context, builder, new ChartRect(0, 0, 180, 58));
        var scene = builder.Build();
        Assert.Contains(scene.Nodes, node => node.Role == "gauge-track");
        Assert.Contains(scene.Nodes, node => node.Role == "gauge-target");
        Assert.Contains(scene.Regions, region => region.Role == "gauge-label" && region.Label == "87");
        Assert.DoesNotContain(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "gauge-label");
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
        var source = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gauge");
        Assert.Equal("87", source.Metadata["data-cfx-value"]);
    }

    [Fact]
    public void LinearSummaryKeepsAuthoredLabelsAndExplicitFontSizesAuthoritative() {
        var chart = Chart.Create().WithSize(596, 230).AddLinearGauge("Readiness", 87).ConfigureGauge(options => options.Target = 90);
        chart.Series[0].WithPointLabel(0, "Observed 87");
        chart.Series[0].DataLabelStyle.FontSize = 9;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var value = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "gauge-label");
        Assert.Equal(9, value.Text.Size);
        Assert.Equal("Observed 87", Assert.Single(value.Text.Lines).Text);
        Assert.Contains(prepared.Regions, region => region.Role == "gauge-label" && region.Label == "Observed 87");
        var source = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gauge");
        Assert.Equal("87", source.Metadata["data-cfx-value"]);
    }
}
