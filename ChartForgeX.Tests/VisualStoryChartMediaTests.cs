using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class VisualStoryChartMediaTests {
    [Fact]
    public void CapturedProductFontStylesRemainValidAfterRegistrationButRejectTampering() {
        var css = ChartFontAssets.StyleSheet;
        using var font = typeof(ChartFontAssets).Assembly.GetManifestResourceStream("ChartForgeX.Typography.Fonts.JetBrainsMono.ttf")!;
        using var bytes = new MemoryStream();
        font.CopyTo(bytes);
        try {
            FontRegistry.Register("Archivo", bytes.ToArray());
            FontRegistry.Register("JetBrains Mono", bytes.ToArray());
            Assert.Empty(ChartFontAssets.StyleSheet);
            var raster = new RgbaImage(1, 1, new byte[4]);
            string Svg(string style) => new XElement(XName.Get("svg", "http://www.w3.org/2000/svg"),
                new XAttribute("viewBox", "0 0 1 1"),
                new XElement(XName.Get("style", "http://www.w3.org/2000/svg"), style)).ToString();
            var svg = Svg(css);
            Assert.Equal(svg, new VisualStoryMediaSurface(raster, "Bundled fonts", svg).Svg);
            foreach (var unsafeCss in new[] {
                css + "@import 'https://example.invalid/active.css';",
                css.Replace("data:font/woff2;base64,", "https://example.invalid/font?payload=")
            }) {
                var error = Assert.Throws<ArgumentException>(() => new VisualStoryMediaSurface(raster, "Altered fonts", Svg(unsafeCss)));
                Assert.Equal("svg", error.ParamName);
                Assert.StartsWith("The vector representation must not contain active or external CSS.", error.Message, StringComparison.Ordinal);
            }
        } finally {
            FontRegistry.Clear();
        }
    }

    [Fact]
    public void DefaultPaletteChartRetainsBothRasterAndVectorStoryMedia() {
        var chart = Chart.Create().WithSize(420, 260).WithTitle("Weekly builds").WithXLabels("Mon", "Tue", "Wed")
            .AddSmoothArea("Builds", new[] { new ChartPoint(1, 12), new ChartPoint(2, 18), new ChartPoint(3, 15) });
        var svg = chart.ToSvg(); var png = chart.ToPng();
        var text = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.NotEmpty(text);
        Assert.All(text, element => Assert.Contains("font-palette:normal", (string)element.Attribute("style")!));
        var media = new VisualStoryMediaSurface(png, "Weekly builds chart", svg);
        Assert.Equal(svg, media.Svg);
        Assert.Equal(420, media.Raster.Width); Assert.Equal(260, media.Raster.Height);
        var story = VisualStory.Create("Build results").WithSize(640, 400);
        story.Scene("result", "See the generated chart", .25).Panel("chart", media);
        story.Outcome("chart-visible", "The chart is visible", "chart");
        Assert.Contains("Weekly builds", story.ToSvg());
        Assert.True(story.ToPng().Length > 64);
    }
}
