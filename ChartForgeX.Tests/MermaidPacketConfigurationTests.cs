using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidPacketConfigurationTests {
    private const string Body = "packet-beta\n0-3: \"Flag\"\n+14: \"Payload\"\n+6: \"Tail\"";
    private const string Configured = "---\nconfig:\n  packet:\n    bitsPerRow: 16\n    showBits: true\n    bitOrder: descending\n---\n" + Body;

    [Fact]
    public void SourceSettingsAndIndependentHostOverridesReachDirectAndMarkdownRendering() {
        var parsed = new MermaidParser().ParsePacket(Configured);
        Assert.Empty(parsed.Diagnostics);
        var block = parsed.Document!.ToPacketLayoutBlock();
        Assert.Equal(16, block.BitsPerRow);
        Assert.True(block.ShowBitNumbers);
        Assert.Equal(PacketBitOrder.Descending, block.BitOrder);
        Assert.Equal(new[] { (0, 3), (4, 17), (18, 23) }, block.Fields.Select(field => (field.StartBit, field.EndBit)));
        var options = new MermaidPacketRenderOptions { ShowBitNumbers = false, BitOrder = PacketBitOrder.Ascending };
        var direct = parsed.Document.ToPacketLayoutBlock(options);
        Assert.Equal(16, direct.BitsPerRow);
        Assert.False(direct.ShowBitNumbers);
        Assert.Equal(PacketBitOrder.Ascending, direct.BitOrder);
        var markup = new MermaidVisualMarkupParser(new MermaidRenderOptions { Packet = options }).Parse("```mermaid\n" + Configured + "\n```");
        Assert.Empty(markup.Diagnostics);
        var artifact = Assert.Single(markup.Artifacts);
        var fenced = Assert.IsType<PacketLayoutBlock>(artifact.Model);
        Assert.Equal(direct.ToSvg(), fenced.ToSvg());
        var json = artifact.ToInterchangeJson();
        Assert.Equal(json, VisualArtifactInterchangeEnvelope.FromJson(json).ToJson());
        var defaults = new MermaidParser().ParsePacket(Body).Document!.ToPacketLayoutBlock();
        Assert.Equal(32, defaults.BitsPerRow);
        Assert.True(defaults.ShowBitNumbers);
        Assert.Equal(PacketBitOrder.Ascending, defaults.BitOrder);
    }

    [Fact]
    public void LastDirectiveWinsAndInvalidSupersededSettingsDoNotLeakDiagnostics() {
        var source = Configured.Replace("bitsPerRow: 16", "bitsPerRow: wrong")
            .Replace("showBits: true", "showBits: wrong").Replace("bitOrder: descending", "bitOrder: wrong");
        source = source.Replace("packet-beta", "%%{init: {'packet':{'bitsPerRow':8,'showBits':false,'bitOrder':'ascending'}}}%%\npacket-beta");
        var parsed = new MermaidParser().ParsePacket(source);
        Assert.Empty(parsed.Diagnostics);
        var block = parsed.Document!.ToPacketLayoutBlock();
        Assert.Equal(8, block.BitsPerRow);
        Assert.False(block.ShowBitNumbers);
        Assert.Equal(PacketBitOrder.Ascending, block.BitOrder);
        Assert.Equal(6, parsed.Document.Configuration.Settings.Count);
    }

    [Theory]
    [InlineData("bitsPerRow", "0")]
    [InlineData("bitsPerRow", "513")]
    [InlineData("bitsPerRow", "1.5")]
    [InlineData("bitsPerRow", "'16'")]
    [InlineData("showBits", "'false'")]
    [InlineData("bitOrder", "sideways")]
    [InlineData("bitOrder", "{value: descending}")]
    public void InvalidEffectiveSettingsPreventArtifactsAtTheirDeclaration(string key, string value) {
        var rendered = MermaidRenderer.Render("---\nconfig:\n  packet:\n    " + key + ": " + value + "\n---\n" + Body);
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        var diagnostic = Assert.Single(rendered.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, diagnostic.Code);
        Assert.Equal(4, diagnostic.Span.Line);
    }

    [Theory]
    [InlineData(false, 320)]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    [InlineData(true, 960)]
    public async Task DescendingRowsMirrorVisibleFieldsWithoutChangingRanges(bool dark, int width) {
        var source = Configured.Replace("config:", "config:\n  theme: " + (dark ? "dark" : "default"));
        var parsed = new MermaidParser().ParsePacket(source);
        Assert.Empty(parsed.Diagnostics);
        var block = parsed.Document!.ToPacketLayoutBlock(new MermaidPacketRenderOptions { Width = width, Height = 300 });
        block.Options.ShowCard = false;
        var descending = XDocument.Parse(block.ToSvg());
        var fields = Fields(descending);
        Assert.Equal(new[] { (0, 3), (4, 15), (16, 17), (18, 23) }, fields.Select(field => (Integer(field, "data-bit-start"), Integer(field, "data-bit-end"))));
        Assert.True(Number(fields[0], "x") > Number(fields[1], "x"));
        Assert.True(Number(fields[2], "x") > Number(fields[3], "x"));
        var starts = descending.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "packet-bit-start").ToArray();
        var ends = descending.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "packet-bit-end").ToArray();
        Assert.Equal(new[] { "0", "4", "16", "18" }, starts.Select(element => element.Value));
        Assert.Equal(new[] { "3", "15", "17", "23" }, ends.Select(element => element.Value));
        Assert.All(starts, element => Assert.Equal("end", (string?)element.Attribute("text-anchor")));
        var native = block.ToPng();
        block.BitOrder = PacketBitOrder.Ascending;
        var ascending = Fields(XDocument.Parse(block.ToSvg()));
        for (var index = 0; index < fields.Length; index++) {
            // Each row is mirrored across the same content rectangle, including partial final rows.
            Assert.Equal(width + block.Options.Padding.Left - block.Options.Padding.Right,
                Number(fields[index], "x") + Number(ascending[index], "x") + Number(fields[index], "width") + 1, 2);
        }
        Assert.False(native.SequenceEqual(block.ToPng()));
        block.BitOrder = PacketBitOrder.Descending;
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + block.ToSvg() + "</body></html>", width, 300);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "packet-descending-" + (dark ? "dark-" : "light-") + width;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), native);
    }

    private static XElement[] Fields(XDocument svg) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "packet-field").ToArray();
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
    private static int Integer(XElement element, string attribute) => int.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
