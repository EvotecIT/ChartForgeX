using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>HTML transports completed native polar facts without mislabeling them as physical Cartesian axes.</summary>
public sealed class PolarViewportHtmlMetadataTests {
    [Fact]
    public void AdaptiveDrawingsCaptureTheSameIndependentDomainsAndDetachedPolarPolicy() {
        var chart = PolarViewportContractTests.Create().WithSecondaryYAxisBounds(1, 1000)
            .AddPolar("Secondary", new[] { new ChartPoint(0, 1), new ChartPoint(1, 10), new ChartPoint(2, 1000) });
        chart.Series[1].UseSecondaryValueAxis(); chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
        chart.Options.SecondaryYAxis.Reversed = true; chart.Options.XAxis.Reversed = true; chart.Options.PolarRotationDegrees = 25;
        var request = new ChartPolarViewport(new ChartAxisWindow(5.5, 6.8), new ChartAxisWindow(20, 80, false), new ChartAxisWindow(10, 100));
        var section = new HtmlInteractiveChartRenderer().PreparePolarSection(chart, request, options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 360; options.Adaptive.NarrowHeight = 340;
        });
        Assert.NotNull(section.NarrowPreparedVisual); Assert.NotEqual(section.PreparedVisual.Size.Width, section.NarrowPreparedVisual!.Size.Width);
        foreach (var drawing in new[] { section.PreparedVisual, section.NarrowPreparedVisual! }) {
            Assert.Same(request, drawing.PolarViewport); Assert.Null(drawing.XProjection); Assert.Null(drawing.YProjection);
            Assert.Equal(20, drawing.PolarProjection!.GetRadiusWindow().Minimum); Assert.False(drawing.PolarProjection.GetRadiusWindow().MaximumInclusive);
            Assert.Equal(10, drawing.PolarProjection.GetRadiusWindow(ChartAxisSide.Secondary).Minimum);
            Assert.Equal(.25 * drawing.PolarProjection.OuterRadius, drawing.PolarProjection.GetInnerRadius(), 8);
            Assert.Equal(.5 * drawing.PolarProjection.OuterRadius, drawing.PolarProjection.GetInnerRadius(ChartAxisSide.Secondary), 8);
        }
        var matches = Regex.Matches(section.Html, "data-cfx-prepared-chart=\"([^\"]+)\""); Assert.Equal(2, matches.Count);
        foreach (Match match in matches) {
            using var document = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value)); var metadata = document.RootElement;
            Assert.False(metadata.TryGetProperty("xProjection", out _)); Assert.False(metadata.TryGetProperty("yProjection", out _));
            var polar = metadata.GetProperty("polarProjection"); Assert.True(polar.GetProperty("angleReversed").GetBoolean());
            Assert.Equal(5.5, polar.GetProperty("angle").GetProperty("minimum").GetDouble());
            Assert.False(polar.GetProperty("radius").GetProperty("maximumInclusive").GetBoolean());
            Assert.Equal("logarithmic", polar.GetProperty("secondaryRadius").GetProperty("scale").GetString());
            Assert.True(polar.GetProperty("secondaryRadius").GetProperty("reversed").GetBoolean());
            Assert.Equal(1, polar.GetProperty("secondaryRadius").GetProperty("fullMinimum").GetDouble());
            Assert.Equal(.25, polar.GetProperty("radius").GetProperty("innerRadius").GetDouble() / polar.GetProperty("outerRadius").GetDouble(), 8);
            Assert.Equal(.5, polar.GetProperty("secondaryRadius").GetProperty("innerRadius").GetDouble() / polar.GetProperty("outerRadius").GetDouble(), 8);
        }
        Assert.Null(chart.Options.YAxis.PreparedViewportWindow); Assert.Null(chart.Options.SecondaryYAxis.PreparedViewportWindow);
        chart.Options.PolarRotationDegrees = 180; chart.Options.SecondaryYAxis.Reversed = false;
        Assert.True(section.PreparedVisual.PolarProjection!.AngleReversed);
        Assert.Equal(25 * Math.PI / 180, section.PreparedVisual.PolarProjection.RotationRadians);
    }
}
