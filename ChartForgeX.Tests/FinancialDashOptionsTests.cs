using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FinancialDashOptionsTests {
    [Fact]
    public void NonrepeatingSubpixelPatternUsesTheSameHighDpiResolutionAcrossRasterPaths() {
        var chart = Chart.Create().WithSize(1200, 100).WithHeader(false).WithLegend(false).WithDataLabels(false);
        chart.AddOhlc("Price", new[] { new ChartCandlestick(1, 4, 9, 1, 7), new ChartCandlestick(2, 7, 9, 1, 4) });
        var pattern = Enumerable.Repeat(.0001, 65).ToArray(); pattern[64] = .0002;
        chart.Series[0].Financial.Rising.DashPattern = pattern;
        chart.Series[0].Financial.Rising.StrokeWidth = 2;
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200, 100)),
            frame: new VisualFrame(showLegend: false)));
        var native = prepared.ToRgba(new VisualRenderOptions(scale: 8, supersampling: 1));
        var svg = SvgRasterizer.ToImage(prepared.ToSvg(), width: 9600, height: 800);
        Assert.Equal(native.Width, svg.Width); Assert.Equal(native.Height, svg.Height);
        Assert.Contains(native.Pixels, value => value != 0); Assert.Contains(svg.Pixels, value => value != 0);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick, .0001, .0001)]
    [InlineData(ChartSeriesKind.Ohlc, .0001, .0001)]
    [InlineData(ChartSeriesKind.Candlestick, 1e-20, 1e-20)]
    [InlineData(ChartSeriesKind.Ohlc, 1e-20, 1e-20)]
    [InlineData(ChartSeriesKind.Candlestick, 4, 1e-20)]
    [InlineData(ChartSeriesKind.Ohlc, 1e-20, 4)]
    public void AcceptedTinyPatternsExportAndRetainExactSvgLengthsAndPhase(ChartSeriesKind kind, double dash, double gap) {
        var chart = FinancialMarkOptionsTests.Prices(kind);
        var item = chart.Series[0].Financial.Rising;
        item.DashPattern = new[] { dash, gap }; item.DashOffset = -1e-21;
        if (kind == ChartSeriesKind.Candlestick) item.FillOpacity = 0;
        var prepared = Prepare(chart);
        var svg = prepared.ToSvg();
        foreach (var node in XDocument.Parse(svg).Descendants().Where(node => node.Attribute("stroke-dasharray") != null)) {
            var lengths = node.Attribute("stroke-dasharray")!.Value.Split(' ').Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(new[] { dash, gap }, lengths);
            Assert.Equal(-1e-21, double.Parse(node.Attribute("stroke-dashoffset")!.Value, System.Globalization.CultureInfo.InvariantCulture));
        }
        Assert.NotEmpty(prepared.ToPng());
        Assert.NotEmpty(SvgRasterizer.ToImage(svg).Pixels);
    }

    [Fact]
    public void PatternsAreCopiedReadOnlyAndInvalidAssignmentsAreAtomic() {
        var item = new ChartFinancialOptions().Rising;
        var input = new[] { 8d, 5d, 2d };
        item.DashPattern = input; item.Wick.DashPattern = input;
        input[0] = 99;
        Assert.Equal(new[] { 8d, 5d, 2d }, item.DashPattern);
        Assert.Equal(item.DashPattern, item.Wick.DashPattern);
        Assert.Throws<NotSupportedException>(() => ((IList<double>)item.DashPattern!)[0] = 99);
        foreach (var invalid in new[] {
            new[] { 0d }, new[] { -1d }, new[] { double.NaN }, new[] { double.PositiveInfinity },
            new[] { double.NegativeInfinity }, new[] { double.MaxValue, double.MaxValue }, new[] { double.MaxValue }
        }) {
            Assert.Throws<ArgumentOutOfRangeException>(() => item.DashPattern = invalid);
            Assert.Throws<ArgumentOutOfRangeException>(() => item.Wick.DashPattern = invalid);
        }
        Assert.Equal(new[] { 8d, 5d, 2d }, item.DashPattern);
        Assert.Equal(item.DashPattern, item.Wick.DashPattern);
        item.DashOffset = -3; item.Wick.DashOffset = 4;
        foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }) {
            Assert.Throws<ArgumentOutOfRangeException>(() => item.DashOffset = invalid);
            Assert.Throws<ArgumentOutOfRangeException>(() => item.Wick.DashOffset = invalid);
        }
        Assert.Equal(-3, item.DashOffset); Assert.Equal(4, item.Wick.DashOffset);
        item.DashOffset = double.MaxValue; item.Wick.DashOffset = -double.MaxValue;
    }

    [Fact]
    public void DashOptionsUseTheExistingFinancialKindBoundary() {
        var line = Chart.Create().AddLine("Line", new[] { new ChartPoint(1, 2) });
        line.Series[0].Financial.Rising.DashOffset = 0;
        Assert.Contains("does not support financial options", Assert.Throws<InvalidOperationException>(() => line.ToSvg()).Message);
        var ohlc = FinancialMarkOptionsTests.Prices(ChartSeriesKind.Ohlc);
        ohlc.Series[0].Financial.Rising.Wick.DashPattern = Array.Empty<double>();
        Assert.Contains("stroke options only", Assert.Throws<InvalidOperationException>(() => ohlc.ToPng()).Message);
    }

    [Fact]
    public void WickPatternAndPhaseIndependentlyInheritOverrideAndReset() {
        var chart = FinancialMarkOptionsTests.Prices(ChartSeriesKind.Candlestick);
        var item = chart.Series[0].Financial.Rising;
        item.FillOpacity = 0; item.DashPattern = new[] { 8d, 5d, 2d }; item.DashOffset = -3;
        VisualSceneLine[] Wicks() => Prepare(chart).Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "candlestick-wick").Take(2).ToArray();
        void Check(IReadOnlyList<double>? pattern, double offset) {
            Assert.Equal(2, Wicks().Length);
            Assert.All(Wicks(), wick => { Assert.Equal(pattern, wick.Dash); Assert.Equal(offset, wick.DashOffset); });
        }
        Check(item.DashPattern, -3);
        item.Wick.DashOffset = 4; Check(item.DashPattern, 4);
        item.Wick.DashPattern = Array.Empty<double>(); Check(null, 4);
        item.Wick.DashOffset = null; Check(null, -3);
        item.Wick.DashPattern = new[] { 3d, 7d }; Check(item.Wick.DashPattern, -3);
        item.Wick.DashPattern = null; Check(item.DashPattern, -3);
        var body = Assert.Single(Prepare(chart).Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "candlestick-body");
        Assert.True(body.Close); Assert.Equal(item.DashPattern, body.Dash); Assert.Equal(-3, body.DashOffset);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick)]
    [InlineData(ChartSeriesKind.Ohlc)]
    public void PreparationCapturesPatternPhaseAndScaledLegendIndependentlyOfLaterChanges(ChartSeriesKind kind) {
        var chart = FinancialMarkOptionsTests.Prices(kind).WithLegend(true);
        var item = chart.Series[0].Financial.Rising;
        item.DashPattern = new[] { 6d, 10d }; item.DashOffset = -4; item.StrokeWidth = 3;
        if (kind == ChartSeriesKind.Candlestick) { item.FillOpacity = 0; item.Wick.DashOffset = 2; }
        var prepared = Prepare(chart);
        var glyph = prepared.Scene.Nodes.OfType<VisualSceneMark>().First(node => node.Role == (kind == ChartSeriesKind.Candlestick ? "legend-financial-body" : "legend-financial-stem"));
        var scale = glyph.StrokeWidth / 3;
        var pattern = glyph is VisualScenePath path ? path.Dash : ((VisualSceneLine)glyph).Dash;
        var offset = glyph is VisualScenePath phased ? phased.DashOffset : ((VisualSceneLine)glyph).DashOffset;
        Assert.InRange(scale, 0.01, .99);
        Assert.Equal(new[] { 6 * scale, 10 * scale }, pattern); Assert.Equal(-4 * scale, offset);
        if (kind == ChartSeriesKind.Candlestick) {
            var wick = prepared.Scene.Nodes.OfType<VisualSceneLine>().First(node => node.Role == "legend-financial-wick");
            Assert.Equal(pattern, wick.Dash); Assert.Equal(2 * scale, wick.DashOffset);
        }
        var svg = prepared.ToSvg(); var pixels = prepared.ToRgba().Pixels;
        item.DashPattern = new[] { 2d, 18d }; item.DashOffset = 9;
        if (kind == ChartSeriesKind.Candlestick) { item.Wick.DashPattern = Array.Empty<double>(); item.Wick.DashOffset = 0; }
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(pixels, prepared.ToRgba().Pixels);
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick)]
    [InlineData(ChartSeriesKind.Ohlc)]
    public void ResettingDashesRestoresDefaultBytesAndHiddenStrokeRetainsSourceFacts(ChartSeriesKind kind) {
        var chart = FinancialMarkOptionsTests.Prices(kind);
        var original = Prepare(chart); var item = chart.Series[0].Financial.Rising;
        item.DashPattern = new[] { 6d, 10d }; item.DashOffset = -2;
        if (kind == ChartSeriesKind.Candlestick) { item.Wick.DashPattern = new[] { 4d, 8d }; item.Wick.DashOffset = 3; }
        item.DashPattern = null; item.DashOffset = null;
        if (kind == ChartSeriesKind.Candlestick) { item.Wick.DashPattern = null; item.Wick.DashOffset = null; }
        Assert.Equal(original.ToSvg(), Prepare(chart).ToSvg()); Assert.Equal(original.ToPng(), Prepare(chart).ToPng());
        item.DashPattern = new[] { 6d, 10d }; item.DashOffset = -2; item.StrokeWidth = 0;
        if (kind == ChartSeriesKind.Candlestick) { item.FillOpacity = 0; item.Wick.StrokeWidth = 0; }
        var hidden = Prepare(chart);
        Assert.Equal(2, hidden.Regions.Count(region => region.Role == "point"));
        var point = XDocument.Parse(hidden.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-source-id") == "series-0-point-0");
        foreach (var (part, value) in new[] { ("open", "4"), ("high", "9"), ("low", "1"), ("close", "7") })
            Assert.Equal(value, point.Attribute("data-cfx-" + part)!.Value);
        Assert.DoesNotContain(hidden.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "candlestick-body");
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick)]
    [InlineData(ChartSeriesKind.Ohlc)]
    public void NativeFinancialStrokesPaintTheSameDashPhaseAsSvgAndRetainVisibleGaps(ChartSeriesKind kind) {
        var chart = FinancialMarkOptionsTests.Prices(kind);
        chart.Series[0].ConfigureFinancial(financial => {
            financial.Rising.DashPattern = new[] { 6d, 10d }; financial.Rising.DashOffset = 2;
            financial.Rising.StrokeWidth = 2;
            if (kind == ChartSeriesKind.Candlestick) financial.Rising.FillOpacity = 0;
        });
        var scene = Prepare(chart).Scene;
        var marks = scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role?.StartsWith(kind == ChartSeriesKind.Candlestick ? "candlestick-" : "ohlc-", StringComparison.Ordinal) == true).ToArray();
        var isolated = new VisualScene(scene.Size, marks, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var native = VisualSceneRasterRenderer.Render(isolated, supersampling: 1);
        var svg = VisualSceneSvgRenderer.Render(isolated);
        var parsed = SvgRasterizer.ToImage(svg);
        AssertAlphaAgreement(native, parsed);
        var first = marks.OfType<VisualSceneLine>().First();
        Assert.True(Alpha(native, (int)first.Start.X, (int)first.Start.Y + 2) > 0);
        Assert.Equal(0, Alpha(native, (int)first.Start.X, (int)first.Start.Y + 10));
        var phases = XDocument.Parse(svg).Descendants().Where(node => node.Attribute("stroke-dasharray") != null).ToArray();
        Assert.NotEmpty(phases); Assert.All(phases, node => Assert.Equal("2", node.Attribute("stroke-dashoffset")!.Value));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public void CapturedScenePhaseMatchesSvgForOddPatternsClippingClosedPathsAndTransforms(bool rotated, int scale) {
        VisualScene Scene(double offset) {
            var builder = new VisualSceneBuilder(new VisualSize(120, 100), new FontSpec());
            using (builder.PushClip(new ChartRect(5, 5, 110, 90)))
            using (rotated ? builder.PushRotation(90, 60, 50) : null) {
                builder.Line(-30, 20, 150, 20, ChartColor.Black, 2, role: "phase-line", dash: new[] { 8d, 5d, 3d }, dashOffset: offset);
                builder.Path(ChartPathBuilder.RoundedRectangle(new ChartRect(20, 40, 80, 40), 0), stroke: ChartColor.Black,
                    strokeWidth: 2, role: "phase-path", close: true, dash: new[] { 8d, 5d, 3d }, dashOffset: -offset);
            }
            return builder.Build();
        }
        var scene = Scene(-4); var svg = VisualSceneSvgRenderer.Render(scene);
        var native = VisualSceneRasterRenderer.Render(scene, scale: scale, supersampling: 1);
        AssertAlphaAgreement(native, SvgRasterizer.ToImage(svg, 120 * scale, 100 * scale));
        Assert.NotEqual(svg, VisualSceneSvgRenderer.Render(Scene(4)));
        Assert.NotEqual(native.Pixels, VisualSceneRasterRenderer.Render(Scene(4), scale: scale, supersampling: 1).Pixels);
    }

    [Theory]
    [InlineData(double.MaxValue)]
    [InlineData(-double.MaxValue)]
    public void LargeFinitePhaseRendersAsItsEquivalentSeventeenPixelPeriod(double offset) {
        var chart = FinancialMarkOptionsTests.Prices(ChartSeriesKind.Candlestick).WithLegend(true);
        var item = chart.Series[0].Financial.Rising;
        item.FillOpacity = 0; item.DashPattern = new[] { 6d, 11d }; item.DashOffset = offset;
        var captured = Prepare(chart);
        var body = captured.Scene.Nodes.OfType<VisualScenePath>().First(node => node.Role == "candlestick-body");
        Assert.Equal(offset, body.DashOffset);
        var expectedPhase = offset % 17;
        var svg = XDocument.Parse(captured.ToSvg());
        var element = svg.Descendants().First(node => (string?)node.Attribute("data-cfx-role") == "candlestick-body");
        Assert.Equal(expectedPhase.ToString(System.Globalization.CultureInfo.InvariantCulture), element.Attribute("stroke-dashoffset")!.Value);
        item.DashOffset = expectedPhase;
        Assert.Equal(Prepare(chart).ToRgba(new VisualRenderOptions(scale: 2)).Pixels,
            captured.ToRgba(new VisualRenderOptions(scale: 2)).Pixels);
        var marks = captured.Scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role?.StartsWith("candlestick-", StringComparison.Ordinal) == true).ToArray();
        var isolated = new VisualScene(captured.Scene.Size, marks, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        AssertAlphaAgreement(VisualSceneRasterRenderer.Render(isolated, supersampling: 1), SvgRasterizer.ToImage(VisualSceneSvgRenderer.Render(isolated)));
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);

    private static byte Alpha(RgbaImage image, int x, int y) => image.Pixels[(y * image.Width + x) * 4 + 3];

    private static void AssertAlphaAgreement(RgbaImage actual, RgbaImage expected) {
        Assert.Equal(expected.Width, actual.Width); Assert.Equal(expected.Height, actual.Height);
        var difference = 0L; var ink = 0L;
        for (var i = 3; i < actual.Pixels.Length; i += 4) {
            difference += Math.Abs(actual.Pixels[i] - expected.Pixels[i]); ink += expected.Pixels[i];
        }
        Assert.True(ink > 0); Assert.InRange((double)difference / ink, 0, .015);
    }
}
