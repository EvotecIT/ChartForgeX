using System.Xml.Linq;
using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

internal static partial class BrowserProofCaptions {
    /// <summary>Checks actual exported caption glyph paths and placements against the accepted live drawing before returning its semantic labels.</summary>
    internal static async Task<string[]> ExportedAsync(IPage page, XDocument exported, string role, ILocator? root = null) {
        var live = XDocument.Parse(await (root ?? page.Locator(".cfx-stage svg").First).EvaluateAsync<string>("node=>new XMLSerializer().serializeToString(node)"));
        var accepted = live.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
        var copies = exported.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
        Assert.NotEmpty(accepted); Assert.Equal(accepted.Length, copies.Length);
        var labels = new List<string>();
        for (var index = 0; index < accepted.Length; index++) {
            var text = string.Join(" ", accepted[index].Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value));
            if (text.Length == 0) text = ReadXml(accepted[index]);
            Assert.False(string.IsNullOrWhiteSpace(text));
            var sourceInk = Ink(accepted[index], live);
            if (sourceInk.Length != 0) {
                Assert.Equal(sourceInk, Ink(copies[index], exported));
            } else Assert.Equal(text, ReadXml(copies[index]));
            labels.Add(text);
        }
        return labels.ToArray();
    }

    /// <summary>Checks the actual displayed finite frame's captured glyph geometry, without asking synthetic fonts to decode in the browser.</summary>
    internal static async Task AssertFrameInkAsync(IPage page, string payload) {
        var frame = page.Locator(".cfx-native-motion-frame");
        var index = int.Parse((await frame.GetAttributeAsync("data-cfx-update-frame"))!, System.Globalization.CultureInfo.InvariantCulture);
        var drawing = await page.Locator(".cfx-interactive-chart").First.GetAttributeAsync("data-cfx-adaptive-drawing") ?? "wide";
        using var capture = JsonDocument.Parse(payload);
        var expected = XDocument.Parse(capture.RootElement.GetProperty("drawings").GetProperty(drawing).GetProperty("frames")[index].GetProperty("svg").GetString()!);
        var actual = XDocument.Parse(await frame.Locator("svg").EvaluateAsync<string>("node=>new XMLSerializer().serializeToString(node)"));
        var ink = Ink(expected.Root!, expected);
        Assert.NotEmpty(ink); Assert.Equal(ink, Ink(actual.Root!, actual));
        Assert.True(await frame.Locator("[data-cfx-native-text-ink]").EvaluateAllAsync<bool>("nodes=>nodes.length>0&&nodes.every(node=>{const b=node.getBoundingClientRect();return b.width>0&&b.height>0&&getComputedStyle(node).display!=='none'})"));
    }

    internal static async Task AssertExportInkAsync(IPage page, XDocument exported) {
        var accepted = XDocument.Parse(await page.Locator(".cfx-stage svg").First.EvaluateAsync<string>("node=>new XMLSerializer().serializeToString(node)"));
        var ink = Ink(accepted.Root!, accepted);
        Assert.NotEmpty(ink); Assert.Equal(ink, Ink(exported.Root!, exported));
    }

    private static string[] Ink(XElement owner, XDocument document) => owner.DescendantsAndSelf()
        .Where(node => node.Attribute("data-cfx-native-text-ink") != null)
        .Select(node => string.Join("|", node.DescendantsAndSelf().Select(shape => {
            var attributes = shape.Attributes().Where(attribute => attribute.Name.LocalName is "transform" or "d" or "x" or "y" or "width" or "height")
                .Select(attribute => attribute.Name.LocalName + "=" + attribute.Value);
            if (shape.Name.LocalName == "use") {
                var reference = (string?)shape.Attribute("href");
                Assert.StartsWith("#", reference, StringComparison.Ordinal);
                var target = Assert.Single(document.Descendants(), path => "#" + (string?)path.Attribute("id") == reference);
                Assert.Equal("path", target.Name.LocalName);
                attributes = attributes.Append("glyph=" + (string?)target.Attribute("d"));
            }
            if (shape.Name.LocalName == "image") attributes = attributes.Append("image=" + (string?)shape.Attribute("href"));
            return shape.Name.LocalName + ":" + string.Join(";", attributes);
        }))).ToArray();
}
