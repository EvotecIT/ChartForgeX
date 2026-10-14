using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.ScatterBubbleColorRenderingTests;

namespace ChartForgeX.Tests;

/// <summary>Quantitative glyphs retain measured fills while sharing neutral outlines, legends and paint bounds.</summary>
public sealed class QuantitativeMarkerOutlineTests {
    [Theory]
    [InlineData(ChartSeriesKind.Scatter, false)]
    [InlineData(ChartSeriesKind.Scatter, true)]
    [InlineData(ChartSeriesKind.Bubble, false)]
    [InlineData(ChartSeriesKind.Bubble, true)]
    public void DefaultOutlineKeepsAuthoredForegroundAlphaAndMeasuredFillInSvgAndNativePixels(ChartSeriesKind kind, bool dark) {
        var chart = Create(kind, dark);
        var expectedStroke = chart.Options.Theme.Resolve(chart.Options.ThemeMode).Foreground;
        var prepared = Prepare(chart);
        var document = XDocument.Parse(prepared.ToSvg());
        var marks = Marks(prepared, kind);
        var expectedFills = new[] { Fill(kind, Low), Fill(kind, High) };
        Assert.Equal(new[] { "-10", "100" }, Role(document, "point").Select(point => (string?)point.Attribute("data-cfx-color-value")));
        Assert.Equal(new[] { "0", "1" }, Role(document, "point").Select(point => (string?)point.Attribute("data-cfx-source-point")));
        Assert.Equal("measured", (string?)Assert.Single(Role(document, "series")).Attribute("data-cfx-series-key"));
        var key = Assert.Single(Role(document, "color-scale-0"));
        Assert.Equal("0", (string?)key.Attribute("data-cfx-min-value"));
        Assert.Equal("20", (string?)key.Attribute("data-cfx-max-value"));
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        for (var index = 0; index < marks.Length; index++) {
            var mark = marks[index];
            Assert.Equal(expectedFills[index], mark.Fill);
            Assert.Equal(expectedStroke, mark.Stroke);
            Assert.Equal(kind == ChartSeriesKind.Bubble ? 2 : 1, mark.StrokeWidth);
            var svg = Role(document, kind == ChartSeriesKind.Bubble ? "bubble" : "marker")[index];
            Assert.Equal(expectedFills[index].ToCss(), (string?)svg.Attribute("fill"));
            Assert.Equal(expectedStroke.ToCss(), (string?)svg.Attribute("stroke"));
            AssertPixel(image, mark.Cx, mark.Cy, expectedFills[index]);
            Assert.True(HasExteriorOutlinePixel(image, mark, expectedStroke), "Native PNG must paint the neutral outline beyond the measured fill.");
        }
        Capture(prepared, $"quantitative-outline-{kind}-{(dark ? "dark" : "light")}");
    }

    [Theory]
    [InlineData(ChartSeriesKind.Scatter, false)]
    [InlineData(ChartSeriesKind.Scatter, true)]
    [InlineData(ChartSeriesKind.Bubble, false)]
    [InlineData(ChartSeriesKind.Bubble, true)]
    public void SurfaceMidpointAndMissingBorderKeepTheirFillsAndGainVisibleNativeOutlines(ChartSeriesKind kind, bool dark) {
        var chart = Create(kind, dark).WithLegend(false);
        var colors = chart.Options.Theme.Resolve(chart.Options.ThemeMode);
        var series = chart.Series[0];
        if (kind == ChartSeriesKind.Bubble) {
            series.UpdateBubble(0, series.Bubbles[0].WithColorValue(0));
            series.UpdateBubble(1, series.Bubbles[1].WithColorValue(null));
        } else {
            series.Points[0] = series.Points[0].WithColorValue(0);
            series.Points[1] = series.Points[1].WithColorValue(null);
        }
        series.WithColorScale(ChartColorScale.Diverging(Low, colors.Surface, High, 0).WithValueRange(-1, 1).WithNoDataColor(colors.Border))
            .WithObservationId(0, new ChartObservationId("surface-midpoint")).WithObservationId(1, new ChartObservationId("missing-reading"));
        chart.Options.ShowPlotBackground = true;
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        var points = Role(document, "point"); var marks = Marks(prepared, kind);
        Assert.Equal(new[] { "surface-midpoint", "missing-reading" }, points.Select(point => (string?)point.Attribute("data-cfx-observation-id")));
        Assert.Equal("0", (string?)points[0].Attribute("data-cfx-color-value")); Assert.Null(points[0].Attribute("data-cfx-color-missing"));
        Assert.Null(points[1].Attribute("data-cfx-color-value")); Assert.Equal("true", (string?)points[1].Attribute("data-cfx-color-missing"));
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var fills = new[] { Fill(kind, colors.Surface), Fill(kind, colors.Border) };
        for (var index = 0; index < marks.Length; index++) {
            var mark = marks[index]; var center = ChartColorMath.CompositeOverOpaque(fills[index], colors.Surface);
            Assert.Equal(fills[index], mark.Fill); Assert.Equal(colors.Foreground, mark.Stroke);
            AssertPixel(image, mark.Cx, mark.Cy, center);
            Assert.True(HasContrastingOutlinePixel(image, mark, colors.Surface, center, colors.Foreground),
                "A surface-colored midpoint or missing-data fill must retain visible neutral edge ink on the plot surface.");
        }
        Capture(prepared, $"quantitative-outline-surface-missing-{kind}-{(dark ? "dark" : "light")}");
    }

    [Theory]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Bubble)]
    public void MarkerOverridesKeepExactStrokeAlphaDisableAndWidthPrecedence(ChartSeriesKind kind) {
        var authored = ChartColor.FromRgba(221, 37, 91, 113);
        foreach (var mode in new[] { "authored", "disabled", "positive", "series-width" }) {
            var chart = Create(kind, false).WithLegend(false);
            var series = chart.Series[0];
            series.WithPointColor(0, ChartColor.FromHex("#23A17C"));
            series.Markers.Fill = ChartColor.FromHex("#D5AA2B");
            if (mode == "authored") series.Markers.Stroke = authored;
            if (mode == "disabled") { series.Markers.Stroke = authored; series.Markers.StrokeWidth = 0; }
            if (mode == "positive") series.Markers.StrokeWidth = 4;
            if (mode == "series-width") series.WithStrokeWidth(6);
            var expectedStroke = mode == "authored" ? authored : chart.Options.Theme.Resolve(chart.Options.ThemeMode).Foreground;
            var expectedWidth = mode == "disabled" ? 0 : mode == "positive" ? 4
                : kind == ChartSeriesKind.Bubble ? mode == "series-width" ? 6 : 2 : 1;
            var prepared = Prepare(chart);
            var marks = Marks(prepared, kind);
            Assert.Equal(Fill(kind, ChartColor.FromHex("#23A17C")), marks[0].Fill);
            Assert.Equal(series.Markers.Fill, marks[1].Fill);
            var document = XDocument.Parse(prepared.ToSvg());
            var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            foreach (var mark in marks) {
                Assert.Equal(expectedWidth, mark.StrokeWidth);
                Assert.Equal(expectedWidth == 0 ? null : expectedStroke, mark.Stroke);
                AssertPixel(image, mark.Cx, mark.Cy, mark.Fill!.Value);
                Assert.Equal(expectedWidth > 0, HasExteriorOutlinePixel(image, mark, expectedWidth > 0 ? expectedStroke : authored));
            }
            Assert.All(Role(document, kind == ChartSeriesKind.Bubble ? "bubble" : "marker"), mark =>
                Assert.Equal(expectedWidth == 0 ? "none" : expectedStroke.ToCss(), (string?)mark.Attribute("stroke")));
            Assert.Equal(new[] { "-10", "100" }, Role(document, "point").Select(point => (string?)point.Attribute("data-cfx-color-value")));
        }
    }

    [Theory]
    [InlineData(ChartSeriesKind.Scatter, false)]
    [InlineData(ChartSeriesKind.Scatter, true)]
    [InlineData(ChartSeriesKind.Bubble, false)]
    [InlineData(ChartSeriesKind.Bubble, true)]
    public void SourceAndPointLegendsUseTheSameNeutralOutlineAndMarkerWidth(ChartSeriesKind kind, bool points) {
        var chart = Create(kind, false);
        if (points) chart.WithPointLegend();
        chart.Series[0].Markers.StrokeWidth = 3;
        var prepared = Prepare(chart);
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(mark => mark.Role == "legend-swatch").ToArray();
        Assert.Equal(points ? 2 : 1, swatches.Length);
        Assert.All(swatches, swatch => {
            Assert.Equal(chart.Options.Theme.Resolve(chart.Options.ThemeMode).Foreground, swatch.Stroke);
            Assert.Equal(3, swatch.StrokeWidth);
        });
        if (points) Assert.Equal(new[] { Fill(kind, Low), Fill(kind, High) }, swatches.Select(swatch => swatch.Fill!.Value));
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.All(Role(svg, "legend-swatch"), swatch => Assert.Equal(chart.Options.Theme.Resolve(chart.Options.ThemeMode).Foreground.ToCss(), (string?)swatch.Attribute("stroke")));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Scatter, false)]
    [InlineData(ChartSeriesKind.Scatter, true)]
    [InlineData(ChartSeriesKind.Bubble, false)]
    [InlineData(ChartSeriesKind.Bubble, true)]
    public void AutomaticPaintBoundsAndPointRegionsIncludeTheResolvedOutline(ChartSeriesKind kind, bool authoredWidth) {
        var chart = Create(kind, false).WithLegend(false).WithAxes();
        chart.Options.XAxis.Minimum = chart.Options.XAxis.Maximum = null;
        chart.Options.YAxis.Minimum = chart.Options.YAxis.Maximum = null;
        if (authoredWidth) chart.Series[0].Markers.StrokeWidth = 6;
        var prepared = Prepare(chart);
        var horizontal = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "axis-x");
        var vertical = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "axis-y");
        var plot = new ChartRect(horizontal.Start.X, vertical.Start.Y, horizontal.End.X - horizontal.Start.X, vertical.End.Y - vertical.Start.Y);
        var marks = Marks(prepared, kind);
        for (var index = 0; index < marks.Length; index++) {
            var mark = marks[index]; var extent = mark.Rx + mark.StrokeWidth / 2;
            Assert.True(mark.Cx - extent >= plot.Left - 1e-7 && mark.Cx + extent <= plot.Right + 1e-7);
            Assert.True(mark.Cy - extent >= plot.Top - 1e-7 && mark.Cy + extent <= plot.Bottom + 1e-7);
            var region = prepared.Regions.Single(region => region.Id == "series-0-point-" + index);
            Assert.Equal(2 * extent, region.Bounds.Width, 7);
            Assert.Equal(2 * extent, region.Bounds.Height, 7);
        }
        Capture(prepared, $"quantitative-outline-bounds-{kind}-{(authoredWidth ? "authored" : "default")}");
    }

    [Theory]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Bubble)]
    public void EqualRgbTokensResolveAtExportAndRemainInsideTheirPanelColorScope(ChartSeriesKind kind) {
        var shared = ChartColor.FromHex("#35495D");
        var theme = VisualTheme.Graphite().Configure(builder => {
            builder.Light.Foreground = shared; builder.Light.Surface = shared;
        });
        Chart Panel() {
            var panel = Create(kind, false).WithTheme(theme).WithLegend(false);
            panel.Series[0].WithColorScale(ChartColorScale.Sequential(shared, shared).WithValueRange(0, 20));
            return panel;
        }
        SvgColorVariables Variables(string prefix) => new SvgColorVariables()
            .Add(prefix + "-surface", shared, SvgColorRole.Surface)
            .Add(prefix + "-ramp", shared, SvgColorRole.Ramp)
            .AddThemeToken(VisualThemeColorToken.Foreground, prefix + "-foreground");
        var panel = Panel(); var prepared = Prepare(panel); var pixels = prepared.ToPng();
        var exported = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: Variables("--export"))));
        Assert.All(Role(exported, kind == ChartSeriesKind.Bubble ? "bubble" : "marker"), mark => {
            Assert.StartsWith("var(--export-foreground,", (string?)mark.Attribute("stroke"), StringComparison.Ordinal);
            Assert.Contains("var(--export-ramp,", (string?)mark.Attribute("fill"));
        });
        Assert.Equal(pixels, prepared.ToPng());
        var grid = ChartGrid.Create().WithColumns(2).WithPanelThemes().WithPanelSize(360, 280)
            .WithSvgColorVariables(Variables("--outer"))
            .Add(Panel()).Add(Panel().WithSvgColorVariables(Variables("--panel")));
        var gridSvg = XDocument.Parse(grid.Prepare(VisualExportRequest.ForGrid(grid).Context)
            .ToSvg(new VisualSvgOptions(colorVariables: Variables("--export"))));
        var panels = Role(gridSvg, "panel");
        var literal = Role(new XDocument(new XElement(panels[0])), kind == ChartSeriesKind.Bubble ? "bubble" : "marker");
        var mapped = Role(new XDocument(new XElement(panels[1])), kind == ChartSeriesKind.Bubble ? "bubble" : "marker");
        Assert.All(literal, mark => Assert.Equal(shared.ToCss(), (string?)mark.Attribute("stroke")));
        Assert.All(mapped, mark => Assert.StartsWith("var(--panel-foreground,", (string?)mark.Attribute("stroke"), StringComparison.Ordinal));
        Assert.DoesNotContain("--outer-", panels[0].ToString()); Assert.DoesNotContain("--export-", panels[1].ToString());
    }

    [Fact]
    public void CanonicalThemePaintScannerEqualsItsPatternReferenceIncludingOpacityAndMalformedNeighbors() {
        var color = ChartColor.FromRgba(23, 41, 67, 149);
        var token = SvgPaint.ThemeToken(color, VisualThemeColorToken.Foreground, SvgColorRole.Text);
        var translucent = token.WithOpacity(ChartColorMath.WithOpacity(color, .5), .5);
        var markup = "<path stroke=\"" + token.Value + "\"/><path fill=\"" + translucent.Value + "\"/>"
            + "\uFDD0broken\uFDD1" + token.Value + token.Value;
        foreach (var variables in new SvgColorVariables?[] { null, new SvgColorVariables().Add("--legacy-text", color, SvgColorRole.Text),
            new SvgColorVariables().AddThemeToken(VisualThemeColorToken.Foreground, "--canonical-text") }) {
            Assert.Equal(SvgPaint.ResolveWithPattern(markup, variables), SvgPaint.Resolve(markup, variables));
            Assert.Equal(SvgPaint.ResolveWithPattern(markup, variables, true), SvgPaint.Resolve(markup, variables, true));
            Assert.DoesNotContain(token.Value!, SvgPaint.Resolve(markup, variables));
        }
    }

    internal static Chart Create(ChartSeriesKind kind, bool dark, int width = 640) {
        var theme = VisualTheme.Graphite().Configure(builder => {
            builder.Light.Foreground = ChartColor.FromRgba(17, 31, 47, 173);
            builder.Dark.Foreground = ChartColor.FromRgba(223, 231, 241, 197);
        });
        var chart = Chart.Create().WithSize(width, 400).WithTheme(theme, dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithHeader(false).WithLegend().WithDataLabels(false).WithAxes(false).WithGrid(false)
            .WithXAxisBounds(0, 4).WithYAxisBounds(0, 4).ConfigureLabels(labels => labels.Color = "Temperature");
        chart.Options.TransparentBackground = true; chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false;
        if (kind == ChartSeriesKind.Bubble) {
            chart.AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("measured"), "Samples", new[] {
                new ChartBubble(1, 1, 4).WithColorValue(-10), new ChartBubble(3, 3, 9).WithColorValue(100) }));
            chart.ConfigureBubble(bubble => { bubble.MinimumRadius = 18; bubble.MaximumRadius = 18; });
        } else {
            chart.AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("measured"), "Samples", new[] {
                new ChartPoint(1, 1).WithColorValue(-10), new ChartPoint(3, 3).WithColorValue(100) }));
            chart.Series[0].Markers.Radius = 18;
        }
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(Low, High).WithValueRange(0, 20));
        return chart;
    }

    private static ChartColor Fill(ChartSeriesKind kind, ChartColor color) => kind == ChartSeriesKind.Bubble
        ? ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BubbleFillOpacity) : color;

    private static VisualSceneEllipse[] Marks(PreparedVisual prepared, ChartSeriesKind kind) => prepared.Scene.Nodes.OfType<VisualSceneEllipse>()
        .Where(mark => mark.Role == (kind == ChartSeriesKind.Bubble ? "bubble" : "marker")).ToArray();

    private static void AssertPixel(RgbaImage image, double x, double y, ChartColor expected) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        // Premultiplication and unpremultiplication may round a translucent RGB channel by one byte.
        Assert.InRange(Math.Abs(image.Pixels[offset] - expected.R), 0, 1);
        Assert.InRange(Math.Abs(image.Pixels[offset + 1] - expected.G), 0, 1);
        Assert.InRange(Math.Abs(image.Pixels[offset + 2] - expected.B), 0, 1);
        Assert.Equal(expected.A, image.Pixels[offset + 3]);
    }

    private static bool HasContrastingOutlinePixel(RgbaImage image, VisualSceneEllipse mark, ChartColor backdrop, ChartColor center, ChartColor foreground) {
        static int Distance(ChartColor first, ChartColor second) => Math.Abs(first.R - second.R) + Math.Abs(first.G - second.G) + Math.Abs(first.B - second.B);
        var outer = mark.Rx + mark.StrokeWidth / 2 + 1;
        for (var y = (int)Math.Floor(mark.Cy - outer); y <= Math.Ceiling(mark.Cy + outer); y++)
            for (var x = (int)Math.Floor(mark.Cx - outer); x <= Math.Ceiling(mark.Cx + outer); x++) {
                if (x < 0 || y < 0 || x >= image.Width || y >= image.Height) continue;
                var radius = Math.Sqrt(Math.Pow(x + .5 - mark.Cx, 2) + Math.Pow(y + .5 - mark.Cy, 2));
                if (radius < mark.Rx - mark.StrokeWidth / 2 || radius > outer) continue;
                var offset = (y * image.Width + x) * 4;
                var pixel = ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
                if (Distance(pixel, backdrop) > Distance(center, backdrop) + 20 && Distance(pixel, foreground) < Distance(center, foreground) - 20) return true;
            }
        return false;
    }

    private static bool HasExteriorOutlinePixel(RgbaImage image, VisualSceneEllipse mark, ChartColor expected) {
        var outer = mark.Rx + Math.Max(1, mark.StrokeWidth) / 2 + 1;
        for (var y = (int)Math.Floor(mark.Cy - outer); y <= Math.Ceiling(mark.Cy + outer); y++)
            for (var x = (int)Math.Floor(mark.Cx - outer); x <= Math.Ceiling(mark.Cx + outer); x++) {
                if (x < 0 || y < 0 || x >= image.Width || y >= image.Height) continue;
                var distance = Math.Sqrt(Math.Pow(x + .5 - mark.Cx, 2) + Math.Pow(y + .5 - mark.Cy, 2));
                if (distance <= mark.Rx + .01 || distance > outer) continue;
                var offset = (y * image.Width + x) * 4;
                if (image.Pixels[offset + 3] > 10 && image.Pixels[offset + 3] <= expected.A
                    && Math.Abs(image.Pixels[offset] - expected.R) <= 1 && Math.Abs(image.Pixels[offset + 1] - expected.G) <= 1
                    && Math.Abs(image.Pixels[offset + 2] - expected.B) <= 1) return true;
            }
        return false;
    }
}
