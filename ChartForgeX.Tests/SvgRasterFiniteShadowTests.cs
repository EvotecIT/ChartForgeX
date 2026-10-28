using System.Text;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterFiniteShadowTests {
    private static SvgRasterizationResult Import(string source) => SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());

    [Theory]
    [InlineData("0")] [InlineData("1")]
    public void FiniteSourceAndNativePlanProduceTheSameCroppedPixels(string sigma) {
        var svg = ShadowFilterFixture.Svg(sigma: sigma); var value = double.Parse(sigma, System.Globalization.CultureInfo.InvariantCulture);
        var result = SvgRasterizer.Rasterize(svg, strict: true, filters: new SvgRasterFilterOptions());
        var layer = ShadowFilterFixture.Source().CaptureLayer(new VisualDropShadow(ChartColor.FromRgba(0, 0, 255, 128), 4, 2, value,
            spread: 1, orderedDither: false), ShadowFilterFixture.Options);
        NativeEffectBeforeCapture.Write("finite-shadow-native-equivalent-" + sigma, svg, ImageComposition.FromImage(result.Image).ToPng(), result.Image);
        var image = layer.ToImage();
        for (var y = 0; y < 48; y++) for (var x = 0; x < 64; x++) {
            var expected = image.Pixels.Skip(((y - (int)layer.Bounds.Y) * image.Width + x - (int)layer.Bounds.X) * 4).Take(4);
            Assert.Equal(expected, result.Image.Pixels.Skip((y * 64 + x) * 4).Take(4));
        }
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("ancestor-opacity")]
    [InlineData("root-opacity")]
    [InlineData("clip")]
    [InlineData("mask")]
    [InlineData("viewport")]
    [InlineData("use")]
    [InlineData("embedded")]
    public void CompletedFilterChannelsSurviveOrdinaryAncestorAndReferenceTransfers(string transfer) {
        var source = ShadowFilterFixture.Svg(sigma: "1");
        var direct = Import(source);
        var document = XDocument.Parse(source);
        var root = document.Root!;
        var ns = root.Name.Namespace;
        var definitions = root.Element(ns + "defs")!;
        var node = root.Elements(ns + "rect").Single();
        var attenuate = transfer is "ancestor-opacity" or "root-opacity";
        if (transfer == "root-opacity") root.SetAttributeValue("opacity", ".5");
        else if (transfer == "use") {
            node.Remove(); definitions.Add(node);
            root.Add(new XElement(ns + "use", new XAttribute("href", "#source")));
        } else if (transfer == "embedded") {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
            document = new XDocument(new XElement(ns + "svg", new XAttribute("width", 64), new XAttribute("height", 48),
                new XElement(ns + "image", new XAttribute("width", 64), new XAttribute("height", 48),
                    new XAttribute("href", "data:image/svg+xml;base64," + encoded))));
        } else {
            node.Remove();
            var ancestor = new XElement(ns + (transfer == "viewport" ? "svg" : "g"));
            if (transfer == "ancestor-opacity") ancestor.SetAttributeValue("opacity", ".5");
            if (transfer == "viewport") { ancestor.SetAttributeValue("width", 64); ancestor.SetAttributeValue("height", 48); }
            if (transfer == "clip") {
                definitions.Add(new XElement(ns + "clipPath", new XAttribute("id", "whole"), new XAttribute("clipPathUnits", "userSpaceOnUse"),
                    new XElement(ns + "rect", new XAttribute("width", 64), new XAttribute("height", 48))));
                ancestor.SetAttributeValue("clip-path", "url(#whole)");
            }
            if (transfer == "mask") {
                definitions.Add(new XElement(ns + "mask", new XAttribute("id", "whole"), new XAttribute("maskUnits", "userSpaceOnUse"),
                    new XAttribute("maskContentUnits", "userSpaceOnUse"), new XAttribute("x", 0), new XAttribute("y", 0),
                    new XAttribute("width", 64), new XAttribute("height", 48),
                    new XElement(ns + "rect", new XAttribute("width", 64), new XAttribute("height", 48), new XAttribute("fill", "white"))));
                ancestor.SetAttributeValue("mask", "url(#whole)");
            }
            ancestor.Add(node); root.Add(ancestor);
        }
        var markup = document.ToString(SaveOptions.DisableFormatting);
        var result = Import(markup);
        NativeEffectBeforeCapture.Write("finite-shadow-channel-transfer-" + transfer, markup,
            ImageComposition.FromImage(result.Image).ToPng(), result.Image);
        Assert.Empty(direct.Diagnostics); Assert.Empty(result.Diagnostics);
        var expected = direct.Image.Pixels.ToArray();
        if (attenuate) for (var offset = 0; offset < expected.Length; offset += 4) {
            expected[offset + 3] = (byte)Math.Round(expected[offset + 3] * .5);
            if (expected[offset + 3] == 0) expected[offset] = expected[offset + 1] = expected[offset + 2] = 0;
        }
        Assert.Equal(expected, result.Image.Pixels);
    }

    [Fact]
    public void UnfilteredEmbeddedImageKeepsTheOrdinaryNullOptionsPath() {
        const string inner = "<svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'><rect width='8' height='8' fill='#f80709' fill-opacity='.2'/></svg>";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(inner));
        var source = "<svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'><image width='8' height='8' href='data:image/svg+xml;base64," + encoded + "'/></svg>";
        var legacy = SvgRasterizer.Rasterize(source, strict: true);
        var explicitNull = SvgRasterizer.Rasterize(source, strict: true, filters: null);
        var optedInWithoutFilter = Import(source);
        NativeEffectBeforeCapture.Write("finite-shadow-unfiltered-image-control", source,
            ImageComposition.FromImage(legacy.Image).ToPng(), legacy.Image);
        Assert.Equal(legacy.Image.Pixels, explicitNull.Image.Pixels);
        Assert.Equal(legacy.Image.Pixels, optedInWithoutFilter.Image.Pixels);
        Assert.Empty(optedInWithoutFilter.Diagnostics);
    }

    [Fact]
    public void FloodUsesRealDefinitionAncestryImportantVariablesAndCurrentColor() {
        var source = ShadowFilterFixture.Svg().Replace("<defs>", "<style>defs .tint{flood-color:var(--shade)!important;flood-opacity:var(--alpha)!important}</style><defs style='--shade:currentColor;--alpha:.5;color:blue'>")
            .Replace("<feFlood flood-color='#0000ff' flood-opacity='.5'", "<feFlood class='tint' flood-color='red' flood-opacity='1' style='flood-color:green'");
        var plain = Import(ShadowFilterFixture.Svg()); var styled = Import(source);
        NativeEffectBeforeCapture.Write("finite-shadow-flood-cascade", source, ImageComposition.FromImage(styled.Image).ToPng(), styled.Image);
        Assert.Equal(plain.Image.Pixels, styled.Image.Pixels); Assert.Empty(styled.Diagnostics);
        var rejected = source.Replace("--shade:currentColor", "--shade:url(#unknown)");
        Assert.Contains(SvgRasterizer.Rasterize(rejected, filters: new SvgRasterFilterOptions()).Diagnostics, item => item.Code == "SFR002");
    }

    [Theory]
    [InlineData(false, 81261L)] [InlineData(true, 105837L)]
    public void ExactPublicArrayBudgetIncludesMorphologyDequeUnionOutputAndOrdinaryAncestor(bool ancestor, long exactBytes) {
        // 64x48 root/output; inverse input69x51; spread71x53; union74x54.
        // Byte arrays: root12288 + source14076 + ping-pong15052 + union15984 + morphology11289 + deque284 + output12288 =81261.
        // One opacity ancestor adds its real12288 canvas and12288 opacity copy, not a guessed layer multiplier.
        var source = ShadowFilterFixture.Svg(ancestor: ancestor); var ordinary = Import(source);
        var exact = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions(maximumLiveBytes: exactBytes));
        NativeEffectBeforeCapture.Write("finite-shadow-budget-" + ancestor, source, ImageComposition.FromImage(exact.Image).ToPng(), exact.Image);
        Assert.Equal(ordinary.Image.Pixels, exact.Image.Pixels);
        Assert.Contains("safety limit", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true,
            filters: new SvgRasterFilterOptions(maximumLiveBytes: exactBytes - 1))).Message);
        Assert.Contains("safety limit", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source,
            filters: new SvgRasterFilterOptions(maximumWorkSamples: 1))).Message);
    }

    [Fact]
    public void ReusedAndEmbeddedInstancesSharePermitsAndCancelledSourceNeverPublishesPixels() {
        var source = ShadowFilterFixture.Svg();
        var uses = source.Replace("<rect id='source'", "<g id='payload'><rect id='source'").Replace("/></svg>", "/></g><use href='#payload'/></svg>");
        Assert.Empty(SvgRasterizer.Rasterize(uses, strict: true, filters: new SvgRasterFilterOptions(maximumApplications: 2)).Diagnostics);
        Assert.Contains("instance count", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(uses, strict: true,
            filters: new SvgRasterFilterOptions(maximumApplications: 1))).Message);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
        var outer = "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='48'><image width='64' height='48' href='data:image/svg+xml;base64," + encoded + "'/><image width='64' height='48' href='data:image/svg+xml;base64," + encoded + "'/></svg>";
        Assert.Contains("instance count", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(outer, strict: true,
            filters: new SvgRasterFilterOptions(maximumApplications: 1))).Message);
        Assert.Throws<OperationCanceledException>(() => SvgRasterizer.Rasterize(source,
            filters: new SvgRasterFilterOptions(cancellationToken: new CancellationToken(true))));
    }

    [Fact]
    public void NullOptionsAndUnrepresentablePrimitiveReferencesKeepHonestStrictDiagnostics() {
        var source = ShadowFilterFixture.Svg(); var ordinary = SvgRasterizer.Rasterize(source); var explicitNull = SvgRasterizer.Rasterize(source, filters: null);
        NativeEffectBeforeCapture.Write("finite-shadow-null-options", source, ImageComposition.FromImage(ordinary.Image).ToPng(), ordinary.Image);
        Assert.Equal(ordinary.Image.Pixels, explicitNull.Image.Pixels); Assert.Contains(ordinary.Diagnostics, item => item.Code == "SFR002");
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true));
        // Ordered-filter controls separately qualify SourceAlpha, arithmetic, linearRGB,
        // newest preceding result names and omitted-input fallback. These inputs remain unrepresented.
        foreach (var unsupported in new[] {
            source.Replace("radius='1'", "radius='.5'"), source.Replace("dx='4'", "dx='4.5'"),
            source.Replace("result='tint'", "result='FillPaint'"),
            source.Replace("in='SourceGraphic'", "in='BackgroundImage'"),
            source.Replace("<feFlood", "<feFlood mask='url(#unsupported)'"), source.Replace("<feMergeNode in='SourceGraphic'", "<feMergeNode opacity='.5' in='SourceGraphic'") }) {
            Assert.Contains(SvgRasterizer.Rasterize(unsupported, filters: new SvgRasterFilterOptions()).Diagnostics, item => item.Code == "SFR002");
            Assert.Throws<NotSupportedException>(() => Import(unsupported));
        }
    }
}
