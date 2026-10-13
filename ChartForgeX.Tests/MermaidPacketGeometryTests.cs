using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.SvgRaster;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidPacketGeometryTests {
    private const string Body = "packet-beta\n0-3: \"Flags\"\n+14: \"Payload\"\n+6: \"Tail\"";
    private const string Source = "---\nconfig:\n  packet:\n    bitsPerRow: 16\n    bitWidth: 24\n    rowHeight: 40\n    paddingX: 3\n    paddingY: 7\n---\n" + Body;

    [Fact]
    public void DimensionsReachNativeGeometryAndIndependentMarkdownOverrides() {
        var parsed = new MermaidParser().ParsePacket(Source);
        Assert.Empty(parsed.Diagnostics);
        var block = parsed.Document!.ToPacketLayoutBlock();
        Assert.Equal(24, block.BitWidth);
        Assert.Equal(40, block.RowHeight);
        Assert.Equal(3, block.FieldGap);
        Assert.Equal(7, block.RowGap);
        var fields = Fields(block);
        Assert.Equal(93, Number(fields[0], "width"));
        Assert.Equal(40, Number(fields[0], "height"));
        Assert.Equal(96, Number(fields[1], "x") - Number(fields[0], "x"), 6);
        var withoutNumbers = parsed.Document!.ToPacketLayoutBlock(new MermaidPacketRenderOptions { ShowBitNumbers = false });
        var rows = Fields(withoutNumbers);
        Assert.Equal(47, Number(rows[2], "y") - Number(rows[0], "y"), 6);

        var options = new MermaidPacketRenderOptions { RowHeight = 28, FieldGap = 0, RowGap = 0 };
        var parser = new MermaidVisualMarkupParser(new MermaidRenderOptions { Packet = options });
        options.RowHeight = 1; options.FieldGap = 90; options.RowGap = 90;
        var markup = parser.Parse("```mermaid\n" + Source + "\n```");
        Assert.Empty(markup.Diagnostics);
        var fenced = Assert.IsType<PacketLayoutBlock>(Assert.Single(markup.Artifacts).Model);
        Assert.Equal(24, fenced.BitWidth);
        Assert.Equal(28, fenced.RowHeight);
        Assert.Equal(0, fenced.FieldGap);
        Assert.Equal(0, fenced.RowGap);
        Assert.Equal(28, Number(Fields(fenced)[0], "height"));
        Assert.Equal(96, Number(Fields(fenced)[0], "width"));
        Assert.NotEmpty(fenced.ToPng());

        var defaults = new MermaidParser().ParsePacket(Body).Document!.ToPacketLayoutBlock();
        Assert.Null(defaults.BitWidth); Assert.Null(defaults.RowHeight);
        Assert.Null(defaults.FieldGap); Assert.Null(defaults.RowGap);
    }

    [Fact]
    public void EffectiveGeometrySettingsUseSharedNumericParsingAndSourceOrder() {
        var radix = new MermaidParser().ParsePacket(Source.Replace("bitWidth: 24", "bitWidth: 0x18")
            .Replace("rowHeight: 40", "rowHeight: 4e1").Replace("paddingX: 3", "paddingX: 0b11").Replace("paddingY: 7", "paddingY: 0o7"));
        Assert.Empty(radix.Diagnostics);
        var numeric = radix.Document!.ToPacketLayoutBlock();
        Assert.Equal(24, numeric.BitWidth); Assert.Equal(40, numeric.RowHeight);
        Assert.Equal(3, numeric.FieldGap); Assert.Equal(7, numeric.RowGap);
        var source = Source.Replace("bitWidth: 24", "bitWidth: bad").Replace("rowHeight: 40", "rowHeight: bad")
            .Replace("paddingX: 3", "paddingX: bad").Replace("paddingY: 7", "paddingY: bad")
            .Replace("packet-beta", "%%{init: {'packet':{'bitWidth':24,'rowHeight':4e1,'paddingX':3,'paddingY':7}}}%%\npacket-beta");
        var parsed = new MermaidParser().ParsePacket(source);
        Assert.Empty(parsed.Diagnostics);
        var block = parsed.Document!.ToPacketLayoutBlock();
        Assert.Equal(24, block.BitWidth); Assert.Equal(40, block.RowHeight);
        Assert.Equal(3, block.FieldGap); Assert.Equal(7, block.RowGap);
    }

    [Theory]
    [InlineData("bitWidth", "0")]
    [InlineData("rowHeight", ".5")]
    [InlineData("paddingX", "-1")]
    [InlineData("paddingY", "-1")]
    [InlineData("bitWidth", "'24'")]
    [InlineData("rowHeight", "true")]
    [InlineData("paddingX", "{value: 3}")]
    [InlineData("paddingY", "1e999")]
    public void InvalidEffectiveGeometryStopsRenderingAtTheDeclaration(string key, string value) {
        var result = MermaidRenderer.Render("---\nconfig:\n  packet:\n    " + key + ": " + value + "\n---\n" + Body);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, diagnostic.Code);
        Assert.Equal(4, diagnostic.Span.Line);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NativeDimensionsRejectNonFiniteOrUndersizedValues(double value) {
        var block = PacketLayoutBlock.Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => block.BitWidth = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => block.RowHeight = value);
        if (value == 0) return;
        Assert.Throws<ArgumentOutOfRangeException>(() => block.FieldGap = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => block.RowGap = value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000000)]
    public void ZeroAndOversizedGapsKeepDenseFieldsInsideTheirBitCells(double gap) {
        var block = PacketLayoutBlock.Create().WithSize(80, 100).WithPadding(4).WithBitsPerRow(512)
            .WithBitNumbers(false).WithFieldGap(gap).WithRowGap(gap).WithBitWidth(1000000).WithRowHeight(1000000)
            .AddField(0, 0, "A").AddField(1, 1, "B").AddField(2, 512, "Payload");
        var fields = Fields(block);
        Assert.True(Number(fields[0], "x") + Number(fields[0], "width") <= Number(fields[1], "x") + .01);
        Assert.All(fields, field => {
            Assert.InRange(Number(field, "width"), 0, 72);
            Assert.InRange(Number(field, "y") + Number(field, "height"), 0, 100);
        });
        Assert.NotEmpty(block.ToPng());
    }

    [Theory]
    [InlineData(false, 320, 3)]
    [InlineData(false, 960, 3)]
    [InlineData(true, 320, 3)]
    [InlineData(true, 960, 3)]
    [InlineData(false, 320, 48)]
    [InlineData(true, 960, 48)]
    [InlineData(false, 320, 1000000)]
    public async Task PreferredDimensionsFitCompactAndWideDescendingRows(bool dark, int width, double gap) {
        var source = Source.Replace("config:", "config:\n  theme: " + (dark ? "dark" : "default"))
            .Replace("bitsPerRow: 16", "bitsPerRow: 16\n    bitOrder: descending");
        var parsed = new MermaidParser().ParsePacket(source);
        Assert.Empty(parsed.Diagnostics);
        var block = parsed.Document!.ToPacketLayoutBlock(new MermaidPacketRenderOptions { Width = width, Height = 300 });
        block.Options.ShowCard = false;
        block.WithFieldGap(gap);
        var fields = Fields(block);
        Assert.All(fields, field => {
            Assert.InRange(Number(field, "x"), 0, width);
            Assert.InRange(Number(field, "x") + Number(field, "width"), 0, width);
            Assert.Equal(40, Number(field, "height"));
        });
        var native = block.ToPng();
        Assert.NotEmpty(native);
        block.BitOrder = PacketBitOrder.Ascending;
        var ascending = Fields(block);
        var rowWidth = Math.Min(24, (width - block.Options.Padding.Left - block.Options.Padding.Right) / 16) * 16;
        for (var index = 0; index < fields.Length; index++) {
            Assert.Equal(2 * block.Options.Padding.Left + rowWidth,
                Number(fields[index], "x") + Number(fields[index], "width") + Number(ascending[index], "x"), 2);
        }
        block.BitOrder = PacketBitOrder.Descending;
        block.WithTransparentBackground();
        var raster = SvgRasterizer.Rasterize(block.ToSvg(), width, 300);
        Assert.Empty(raster.Diagnostics);
        var nativeImage = block.RenderRgba();
        foreach (var row in fields.Select(field => (int)(Number(field, "y") + Number(field, "height") / 4)).Distinct()) {
            var expectedInk = Enumerable.Range(0, width).Where(x => raster.Image.Pixels[(row * width + x) * 4 + 3] > 32).ToArray();
            var actualInk = Enumerable.Range(0, width).Where(x => nativeImage.Pixels[(row * width + x) * 4 + 3] > 32).ToArray();
            Assert.NotEmpty(expectedInk); Assert.NotEmpty(actualInk);
            Assert.InRange(actualInk[0] - expectedInk[0], -1, 1);
            Assert.InRange(actualInk[actualInk.Length - 1] - expectedInk[expectedInk.Length - 1], -1, 1);
        }
        block.WithTransparentBackground(false);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + block.ToSvg() + "</body></html>", width, 300);
        var bounds = await session.Page.EvaluateAsync<string>("() => JSON.stringify([...document.querySelectorAll('[data-cfx-role=packet-field]')].map(n=>n.getBoundingClientRect().toJSON()))");
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('[data-cfx-role=packet-field]')].every(n=>{const b=n.getBoundingClientRect();return b.left>=-.01&&b.right<=innerWidth+.01&&Math.abs(b.height-40)<.01;})"), bounds);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "packet-geometry-" + (dark ? "dark-" : "light-") + width + "-gap-" + gap.ToString(CultureInfo.InvariantCulture);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), native);
    }

    private static XElement[] Fields(PacketLayoutBlock block) => XDocument.Parse(block.ToSvg()).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "packet-field").ToArray();
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
