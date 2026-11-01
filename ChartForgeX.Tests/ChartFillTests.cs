using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartFillTests {
    private static readonly ChartColor Red = ChartColor.FromHex("#FF0000");
    private static readonly ChartColor Blue = ChartColor.FromHex("#0000FF");

    [Fact]
    public void AuthoringSnapshotsStopsAndPixelsAndRejectsUnusablePaintDefinitions() {
        var stops = Stops(); var fill = ChartFill.LinearGradient(new ChartPoint(0, 0), new ChartPoint(1, 0), stops);
        stops[0] = new ChartGradientStop(0, Blue);
        Assert.Equal(Red, fill.Stops[0].Color);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartGradientStop>)fill.Stops)[0] = stops[0]);
        Assert.Throws<ArgumentNullException>(() => ChartFill.LinearGradient(default, new ChartPoint(1, 0), null!));
        Assert.Throws<ArgumentException>(() => ChartFill.LinearGradient(default, new ChartPoint(1, 0), new ChartGradientStop(0, Red)));
        Assert.Throws<ArgumentException>(() => ChartFill.LinearGradient(default, default, Stops()));
        Assert.Throws<ArgumentException>(() => ChartFill.RadialGradient(default, 1,
            new ChartGradientStop(1, Red), new ChartGradientStop(0, Blue)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartGradientStop(double.NaN, Red));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartGradientStop(1.01, Red));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartFill.RadialGradient(default, 0, Stops()));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartFill.Pattern(Pixels(), double.PositiveInfinity, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartFill.Pattern(Pixels(), 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartFill.Image(default));

        var pixels = Pixels(); var imageFill = ChartFill.Image(pixels);
        Array.Clear(pixels.Pixels, 0, pixels.Pixels.Length);
        var prepared = Surface(imageFill); var svg = prepared.ToSvg();
        var embedded = XDocument.Parse(svg).Descendants().Single(element => element.Name.LocalName == "image");
        var decoded = PngReader.Decode(Convert.FromBase64String(embedded.Attribute("href")!.Value.Substring("data:image/png;base64,".Length)));
        Assert.Equal(new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 }, decoded.Pixels);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, Pixel(PngReader.Decode(prepared.ToPng(NativeOptions())), 12, 20));
    }

    [Theory]
    [InlineData("linear")]
    [InlineData("radial")]
    [InlineData("image")]
    [InlineData("pattern")]
    public void AuthoredInteriorsReachSvgAndDecodedNativePngWithTheirOwnSampling(string kind) {
        var prepared = Surface(Fill(kind));
        var svg = XDocument.Parse(prepared.ToSvg());
        var mark = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "fill-surface");
        Assert.StartsWith("url(#", mark.Attribute("fill")!.Value);
        Assert.Equal("#000000", mark.Attribute("stroke")!.Value);
        var definition = svg.Descendants().Single(element => element.Name.LocalName ==
            (kind == "linear" ? "linearGradient" : kind == "radial" ? "radialGradient" : "pattern"));
        Assert.Equal("objectBoundingBox", definition.Attribute(kind is "image" or "pattern" ? "patternUnits" : "gradientUnits")!.Value);
        var native = PngReader.Decode(prepared.ToPng(NativeOptions()));
        if (kind == "radial") {
            Assert.InRange(Pixel(native, 24, 20)[0], 235, 255);
            Assert.InRange(Pixel(native, 10, 10)[2], 230, 255);
        } else if (kind == "pattern") {
            Assert.Equal(new byte[] { 255, 0, 0, 255 }, Pixel(native, 9, 20));
            Assert.InRange(Pixel(native, 13, 20)[2], 220, 255);
            Assert.Equal(new byte[] { 255, 0, 0, 255 }, Pixel(native, 17, 20));
        } else {
            Assert.InRange(Pixel(native, 11, 20)[0], 220, 255);
            Assert.InRange(Pixel(native, 36, 20)[2], 220, 255);
        }
        Assert.Equal(0, Pixel(native, 2, 2)[3]);
        var imported = SvgRasterizer.Rasterize(prepared.ToSvg("fill-oracle"), strict: true).Image;
        foreach (var coordinate in new[] { (12, 20), (24, 20), (35, 20) }) {
            var expected = Pixel(native, coordinate.Item1, coordinate.Item2); var actual = Pixel(imported, coordinate.Item1, coordinate.Item2);
            for (var channel = 0; channel < 4; channel++) Assert.InRange(Math.Abs(actual[channel] - expected[channel]), 0, 3);
        }
        Capture(prepared, "surface-" + kind);
    }

    [Fact]
    public void NonsquareDiagonalFillKeepsSvgColorPlanesInNativePngBeforeAndAfterRigidTransform() {
        var fill = ChartFill.LinearGradient(default, new ChartPoint(1, 1), Stops());
        foreach (var transformed in new[] { false, true }) {
            var builder = new VisualSceneBuilder(new VisualSize(160, 160), FontSpec.SystemSans());
            using (transformed ? builder.PushTranslation(136, 0) : null)
            using (transformed ? builder.PushRotation(90, 0, 0) : null)
                builder.Rect(new ChartRect(8, 8, 100, 20), Red, role: "diagonal-fill");
            var source = builder.Build();
            var nodes = source.Nodes.Select(node => node is VisualSceneMark mark ? VisualSceneChartFills.ApplyFill(mark, fill) : node).ToArray();
            var prepared = new PreparedVisual(new VisualScene(source.Size, nodes, source.Diagnostics, source.Regions));
            var svg = prepared.ToSvg("nonsquare-diagonal");
            var definition = Assert.Single(XDocument.Parse(svg).Descendants(), element => element.Name.LocalName == "linearGradient");
            Assert.Equal("objectBoundingBox", definition.Attribute("gradientUnits")!.Value);
            Assert.Equal("1", definition.Attribute("x2")!.Value); Assert.Equal("1", definition.Attribute("y2")!.Value);
            var native = PngReader.Decode(prepared.ToPng(NativeOptions()));
            var imported = SvgRasterizer.Rasterize(svg, strict: true).Image;
            var samples = transformed ? new[] { (125, 98), (109, 18) } : new[] { (98, 10), (18, 26) };
            foreach (var (x, y) in samples) {
                // Both off-diagonal centers have normalized x+y = 1.03, hence t = .515.
                var actual = Pixel(native, x, y); var svgPixel = Pixel(imported, x, y);
                Assert.InRange(actual[0], 123, 125); Assert.Equal(0, actual[1]);
                Assert.InRange(actual[2], 130, 132); Assert.Equal(255, actual[3]);
                for (var channel = 0; channel < 4; channel++) Assert.True(Math.Abs(actual[channel] - svgPixel[channel]) <= 2,
                    $"Transform={transformed}, pixel=({x},{y}), channel={channel}: native={actual[channel]}, SVG={svgPixel[channel]}");
            }
            Capture(prepared, transformed ? "diagonal-rotated" : "diagonal-nonsquare");
        }
    }

    [Theory]
    [InlineData("ellipse")]
    [InlineData("compound-path")]
    [InlineData("slice")]
    public void SharedFillOwnerPreservesContoursHolesTransformAndCompletedElementOpacity(string shape) {
        var builder = new VisualSceneBuilder(new VisualSize(64, 64), FontSpec.SystemSans());
        using (builder.PushTranslation(8, 4)) using (builder.PushClip(new ChartRect(0, 0, 40, 40))) {
            if (shape == "ellipse") builder.Ellipse(20, 20, 16, 12, Red, Red, 4, "interior", opacity: .5);
            else if (shape == "slice") builder.Slice(20, 20, 16, 8, 0, Math.PI * 2, Red, Red, 4, "interior", opacity: .5);
            else builder.Path(new ChartPath(Rectangle(4, 4, 32, 32).Concat(Rectangle(12, 12, 16, 16)).ToArray()),
                Red, Red, 4, "interior", close: true, opacity: .5);
        }
        var source = builder.Build(); var nodes = source.Nodes.Select(node => node is VisualSceneMark mark
            ? VisualSceneChartFills.ApplyFill(mark, ChartFill.Pattern(new RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 }), 4, 4)) : node).ToArray();
        Assert.Equal(source.Nodes.Count, nodes.Length);
        var prepared = new PreparedVisual(new VisualScene(source.Size, nodes, source.Diagnostics, source.Regions));
        var image = PngReader.Decode(prepared.ToPng(NativeOptions()));
        Assert.Equal(128, Pixel(image, 41, 24)[3]);
        Assert.Equal(0, Pixel(image, 2, 2)[3]);
        Assert.Equal(shape == "ellipse" ? 128 : 0, Pixel(image, 28, 24)[3]);
        var svg = prepared.ToSvg(); Assert.Contains("opacity=\"0.5\"", svg); Assert.Contains("translate(8 4)", svg);
        if (shape == "compound-path") Assert.Contains("fill-rule=\"evenodd\"", svg);
        Capture(prepared, "contour-" + shape);
    }

    [Theory]
    [InlineData("bar")]
    [InlineData("horizontal-bar")]
    [InlineData("area")]
    [InlineData("scatter")]
    [InlineData("pie")]
    [InlineData("donut")]
    [InlineData("radar")]
    [InlineData("heatmap")]
    [InlineData("treemap")]
    [InlineData("funnel")]
    [InlineData("circle")]
    [InlineData("progress-ring")]
    public void SeriesFillUsesTheSharedFamilyPassAndPreparationDetachesAuthoredEdits(string family) {
        var chart = Family(family); var fill = Fill("linear");
        Assert.Same(chart.Series[0], chart.Series[0].WithFill(fill));
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng(NativeOptions());
        var document = XDocument.Parse(svg);
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneGradient>(), node =>
            node.Shape.Role?.EndsWith("-center", StringComparison.Ordinal) == true || node.Shape.Role?.Contains("track", StringComparison.Ordinal) == true);
        Assert.Contains(document.Descendants(), element => element.Name.LocalName == "linearGradient"
            && (string?)element.Attribute("gradientUnits") == "objectBoundingBox");
        Assert.Contains(document.Descendants(), element => ((string?)element.Attribute("fill"))?.StartsWith("url(#", StringComparison.Ordinal) == true);
        var pixels = PngReader.Decode(png).Pixels;
        Assert.Contains(Enumerable.Range(0, pixels.Length / 4), index =>
            pixels[index * 4] > 40 && pixels[index * 4 + 1] < 10 && pixels[index * 4 + 2] > 40 && pixels[index * 4 + 3] > 200);
        chart.Series[0].WithFill(null);
        Assert.DoesNotContain(Prepare(chart).Scene.Nodes.OfType<VisualSceneGradient>(), node => node.AuthoredFill != null);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(NativeOptions()));
        Capture(prepared, "family-" + family);
    }

    [Fact]
    public void DatumFillOverridesSeriesFillAndNullCallbackResultsRetainTheSeriesPaint() {
        var chart = Family("bar"); var calls = 0;
        chart.Series[0].PointFillOverrides[1] = Fill("image");
        chart.Series[0].WithFill(Fill("linear")).WithItemStyler<ChartPoint>(context => {
            calls++; return context.Index == 1 ? new ChartItemStyle(fill: Fill("radial")) : null;
        });
        var prepared = Prepare(chart); var svg = prepared.ToSvg();
        Assert.Equal(3, calls);
        Assert.Equal(2, XDocument.Parse(svg).Descendants().Count(element => element.Name.LocalName == "linearGradient"));
        Assert.Single(XDocument.Parse(svg).Descendants(), element => element.Name.LocalName == "radialGradient");
        var native = PngReader.Decode(prepared.ToPng(NativeOptions()));
        var surfaces = prepared.Scene.Nodes.OfType<VisualSceneGradient>().Where(node => node.Shape.Role == "bar").ToArray();
        Assert.Equal(3, surfaces.Length);
        var radial = Assert.Single(surfaces, node => node.AuthoredFill?.Kind == ChartFillKind.RadialGradient);
        var bounds = radial.FillBounds;
        Assert.InRange(Pixel(native, (int)(bounds.X + bounds.Width / 2), (int)(bounds.Y + bounds.Height / 2))[0], 220, 255);
        chart.Series[0].WithItemStyler<ChartPoint>(null).WithFill(null);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(3, calls);
        Capture(prepared, "datum-override");
    }

    [Fact]
    public void ExplicitDatumColorSuppressesInheritedSeriesGradientInSvgAndNativePng() {
        var green = ChartColor.FromHex("#00FF00"); var chart = Family("bar");
        chart.Series[0].WithFill(Fill("linear")).WithItemStyler<ChartPoint>(context =>
            context.Index == 1 ? new ChartItemStyle(color: green) : null);
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg());
        var solid = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneMark>(), mark => mark.Role == "bar" && mark.Fill.Equals(green));
        var element = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "bar"
            && (string?)node.Attribute("fill") == "#00FF00");
        Assert.Equal("#00FF00", element.Attribute("fill")!.Value);
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneGradient>().Count(node => node.Shape.Role == "bar"));
        var bounds = VisualSceneChartFills.Bounds(solid); var image = PngReader.Decode(prepared.ToPng(NativeOptions()));
        Assert.Equal(new byte[] { 0, 255, 0, 255 }, Pixel(image, (int)(bounds.X + bounds.Width / 2), (int)(bounds.Y + bounds.Height / 2)));
        Capture(prepared, "datum-solid-over-gradient");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeVisibilityReflowRetainsCapturedSeriesAndDatumFillsWithoutCallingTheDelegate(bool pointOverride) {
        var calls = 0; var seriesId = new ChartSeriesId("fills");
        var series = new ChartSeries(seriesId, "Values", ChartSeriesKind.Bar,
            new[] { new ChartPoint(0, 4), new ChartPoint(1, 8), new ChartPoint(2, 6) });
        series.WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("hidden"), new ChartObservationId("last") });
        series.WithFill(Fill("linear"));
        if (pointOverride) {
            series.WithFill(null);
            series.PointFillOverrides[0] = Fill("linear");
            series.PointFillOverrides[2] = Fill("radial");
        }
        else series.WithItemStyler<ChartPoint>(context => {
            calls++; return context.Index == 2 ? new ChartItemStyle(fill: Fill("radial")) : null;
        });
        var chart = Chart.Create().WithSize(320, 240).WithHeader(false).WithLegend(false).WithDataLabels(false).AddSeries(series);
        var renderer = new HtmlInteractiveChartRenderer();
        var section = renderer.PrepareVisibilitySection(chart, new ChartVisibilityView(Array.Empty<ChartVisibilityTarget>(), ChartVisibilityLayout.Reflow));
        var source = section.PreparedVisual.Scene.VisibilityCapture!.ReflowSource!;
        var before = calls;
        series.WithFill(null).WithItemStyler<ChartPoint>(null);
        series.PointFillOverrides.Clear();
        var retained = source.CreateNativeView(new ChartVisibilityView(new[] {
            ChartVisibilityTarget.Observation(seriesId, new ChartObservationId("hidden")) }, ChartVisibilityLayout.Reflow));
        var prepared = Prepare(retained); var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(before, calls); Assert.Equal(!pointOverride, before > 0);
        Assert.Single(svg.Descendants(), element => element.Name.LocalName == "radialGradient");
        Assert.Single(svg.Descendants(), element => element.Name.LocalName == "linearGradient");
        var radial = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>(), node => node.AuthoredFill?.Kind == ChartFillKind.RadialGradient);
        var bounds = radial.FillBounds; var image = PngReader.Decode(prepared.ToPng(NativeOptions()));
        Assert.InRange(Pixel(image, (int)(bounds.X + bounds.Width / 2), (int)(bounds.Y + bounds.Height / 2))[0], 220, 255);
        Capture(prepared, "visibility-retained-fills");
    }

    [Fact]
    public void NativeSourceKeyIncludesHiddenPointFillOverrides() {
        var renderer = new HtmlInteractiveChartRenderer();
        var first = IdentityChart(Fill("linear"), false);
        var second = IdentityChart(Fill("linear"), false);
        first.Series[0].PointFillOverrides[1] = Fill("radial");
        second.Series[0].PointFillOverrides[1] = Fill("image");
        var before = renderer.PrepareVisibilitySection(first, HiddenFillView());
        var after = renderer.PrepareVisibilitySection(second, HiddenFillView());
        Assert.Equal(before.PreparedVisual.ToSvg("hidden-point-fill"), after.PreparedVisual.ToSvg("hidden-point-fill"));
        Assert.Equal(before.PreparedVisual.ToPng(NativeOptions()), after.PreparedVisual.ToPng(NativeOptions()));
        Assert.NotEqual(before.PreparedVisual.Scene.VisibilityCapture!.SourceKey, after.PreparedVisual.Scene.VisibilityCapture!.SourceKey);
    }

    [Fact]
    public void EmbeddedFillResourcesUseExistingSvgOutputAndNativeMemoryBudgets() {
        var pixels = new byte[128 * 128 * 4]; var random = new Random(419);
        random.NextBytes(pixels);
        var fill = ChartFill.Pattern(new RgbaImage(128, 128, pixels), 16, 16); var source = Surface(fill).Scene;
        var scene = new VisualScene(source.Size, new[] { source.Nodes[0], source.Nodes[0] }, source.Diagnostics, source.Regions);
        Assert.Equal(pixels.LongLength, VisualSceneRasterRenderer.BorrowedNativeImageBytes(scene));
        var plain = ((VisualSceneGradient)source.Nodes[0]).Shape;
        var control = new VisualScene(source.Size, new[] { plain }, source.Diagnostics, source.Regions);
        Assert.NotEmpty(VisualSceneSvgRenderer.Render(control, maximumCharacters: 1024));
        var error = Assert.Throws<VisualResourceLimitException>(() => VisualSceneSvgRenderer.Render(scene, maximumCharacters: 1024));
        Assert.Equal("MaximumOutputBytes", error.Limit);
    }

    [Theory]
    [InlineData("gradient-end", false)] [InlineData("gradient-end", true)]
    [InlineData("gradient-offset", false)] [InlineData("gradient-offset", true)]
    [InlineData("gradient-color", false)] [InlineData("gradient-color", true)]
    [InlineData("radial-center", false)] [InlineData("radial-center", true)]
    [InlineData("radial-radius", false)] [InlineData("radial-radius", true)]
    [InlineData("image-pixels", false)] [InlineData("image-pixels", true)]
    [InlineData("image-shape", false)] [InlineData("image-shape", true)]
    [InlineData("pattern-width", false)] [InlineData("pattern-width", true)]
    [InlineData("pattern-height", false)] [InlineData("pattern-height", true)]
    [InlineData("fill-kind", false)] [InlineData("fill-kind", true)]
    public void NativeSourceKeyIncludesUnpaintedSeriesAndHiddenDatumFillFacts(string difference, bool datum) {
        var (before, after) = IdentityFills(difference); var renderer = new HtmlInteractiveChartRenderer();
        var first = renderer.PrepareVisibilitySection(IdentityChart(before, datum), HiddenFillView());
        var second = renderer.PrepareVisibilitySection(IdentityChart(after, datum), HiddenFillView());
        Assert.Equal(first.PreparedVisual.ToSvg("same-unpainted-fills"), second.PreparedVisual.ToSvg("same-unpainted-fills"));
        Assert.Equal(first.PreparedVisual.ToPng(NativeOptions()), second.PreparedVisual.ToPng(NativeOptions()));
        Assert.NotEqual(first.PreparedVisual.Scene.VisibilityCapture!.SourceKey, second.PreparedVisual.Scene.VisibilityCapture!.SourceKey);
        var repeated = renderer.PrepareVisibilitySection(IdentityChart(before, datum), HiddenFillView());
        Assert.Equal(first.PreparedVisual.Scene.VisibilityCapture.SourceKey, repeated.PreparedVisual.Scene.VisibilityCapture!.SourceKey);
    }

    private static Chart IdentityChart(ChartFill fill, bool datum) {
        var series = new ChartSeries(new ChartSeriesId("fill-identity"), "Values", ChartSeriesKind.Line,
            new[] { new ChartPoint(0, 4), new ChartPoint(1, 8), new ChartPoint(2, 6) });
        series.WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("hidden"), new ChartObservationId("last") });
        series.ConfigureMarkers(markers => markers.Enabled = false);
        if (datum) series.WithItemStyler<ChartPoint>(context => context.Index == 1 ? new ChartItemStyle(fill: fill) : null);
        else series.WithFill(fill);
        return Chart.Create().WithSize(320, 240).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithLineMarkers(ChartLineMarkerMode.None).AddSeries(series);
    }

    private static ChartVisibilityView HiddenFillView() => new(new[] {
        ChartVisibilityTarget.Observation(new ChartSeriesId("fill-identity"), new ChartObservationId("hidden")) }, ChartVisibilityLayout.Reflow);

    private static (ChartFill Before, ChartFill After) IdentityFills(string difference) {
        var linear = Fill("linear"); var image = Pixels();
        return difference switch {
            "gradient-end" => (linear, ChartFill.LinearGradient(default, new ChartPoint(0, 1), Stops())),
            "gradient-offset" => (linear, ChartFill.LinearGradient(default, new ChartPoint(1, 0), new ChartGradientStop(.25, Red), new ChartGradientStop(1, Blue))),
            "gradient-color" => (linear, ChartFill.LinearGradient(default, new ChartPoint(1, 0), new ChartGradientStop(0, Red.WithAlpha(128)), new ChartGradientStop(1, Blue))),
            "radial-center" => (Fill("radial"), ChartFill.RadialGradient(new ChartPoint(.25, .5), .5, Stops())),
            "radial-radius" => (Fill("radial"), ChartFill.RadialGradient(new ChartPoint(.5, .5), .75, Stops())),
            "image-pixels" => (ChartFill.Image(image), ChartFill.Image(new RgbaImage(2, 1, new byte[] { 254, 0, 0, 255, 0, 0, 255, 255 }))),
            "image-shape" => (ChartFill.Image(image), ChartFill.Image(new RgbaImage(1, 2, image.Pixels))),
            "pattern-width" => (ChartFill.Pattern(image, 8, 8), ChartFill.Pattern(image, 9, 8)),
            "pattern-height" => (ChartFill.Pattern(image, 8, 8), ChartFill.Pattern(image, 8, 9)),
            "fill-kind" => (ChartFill.Image(image), ChartFill.Pattern(image, 8, 8)),
            _ => throw new ArgumentOutOfRangeException(nameof(difference))
        };
    }

    private static Chart Family(string family) {
        var chart = Chart.Create().WithSize(320, 240).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false);
        var points = new[] { new ChartPoint(0, 4), new ChartPoint(1, 8), new ChartPoint(2, 6) };
        return family switch {
            "bar" => chart.AddBar("Values", points), "horizontal-bar" => chart.AddHorizontalBar("Values", points),
            "area" => chart.AddArea("Values", points), "scatter" => chart.AddScatter("Values", points),
            "pie" => chart.AddPie("Values", points), "donut" => chart.AddDonut("Values", points),
            "radar" => chart.AddRadarArea("Values", points), "heatmap" => chart.AddHeatmapRow("Values", points),
            "treemap" => chart.AddTreemap("Values", new[] { new ChartHierarchyItem("a", "A", value: 4), new ChartHierarchyItem("b", "B", value: 8) }),
            "funnel" => chart.AddFunnel("Values", points), "circle" => chart.AddCircle("Values", 65),
            "progress-ring" => chart.AddProgressRing("Values", new[] { new ChartPoint(0, 65), new ChartPoint(1, 40) }),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
    }
    private static ChartGradientStop[] Stops() => new[] { new ChartGradientStop(0, Red), new ChartGradientStop(1, Blue) };
    private static RgbaImage Pixels() => new(2, 1, new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 });
    private static ChartFill Fill(string kind) => kind switch {
        "linear" => ChartFill.LinearGradient(default, new ChartPoint(1, 0), Stops()),
        "radial" => ChartFill.RadialGradient(new ChartPoint(.5, .5), .5, Stops()),
        "image" => ChartFill.Image(Pixels()), "pattern" => ChartFill.Pattern(Pixels(), 8, 8),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static PreparedVisual Surface(ChartFill fill) {
        var mark = new VisualSceneRectangle(new ChartRect(8, 8, 32, 24), 0, Red, ChartColor.Black, 2, "fill-surface", "fill-id");
        return new PreparedVisual(new VisualScene(new VisualSize(48, 40), new[] { VisualSceneChartFills.ApplyFill(mark, fill) },
            Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>()));
    }
    private static IEnumerable<ChartPathCommand> Rectangle(double x, double y, double width, double height) => new[] {
        ChartPathCommand.MoveTo(x, y), ChartPathCommand.LineTo(x + width, y), ChartPathCommand.LineTo(x + width, y + height),
        ChartPathCommand.LineTo(x, y + height), ChartPathCommand.LineTo(x, y)
    };
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualRenderOptions NativeOptions() => new(supersampling: 1);
    private static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_FILL_CAPTURE_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng(NativeOptions()));
    }
}
