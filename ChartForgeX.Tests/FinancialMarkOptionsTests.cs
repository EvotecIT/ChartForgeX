using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FinancialMarkOptionsTests {
    private static readonly ChartColor Green = ChartColor.FromHex("#22AA66");
    private static readonly ChartColor Blue = ChartColor.FromHex("#3377CC");
    private static readonly ChartColor Orange = ChartColor.FromHex("#CC7733");

    [Fact]
    public void AllEqualCandleRegionCoversItsMinimumBodyAndOutline() {
        var chart = Chart.Create().WithSize(400, 280).WithHeader(false).WithLegend(false)
            .AddCandlestick("Flat price", new[] { new ChartCandlestick(1, 10, 10, 10, 10) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var body = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "candlestick-body");
        var region = Assert.Single(prepared.Regions, item => item.Id == "series-0-point-0");
        Assert.True(region.Bounds.Top <= body.Bounds.Top - body.StrokeWidth / 2
            && region.Bounds.Bottom >= body.Bounds.Bottom + body.StrokeWidth / 2,
            "Observation height " + region.Bounds.Height + " must cover the " + body.Bounds.Height
            + " pixel candle body and its " + body.StrokeWidth + " pixel outline.");
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-.1)]
    public void FinancialNumbersRejectNonFiniteAndNegativeValues(double value) {
        var item = new ChartFinancialOptions().Rising;
        foreach (var setter in new Action<double>[] {
            number => item.FillOpacity = number, number => item.StrokeOpacity = number,
            number => item.StrokeWidth = number, number => item.Wick.StrokeOpacity = number, number => item.Wick.StrokeWidth = number
        }) Assert.Throws<ArgumentOutOfRangeException>(() => setter(value));
    }

    [Fact]
    public void FinancialOpacityAndKindValidationUseThePublicPreparationBoundary() {
        var options = new ChartFinancialOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Rising.FillOpacity = 1.1);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Falling.StrokeOpacity = 1.1);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Rising.Wick.StrokeOpacity = 1.1);
        var line = Chart.Create().AddLine("Line", new[] { new ChartPoint(1, 2) });
        Assert.Throws<ArgumentNullException>(() => line.Series[0].ConfigureFinancial(null!));
        line.Series[0].ConfigureFinancial(financial => financial.Rising.StrokeWidth = 0);
        Assert.Contains("does not support financial options", Assert.Throws<InvalidOperationException>(() => line.ToSvg()).Message);
        Assert.Throws<InvalidOperationException>(() => line.ToPng());
        foreach (var configure in new Action<ChartFinancialOptions>[] {
            financial => financial.Rising.Fill = Green, financial => financial.Falling.FillOpacity = 0,
            financial => financial.Rising.Wick.StrokeWidth = 0
        }) {
            var ohlc = Prices(ChartSeriesKind.Ohlc);
            ohlc.Series[0].ConfigureFinancial(configure);
            Assert.Contains("stroke options only", Assert.Throws<InvalidOperationException>(() => Prepare(ohlc)).Message);
        }
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick)]
    [InlineData(ChartSeriesKind.Ohlc)]
    public void UnconfiguredDirectionsRetainTheirDefaultGeometryPaintAndLegend(ChartSeriesKind kind) {
        var chart = Prices(kind).WithLegend(true);
        var before = Prepare(chart).ToSvg();
        var item = chart.Series[0].Financial.Rising;
        item.StrokeWidth = 9; item.Stroke = Green; item.StrokeOpacity = .5;
        item.StrokeWidth = null; item.Stroke = null; item.StrokeOpacity = null;
        Assert.Equal(before, Prepare(chart).ToSvg());
        var scene = Prepare(chart).Scene;
        Assert.DoesNotContain(scene.Nodes, node => node.Role?.StartsWith("legend-financial", StringComparison.Ordinal) == true);
        var strokes = scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == (kind == ChartSeriesKind.Candlestick ? "candlestick-wick" : "ohlc-stem")).ToArray();
        Assert.Equal(2, strokes.Length);
        Assert.All(strokes, stroke => Assert.Equal(kind == ChartSeriesKind.Candlestick ? 2 : 2.2, stroke.StrokeWidth));
        if (kind == ChartSeriesKind.Candlestick) {
            var bodies = scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "candlestick-body").ToArray();
            Assert.Equal(ChartColorMath.WithOpacity(bodies[0].Stroke!.Value, .23), bodies[0].Fill);
            Assert.Equal(ChartColorMath.WithOpacity(bodies[1].Stroke!.Value, .84), bodies[1].Fill);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ObservationColorsOverrideAuthoredPartsAndAlphaMultipliersRemainIndependent(bool observationOverride) {
        var chart = Prices(ChartSeriesKind.Candlestick);
        var series = chart.Series[0]; series.StateRole = ChartSeriesState.Warning;
        series.ConfigureFinancial(financial => {
            financial.Rising.Fill = Orange.WithAlpha(128); financial.Rising.FillOpacity = .25;
            financial.Rising.Stroke = Green.WithAlpha(128); financial.Rising.StrokeOpacity = .5;
            financial.Rising.StrokeWidth = 6;
            financial.Rising.Wick.Stroke = Blue.WithAlpha(128); financial.Rising.Wick.StrokeOpacity = .75;
            financial.Rising.Wick.StrokeWidth = 3;
        });
        if (observationOverride) series.WithPointColor(0, Blue.WithAlpha(200));
        var prepared = Prepare(chart);
        var body = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "candlestick-body");
        var wick = prepared.Scene.Nodes.OfType<VisualSceneLine>().First(node => node.Role == "candlestick-wick");
        Assert.Equal(ChartColorMath.WithOpacity(observationOverride ? Blue.WithAlpha(200) : Orange.WithAlpha(128), .25), body.Fill);
        Assert.Equal(ChartColorMath.WithOpacity(observationOverride ? Blue.WithAlpha(200) : Green.WithAlpha(128), .5), body.Stroke);
        Assert.Equal(ChartColorMath.WithOpacity(observationOverride ? Blue.WithAlpha(200) : Blue.WithAlpha(128), .75), wick.Stroke);
        Assert.Equal(6, body.StrokeWidth); Assert.Equal(3, wick.StrokeWidth);
        var variables = Variables();
        var inherited = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "candlestick-body").ElementAt(1);
        variables.Add("--status", inherited.Stroke!.Value, SvgColorRole.Status);
        var svg = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: variables)));
        Assert.Contains("var(--series,", Attribute(svg, "candlestick-body", "fill", 0));
        Assert.Contains("var(--series,", Attribute(svg, "candlestick-body", "stroke", 0));
        Assert.Contains("var(--series,", Attribute(svg, "candlestick-wick", "stroke", 0));
        Assert.Contains("var(--status,", Attribute(svg, "candlestick-body", "stroke", 1));
        Assert.Contains("color-mix(", Attribute(svg, "candlestick-wick", "stroke", 0));
        var pixels = prepared.ToRgba().Pixels;
        prepared.ToSvg(new VisualSvgOptions(colorVariables: variables));
        Assert.Equal(pixels, prepared.ToRgba().Pixels);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick)]
    [InlineData(ChartSeriesKind.Ohlc)]
    public void StrokeOverridesInheritSeriesWidthAndZeroRemovesInkWithoutRemovingSource(ChartSeriesKind kind) {
        var chart = Prices(kind); var series = chart.Series[0].WithStrokeWidth(7);
        series.ConfigureFinancial(financial => { financial.Rising.Stroke = Green; financial.Rising.StrokeOpacity = .5; });
        var scene = Prepare(chart).Scene;
        var strokes = scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role == "candlestick-body" || node.Role == "candlestick-wick" || node.Role?.StartsWith("ohlc-", StringComparison.Ordinal) == true);
        Assert.All(strokes, mark => Assert.Equal(7, mark.StrokeWidth));
        series.Financial.Rising.StrokeWidth = 4;
        if (kind == ChartSeriesKind.Candlestick) {
            scene = Prepare(chart).Scene;
            var wick = scene.Nodes.OfType<VisualSceneLine>().First(node => node.Role == "candlestick-wick");
            Assert.Equal(Green.WithAlpha(128), wick.Stroke); Assert.Equal(4, wick.StrokeWidth);
            series.Financial.Rising.Wick.StrokeWidth = 0;
            series.Financial.Rising.FillOpacity = 0;
        }
        series.Financial.Rising.StrokeWidth = 0;
        var prepared = Prepare(chart);
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point"));
        if (kind == ChartSeriesKind.Ohlc)
            Assert.Equal(3, prepared.Scene.Nodes.OfType<VisualSceneLine>().Count(node => node.Role?.StartsWith("ohlc-", StringComparison.Ordinal) == true));
        else {
            var first = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "candlestick-body");
            Assert.Null(first.Fill); Assert.Null(first.Stroke);
            Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "candlestick-wick");
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void HollowCandlesLeaveTheInteriorTransparentAcrossReversalAndBothHollowInputs(bool reversed, bool transparentColor) {
        var chart = Prices(ChartSeriesKind.Candlestick).WithAxes(false).WithGrid(false);
        chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch).ConfigureFinancial(financial => {
            if (transparentColor) financial.Rising.Fill = ChartColor.Transparent;
            else financial.Rising.FillOpacity = 0;
            financial.Rising.Stroke = Green; financial.Rising.StrokeWidth = 3;
            financial.Rising.Wick.Stroke = Blue; financial.Rising.Wick.StrokeWidth = 2;
        });
        chart.Options.YAxis.WithReversal(reversed);
        var prepared = Prepare(chart);
        var body = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "candlestick-body");
        Assert.Null(body.Fill);
        var wicks = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "candlestick-wick").Take(2).ToArray();
        Assert.Equal(2, wicks.Length);
        Assert.All(wicks, wick => Assert.True(Math.Max(wick.Start.Y, wick.End.Y) + wick.StrokeWidth / 2 <= body.Bounds.Top
            || Math.Min(wick.Start.Y, wick.End.Y) - wick.StrokeWidth / 2 >= body.Bounds.Bottom));
        var svg = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: Variables())));
        Assert.Equal("none", Attribute(svg, "candlestick-body", "fill", 0));
        var group = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "point"
            && (string?)element.Attribute("data-cfx-series") == "0" && (string?)element.Attribute("data-cfx-point") == "0");
        Assert.DoesNotContain(group.Descendants(), element => ((string?)element.Attribute("data-cfx-role")) == "candlestick-pattern");
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        Assert.Equal(0, Pixel(image, body.Bounds.Left + body.Bounds.Width / 2, body.Bounds.Top + body.Bounds.Height / 2)[3]);
        Assert.True(Pixel(image, wicks[0].Start.X, (wicks[0].Start.Y + wicks[0].End.Y) / 2)[3] > 0);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick, false)]
    [InlineData(ChartSeriesKind.Candlestick, true)]
    [InlineData(ChartSeriesKind.Ohlc, false)]
    [InlineData(ChartSeriesKind.Ohlc, true)]
    public void ConfiguredLegendsUseFinancialGlyphsAndPrepareFreezesAllOptions(ChartSeriesKind kind, bool pointLegend) {
        var chart = Prices(kind).WithLegend(true).WithPointLegend(pointLegend);
        chart.Series[0].ConfigureFinancial(financial => {
            financial.Rising.Stroke = Green; financial.Falling.Stroke = Orange;
            financial.Rising.StrokeWidth = 3; financial.Falling.StrokeWidth = 4;
            if (kind == ChartSeriesKind.Candlestick) financial.Rising.FillOpacity = 0;
        }).WithPointColor(0, Blue);
        var prepared = Prepare(chart);
        var role = kind == ChartSeriesKind.Candlestick ? "legend-financial-body" : "legend-financial-stem";
        var glyphs = prepared.Scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role == role).ToArray();
        Assert.Equal(2, glyphs.Length);
        Assert.Equal(pointLegend ? Blue : Green, glyphs[0].Stroke);
        Assert.Equal(Orange, glyphs[1].Stroke);
        if (kind == ChartSeriesKind.Candlestick) Assert.Null(glyphs[0].Fill);
        var regions = prepared.Regions.Where(region => region.Role == "legend").ToArray();
        Assert.Equal(pointLegend ? 2 : 1, regions.Length);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Series[0].Financial.Rising.Stroke = Orange;
        chart.Series[0].Financial.Rising.StrokeWidth = 20;
        if (kind == ChartSeriesKind.Candlestick) chart.Series[0].Financial.Rising.FillOpacity = 1;
        chart.Series[0].PointColors.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick)]
    [InlineData(ChartSeriesKind.Ohlc)]
    public void PaintedStrokeUnionDeterminesObservationBounds(ChartSeriesKind kind) {
        var chart = Prices(kind);
        chart.Series[0].ConfigureFinancial(financial => {
            financial.Rising.StrokeWidth = 8;
            if (kind == ChartSeriesKind.Candlestick) financial.Rising.Wick.StrokeWidth = 12;
        });
        var prepared = Prepare(chart); var region = prepared.Regions.Single(item => item.Id == "series-0-point-0");
        var firstLines = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == (kind == ChartSeriesKind.Candlestick ? "candlestick-wick" : "ohlc-stem")).Take(1);
        foreach (var line in firstLines) {
            Assert.True(region.Bounds.Top <= Math.Min(line.Start.Y, line.End.Y) - line.StrokeWidth / 2);
            Assert.True(region.Bounds.Bottom >= Math.Max(line.Start.Y, line.End.Y) + line.StrokeWidth / 2);
        }
        if (kind == ChartSeriesKind.Candlestick) {
            var body = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "candlestick-body");
            Assert.Equal(body.Bounds.Left - 4, region.Bounds.Left, 7);
            Assert.Equal(body.Bounds.Right + 4, region.Bounds.Right, 7);
        } else {
            var ticks = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role is "ohlc-open" or "ohlc-close").Take(2).ToArray();
            Assert.Equal(ticks[0].Start.X - 4, region.Bounds.Left, 7);
            Assert.Equal(ticks[1].End.X + 4, region.Bounds.Right, 7);
        }
    }

    internal static Chart Prices(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(400, 280).WithHeader(false).WithLegend(false).WithDataLabels(false);
        var source = new[] { new ChartCandlestick(1, 4, 9, 1, 7), new ChartCandlestick(2, 7, 9, 1, 4) };
        return kind == ChartSeriesKind.Candlestick ? chart.AddCandlestick("Price", source) : chart.AddOhlc("Price", source);
    }

    private static PreparedVisual Prepare(Chart chart) {
        var request = VisualExportRequest.ForChart(chart).Context;
        return chart.Prepare(new VisualRenderContext(request.Layout, request.Theme, request.ThemeMode,
            new VisualFrame(showLegend: chart.Options.ShowLegend, transparentBackground: true), request.Font));
    }

    private static SvgColorVariables Variables() {
        return new SvgColorVariables().Add("--series", Green, SvgColorRole.Series)
            .Add("--series", Blue, SvgColorRole.Series).Add("--series", Orange, SvgColorRole.Series);
    }
    private static string Attribute(XDocument document, string role, string attribute, int index) =>
        document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ElementAt(index).Attribute(attribute)!.Value;
    private static byte[] Pixel(RgbaImage image, double x, double y) => image.Pixels.Skip(((int)y * image.Width + (int)x) * 4).Take(4).ToArray();
}
