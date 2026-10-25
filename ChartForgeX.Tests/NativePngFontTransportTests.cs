using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>The PNG transport captures native face decisions without changing ordinary static artifacts.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativePngFontTransportTests : IDisposable {
    private const string Family = "CFX Private Export Snapshot";

    [Fact]
    public void PrivateCaptureRetainsOrdinarySvgBytesAndNativePixels() {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        var prepared = Prepare("HHOH", FontSpec.FromFamily(Family));
        var ordinary = prepared.ToSvg(); var png = prepared.ToPng();
        var decorated = prepared.ForInteractivePngExport(); var captured = decorated.ToSvg();
        Assert.Equal(ordinary, StripCapture(captured));
        Assert.Equal(ordinary, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(png, decorated.ToPng());
        Assert.Equal("HHOH", Assert.Single(Texts(captured)).Value);
        Assert.DoesNotContain(XDocument.Parse(captured).Descendants(), node => node.Name.LocalName is "script" or "image" or "foreignObject");
        Assert.DoesNotContain("@font-face", captured, StringComparison.Ordinal);
    }

    [Fact]
    public void RepeatedGlyphsReuseResourcesButRetainSeparatePaintPlacements() {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        var captured = Prepare("HHOH", FontSpec.FromFamily(Family)).ForInteractivePngExport().ToSvg();
        var document = XDocument.Parse(captured);
        var resources = Resources(document); Assert.Equal(2, resources.Length);
        var placements = Assert.Single(Texts(captured)).Attribute("data-cfx-png-glyphs")!.Value.Split('|');
        Assert.Equal(4, placements.Length);
        Assert.Equal(placements[0].Split(';')[0], placements[1].Split(';')[0]);
        Assert.Equal(placements[0].Split(';')[0], placements[3].Split(';')[0]);
        Assert.NotEqual(placements[0], placements[1]);
        Assert.All(placements, placement => Assert.Contains(resources, resource => resource.Attribute("id")!.Value == placement.Split(';')[0]));
    }

    [Fact]
    public void LogicalGlyphResourcesKeepStrokeUnitsAndSeparateCapturedSizes() {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        var font = FontSpec.FromFamily(Family); var builder = new VisualSceneBuilder(new VisualSize(240, 130), font);
        foreach (var size in new[] { 20d, 40d }) {
            var style = TextStyle.Create(size, ChartColor.Black); style.Font = font; style.Hinting = TextHinting.None;
            builder.Text("HH", 12, size == 20 ? 40 : 105, style, role: "captured-caption", stroke: ChartColor.Black, strokeWidth: 1.5);
        }
        var document = XDocument.Parse(new PreparedVisual(builder.Build()).ForInteractivePngExport().ToSvg());
        var resources = Resources(document); Assert.Equal(2, resources.Length);
        Assert.NotEqual(resources[0].Attribute("d")!.Value, resources[1].Attribute("d")!.Value);
        foreach (var text in document.Descendants().Where(node => node.Name.LocalName == "text")) {
            Assert.Equal("1.5", text.Attribute("stroke-width")!.Value);
            var placements = text.Attribute("data-cfx-png-glyphs")!.Value.Split('|'); Assert.Equal(2, placements.Length);
            Assert.All(placements, placement => Assert.Contains(";matrix(1 0 0 1 ", placement, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void PreparedGlyphFacesSurviveFamilyReplacementAndSourceMutation() {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(includePrivateUse: false));
        FontRegistry.Register(Family + " Fallback", OpenTypeTestFonts.NameKeyed());
        var style = FontSpec.FromFamily(Family + ", " + Family + " Fallback");
        var prepared = Prepare("H" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter), style);
        var ordinary = prepared.ToSvg(); var png = prepared.ToPng();
        var captured = prepared.ForInteractivePngExport().ToSvg();
        Assert.Equal(2, Resources(XDocument.Parse(captured)).Length);
        style.Family = "No Such Family";
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(weight: 700, italic: true));
        FontRegistry.Clear();
        Assert.Equal(captured, prepared.ForInteractivePngExport().ToSvg());
        Assert.Equal(ordinary, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void VariableFaceInstancesAreNotCollapsedByFamilyOrGlyphNumber() {
        FontRegistry.Register(Family, Embedded("variable-true-type.ttf"));
        var builder = new VisualSceneBuilder(new VisualSize(240, 130), FontSpec.FromFamily(Family));
        foreach (var weight in new[] { 100d, 900d }) {
            var style = TextStyle.Create(40, ChartColor.Black);
            style.Font = FontSpec.FromFamily(Family).WithVariation("wght", weight); style.Hinting = TextHinting.None;
            builder.Text("HH", 12, weight == 100 ? 45 : 105, style, role: "captured-caption");
        }
        var document = XDocument.Parse(new PreparedVisual(builder.Build()).ForInteractivePngExport().ToSvg());
        var resources = Resources(document); Assert.Equal(2, resources.Length);
        Assert.NotEqual(resources[0].Attribute("d")!.Value, resources[1].Attribute("d")!.Value);
        var placements = document.Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Attribute("data-cfx-png-glyphs")!.Value.Split('|')).ToArray();
        Assert.All(placements, row => Assert.Equal(2, row.Length));
        Assert.NotEqual(placements[0][0].Split(';')[0], placements[1][0].Split(';')[0]);
    }

    [Theory]
    [InlineData("color-colr0.ttf")]
    [InlineData("color-colr1.ttf")]
    public void ColourGlyphsKeepAnExplicitBrowserTextFallbackInsteadOfInventedMonochromePaint(string fixture) {
        FontRegistry.Register(Family, Embedded(fixture));
        var captured = Prepare("😀", FontSpec.FromFamily(Family)).ForInteractivePngExport().ToSvg();
        var text = Assert.Single(Texts(captured));
        Assert.Equal("😀", text.Value); Assert.Equal("browser-text", (string?)text.Attribute("data-cfx-png-font-fallback"));
        Assert.Null(text.Attribute("data-cfx-png-glyphs")); Assert.Empty(Resources(XDocument.Parse(captured)));
    }

    [Fact]
    public void SyntheticBoldIsNotFalselyRepresentedByARegularOutline() {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        var font = FontSpec.FromFamily(Family); font.Weight = 700;
        var captured = Prepare("H", font).ForInteractivePngExport().ToSvg();
        Assert.Equal("browser-text", (string?)Assert.Single(Texts(captured)).Attribute("data-cfx-png-font-fallback"));
        Assert.Empty(Resources(XDocument.Parse(captured)));
    }

    [Fact]
    public void ChartAndTopologyCallbackCaptureAreOptedInByTheirExistingExportFeature() {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(weight: 700), weight: 700);
        var chart = Chart.Create().WithSize(360, 240).WithTheme(VisualTheme.Report().WithFontFamily(Family))
            .WithTitle("HH").WithXLabels("H").AddBar("H", ChartPoints.FromValues(12));
        var renderer = new HtmlInteractiveChartRenderer();
        var plain = renderer.PrepareSection(chart, configure: options => options.Interaction.Disable(ChartInteractionFeatures.Export));
        var exported = renderer.PrepareSection(chart, configure: options => options.Interaction.Enable(ChartInteractionFeatures.Export));
        Assert.DoesNotContain("data-cfx-png-glyphs", plain.Html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-png-glyphs", exported.Html, StringComparison.Ordinal);
        Assert.Equal(plain.PreparedVisual.ToSvg(), exported.PreparedVisual.ToSvg());
        var topology = TopologyChart.Create().WithViewport(360, 240).WithTheme(VisualTheme.Report().WithFontFamily(Family));
        topology.Title = "HH";
        topology.Nodes.Add(new TopologyNode { Id = "node", Label = "H", X = 30, Y = 60, Width = 160, Height = 60 });
        var ordinary = topology.Prepare().ToSvg(); var calls = 0; string? callbackSvg = null;
        var html = new HtmlInteractiveTopologyRenderer().RenderPresentationFragment(topology, prepared => {
            calls++; var value = prepared.ToSvg(); callbackSvg = value; return value;
        }, new TopologyRenderOptions { EnableHtmlExportControls = true });
        Assert.Equal(1, calls); var capturedCallback = callbackSvg ?? throw new InvalidOperationException("The native callback did not run.");
        Assert.Contains("data-cfx-png-glyphs", capturedCallback, StringComparison.Ordinal);
        Assert.Contains(capturedCallback, html, StringComparison.Ordinal); Assert.Equal(ordinary, topology.Prepare().ToSvg());
    }

    private static PreparedVisual Prepare(string text, FontSpec font) {
        var style = TextStyle.Create(20, ChartColor.Black); style.Font = font; style.Hinting = TextHinting.None;
        var builder = new VisualSceneBuilder(new VisualSize(240, 80), font);
        builder.Text(text, 12, 48, style, role: "captured-caption");
        return new PreparedVisual(builder.Build());
    }
    private static XElement[] Texts(string svg) => XDocument.Parse(svg).Descendants().Where(node => node.Name.LocalName == "text").ToArray();
    private static XElement[] Resources(XDocument document) => document.Descendants().Where(node => node.Attribute("data-cfx-png-text-resources") != null)
        .SelectMany(node => node.Elements()).ToArray();
    private static string StripCapture(string svg) => Regex.Replace(Regex.Replace(svg,
        " data-cfx-png-(?:glyphs|font-fallback)=\"[^\"]*\"", string.Empty),
        "<defs data-cfx-png-text-resources=\"true\">.*?</defs>", string.Empty, RegexOptions.Singleline);
    private static byte[] Embedded(string name) {
        using var input = typeof(NativePngFontTransportTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + name)
            ?? throw new InvalidOperationException("Missing existing font fixture " + name);
        using var output = new MemoryStream(); input.CopyTo(output); return output.ToArray();
    }
    public void Dispose() => FontRegistry.Clear();
}
