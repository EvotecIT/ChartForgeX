using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the script-free SVG coordinate contract; browser dimensions are verified separately.</summary>
public sealed class ScriptFreeFluidSvgTests {
    [Fact]
    public void FluidExport_StretchesHorizontalGeometryButKeepsPixelSizedMarkersAndStrokes() {
        var builder = Builder();
        builder.Rect(new ChartRect(40, 20, 200, 18), ChartColor.Black, radius: 4, role: "bar");
        builder.Ellipse(200, 60, 5, 5, ChartColor.Black, role: "marker");
        builder.Line(40, 100, 360, 100, ChartColor.Black, 2, role: "axis", dash: new[] { 4d, 2d }, dashOffset: 1);
        builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(40, 80), ChartPathCommand.LineTo(360, 40) }),
            stroke: ChartColor.Black, strokeWidth: 2, role: "line");
        var scene = builder.Build();
        var root = Render(scene);
        Assert.Equal("100%", (string?)root.Attribute("width"));
        Assert.Equal("140", (string?)root.Attribute("height"));
        Assert.Null(root.Attribute("viewBox"));
        Assert.Contains("height:140px", (string)root.Attribute("style")!);
        var bar = Role(root, "bar");
        Assert.Equal("10%", (string?)bar.Attribute("x"));
        Assert.Equal("50%", (string?)bar.Attribute("width"));
        Assert.Equal("18", (string?)bar.Attribute("height"));
        Assert.Equal("4", (string?)bar.Attribute("rx"));
        Assert.Equal("4", (string?)bar.Attribute("ry"));
        var circle = Role(root, "marker");
        Assert.Equal("50%", (string?)circle.Attribute("cx"));
        Assert.Equal("5", (string?)circle.Attribute("rx"));
        Assert.Equal("5", (string?)circle.Attribute("ry"));
        var axis = Role(root, "axis");
        Assert.Equal("10%", (string?)axis.Attribute("x1"));
        Assert.Equal("90%", (string?)axis.Attribute("x2"));
        Assert.Equal("4 2", (string?)axis.Attribute("stroke-dasharray"));
        Assert.Equal("1", (string?)axis.Attribute("stroke-dashoffset"));
        var line = Role(root, "line");
        var viewport = line.Ancestors().First(element => element.Name.LocalName == "svg");
        Assert.Equal("100%", (string?)viewport.Attribute("width"));
        Assert.Equal("none", (string?)viewport.Attribute("preserveAspectRatio"));
        Assert.All(new[] { bar, circle, axis, line }, mark => Assert.Equal("non-scaling-stroke", (string?)mark.Attribute("vector-effect")));
        Assert.DoesNotContain(root.Descendants(), element => element.Name.LocalName is "script" or "foreignObject");
        Assert.Equal(root.ToString(), Render(scene).ToString());
    }

    [Fact]
    public void DiamondAndCustomPointRolePaths_UseFixedPixelViewportsAtTheirFluidCenters() {
        var builder = Builder();
        var diamond = new ChartPath(new[] { ChartPathCommand.MoveTo(200, 45), ChartPathCommand.LineTo(205, 50),
            ChartPathCommand.LineTo(200, 55), ChartPathCommand.LineTo(195, 50) });
        builder.Path(diamond, ChartColor.Black, role: "gantt-milestone-shape", close: true);
        using (builder.PushGroup("observation", "point"))
            builder.Path(diamond, ChartColor.Black, role: "host-defined-observation", close: true);
        var root = Render(builder.Build());
        foreach (var role in new[] { "gantt-milestone-shape", "host-defined-observation" }) {
            var mark = Role(root, role);
            var viewport = mark.Ancestors().First(element => element.Name.LocalName == "svg");
            Assert.Equal("50%", (string?)viewport.Attribute("x"));
            Assert.Equal("50", (string?)viewport.Attribute("y"));
            Assert.Equal("400", (string?)viewport.Attribute("width"));
            Assert.Equal("140", (string?)viewport.Attribute("height"));
            Assert.Equal("0 0 400 140", (string?)viewport.Attribute("viewBox"));
            Assert.Equal("matrix(1 0 0 1 -200 -50)", (string?)mark.Parent!.Attribute("transform"));
        }
    }

    [Fact]
    public void TranslatedRotatedText_KeepsPreparedFontAndAlignmentInsideAFixedViewport() {
        var builder = Builder();
        using (builder.PushTranslation(20, 10))
        using (builder.PushRotation(90, 200, 50))
            builder.Text("Axis caption", 200, 50, 14, ChartColor.Black, role: "axis-label", alignment: TextAlignment.Right);
        var root = Render(builder.Build());
        var textGroup = Role(root, "axis-label");
        var viewport = textGroup.Ancestors().First(element => element.Name.LocalName == "svg");
        Assert.Equal("55%", (string?)viewport.Attribute("x"));
        Assert.Equal("60", (string?)viewport.Attribute("y"));
        Assert.Equal("400", (string?)viewport.Attribute("width"));
        var text = Assert.Single(textGroup.Elements(), element => element.Name.LocalName == "text");
        Assert.Equal("14", (string?)text.Attribute("font-size"));
        Assert.Equal("Axis caption", text.Value);
        Assert.True(double.Parse((string)text.Attribute("x")!, System.Globalization.CultureInfo.InvariantCulture) < 200);
        Assert.StartsWith("matrix(0 1 -1 0 ", (string)textGroup.Parent!.Attribute("transform")!);
        Assert.DoesNotContain(root.Descendants().Attributes("transform"), attribute => attribute.Value.StartsWith("rotate(", StringComparison.Ordinal));
    }

    [Fact]
    public void TranslatedRoundedClipsAndFades_ShareTheFluidRootCoordinateSystem() {
        var builder = Builder();
        var bounds = new ChartRect(40, 20, 200, 18);
        using (builder.PushTranslation(40, 10)) {
            for (var i = 0; i < 2; i++)
                using (builder.PushClip(ChartPathBuilder.RoundedRectangle(bounds, 4)))
                    builder.RectGradient(bounds, new ChartPoint(40, 20), new ChartPoint(240, 20),
                        new[] { new VisualGradientStop(0, ChartColor.Black), new VisualGradientStop(1, ChartColor.Transparent) }, role: "fade-" + i);
        }
        var root = Render(builder.Build());
        var clip = Assert.Single(root.Descendants(), element => element.Name.LocalName == "clipPath");
        Assert.Equal("userSpaceOnUse", (string?)clip.Attribute("clipPathUnits"));
        var rect = Assert.Single(clip.Elements());
        Assert.Equal("20%", (string?)rect.Attribute("x"));
        Assert.Equal("30", (string?)rect.Attribute("y"));
        Assert.Equal("50%", (string?)rect.Attribute("width"));
        Assert.Equal("4", (string?)rect.Attribute("rx"));
        var references = root.Descendants().Attributes("clip-path").Select(attribute => attribute.Value).ToArray();
        Assert.Equal(2, references.Length);
        Assert.All(references, reference => Assert.Equal("url(#" + (string)clip.Attribute("id")! + ")", reference));
        foreach (var gradient in root.Descendants().Where(element => element.Name.LocalName == "linearGradient")) {
            Assert.Equal("20%", (string?)gradient.Attribute("x1"));
            Assert.Equal("70%", (string?)gradient.Attribute("x2"));
            Assert.Equal("30", (string?)gradient.Attribute("y1"));
            Assert.Equal(2, gradient.Elements().Count());
            Assert.Equal("#000000", (string?)gradient.Elements().Last().Attribute("stop-color"));
            Assert.Equal("0", (string?)gradient.Elements().Last().Attribute("stop-opacity"));
        }
        Assert.Equal("20%", (string?)Role(root, "fade-0").Attribute("x"));
    }

    [Fact]
    public void ExportPolicyClones_RetainFluidWidthAlongsideOtherHostSettings() {
        var original = new VisualSvgOptions(idPrefix: "page", responsive: true, linkTarget: VisualSvgLinkTarget.NewContext);
        var fluid = original.WithFluidWidth();
        var cloned = fluid.WithPatternTiles().WithTextPresentation(VisualSvgTextPresentation.Browser).WithInheritedFonts();
        Assert.False(original.FluidWidth);
        Assert.True(cloned.FluidWidth);
        Assert.True(cloned.PatternTiles);
        Assert.True(cloned.Responsive);
        Assert.Equal("page", cloned.IdPrefix);
        Assert.Equal(VisualSvgLinkTarget.NewContext, cloned.LinkTarget);
        Assert.False(cloned.WithFluidWidth(false).FluidWidth);
        Assert.True(cloned.FluidWidth);
    }

    [Fact]
    public void CapturedEmissionRetainsTheCompletePagePolicyWhenReexported() {
        var builder = Builder();
        builder.Text("Caption", 40, 20, 14, ChartColor.Black, role: "caption");
        var prepared = new PreparedVisual(builder.Build());
        var policy = new VisualSvgOptions("page").WithFluidWidth().WithFontVariables("--body-font", "--chart-size");
        var emission = prepared.ExportSvgEmission(policy);
        Assert.Equal(emission.Svg, prepared.ToSvg(emission.Options));
        var scoped = emission.Options.WithIdPrefix("other-page");
        var root = XDocument.Parse(prepared.ToSvg(scoped)).Root!;
        Assert.Equal("100%", (string?)root.Attribute("width"));
        Assert.Null(root.Attribute("viewBox"));
        Assert.Contains(root.Descendants().Attributes(), attribute => attribute.Value == "var(--body-font, inherit)");
        Assert.Contains(root.Descendants().Attributes(), attribute => attribute.Value == "var(--chart-size, inherit)");
    }

    [Theory]
    [InlineData("polar slices")]
    [InlineData("rotated non-text geometry")]
    [InlineData("arbitrary path clips")]
    public void UnsupportedGeometry_FailsClearlyBeforeReturningADistortedExport(string feature) {
        var builder = Builder();
        if (feature == "polar slices") builder.Slice(200, 60, 20, 0, 0, 1, ChartColor.Black);
        else if (feature == "rotated non-text geometry") {
            using (builder.PushRotation(45, 200, 50)) builder.Rect(new ChartRect(100, 20, 40, 20), ChartColor.Black);
        } else {
            using (builder.PushClip(new ChartPath(new[] { ChartPathCommand.MoveTo(10, 10), ChartPathCommand.LineTo(50, 10), ChartPathCommand.LineTo(30, 50) })))
                builder.Rect(new ChartRect(0, 0, 100, 100), ChartColor.Black);
        }
        var exception = Assert.Throws<NotSupportedException>(() => Render(builder.Build()));
        Assert.Contains(feature, exception.Message);
    }

    [Fact]
    public void FluidAndProportionalExports_UseDistinctNamespacesAndIndependentSizing() {
        var builder = Builder();
        builder.Text("Caption", 200, 50, 14, ChartColor.Black, role: "caption");
        var scene = builder.Build();
        var fluid = Render(scene, new VisualSvgOptions(responsive: true).WithFluidWidth());
        var proportional = XDocument.Parse(VisualSceneSvgRenderer.Render(scene, title: "Chart", options: new VisualSvgOptions(responsive: true))).Root!;
        Assert.Null(fluid.Attribute("viewBox"));
        Assert.Equal("0 0 400 140", (string?)proportional.Attribute("viewBox"));
        Assert.Equal("400", (string?)proportional.Attribute("width"));
        Assert.DoesNotContain("height:auto", (string)fluid.Attribute("style")!);
        Assert.NotEqual((string?)fluid.Attribute("aria-labelledby"), (string?)proportional.Attribute("aria-labelledby"));
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResizingTheHostWithoutScripts_KeepsTextCirclesAndDiamondsInCssPixels(bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var ink = dark ? ChartColor.White : ChartColor.Black;
        var builder = Builder();
        builder.Text("Readable caption", 40, 20, 14, ink, role: "caption");
        builder.Ellipse(200, 50, 5, 5, ink, role: "marker");
        builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(300, 65), ChartPathCommand.LineTo(305, 70),
            ChartPathCommand.LineTo(300, 75), ChartPathCommand.LineTo(295, 70) }), ink, role: "gantt-milestone-shape", close: true);
        builder.Rect(new ChartRect(40, 80, 320, 12), ink, role: "bar");
        builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(40, 95), ChartPathCommand.LineTo(360, 95) }),
            stroke: ink, strokeWidth: 2, role: "line");
        using (builder.PushClip(new ChartRect(100, 100, 200, 20)))
            builder.Rect(new ChartRect(0, 100, 400, 20), ink, role: "clipped-mark");
        var html = "<style>html,body{margin:0;padding:0}body{background:" + (dark ? "#101820" : "#ffffff")
            + "}</style><main>" + Render(builder.Build()).ToString(SaveOptions.DisableFormatting) + "</main>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 400, 220);
        double? captionWidth = null;
        foreach (var width in new[] { 400, 800, 390 }) {
            await session.Page.SetViewportSizeAsync(width, 220);
            var measured = await session.Page.EvaluateAsync<JsonElement>("""
                () => {
                    const root=document.querySelector('main>svg');
                    const mark=role=>root.querySelector('[data-cfx-role="'+role+'"]');
                    const bounds=role=>mark(role).getBoundingClientRect().toJSON();
                    const text=mark('caption').querySelector('text');
                    const w=root.getBoundingClientRect().width;
                    return {root:root.getBoundingClientRect().toJSON(),circle:bounds('marker'),diamond:bounds('gantt-milestone-shape'),
                        bar:bounds('bar'),line:bounds('line'),text:text.getBoundingClientRect().toJSON(),font:getComputedStyle(text).fontSize,
                        lineEffect:getComputedStyle(mark('line')).vectorEffect,
                        outside:document.elementFromPoint(w*.1,110)?.getAttribute('data-cfx-role')==='clipped-mark',
                        inside:document.elementFromPoint(w*.5,110)?.getAttribute('data-cfx-role')==='clipped-mark',
                        scripts:document.querySelectorAll('script,foreignObject').length};
                }
                """);
            Near(width, measured.GetProperty("root").GetProperty("width").GetDouble());
            Near(140, measured.GetProperty("root").GetProperty("height").GetDouble());
            foreach (var marker in new[] { "circle", "diamond" }) {
                Near(10, measured.GetProperty(marker).GetProperty("width").GetDouble());
                Near(10, measured.GetProperty(marker).GetProperty("height").GetDouble());
            }
            Near(width * .8, measured.GetProperty("bar").GetProperty("width").GetDouble());
            Near(width * .8, measured.GetProperty("line").GetProperty("width").GetDouble());
            var currentCaptionWidth = measured.GetProperty("text").GetProperty("width").GetDouble();
            captionWidth ??= currentCaptionWidth;
            Near(captionWidth.Value, currentCaptionWidth);
            Assert.Equal("14px", measured.GetProperty("font").GetString());
            Assert.Equal("non-scaling-stroke", measured.GetProperty("lineEffect").GetString());
            Assert.False(measured.GetProperty("outside").GetBoolean());
            Assert.True(measured.GetProperty("inside").GetBoolean());
            Assert.Equal(0, measured.GetProperty("scripts").GetInt32());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);

        static void Near(double expected, double actual) => Assert.InRange(actual - expected, -.05, .05);
    }

    /// <summary>Design rule 4 under fluid width: a column keeps its prepared thickness while its category position stretches.</summary>
    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task FluidColumns_KeepTheirPreparedThicknessAtEveryHostWidth() {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Core.Chart.Create().WithStackedBars().WithXLabels("Mon", "Tue", "Wed")
            .AddBar("Updates", new[] { new ChartPoint(1, 3), new ChartPoint(2, 5), new ChartPoint(3, 2) })
            .AddBar("Notes", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2), new ChartPoint(3, 4) });
        var svg = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 240), 8))).ToSvg(new VisualSvgOptions("columns").WithFluidWidth());
        await using var session = await InteractiveChartBrowser.OpenAsync("<style>html,body{margin:0}</style><main>" + svg + "</main>", 320, 300);
        double? prepared = null; double? previousCenter = null;
        foreach (var width in new[] { 320, 900 }) {
            await session.Page.SetViewportSizeAsync(width, 300);
            var column = await session.Page.EvaluateAsync<double[]>(
                "() => { const r = document.querySelector('main svg [data-cfx-point=\"1\"] [data-cfx-role=\"bar\"]').getBoundingClientRect(); return [r.width, r.left + r.width / 2]; }");
            prepared ??= column[0];
            Assert.InRange(column[0], prepared.Value - .05, prepared.Value + .05);
            Assert.True(column[0] <= 24.05, "A stacked column is at most 24px thick.");
            if (previousCenter is { } before) Assert.True(column[1] > before * 2, "The column's category position stretches with the host.");
            previousCenter = column[1];
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static VisualSceneBuilder Builder() => new(new VisualSize(400, 140), FontSpec.SystemSans());
    private static XElement Render(VisualScene scene, VisualSvgOptions? options = null) =>
        XDocument.Parse(VisualSceneSvgRenderer.Render(scene, title: "Chart", options: options ?? new VisualSvgOptions().WithFluidWidth())).Root!;
    private static XElement Role(XElement root, string role) =>
        Assert.Single(root.Descendants(), element => (string?)element.Attribute("data-cfx-role") == role);
}
