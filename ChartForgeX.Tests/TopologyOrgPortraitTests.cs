using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyOrgPortraitTests {
    [Theory]
    [InlineData(false, TopologyPortraitFit.Meet, VisualThemeMode.Light, 900)]
    [InlineData(true, TopologyPortraitFit.Slice, VisualThemeMode.Dark, 390)]
    [InlineData(false, TopologyPortraitFit.Slice, VisualThemeMode.Light, 900)]
    [InlineData(true, TopologyPortraitFit.Meet, VisualThemeMode.Dark, 390)]
    public void HierarchyAndTeamPortraitsShareExactNativeSvgImageGeometryAndCrop(bool team, TopologyPortraitFit fit, VisualThemeMode mode, int width) {
        var artwork = TopologyIconArtwork.Portrait(Stripes(), fit);
        var chart = Model(team, artwork).WithViewport(width, 320);
        var prepared = chart.Prepare(Context(width, mode), Options());
        Save(prepared, "portrait-" + team + "-" + fit + "-" + mode + "-" + width);
        var image = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneImage>(), node => node.Role == "topology-node-artwork");
        var svg = XDocument.Parse(prepared.ToSvg("org-portrait"));
        var element = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "topology-node-artwork");
        Assert.Equal(image.Bounds.X.ToString("0.###", CultureInfo.InvariantCulture), (string?)element.Attribute("x"));
        Assert.Equal(image.Bounds.Y.ToString("0.###", CultureInfo.InvariantCulture), (string?)element.Attribute("y"));
        Assert.Equal(image.Bounds.Width.ToString("0.###", CultureInfo.InvariantCulture), (string?)element.Attribute("width"));
        Assert.Equal(image.Bounds.Height.ToString("0.###", CultureInfo.InvariantCulture), (string?)element.Attribute("height"));
        Assert.Equal("none", (string?)element.Attribute("preserveAspectRatio"));
        var href = (string)element.Attribute("href")!;
        var embedded = RasterImageDecoder.Decode(Convert.FromBase64String(href.Substring(href.IndexOf(',') + 1)));
        Assert.Equal(image.Image.Width, embedded.Width); Assert.Equal(image.Image.Height, embedded.Height); Assert.Equal(image.Image.Pixels, embedded.Pixels);
        var native = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var fromSvg = RasterImageDecoder.Decode(SvgRasterizer.ToPng(prepared.ToSvg("org-portrait")));
        ChartRect visible;
        if (fit == TopologyPortraitFit.Slice) {
            var index = prepared.Scene.Nodes.ToList().IndexOf(image);
            var clip = Assert.IsType<VisualSceneGroup>(prepared.Scene.Nodes[index - 1]); Assert.True(clip.Clip.HasValue);
            visible = clip.Clip!.Value;
            // Explicit team Card uses an inline square glyph; the hierarchy's implicit Artwork mode uses its full rectangular node.
            var nodeRegion = Assert.Single(prepared.Regions, region => region.Role == "topology-node");
            if (team) Assert.Equal(visible.Width, visible.Height, 8);
            else Assert.Equal(nodeRegion.Bounds, visible);
            var scale = Math.Max(visible.Width / 12, visible.Height / 4);
            var expectedWidth = 12 * scale; var expectedHeight = 4 * scale;
            Assert.Equal(expectedWidth, image.Bounds.Width, 8); Assert.Equal(expectedHeight, image.Bounds.Height, 8);
            Assert.Equal(visible.X + (visible.Width - expectedWidth) / 2, image.Bounds.X, 8);
            Assert.Equal(visible.Y + (visible.Height - expectedHeight) / 2, image.Bounds.Y, 8);
            var svgClipReference = element.Ancestors().First(node => node.Attribute("clip-path") != null).Attribute("clip-path")!.Value;
            Assert.StartsWith("url(#", svgClipReference); Assert.EndsWith(")", svgClipReference);
            var svgClipId = svgClipReference.Substring(5, svgClipReference.Length - 6);
            var svgClip = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("id") == svgClipId);
            var svgClipRect = Assert.Single(svgClip.Elements(), node => node.Name.LocalName == "rect");
            Assert.Equal(visible.X.ToString("0.###", CultureInfo.InvariantCulture), (string?)svgClipRect.Attribute("x"));
            Assert.Equal(visible.Y.ToString("0.###", CultureInfo.InvariantCulture), (string?)svgClipRect.Attribute("y"));
            Assert.Equal(visible.Width.ToString("0.###", CultureInfo.InvariantCulture), (string?)svgClipRect.Attribute("width"));
            Assert.Equal(visible.Height.ToString("0.###", CultureInfo.InvariantCulture), (string?)svgClipRect.Attribute("height"));
            foreach (var fraction in new[] { 1d / 6, 5d / 6 }) {
                // Predict the source stripe independently from the authored 12x4 image and centered Slice rectangle.
                var sourceFraction = .5 + (fraction - .5) * visible.Width / expectedWidth;
                Assert.InRange(sourceFraction, 0, 1); var stripe = (int)Math.Floor(sourceFraction * 3);
                Assert.InRange(stripe, 0, 2);
                AssertColor(native, visible, fraction, stripe == 0 ? (byte)255 : (byte)0, stripe == 1 ? (byte)255 : (byte)0, stripe == 2 ? (byte)255 : (byte)0);
                AssertColor(fromSvg, visible, fraction, stripe == 0 ? (byte)255 : (byte)0, stripe == 1 ? (byte)255 : (byte)0, stripe == 2 ? (byte)255 : (byte)0);
            }
        } else {
            visible = image.Bounds; Assert.Equal(visible.Width, visible.Height * 3, 8);
            AssertColor(native, visible, 1d / 6, 255, 0, 0); AssertColor(native, visible, 5d / 6, 0, 0, 255);
            AssertColor(fromSvg, visible, 1d / 6, 255, 0, 0); AssertColor(fromSvg, visible, 5d / 6, 0, 0, 255);
        }
        AssertColor(native, visible, .5, 0, 255, 0); AssertColor(fromSvg, visible, .5, 0, 255, 0);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code.Contains("artwork", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InputArtworkClonePreparedPixelsAndNativeInterchangeRemainIndependent(bool team) {
        var pixels = Stripes(); var artwork = TopologyIconArtwork.Portrait(pixels);
        var originalHref = artwork.ImageHref; Array.Clear(pixels.Pixels, 0, pixels.Pixels.Length);
        Assert.Equal(originalHref, artwork.ImageHref);
        var chart = Model(team, artwork); var node = Assert.Single(chart.Nodes);
        Assert.NotSame(artwork, node.Artwork); Assert.Equal(originalHref, node.Artwork!.ImageHref);
        artwork.ImageHref = "https://example.invalid/not-fetched.png"; artwork.PreserveAspectRatio = "none";
        Assert.Equal(originalHref, node.Artwork.ImageHref); Assert.Equal("xMidYMid slice", node.Artwork.PreserveAspectRatio);
        var prepared = chart.Prepare(Context(900, VisualThemeMode.Light), Options());
        var svg = prepared.ToSvg("captured-portrait"); var png = prepared.ToPng();
        var envelope = chart.ToVisualArtifact().ToInterchangeEnvelope(); var restored = envelope.ToTopologyChart();
        var restoredNode = Assert.Single(restored.Nodes); Assert.Equal(node.Id, restoredNode.Id);
        Assert.Equal(originalHref, restoredNode.Artwork!.ImageHref); Assert.Equal("xMidYMid slice", restoredNode.Artwork.PreserveAspectRatio);
        node.Artwork.ImageHref = null; node.Artwork.SvgBody = "<rect width='24' height='24' fill='red'/>";
        Assert.Equal(svg, prepared.ToSvg("captured-portrait")); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(originalHref, restoredNode.Artwork.ImageHref); Assert.NotEmpty(restored.ToPng(Options()));
    }

    [Fact]
    public void PortraitFactoryRejectsInvalidFitAndDefaultImageBeforeProducingArtwork() {
        Assert.Throws<ArgumentOutOfRangeException>(() => TopologyIconArtwork.Portrait(Stripes(), (TopologyPortraitFit)42));
        Assert.Throws<ArgumentOutOfRangeException>(() => TopologyIconArtwork.Portrait(default));
        Assert.Equal("xMidYMid slice", TopologyIconArtwork.Portrait(Stripes()).PreserveAspectRatio);
        Assert.Equal("xMidYMid meet", TopologyIconArtwork.Portrait(Stripes(), TopologyPortraitFit.Meet).PreserveAspectRatio);
    }

    private static TopologyChart Model(bool team, TopologyIconArtwork artwork) {
        var chart = TopologyChart.Create().WithViewport(900, 320).WithLegend(null);
        if (team) chart.AddTeam("team", "Example team", new[] { new TopologyTeamMember("person", "Avery Quinn", "Example role") { Artwork = artwork } },
            new TopologyTeamOptions { IncludeTeamNode = false });
        else chart.AddHierarchy(new[] { new TopologyHierarchyItem("person", "Avery Quinn") { Kind = TopologyNodeKind.Person, Artwork = artwork, Width = 164, Height = 64 } });
        return chart;
    }
    internal static RgbaImage Stripes() {
        var bytes = new byte[12 * 4 * 4];
        for (var y = 0; y < 4; y++) for (var x = 0; x < 12; x++) { var offset = (y * 12 + x) * 4; bytes[offset + x / 4] = 255; bytes[offset + 3] = 255; }
        return new RgbaImage(12, 4, bytes);
    }
    private static void AssertColor(RgbaImage image, ChartRect bounds, double fraction, byte red, byte green, byte blue) {
        var x = (int)(bounds.X + bounds.Width * fraction); var y = (int)(bounds.Y + bounds.Height / 2);
        Assert.InRange(x, 0, image.Width - 1); Assert.InRange(y, 0, image.Height - 1);
        var offset = (y * image.Width + x) * 4;
        Assert.Equal(new[] { red, green, blue, (byte)255 }, image.Pixels.Skip(offset).Take(4));
    }
    private static TopologyRenderOptions Options() => new() { IncludeLegend = false, IncludeTitle = false, FitContentToViewport = true };
    private static VisualRenderContext Context(int width, VisualThemeMode mode) => new(new VisualLayoutOptions(new VisualSize(width, 320)),
        VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode, new VisualFrame(showLegend: false));
    private static void Save(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Portrait captures require an absolute task path.");
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg("portrait-proof"));
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
    }
}
