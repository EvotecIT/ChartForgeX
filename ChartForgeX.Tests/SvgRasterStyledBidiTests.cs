using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Styled SVG spans retain their paint while sharing bidi and Arabic joining within a positioned chunk.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class SvgRasterStyledBidiTests : IDisposable {
    private readonly string _path = Path.Combine(Path.GetTempPath(), "cfx-styled-bidi-" + Guid.NewGuid().ToString("N") + ".otf");

    public SvgRasterStyledBidiTests() {
        File.WriteAllBytes(_path, OpenTypeTestFonts.NameKeyed(extraGlyphs: new Dictionary<int, int> {
            [0x05D0] = OpenTypeTestFonts.H, [0x05D1] = OpenTypeTestFonts.O,
            [0x05D2] = OpenTypeTestFonts.E, [0x05D3] = OpenTypeTestFonts.Flex,
            [0x0628] = OpenTypeTestFonts.E, [0xFE8F] = OpenTypeTestFonts.E,
            [0xFE91] = OpenTypeTestFonts.H, [0xFE90] = OpenTypeTestFonts.O,
            [0x0644] = OpenTypeTestFonts.H, [0x0627] = OpenTypeTestFonts.H, [0xFEFB] = OpenTypeTestFonts.Box,
            [0xFEDF] = OpenTypeTestFonts.H, [0xFE8E] = OpenTypeTestFonts.Box
        }));
        FontRegistry.Register("CFX Styled Bidi", _path);
    }

    public void Dispose() { FontRegistry.Clear(); File.Delete(_path); }

    [Fact]
    public void HebrewSpansShareVisualOrderAndKeepTheirColors() {
        var pixels = Render("<tspan fill='blue'>אב</tspan><tspan fill='red'>גד</tspan>");
        Assert.True(ColorCenter(pixels, red: true) < ColorCenter(pixels, red: false));
    }

    [Theory]
    [InlineData("start")]
    [InlineData("middle")]
    [InlineData("end")]
    public void SpanBoundariesDoNotChangeMixedTextLayout(string anchor) {
        var reference = Render("H אב12 גד O", anchor);
        var styled = Render("H <tspan fill='red'>אב</tspan><tspan fill='blue'>12 גד</tspan> O", anchor);
        Assert.Equal(Alpha(reference), Alpha(styled));
    }

    [Fact]
    public void AbsolutePositionsStartIndependentChunks() {
        var pixels = Render("<tspan x='15' fill='blue'>אב</tspan><tspan x='100' dx='8' dy='5' fill='red'>גד</tspan>");
        Assert.True(ColorCenter(pixels, red: true) > 100);
        Assert.True(ColorCenter(pixels, red: false) < 40);
    }

    [Fact]
    public void ReorderedSpanKeepsItsClipMaskOpacityAndBaselineShift() {
        var defs = "<defs><clipPath id='c'><rect x='70' y='0' width='22' height='60'/></clipPath><mask id='m' mask-type='alpha'><rect width='240' height='60' fill='white' opacity='.5'/></mask></defs>";
        var pixels = Render("<tspan fill='blue'>אב</tspan><tspan fill='red' clip-path='url(#c)' mask='url(#m)' opacity='.5' baseline-shift='5'>גד</tspan>", definitions: defs);
        Assert.InRange(ColorCenter(pixels, red: true), 70, 92);
        var redAlpha = Enumerable.Range(0, pixels.Length / 4).Where(i => pixels[i * 4] > pixels[i * 4 + 2]).Select(i => pixels[i * 4 + 3]).ToArray();
        Assert.NotEmpty(redAlpha);
        Assert.InRange(redAlpha.Max(), 45, 70);
    }

    [Fact]
    public void ArabicJoinsAcrossPaintSpans() {
        const string text = "بب";
        var reference = Render(text, "end");
        var styled = Render("<tspan fill='red'>" + text[0] + "</tspan><tspan fill='blue'>" + text[1] + "</tspan>", "end");
        Assert.Equal(Alpha(reference), Alpha(styled));
        Assert.Contains(Alpha(styled), alpha => alpha > 128);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(40)]
    public void LamAlefAcrossTextNodesPreservesBothSpanPaintsAndSizes(int size) {
        var pixels = Render("<tspan fill='red'>ل</tspan><tspan fill='blue' font-size='" + size + "'>ا</tspan>", "end");
        Assert.True(ColorCenter(pixels, red: false) < ColorCenter(pixels, red: true));
        var blueHeight = Enumerable.Range(0, pixels.Length / 4).Where(i => pixels[i * 4 + 2] > pixels[i * 4]).Select(i => i / 240).ToArray();
        Assert.InRange(blueHeight.Max() - blueHeight.Min(), size * .7 - 2, size * .7 + 2);
    }

    [Fact]
    public void CombiningMarkInAnotherSpanKeepsItsPaint() {
        var pixels = Render("אב e<tspan fill='red'>́</tspan>");
        Assert.True(ColorCenter(pixels, red: true) > 0);
    }

    private static byte[] Render(string content, string anchor = "start", string definitions = "") =>
        SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='240' height='60'>" + definitions + "<text x='70' y='40' font-family='CFX Styled Bidi' font-size='20' text-anchor='" + anchor + "'>" + content + "</text></svg>").Pixels;

    private static byte[] Alpha(byte[] pixels) => Enumerable.Range(0, pixels.Length / 4).Select(i => pixels[i * 4 + 3]).ToArray();

    private static double ColorCenter(byte[] pixels, bool red) {
        var weight = 0.0;
        var weightedX = 0.0;
        for (var i = 0; i < pixels.Length / 4; i++) {
            if (red ? pixels[i * 4] <= pixels[i * 4 + 2] : pixels[i * 4 + 2] <= pixels[i * 4]) continue;
            weight += pixels[i * 4 + 3];
            weightedX += (i % 240) * pixels[i * 4 + 3];
        }
        Assert.True(weight > 0, "Expected visible colored text.");
        return weightedX / weight;
    }
}
