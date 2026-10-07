using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects role-aware chart paints at the exported artifact boundary.</summary>
public sealed class V2ChartPaintPolicyTests {
    private static readonly ChartColor Shared = ChartColor.FromHex("#2864B4");
    private static readonly ChartPoint[] Points = { new(1, 35), new(2, 75) };

    [Theory]
    [InlineData(ChartSeriesKind.Line, "line", "stroke")]
    [InlineData(ChartSeriesKind.StepLine, "line", "stroke")]
    [InlineData(ChartSeriesKind.Area, "area", "fill")]
    [InlineData(ChartSeriesKind.StepArea, "area", "fill")]
    [InlineData(ChartSeriesKind.StackedArea, "area", "fill")]
    [InlineData(ChartSeriesKind.Bar, "bar", "fill")]
    [InlineData(ChartSeriesKind.Scatter, "marker", "fill")]
    [InlineData(ChartSeriesKind.Pie, "pie-slice", "fill")]
    [InlineData(ChartSeriesKind.Donut, "donut-slice", "fill")]
    [InlineData(ChartSeriesKind.Gauge, "gauge-value", "fill")]
    [InlineData(ChartSeriesKind.RadialBar, "radial-bar-ring", "fill")]
    [InlineData(ChartSeriesKind.LayeredRadial, "layered-radial-layer", "fill")]
    public void ChartMarksAndLegendsRetainStatusOrExplicitSeriesProvenance(ChartSeriesKind kind, string role, string attribute) {
        var chart = Fixture(kind); chart.Series[0].StateRole = ChartSeriesState.Danger;
        chart.Options.SvgColorVariables = Variables();
        var state = XDocument.Parse(chart.Prepare(Context()).ToSvg());
        Assert.All(Paints(state, role, attribute), paint => Assert.Contains("var(--status,", paint));
        Assert.Contains("var(--status,", Paints(state, "legend-swatch", kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine ? "stroke" : "fill").First());
        chart.Series[0].Color = Shared;
        var explicitSeries = chart.Prepare(Context());
        Assert.All(Paints(XDocument.Parse(explicitSeries.ToSvg()), role, attribute), paint => Assert.Contains("var(--series,", paint));
        Assert.DoesNotContain("var(--status,", explicitSeries.ToSvg());
    }

    [Fact]
    public void GridOpacityMultipliesAuthoredAlphaForNativeAndSvgPaints() {
        var border = ChartColor.FromRgba(40, 100, 180, 96);
        var tokens = new VisualDesignTokens { Border = border };
        var context = new VisualRenderContext(theme: new VisualTheme(tokens, tokens));
        var chart = Fixture(ChartSeriesKind.Line)
            .WithGridStyle(new ChartGridLineStyle().WithHorizontalOpacity(0.5).WithVerticalOpacity(0.25));
        var prepared = chart.Prepare(context);
        var literal = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions()));
        foreach (var role in new[] { "grid-x", "grid-y" }) {
            var alpha = role == "grid-x" ? 24 : 48;
            var marks = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(mark => mark.Role == role).ToArray();
            Assert.NotEmpty(marks);
            Assert.All(marks, mark => Assert.Equal(alpha, mark.Stroke!.Value.A));
            Assert.All(Paints(literal, role, "stroke"), paint => Assert.Equal(border.WithAlpha((byte)alpha).ToCss(), paint));
        }
        var mapped = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables:
            new SvgColorVariables().Add("--authored-grid", border, SvgColorRole.Grid))));
        Assert.All(Paints(mapped, "grid-y", "stroke"), paint => {
            Assert.Contains("var(--authored-grid,", paint);
            Assert.Contains("50%", paint);
        });
    }

    [Fact]
    public void FrameAxesGridAndExplicitPointColorsSeparateEqualRgbRolesWithoutChangingPixels() {
        var chart = Fixture(ChartSeriesKind.Scatter).WithTitle("Observation");
        chart.Series[0].StateRole = ChartSeriesState.Danger;
        chart.Series[0].WithPointColor(0, Shared);
        chart.Options.SvgColorVariables = Variables();
        var context = Context(); var prepared = chart.Prepare(context);
        var mapped = XDocument.Parse(prepared.ToSvg());
        Assert.StartsWith("var(--surface,", Paints(mapped, "background", "fill").Single());
        Assert.StartsWith("var(--surface,", Paints(mapped, "content-surface", "fill").Single());
        Assert.StartsWith("var(--text,", Paints(mapped, "frame-heading", "fill").Single());
        Assert.All(Paints(mapped, "grid-y", "stroke"), paint => Assert.Contains("var(--grid,", paint));
        Assert.All(Paints(mapped, "axis-x", "stroke"), paint => Assert.Contains("var(--axis,", paint));
        var markers = Paints(mapped, "marker", "fill");
        Assert.Contains("var(--series,", markers[0]); Assert.Contains("var(--status,", markers[1]);
        var pixels = prepared.ToRgba(new VisualRenderOptions(supersampling: 1)).Pixels;
        chart.Options.SvgColorVariables = null;
        Assert.Equal(pixels, chart.Prepare(context).ToRgba(new VisualRenderOptions(supersampling: 1)).Pixels);
        Assert.DoesNotContain("var(--", prepared.ToSvg(new VisualSvgOptions()));
    }

    [Fact]
    public void OpaqueBarGradientFollowsSourceRoleWhileDerivedSheenStaysLiteral() {
        var chart = Fixture(ChartSeriesKind.Bar).WithBarStyle(ChartBarStyle.Solid);
        chart.Series[0].StateRole = ChartSeriesState.Danger;
        var variables = Variables().Add("--white-surface", ChartColor.White, SvgColorRole.Surface);
        var prepared = chart.Prepare(Context()); var mapped = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: variables)));
        var stops = mapped.Descendants().Where(element => element.Name.LocalName == "stop").Select(element => element.Attribute("stop-color")!.Value).ToArray();
        Assert.NotEmpty(stops); Assert.All(stops, paint => { Assert.Contains("color-mix(in srgb,", paint); Assert.Contains("var(--status,", paint); });
        Assert.All(Paints(mapped, "bar-highlight", "stroke"), paint => Assert.DoesNotContain("var(", paint));
        chart.Series[0].Color = Shared.WithAlpha(128);
        var translucent = chart.Prepare(Context());
        var staticStops = XDocument.Parse(translucent.ToSvg()).Descendants().Where(element => element.Name.LocalName == "stop").Select(element => element.Attribute("stop-color")!.Value);
        var dynamicStops = XDocument.Parse(translucent.ToSvg(new VisualSvgOptions(colorVariables: variables))).Descendants().Where(element => element.Name.LocalName == "stop").Select(element => element.Attribute("stop-color")!.Value);
        Assert.Equal(staticStops, dynamicStops);
    }

    [Fact]
    public void LayerOpacityRetainsExplicitAlphaAndPieContrastUsesTheConfiguredInk() {
        var alpha = Shared.WithAlpha(128);
        var layer = new ChartRadialLayer("Observed", 70, color: alpha) { Opacity = .5 };
        var chart = Chart.Create().AddLayeredRadial("Observed", new[] { layer });
        chart.Series[0].StateRole = ChartSeriesState.Danger;
        var variables = new SvgColorVariables().Add("--alpha", alpha, SvgColorRole.Series).Add("--status", Shared, SvgColorRole.Status);
        var mapped = chart.Prepare(Context()).ToSvg(new VisualSvgOptions(colorVariables: variables));
        Assert.Contains("color-mix(in srgb, var(--alpha, #2864B480) 50%, transparent)", mapped);
        var pie = Fixture(ChartSeriesKind.Pie).WithDataLabels();
        pie.Series[0].Color = Shared;
        variables = Variables().AddInk("--ink", Shared, ChartColor.White, SvgColorRole.Series);
        var ink = XDocument.Parse(pie.Prepare(Context()).ToSvg(new VisualSvgOptions(colorVariables: variables)));
        Assert.All(Paints(ink, "data-label", "fill"), paint => Assert.StartsWith("var(--ink,", paint));
        pie.Options.DataLabelStyle.Color = Shared;
        var overridden = XDocument.Parse(pie.Prepare(Context()).ToSvg(new VisualSvgOptions(colorVariables: variables)));
        Assert.All(Paints(overridden, "data-label", "fill"), paint => Assert.StartsWith("var(--text,", paint));
    }

    private static Chart Fixture(ChartSeriesKind kind) {
        var chart = kind switch {
            ChartSeriesKind.Line => Chart.Create().AddLine("Observed", Points),
            ChartSeriesKind.StepLine => Chart.Create().AddStepLine("Observed", Points),
            ChartSeriesKind.Area => Chart.Create().AddArea("Observed", Points),
            ChartSeriesKind.StepArea => Chart.Create().AddStepArea("Observed", Points),
            ChartSeriesKind.StackedArea => Chart.Create().AddStackedArea("Observed", Points),
            ChartSeriesKind.Bar => Chart.Create().AddBar("Observed", Points).WithBarStyle(ChartBarStyle.Flat),
            ChartSeriesKind.Scatter => Chart.Create().AddScatter("Observed", Points),
            ChartSeriesKind.Pie => Chart.Create().AddPie("Observed", Points),
            ChartSeriesKind.Donut => Chart.Create().AddDonut("Observed", Points),
            ChartSeriesKind.Gauge => Chart.Create().AddGauge("Observed", 35),
            ChartSeriesKind.RadialBar => Chart.Create().AddRadialBar("Observed", Points),
            ChartSeriesKind.LayeredRadial => Chart.Create().AddLayeredRadial("Observed", new[] { new ChartRadialLayer("A", 35), new ChartRadialLayer("B", 75) }),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        chart.Options.ShowLegend = true;
        return chart;
    }

    private static VisualRenderContext Context() {
        var tokens = new VisualDesignTokens { Background = Shared, Surface = Shared, Foreground = Shared, MutedForeground = Shared, Border = Shared, Palette = new[] { Shared } };
        tokens.Status.Critical = new VisualTokenColor(Shared, ChartColor.White);
        return new VisualRenderContext(theme: new VisualTheme(tokens, tokens), frame: new VisualFrame(showSurface: true));
    }

    private static SvgColorVariables Variables() => new SvgColorVariables().Add("--surface", Shared, SvgColorRole.Surface)
        .Add("--status", Shared, SvgColorRole.Status).Add("--series", Shared, SvgColorRole.Series)
        .Add("--axis", Shared, SvgColorRole.Axis).Add("--grid", Shared, SvgColorRole.Grid).Add("--text", Shared, SvgColorRole.Text);

    private static string[] Paints(XDocument document, string role, string attribute) {
        var paints = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role)
            .Select(element => element.Attribute(attribute) ?? element.Descendants().Attributes(attribute).FirstOrDefault())
            .Where(paint => paint != null).Select(paint => paint!.Value).ToArray();
        Assert.NotEmpty(paints); return paints;
    }
}
