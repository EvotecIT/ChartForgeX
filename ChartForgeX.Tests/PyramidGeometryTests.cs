using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Markup;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects independent triangle proportions, ordered source facts and shared static/adapter exports.</summary>
public sealed class PyramidGeometryTests {
    [Fact]
    public void OptionsAreExplicitAndInvalidInputDoesNotMutateTheChart() {
        var chart = Chart.Create();
        Assert.Equal(ChartPyramidValueEncoding.Height, chart.Options.Pyramid.ValueEncoding);
        Assert.Equal(ChartOrientation.Vertical, chart.Options.Pyramid.Orientation);
        Assert.False(chart.Options.Pyramid.Reversed); Assert.Null(chart.Options.Pyramid.AspectRatio);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Pyramid.ValueEncoding = (ChartPyramidValueEncoding)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Pyramid.Orientation = (ChartOrientation)99);
        Assert.Throws<ArgumentNullException>(() => chart.WithPyramid(null!));
        foreach (var ratio in new[] { 0d, -1, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Pyramid.AspectRatio = ratio);
        foreach (var values in new[] { new[] { 1d, -1 }, new[] { 1d, double.NaN }, new[] { double.PositiveInfinity }, new[] { double.MaxValue, double.MaxValue } }) {
            Assert.Throws<ArgumentOutOfRangeException>(() => chart.AddPyramid("Invalid", Points(values)));
            Assert.Empty(chart.Series);
        }
        Assert.True(ChartSeriesKindCapabilities.IsExclusive(ChartSeriesKind.Pyramid));
        chart.AddPyramid("Valid", Points(1));
        chart.Series[0].Points[0] = new ChartPoint(1, -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Prepare(new VisualRenderContext()));
    }

    [Theory]
    [InlineData(ChartOrientation.Vertical, false, ChartPyramidValueEncoding.Height)]
    [InlineData(ChartOrientation.Vertical, true, ChartPyramidValueEncoding.Height)]
    [InlineData(ChartOrientation.Horizontal, false, ChartPyramidValueEncoding.Height)]
    [InlineData(ChartOrientation.Horizontal, true, ChartPyramidValueEncoding.Height)]
    [InlineData(ChartOrientation.Vertical, false, ChartPyramidValueEncoding.Area)]
    [InlineData(ChartOrientation.Vertical, true, ChartPyramidValueEncoding.Area)]
    [InlineData(ChartOrientation.Horizontal, false, ChartPyramidValueEncoding.Area)]
    [InlineData(ChartOrientation.Horizontal, true, ChartPyramidValueEncoding.Area)]
    public void IndependentLengthsAreasAndNativePixelsFollowTheDeclaredEncoding(ChartOrientation orientation, bool reversed, ChartPyramidValueEncoding encoding) {
        var chart = Pyramid(50, 30, 20).WithPyramid(options => { options.Orientation = orientation; options.Reversed = reversed; options.ValueEncoding = encoding; });
        var scene = Compile(chart); var marks = Marks(scene); var stages = Stages(scene);
        var expected = new[] { .5, .3, .2 };
        var areas = marks.Select(PolygonArea).ToArray();
        var length = orientation == ChartOrientation.Vertical ? 240 : 400;
        Assert.Equal(400 * 240 / 2d, areas.Sum(), 8);
        var png = RasterImageDecoder.Decode(new PreparedVisual(scene).ToPng());
        for (var index = 0; index < marks.Length; index++) {
            var span = orientation == ChartOrientation.Vertical ? Extent(marks[index], false) : Extent(marks[index], true);
            Assert.Equal(expected[index], encoding == ChartPyramidValueEncoding.Height ? span / length : areas[index] / areas.Sum(), 10);
            Assert.Equal(expected[index], Number(stages[index], "data-cfx-value-fraction"), 10);
            Assert.Equal(span / length, Number(stages[index], "data-cfx-length-fraction"), 10);
            Assert.Equal(areas[index] / areas.Sum(), Number(stages[index], "data-cfx-area-fraction"), 10);
            Assert.Equal("series-0-point-" + index, stages[index].Id);
            Assert.Equal(index.ToString(CultureInfo.InvariantCulture), stages[index].Metadata["data-cfx-point"]);
            Assert.Equal((index + 1).ToString(CultureInfo.InvariantCulture), stages[index].Metadata["data-cfx-x"]);
            var bounds = scene.Regions.Single(region => region.Role == "pyramid-stage" && region.Id == stages[index].Id).Bounds;
            var pixel = Pixel(png, bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
            var color = chart.Series[0].PointColors[index]!.Value;
            Assert.Equal(new[] { color.R, color.G, color.B, color.A }, pixel);
            Assert.All(marks[index].Commands, command => { Assert.InRange(command.X, 0, 400); Assert.InRange(command.Y, 0, 240); });
        }
        var firstCenter = Center(marks[0], orientation); var lastCenter = Center(marks[2], orientation);
        Assert.Equal(reversed, firstCenter > lastCenter);
        var svg = XDocument.Parse(new PreparedVisual(scene).ToSvg());
        Assert.Equal(3, Roles(svg, "pyramid-segment").Length);
        Assert.Equal(encoding == ChartPyramidValueEncoding.Height ? "height" : "area", Assert.Single(Roles(svg, "pyramid-chart")).Attribute("data-cfx-value-encoding")!.Value);
        Assert.Equal(new[] { "50", "30", "20" }, Roles(svg, "pyramid-stage").Select(element => element.Attribute("data-cfx-value")!.Value));
    }

    [Theory]
    [InlineData(ChartPyramidValueEncoding.Height)]
    [InlineData(ChartPyramidValueEncoding.Area)]
    public void ZeroSingletonAndAllZeroInputsKeepHonestGeometryAndSourceSlots(ChartPyramidValueEncoding encoding) {
        var scene = Compile(Pyramid(0, 50, 0, 50, 0).WithPyramid(options => options.ValueEncoding = encoding));
        Assert.Equal(2, Marks(scene).Length); Assert.Equal(5, Stages(scene).Length);
        foreach (var index in new[] { 0, 2, 4 }) {
            Assert.Equal(0, Number(Stages(scene)[index], "data-cfx-length-fraction"));
            Assert.Equal(0, Number(Stages(scene)[index], "data-cfx-area-fraction"));
            Assert.Equal("true", Stages(scene)[index].Metadata["data-cfx-zero"]);
            Assert.Equal(0, scene.Regions.Single(region => region.Id == "series-0-point-" + index).Bounds.Height);
        }
        var singleton = Compile(Pyramid(8).WithPyramid(options => options.ValueEncoding = encoding));
        Assert.Equal(400 * 240 / 2d, PolygonArea(Assert.Single(Marks(singleton))), 8);
        var empty = Compile(Pyramid(0, 0, 0).WithPyramid(options => options.ValueEncoding = encoding));
        Assert.Empty(Marks(empty)); Assert.Equal(3, Stages(empty).Length);
        Assert.All(Stages(empty), stage => {
            Assert.Equal(0, Number(stage, "data-cfx-area-fraction"));
            Assert.Equal("false", stage.Metadata["data-cfx-value-fraction-defined"]);
        });
        Assert.All(empty.Regions.Where(region => region.Role == "pyramid-stage"), region => Assert.EndsWith("no positive total", region.Label));
        Assert.Contains(empty.Diagnostics, diagnostic => diagnostic.Code == "pyramid.all-zero");
        Assert.Single(empty.Nodes, node => node.Role == "pyramid-no-data");
    }

    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(1E-200)]
    [InlineData(1E200)]
    [InlineData(double.MaxValue / 4)]
    public void FiniteWeightsUseRatiosBeforeGeometryMultiplication(double value) {
        var scene = Compile(Pyramid(value, value, value));
        Assert.All(Stages(scene), stage => Assert.Equal(1d / 3, Number(stage, "data-cfx-length-fraction"), 12));
        Assert.All(Marks(scene).SelectMany(mark => mark.Commands), command => { Assert.True(double.IsFinite(command.X)); Assert.True(double.IsFinite(command.Y)); });
        var tiny = Compile(Pyramid(1E-12, 1));
        Assert.True(Number(Stages(tiny)[0], "data-cfx-length-fraction") < 1E-10);
        Assert.True(Extent(Marks(tiny)[0], false) < 1E-7);
    }

    [Theory]
    [InlineData(false, ChartPyramidValueEncoding.Height)]
    [InlineData(true, ChartPyramidValueEncoding.Height)]
    [InlineData(false, ChartPyramidValueEncoding.Area)]
    [InlineData(true, ChartPyramidValueEncoding.Area)]
    public void FiniteRoundedAggregateIsAcceptedInBothOrdersWithoutChangingSourceWeights(bool maximumFirst, ChartPyramidValueEncoding encoding) {
        var values = maximumFirst ? new[] { double.MaxValue, 1d } : new[] { 1d, double.MaxValue };
        var chart = Pyramid(values).WithPyramid(options => options.ValueEncoding = encoding);
        var prepared = chart.Prepare(new VisualRenderContext());
        var stages = Stages(prepared.Scene);
        Assert.Equal(values, chart.Series[0].Points.Select(point => point.Y));
        Assert.Equal(values, stages.Select(stage => Number(stage, "data-cfx-value")));
        var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "pyramid-chart");
        Assert.Equal(double.MaxValue, Number(group, "data-cfx-total"));
        var tinyIndex = maximumFirst ? 1 : 0;
        Assert.Equal("series-0-point-" + tinyIndex, stages[tinyIndex].Id);
        Assert.Equal("false", stages[tinyIndex].Metadata["data-cfx-zero"]);
        Assert.Equal("true", stages[tinyIndex].Metadata["data-cfx-geometry-collapsed"]);
        Assert.True(Number(stages[tinyIndex], "data-cfx-value-fraction") > 0);
        Assert.Single(Marks(prepared.Scene));
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "pyramid.precision");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TrueAggregateOverflowIsRejectedInBothOrdersBeforeAppendAndAtPreparationOrLayout(bool maximumFirst) {
        var values = maximumFirst ? new[] { double.MaxValue, 1E292 } : new[] { 1E292, double.MaxValue };
        var chart = Pyramid(7, 8);
        var existingSeries = Assert.Single(chart.Series);
        var existingPoints = existingSeries.Points.ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.AddPyramid("Overflow", Points(values)));
        Assert.Same(existingSeries, Assert.Single(chart.Series));
        Assert.Equal(existingPoints, existingSeries.Points);
        for (var index = 0; index < values.Length; index++) existingSeries.Points[index] = new ChartPoint(index + 1, values[index]);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Prepare(new VisualRenderContext()));
        Assert.Throws<ArgumentOutOfRangeException>(() => Compile(chart));
    }

    [Theory]
    [InlineData(ChartPyramidValueEncoding.Height)]
    [InlineData(ChartPyramidValueEncoding.Area)]
    public void PositiveWeightsBelowBoundaryPrecisionKeepTheirSourceFactAndExplicitDiagnostic(ChartPyramidValueEncoding encoding) {
        var scene = Compile(Pyramid(1, 1E-20).WithPyramid(options => options.ValueEncoding = encoding));
        Assert.Single(Marks(scene));
        var tiny = Stages(scene)[1];
        Assert.Equal("1E-20", tiny.Metadata["data-cfx-value"]);
        Assert.Equal("false", tiny.Metadata["data-cfx-zero"]);
        Assert.Equal("true", tiny.Metadata["data-cfx-geometry-collapsed"]);
        Assert.Equal(1E-20, Number(tiny, "data-cfx-value-fraction"));
        Assert.Equal(0, Number(tiny, "data-cfx-length-fraction"));
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "pyramid.precision");
    }

    [Theory]
    [InlineData(ChartOrientation.Vertical)]
    [InlineData(ChartOrientation.Horizontal)]
    public void AspectRatioFitsAndCentersTheTriangleWithoutChangingTheEncoding(ChartOrientation orientation) {
        var chart = Pyramid(50, 30, 20).WithPyramid(options => { options.Orientation = orientation; options.AspectRatio = 1.5; });
        var marks = Marks(Compile(chart));
        var cross = orientation == ChartOrientation.Vertical ? marks.Max(mark => Extent(mark, true)) : marks.Max(mark => Extent(mark, false));
        var process = marks.Sum(mark => Extent(mark, orientation == ChartOrientation.Horizontal));
        Assert.Equal(1.5, cross / process, 12);
        var points = marks.SelectMany(mark => mark.Commands).ToArray();
        Assert.Equal(200, (points.Max(point => point.X) + points.Min(point => point.X)) / 2, 10);
        Assert.Equal(120, (points.Max(point => point.Y) + points.Min(point => point.Y)) / 2, 10);
        foreach (var ratio in new[] { double.Epsilon, double.MaxValue }) {
            chart.Options.Pyramid.AspectRatio = ratio;
            Assert.All(Marks(Compile(chart)).SelectMany(mark => mark.Commands), command => { Assert.True(double.IsFinite(command.X)); Assert.True(double.IsFinite(command.Y)); });
        }
    }

    [Fact]
    public void PreparedPaintPatternsReadableLabelsAndDetachedExportsSurviveSourceMutation() {
        var chart = Pyramid(50, 30, 20, 0).WithDataLabels().WithSize(360, 360).WithPointLegend()
            .WithPyramid(options => { options.ValueEncoding = ChartPyramidValueEncoding.Area; options.Reversed = true; });
        chart.Series[0].WithPointFillPattern(1, ChartFillPattern.Crosshatch).WithPointLabel(0, "Complete")
            .WithPointDataLabelStyle(0, style => { style.Color = ChartColor.FromHex("#B14091"); style.FontWeight = "700"; });
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 360))));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "fill-pattern");
        Assert.Equal(chart.Series[0].PointColors[0], Marks(prepared.Scene)[0].Fill);
        Assert.Equal(ChartColor.FromHex("#B14091"), prepared.Scene.Nodes.OfType<VisualSceneText>().First(text => text.Role == "pyramid-label").Color);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "pyramid-label"), text => Assert.True(text.Text.Size >= 10));
        Assert.Contains(prepared.Regions, region => region.Role == "pyramid-label" && region.Label == "Stage 0: Complete");
        var artifact = prepared.ToArtifact("pyramid", VisualArtifactKind.Chart);
        Assert.Contains("data-cfx-value-encoding=\"area\"", artifact.ToSvg());
        Assert.Contains("data-cfx-value=\"50\"", artifact.ToHtmlPage());
        Assert.DoesNotContain("<script", artifact.ToHtmlPage());
        var json = artifact.ToInterchangeJson();
        chart.Series[0].WithPointColor(0, "#000000"); chart.Series[0].Points.Clear();
        chart.Options.Pyramid.ValueEncoding = ChartPyramidValueEncoding.Height; chart.Options.Pyramid.Reversed = false;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(png, artifact.ToPng());
        Assert.Equal(json, artifact.ToInterchangeJson());
    }

    [Fact]
    public void MarkupAndCanonicalGalleryFactoriesReachThePyramidProducer() {
        var parsed = new MarkupChartParser().Parse("```chartforgex chart v1 {type=\"pyramid\"}\nlabels A B C\nseries Allocation values 50 30 20\n```");
        Assert.False(parsed.HasErrors); Assert.Equal(ChartSeriesKind.Pyramid, Assert.Single(parsed.Document!.Chart.Series).Kind);
        Assert.Contains("pyramid-stage", parsed.Document.Chart.ToSvg());
        foreach (var variant in new[] { "wide", "compact", "options", "compact-options" }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var chart = V2GalleryModels.Create(ChartSeriesKind.Pyramid, variant, mode);
            var size = variant.StartsWith("compact", StringComparison.Ordinal) ? new VisualSize(360, 360) : new VisualSize(800, 440);
            var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(size), VisualTheme.Graphite(), mode));
            Assert.Equal(3, Marks(prepared.Scene).Length); Assert.NotEmpty(prepared.ToPng());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReversedPyramidUsesAuthoredSourceIdentityForRealKeyboardSelection(bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Pyramid(50, 30, 20, 0).WithSize(596, 338).WithDataLabels()
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithPyramid(options => { options.Orientation = ChartOrientation.Horizontal; options.Reversed = true; });
        chart.Series[0].WithInteractionKey("allocation");
        await using var session = await InteractiveChartBrowser.OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        var stage = page.Locator("g[data-cfx-role='pyramid-stage'][data-cfx-point='1']");
        Assert.Equal("allocation:1", await stage.GetAttributeAsync("data-cfx-target-id"));
        await page.EvaluateAsync("() => { window.pyramidSelection = null; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.pyramidSelection = event.detail); }");
        await stage.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await stage.GetAttributeAsync("aria-selected"));
        Assert.Equal("1", await page.EvaluateAsync<string>("() => window.pyramidSelection.target.sourcePoint"));
        Assert.Equal("30", await stage.GetAttributeAsync("data-cfx-value"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions {
                Path = Path.Combine(capture, "pyramid-selection-" + (dark ? "dark" : "light") + ".png")
            });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static Chart Pyramid(params double[] values) {
        var chart = Chart.Create().WithXLabels(values.Select((_, index) => "Stage " + index).ToArray()).AddPyramid("Allocation", Points(values));
        var colors = new[] { "#2468AC", "#BA7542", "#34957A", "#8762A3", "#7297AA" };
        for (var index = 0; index < values.Length; index++) chart.Series[0].WithPointColor(index, colors[index % colors.Length]);
        return chart;
    }
    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
    private static VisualScene Compile(Chart chart) {
        var context = new VisualRenderContext(); var builder = new VisualSceneBuilder(new VisualSize(400, 240), context.Font);
        VisualSpecialtyCompiler.Build(chart, context, builder, new ChartRect(0, 0, 400, 240)); return builder.Build();
    }
    private static VisualScenePath[] Marks(VisualScene scene) => scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "pyramid-segment").ToArray();
    private static VisualSceneGroup[] Stages(VisualScene scene) => scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "pyramid-stage").ToArray();
    private static double Number(VisualSceneGroup group, string key) => double.Parse(group.Metadata[key], CultureInfo.InvariantCulture);
    private static double Extent(VisualScenePath path, bool x) => x ? path.Commands.Max(command => command.X) - path.Commands.Min(command => command.X)
        : path.Commands.Max(command => command.Y) - path.Commands.Min(command => command.Y);
    private static double Center(VisualScenePath path, ChartOrientation orientation) => orientation == ChartOrientation.Vertical
        ? (path.Commands.Max(command => command.Y) + path.Commands.Min(command => command.Y)) / 2
        : (path.Commands.Max(command => command.X) + path.Commands.Min(command => command.X)) / 2;
    private static double PolygonArea(VisualScenePath path) {
        var area = 0d;
        for (var index = 0; index < path.Commands.Count; index++) {
            var point = path.Commands[index]; var next = path.Commands[(index + 1) % path.Commands.Count];
            area += point.X * next.Y - next.X * point.Y;
        }
        return Math.Abs(area) / 2;
    }
    private static byte[] Pixel(RgbaImage image, double x, double y) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        return new[] { image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3] };
    }
    private static XElement[] Roles(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
