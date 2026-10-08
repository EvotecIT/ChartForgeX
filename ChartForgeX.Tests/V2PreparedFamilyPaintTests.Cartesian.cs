using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class V2PreparedFamilyPaintTests {
    [Theory]
    [InlineData(ChartSeriesKind.Bubble, "bubble", "fill")]
    [InlineData(ChartSeriesKind.ErrorBar, "error-range", "stroke")]
    [InlineData(ChartSeriesKind.Candlestick, "candlestick-body", "fill")]
    [InlineData(ChartSeriesKind.Ohlc, "ohlc-stem", "stroke")]
    [InlineData(ChartSeriesKind.BoxPlot, "boxplot-body", "fill")]
    [InlineData(ChartSeriesKind.RangeBand, "range-band", "fill")]
    [InlineData(ChartSeriesKind.RangeArea, "range-midline", "stroke")]
    [InlineData(ChartSeriesKind.RangeBar, "range-bar-cap", "stroke")]
    [InlineData(ChartSeriesKind.Lollipop, "lollipop-stem", "stroke")]
    [InlineData(ChartSeriesKind.Dumbbell, "dumbbell-connector", "stroke")]
    [InlineData(ChartSeriesKind.HorizontalBar, "horizontal-bar", "fill")]
    [InlineData(ChartSeriesKind.Waterfall, "waterfall-bar", "fill")]
    [InlineData(ChartSeriesKind.Slope, "slope-line", "stroke")]
    [InlineData(ChartSeriesKind.TrendLine, "trend-line", "stroke")]
    public void CartesianExtensionMarksRetainStateAndAuthoredSeriesProvenance(ChartSeriesKind kind, string role, string attribute) {
        var chart = V2GalleryModels.Create(kind).WithBarStyle(ChartBarStyle.Flat);
        foreach (var series in chart.Series) series.StateRole = ChartSeriesState.Danger;
        Assert.All(Paints(chart.Prepare(Context()), Variables(), role, attribute), paint => Assert.Contains("var(--status,", paint));
        foreach (var series in chart.Series) series.Color = Shared;
        Assert.All(Paints(chart.Prepare(Context()), Variables(), role, attribute), paint => Assert.Contains("var(--series,", paint));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick, "candlestick-body", "stroke")]
    [InlineData(ChartSeriesKind.Ohlc, "ohlc-stem", "stroke")]
    [InlineData(ChartSeriesKind.Waterfall, "waterfall-bar", "fill")]
    public void FinancialDefaultsUseStatusAndExplicitPointsUseSeriesPaint(ChartSeriesKind kind, string role, string attribute) {
        var chart = V2GalleryModels.Create(kind).WithBarStyle(ChartBarStyle.Flat);
        Assert.All(Paints(chart.Prepare(Context()), Variables(), role, attribute), paint => Assert.Contains("var(--status,", paint));
        chart.Series[0].WithPointColor(0, Shared);
        var paints = Paints(chart.Prepare(Context()), Variables(), role, attribute);
        Assert.Contains("var(--series,", paints[0]);
        Assert.All(paints.Skip(1), paint => Assert.Contains("var(--status,", paint));
    }

    [Fact]
    public void BubblePointAlphaAndNativePixelsRemainIndependentOfSvgPolicy() {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Bubble); chart.Series[0].StateRole = ChartSeriesState.Danger;
        chart.Series[0].WithPointColor(0, Shared.WithAlpha(128));
        var prepared = chart.Prepare(Context()); var variables = Variables();
        var marks = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "bubble").ToArray();
        Assert.Equal(ChartColorMath.WithOpacity(Shared.WithAlpha(128), ChartVisualPrimitives.BubbleFillOpacity), marks[0].Fill!.Value);
        Assert.Equal(ChartColorMath.WithOpacity(Shared.WithAlpha(128), ChartVisualPrimitives.BubbleStrokeOpacity), marks[0].Stroke!.Value);
        Assert.Equal(marks[0].Fill!.Value.ToCss(), Paints(prepared, new SvgColorVariables(), "bubble", "fill")[0]);
        Assert.Contains("var(--series,", Paints(prepared, variables, "bubble", "fill")[0]);
        Assert.Contains("color-mix(in srgb,", Paints(prepared, variables, "bubble", "stroke")[0]);
        Assert.All(Paints(prepared, variables, "bubble", "fill").Skip(1), paint => Assert.Contains("var(--status,", paint));
        var pixels = prepared.ToRgba(new VisualRenderOptions(supersampling: 1)).Pixels;
        prepared.ToSvg(new VisualSvgOptions(colorVariables: variables));
        Assert.Equal(pixels, prepared.ToRgba(new VisualRenderOptions(supersampling: 1)).Pixels);
        chart.Options.SvgColorVariables = variables;
        Assert.Equal(pixels, chart.Prepare(Context()).ToRgba(new VisualRenderOptions(supersampling: 1)).Pixels);
    }

    [Fact]
    public void RangeBoundariesRetainLayerOpacityWhileDerivedSheenStaysLiteral() {
        var chart = V2GalleryModels.Create(ChartSeriesKind.RangeArea).WithLineVisualStyle(ChartLineVisualStyle.Premium());
        chart.Series[0].StateRole = ChartSeriesState.Danger; chart.Series[0].WithPointColor(0, Shared);
        var prepared = chart.Prepare(Context()); var variables = Variables();
        foreach (var role in new[] { "range-area", "range-upper", "range-upper-halo", "range-lower", "range-midline" })
            Assert.All(Paints(prepared, variables, role, role == "range-area" ? "fill" : "stroke"), paint => Assert.Contains("var(--status,", paint));
        Assert.All(Paints(prepared, variables, "range-upper-highlight", "stroke"), paint => Assert.DoesNotContain("var(", paint));
        Assert.All(Paints(prepared, variables, "range-marker", "fill"), paint => Assert.Contains("var(--series,", paint));
    }

    [Fact]
    public void NeutralEndpointAndMarkerOutlineKeepTheirThemeRoles() {
        var lollipop = V2GalleryModels.Create(ChartSeriesKind.Lollipop); lollipop.Series[0].StateRole = ChartSeriesState.Danger;
        Assert.All(Paints(lollipop.Prepare(Context()), Variables(), "lollipop-marker", "fill"), paint => Assert.Contains("var(--status,", paint));
        Assert.All(Paints(lollipop.Prepare(Context()), Variables(), "lollipop-marker", "stroke"), paint => Assert.Contains("var(--surface,", paint));
        var dumbbell = V2GalleryModels.Create(ChartSeriesKind.Dumbbell); dumbbell.Series[0].StateRole = ChartSeriesState.Danger;
        Assert.All(Paints(dumbbell.Prepare(Context()), Variables(), "dumbbell-start", "fill"), paint => Assert.Contains("var(--text,", paint));
        Assert.All(Paints(dumbbell.Prepare(Context()), Variables(), "dumbbell-end", "fill"), paint => Assert.Contains("var(--status,", paint));
    }

    [Fact]
    public void WaterfallGradientAndSegmentedRangeCapsKeepTheirSourceRoles() {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Waterfall).WithBarStyle(ChartBarStyle.Solid);
        var document = XDocument.Parse(chart.Prepare(Context()).ToSvg(new VisualSvgOptions(colorVariables: Variables())));
        var stops = document.Descendants().Where(element => element.Name.LocalName == "stop").Select(element => element.Attribute("stop-color")!.Value).ToArray();
        Assert.NotEmpty(stops);
        Assert.All(stops, paint => { Assert.Contains("color-mix(in srgb,", paint); Assert.Contains("var(--status,", paint); });
        var range = V2GalleryModels.Create(ChartSeriesKind.RangeBar).WithBarStyle(ChartBarStyle.SegmentedCapsule);
        range.Series[0].StateRole = ChartSeriesState.Danger; range.Series[0].WithPointColor(0, Shared);
        var caps = Paints(range.Prepare(Context()), Variables(), "range-bar-cap", "stroke");
        Assert.All(caps.Take(2), paint => Assert.Contains("var(--series,", paint));
        Assert.All(caps.Skip(2), paint => Assert.Contains("var(--status,", paint));
        Assert.All(Paints(range.Prepare(Context()), Variables(), "range-bar-cap-highlight", "stroke"), paint => Assert.DoesNotContain("var(", paint));
    }
}
