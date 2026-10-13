using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PacketPreferredRowGapTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreferredGapThatFitsIsPreservedThroughNativeSourceAndMarkdown(bool numbers) {
        var source = "---\nconfig:\n  packet:\n    bitsPerRow: 8\n    rowHeight: 20\n    paddingY: 100\n    showBits: " + (numbers ? "true" : "false") + "\n---\npacket-beta\n0-7: \"Header\"\n8-15: \"Payload\"";
        var options = new MermaidPacketRenderOptions { Width = 520, Height = 400, Padding = 20 };
        var parsed = new MermaidParser().ParsePacket(source);
        Assert.Empty(parsed.Diagnostics);
        var converted = parsed.Document!.ToPacketLayoutBlock(options);
        var markup = new MermaidVisualMarkupParser(new MermaidRenderOptions { Packet = options }).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(markup.Diagnostics);
        var fenced = Assert.IsType<PacketLayoutBlock>(Assert.Single(markup.Artifacts).Model);
        Assert.Equal(converted.ToSvg(), fenced.ToSvg());
        var native = PacketLayoutBlock.Create().WithSize(520, 400).WithPadding(20).WithBitsPerRow(8)
            .WithBitNumbers(numbers).WithRowHeight(20).WithRowGap(100)
            .AddField(0, 7, "Header").AddField(8, 15, "Payload");
        var labelTop = Number(Fields(native)[0], "y") - 20;
        foreach (var block in new[] { native, converted, fenced }) {
            var fields = Fields(block);
            Assert.Equal(2, fields.Length);
            Assert.All(fields, field => Assert.Equal(20, Number(field, "height")));
            Assert.InRange(Number(fields[1], "y") - Number(fields[0], "y") - 20 - labelTop, 99.998, 100.002);
            Assert.InRange(Number(fields[1], "y") + 20, 0, 380);
            Assert.NotEmpty(block.ToPng());
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + converted.ToSvg() + "</body></html>", 520, 400);
        var separation = await session.Page.EvaluateAsync<double>("() => { const f=[...document.querySelectorAll('[data-cfx-role=packet-field]')].map(n=>n.getBoundingClientRect()); return f[1].top-f[0].bottom; }");
        Assert.InRange(separation - labelTop, 99.998, 100.002);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "packet-preferred-row-gap-" + (numbers ? "numbered" : "plain");
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png") });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), converted.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OversizedGapUsesOnlySpaceLeftAfterPreferredRowsAndBitLabels(bool numbers) {
        var block = PacketLayoutBlock.Create().WithSize(520, 400).WithPadding(20).WithBitsPerRow(8)
            .WithBitNumbers(numbers).WithRowHeight(20).WithRowGap(400)
            .AddField(0, 7, "Header").AddField(8, 15, "Payload");
        var fields = Fields(block);
        var labelTop = Number(fields[0], "y") - 20;
        var expectedGap = 320 - 2 * labelTop;
        Assert.All(fields, field => Assert.Equal(20, Number(field, "height")));
        Assert.InRange(Number(fields[1], "y") - Number(fields[0], "y") - 20 - labelTop, expectedGap - .002, expectedGap + .002);
        Assert.Equal(380, Number(fields[1], "y") + Number(fields[1], "height"), 6);
        Assert.NotEmpty(block.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuppressedBitNumbersReleaseTheirSpaceForThePreferredGap(bool numbers) {
        var source = "---\nconfig:\n  packet:\n    bitsPerRow: 512\n    rowHeight: 20\n    paddingY: 300\n    showBits: " + (numbers ? "true" : "false") + "\n---\npacket-beta\n0-1: \"Tag\"\n2-511: \"Payload\"\n512-513: \"Footer\"";
        var options = new MermaidPacketRenderOptions { Width = 520, Height = 400, Padding = 20 };
        var parsed = new MermaidParser().ParsePacket(source);
        Assert.Empty(parsed.Diagnostics);
        var converted = parsed.Document!.ToPacketLayoutBlock(options).WithTitle("").WithSubtitle("");
        var markup = new MermaidVisualMarkupParser(new MermaidRenderOptions { Packet = options }).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(markup.Diagnostics);
        var fenced = Assert.IsType<PacketLayoutBlock>(Assert.Single(markup.Artifacts).Model).WithTitle("").WithSubtitle("");
        var native = PacketLayoutBlock.Create().WithSize(520, 400).WithPadding(20).WithBitsPerRow(512)
            .WithBitNumbers(numbers).WithRowHeight(20).WithRowGap(300)
            .AddField(0, 1, "Tag").AddField(2, 511, "Payload").AddField(512, 513, "Footer");
        foreach (var block in new[] { native, converted, fenced }) {
            var fields = Fields(block);
            Assert.Equal(3, fields.Length);
            Assert.Equal(20, Number(fields[0], "y"));
            Assert.All(fields, field => Assert.Equal(20, Number(field, "height")));
            Assert.InRange(Number(fields[2], "y") - Number(fields[0], "y") - 20, 299.998, 300.002);
            Assert.NotEmpty(block.ToPng());
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + converted.ToSvg() + "</body></html>", 520, 400);
        var separation = await session.Page.EvaluateAsync<double>("() => { const f=[...document.querySelectorAll('[data-cfx-role=packet-field]')].map(n=>n.getBoundingClientRect()); return f[2].top-f[0].bottom; }");
        Assert.InRange(separation, 299.998, 300.002);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "packet-suppressed-bit-gap-" + numbers;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png") });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), converted.ToPng());
    }

    private static XElement[] Fields(PacketLayoutBlock block) => XDocument.Parse(block.ToSvg()).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "packet-field").ToArray();
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
