using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Independent color facts keep their own domain, paint, raw readouts and detached preparation.</summary>
public sealed class ScatterBubbleColorRenderingTests {
    internal static readonly ChartColor Low = ChartColor.FromHex("#2166AC"), High = ChartColor.FromHex("#B2182B"), Empty = ChartColor.FromHex("#9CA3AF");
    internal static Chart Create(bool bubble, params double?[] colors) => Create(new ChartSeriesId("samples"), bubble, colors);
    private static Chart Create(ChartSeriesId id, bool bubble, params double?[] colors) {
        var chart = Chart.Create().WithSize(800, 500).WithDataLabels(false).ConfigureLabels(labels => labels.Color = "Temperature (°C)");
        if (bubble) chart.AddSeries(ChartSeries.CreateBubble(id, "Samples", colors.Select((value, i) => new ChartBubble(i + 1, 10 + i * 4, (i + 1) * (i + 1)).WithColorValue(value))));
        else chart.AddSeries(ChartSeries.CreateScatter(id, "Samples", colors.Select((value, i) => new ChartPoint(i + 1, 10 + i * 4).WithColorValue(value))));
        return chart;
    }
    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    internal static XElement[] Role(XDocument document, string role) => document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    internal static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ColorDimensionIsIndependentOfYAndBubbleSizeAndKeepsRawMissingAndClampedFacts(bool bubble) {
        var chart = Create(bubble, -10, 0, 0, 100, null);
        chart.Options.ShowGrid = false;
        chart.Options.TransparentBackground = false; chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false;
        // Keep sampled centers away from axis clipping so this test isolates fill and alpha composition.
        chart.Options.XAxis.Minimum = 0; chart.Options.XAxis.Maximum = 6;
        chart.Options.YAxis.Minimum = 0; chart.Options.YAxis.Maximum = 40;
        var scale = ChartColorScale.Sequential(Low, High).WithValueRange(0, 20).WithNoDataColor(Empty);
        chart.Series[0].WithColorScale(scale);
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg());
        var points = Role(svg, "point");
        Assert.Equal(new[] { "-10", "0", "0", "100", null }, points.Select(point => (string?)point.Attribute("data-cfx-color-value")));
        Assert.Equal("true", (string?)points[4].Attribute("data-cfx-color-missing"));
        Assert.Equal("Temperature (°C)", (string?)points[0].Attribute("data-cfx-label-color"));
        Assert.Contains("Temperature (°C): -10", prepared.Regions.Single(region => region.Id == "series-0-point-0").Label);
        var marks = Role(svg, bubble ? "bubble" : "marker");
        Assert.Equal(5, marks.Length);
        Assert.Equal((string?)marks[1].Attribute("fill"), (string?)marks[2].Attribute("fill"));
        Assert.NotEqual((string?)points[1].Attribute("data-cfx-y"), (string?)points[2].Attribute("data-cfx-y"));
        if (bubble) {
            Assert.NotEqual(Number(marks[1], "rx"), Number(marks[2], "rx"));
            Assert.Equal(5, chart.Series[0].Bubbles.Count);
            var bubbles = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "bubble").ToArray();
            var image = ChartForgeX.Raster.RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            var backdrop = ChartColor.FromRgba(image.Pixels[0], image.Pixels[1], image.Pixels[2], image.Pixels[3]);
            var expected = new[] { Low, Low, Low, High, Empty };
            for (var index = 0; index < bubbles.Length; index++) {
                var bubbleMark = bubbles[index];
                Assert.Equal(ChartColorMath.WithOpacity(expected[index], ChartVisualPrimitives.BubbleFillOpacity), bubbleMark.Fill);
                var fill = bubbleMark.Fill!.Value;
                var composite = ChartColorMath.Blend(backdrop, ChartColor.FromRgb(fill.R, fill.G, fill.B), fill.A / 255d);
                var pixel = ((int)bubbleMark.Cy * image.Width + (int)bubbleMark.Cx) * 4;
                Assert.InRange(Math.Abs(image.Pixels[pixel] - composite.R), 0, 1);
                Assert.InRange(Math.Abs(image.Pixels[pixel + 1] - composite.G), 0, 1);
                Assert.InRange(Math.Abs(image.Pixels[pixel + 2] - composite.B), 0, 1);
            }
        } else {
            Assert.Equal(Low.ToHex(), (string?)marks[0].Attribute("fill"));
            Assert.Equal(High.ToHex(), (string?)marks[3].Attribute("fill"));
            Assert.Equal(Empty.ToHex(), (string?)marks[4].Attribute("fill"));
            var image = ChartForgeX.Raster.RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            foreach (var mark in marks) {
                var pixel = ((int)Number(mark, "cy") * image.Width + (int)Number(mark, "cx")) * 4;
                Assert.Equal(ChartColor.FromHex((string)mark.Attribute("fill")!), ChartColor.FromRgba(image.Pixels[pixel], image.Pixels[pixel + 1], image.Pixels[pixel + 2], image.Pixels[pixel + 3]));
            }
        }
        Assert.NotEmpty(Role(svg, "color-scale-0")); // A single configured series shows its color key automatically.
        Capture(prepared, bubble ? "bubble-color-facts" : "scatter-color-facts");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InferenceIncludesOffViewportHiddenAndOverriddenRetainedFactsAndDoesNotPoolSeries(bool bubble) {
        var chart = Create(bubble, -10, 0, 100);
        chart.Options.XAxis.Minimum = 1; chart.Options.XAxis.Maximum = 2;
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(Low, High));
        chart.Series[0].WithPointColor(2, ChartColor.Black);
        chart.Series[0].Markers.Enabled = false;
        var first = XDocument.Parse(Prepare(chart).ToSvg());
        Assert.Equal("100", (string?)Role(first, "color-scale-0").Single().Attribute("data-cfx-max-value"));
        chart.AddScatter("Samples", new[] { new ChartPoint(1, 30).WithColorValue(900), new ChartPoint(2, 35).WithColorValue(1000) });
        chart.Series[1].WithColorScale(chart.Series[0].ColorScale); chart.Series[1].YAxis = ChartAxisSide.Secondary;
        var document = XDocument.Parse(Prepare(chart).ToSvg());
        Assert.Equal("-10", (string?)Role(document, "color-scale-0").Single().Attribute("data-cfx-min-value"));
        Assert.Equal("900", (string?)Role(document, "color-scale-1").Single().Attribute("data-cfx-min-value"));
    }

    [Fact]
    public void DuplicateNamesInPanelsKeepTheirOwnColorDomainsIdentitiesAndDetachedFacts() {
        var first = Create(new ChartSeriesId("first"), false, -10, 10); var second = Create(new ChartSeriesId("second"), true, 900, 1000);
        var scale = ChartColorScale.Sequential(Low, High);
        first.Series[0].WithColorScale(scale);
        second.Series[0].WithColorScale(scale);
        var grid = ChartGrid.Create().WithColumns(2).Add(first).Add(second);
        var prepared = grid.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200, 500))));
        var document = XDocument.Parse(prepared.ToSvg()); var keys = Role(document, "color-scale-0");
        Assert.Equal(new[] { "-10", "900" }, keys.Select(key => (string?)key.Attribute("data-cfx-min-value")));
        Assert.Equal(new[] { "first", "second" }, Role(document, "color-scale-source").Select(key => (string?)key.Attribute("data-cfx-series-key")));
        Assert.Contains(prepared.Regions, region => region.Id == "panel-0/series-0-point-0" && region.Label?.Contains("Temperature (°C): -10") == true);
        Assert.Contains(prepared.Regions, region => region.Id == "panel-1/series-0-point-0" && region.Label?.Contains("Temperature (°C): 900") == true);
        var svg = prepared.ToSvg(); second.Series[0].UpdateBubble(0, second.Series[0].Bubbles[0].WithColorValue(9999));
        Assert.Equal(svg, prepared.ToSvg());
    }

    [Fact]
    public void OverridesPrecedeMappedPaintAndResetKeepsRawFactsAndOrdinaryPatternStroke() {
        var chart = Create(false, 0, 10, null);
        var series = chart.Series[0];
        series.WithColorScale(ChartColorScale.Sequential(Low, High).WithNoDataColor(Empty));
        series.Markers.Fill = ChartColor.White; series.Markers.Stroke = ChartColor.Black; series.Markers.StrokeWidth = 2;
        series.WithPointColor(0, ChartColor.FromHex("#123456")); series.WithPointFillPattern(1, ChartFillPattern.DiagonalForward);
        var document = XDocument.Parse(Prepare(chart).ToSvg()); var marks = Role(document, "marker");
        Assert.Equal("#123456", (string?)marks[0].Attribute("fill"));
        Assert.All(marks.Skip(1), mark => Assert.Equal("#FFFFFF", (string?)mark.Attribute("fill")));
        Assert.All(marks, mark => Assert.Equal("#000000", (string?)mark.Attribute("stroke")));
        Assert.NotEmpty(Role(document, "marker-pattern"));
        series.Markers.Fill = null; series.WithColorScale(null);
        document = XDocument.Parse(Prepare(chart).ToSvg());
        Assert.Equal("0", (string?)Role(document, "point")[0].Attribute("data-cfx-color-value"));
        Assert.Null(Role(document, "point")[2].Attribute("data-cfx-color-missing"));
        Assert.Empty(Role(document, "color-scale-0"));
    }

    [Fact]
    public void PreparedColorFactsPaintsAndKeysDoNotObserveLaterSourceOrStyleChanges() {
        var chart = Create(true, -10, 0, null);
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(Low, High));
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Series[0].UpdateBubble(0, chart.Series[0].Bubbles[0].WithColorValue(999));
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(ChartColor.White, ChartColor.Black).WithValueRange(900, 1000));
        chart.Series[0].Markers.Fill = ChartColor.Black; chart.ConfigureLabels(labels => labels.Color = "Changed");
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
    }

    [Fact]
    public void BubbleSizeLabelIsLocalizedWithoutAColorScaleAndDetachedWithNumericKeys() {
        Assert.Equal("Size", new ChartLabels().Size);
        Assert.Throws<ArgumentException>(() => new ChartLabels().Size = " ");
        var chart = Create(true, 0, null).ConfigureLabels(labels => { labels.Size = "Volume (m³)"; labels.NoData = "Not sampled"; });
        var withoutScale = XDocument.Parse(Prepare(chart).ToSvg());
        Assert.All(Role(withoutScale, "point"), point => Assert.Equal("Volume (m³)", (string?)point.Attribute("data-cfx-label-size")));
        Assert.Equal(new[] { "1", "4" }, Role(withoutScale, "point").Select(point => (string?)point.Attribute("data-cfx-size")));
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(Low, High));
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var document = XDocument.Parse(svg);
        Assert.Equal("Samples · Temperature (°C)", (string?)Role(document, "color-scale-source").Single().Attribute("data-cfx-label"));
        Assert.Equal("Not sampled", (string?)Role(document, "color-scale-source").Single().Attribute("data-cfx-label-no-data"));
        chart.ConfigureLabels(labels => { labels.Color = "Changed color"; labels.Size = "Changed size"; labels.NoData = "Changed missing"; });
        Assert.Equal(svg, prepared.ToSvg());
        var changed = XDocument.Parse(Prepare(chart).ToSvg());
        Assert.All(Role(changed, "point"), point => Assert.Equal("Changed size", (string?)point.Attribute("data-cfx-label-size")));
        Assert.Equal("Samples · Changed color", (string?)Role(changed, "color-scale-source").Single().Attribute("data-cfx-label"));
    }

    [Theory]
    [InlineData(ChartDecimationMode.MinMax, false)]
    [InlineData(ChartDecimationMode.LargestTriangleThreeBuckets, true)]
    public void NativeDecimatedFactsAndInferredKeysUseRetainedLogicalObservations(ChartDecimationMode mode, bool adaptive) {
        var source = Enumerable.Range(0, 16).Select(index => new ChartPoint(index, Math.Sin(index), index == 8)
            .WithColorValue(index % 4 == 0 ? null : index * 10)).ToArray();
        var chart = Chart.Create().WithSize(800, 500).WithDataLabels(false);
        if (adaptive) chart.AddAdaptiveScatter("Retained", source, 4, new ChartResolutionPolicy {
            MinimumPointCount = 8, MaximumPointCount = 8, PointsPerPixel = 1, DecimationMode = mode
        });
        else chart.AddDecimatedScatter("Retained", source, 8, mode);
        var series = chart.Series[0]; series.WithColorScale(ChartColorScale.Sequential(Low, High));
        var document = XDocument.Parse(Prepare(chart).ToSvg()); var points = Role(document, "point");
        Assert.Equal(series.Points.Count, points.Length);
        for (var index = 0; index < points.Length; index++) {
            var original = series.SourcePointIndices[index];
            Assert.Equal(original.ToString(CultureInfo.InvariantCulture), (string?)points[index].Attribute("data-cfx-source-point"));
            Assert.Equal(source[original].ColorValue?.ToString("R", CultureInfo.InvariantCulture), (string?)points[index].Attribute("data-cfx-color-value"));
        }
        var retained = series.Points.Where(point => point.ColorValue.HasValue).Select(point => point.ColorValue!.Value).ToArray();
        Assert.Equal(retained.Min().ToString("R", CultureInfo.InvariantCulture), (string?)Role(document, "color-scale-0").Single().Attribute("data-cfx-min-value"));
        Assert.Equal(retained.Max().ToString("R", CultureInfo.InvariantCulture), (string?)Role(document, "color-scale-0").Single().Attribute("data-cfx-max-value"));
    }

    [Fact]
    public void ColorPaintKeepsRampProvenanceEvenWhenTheSeriesHasAStatusRole() {
        var chart = Create(false, 0, 10); chart.Series[0].StateRole = ChartSeriesState.Danger;
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(Low, High));
        var svg = Prepare(chart).ToSvg(new VisualSvgOptions(colorVariables: new SvgColorVariables().Add("--color-low", Low, SvgColorRole.Ramp).Add("--color-high", High, SvgColorRole.Ramp)));
        Assert.Contains("var(--color-low,", svg); Assert.Contains("var(--color-high,", svg);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CenteredLabelsUseActualMappedAndOverriddenMarkerFill(bool bubble) {
        var chart = Create(bubble, 0, 1).WithSize(600, 400).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Center)
            .WithXAxisBounds(0, 3).WithYAxisBounds(0, 30).WithAxes(false).WithGrid(false);
        var series = chart.Series[0]; series.Color = ChartColor.Black;
        if (bubble) { chart.Options.Bubble.MinimumRadius = 30; chart.Options.Bubble.MaximumRadius = 30; }
        else series.MarkerRadius = 30;
        series.WithColorScale(ChartColorScale.Sequential(ChartColor.White, ChartColor.Black));
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        var texts = Role(document, "data-label").Select(group => group.Descendants().Single(node => node.Name.LocalName == "text")).ToArray();
        Assert.Equal(ChartColor.Black.ToHex(), (string?)texts[0].Attribute("fill"));
        Assert.Equal(bubble ? ChartColor.Black.ToHex() : ChartColor.White.ToHex(), (string?)texts[1].Attribute("fill"));
        Capture(prepared, bubble ? "bubble-inside-label" : "scatter-inside-label");
        series.Markers.Fill = ChartColor.White; series.WithPointColor(0, ChartColor.Black);
        document = XDocument.Parse(Prepare(chart).ToSvg());
        texts = Role(document, "data-label").Select(group => group.Descendants().Single(node => node.Name.LocalName == "text")).ToArray();
        Assert.Equal(bubble ? ChartColor.Black.ToHex() : ChartColor.White.ToHex(), (string?)texts[0].Attribute("fill"));
        Assert.Equal(ChartColor.Black.ToHex(), (string?)texts[1].Attribute("fill"));
    }
    internal static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

}
