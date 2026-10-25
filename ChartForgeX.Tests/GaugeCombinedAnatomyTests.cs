using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GaugeCombinedAnatomyTests {
    [Theory]
    [InlineData(390, false)]
    [InlineData(760, true)]
    public void CombinedReadingSharesOneClampedAngleAndRetainsStyledRawBands(int width, bool dark) {
        var chart = CombinedGaugeRecipe.Create(width == 390 ? "compact" : "wide", dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "gauge-track");
        var value = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "gauge-value");
        var needle = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "gauge-needle");
        Assert.Equal(track.Sweep, value.Sweep, 9);
        Assert.True(needle.End.X > needle.Start.X); Assert.Equal(needle.Start.Y, needle.End.Y, 8);
        Assert.Equal(ChartColor.FromHex("#9D3C20"), needle.Stroke);
        var bands = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(node => node.Role == "gauge-band").ToArray();
        Assert.Equal(3, bands.Length); Assert.Equal((byte)204, bands[0].Fill!.Value.A);
        Assert.True(bands[0].Start > track.Start); Assert.True(bands[0].Start + bands[0].Sweep < bands[1].Start);
        var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "gauge");
        Assert.Equal("3.25", group.Metadata["data-cfx-value"]); Assert.Equal("3", group.Metadata["data-cfx-max"]);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        chart.Series[0].UpdateGaugeValue(0, new ChartGaugeValue(2.1, 2, 3)); chart.Options.Gauge.Bands.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        ScalarDepthCapture.Write(prepared, "combined-gauge-" + width + "-" + dark);
    }

    [Fact]
    public void BandConfigurationIsDetachedAtomicAndDescriptiveInterchangeKeepsStyleFacts() {
        var chart = CombinedGaugeRecipe.Create(); ChartGaugeOptions? captured = null;
        chart.ConfigureGauge(options => captured = options); captured!.Bands.Clear(); captured.BandGap = 30;
        Assert.Equal(3, chart.Options.Gauge.Bands.Count); Assert.Equal(4, chart.Options.Gauge.BandGap);
        Assert.Throws<InvalidOperationException>(() => chart.ConfigureGauge(options => { options.Bands.Clear(); throw new InvalidOperationException(); }));
        Assert.Equal(3, chart.Options.Gauge.Bands.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartGaugeBand(0, 1, ChartSeriesState.Success, opacity: double.NaN));
        var envelope = chart.ToVisualArtifact("gauge").ToInterchangeEnvelope(); var json = envelope.ToJson();
        Assert.Contains("ArcWithNeedle", envelope.Extensions["chart.gauge.options"], StringComparison.Ordinal);
        Assert.Contains("opacity", envelope.Extensions["chart.gauge.options"], StringComparison.Ordinal);
        chart.Options.Gauge.Bands.Clear(); Assert.Equal(json, envelope.ToJson());
        Assert.Null(envelope.Waterfall); // Descriptive source facts do not fabricate editable scalar data.
    }
}

internal static class ScalarDepthCapture {
    internal static void Write(PreparedVisual prepared, string stem) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, stem + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, stem + ".native.png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}
