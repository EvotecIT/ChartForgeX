using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class HtmlOwnedFontDeliveryTests {
    [Fact]
    public void CapturedFrameScopesReuseActualFaceIdentityWithoutChangingGlyphResourceScopes() {
        var renderer = new HtmlInteractiveChartRenderer();
        var chart = Model();
        var first = renderer.PrepareSection(chart, configure: value => value.IdScope = "first-font-scope");
        var second = renderer.PrepareSection(chart, configure: value => value.IdScope = "other-font-scope");
        var original = first.WideEmission!.Fonts!.Faces;
        Assert.NotEmpty(original);
        Assert.Equal(original.Select(face => (face.Alias, face.Font.CapturedByteIdentity, face.Weight, face.Italic)),
            second.WideEmission!.Fonts!.Faces.Select(face => (face.Alias, face.Font.CapturedByteIdentity, face.Weight, face.Italic)));
        Assert.NotEqual(first.WideEmission.Svg, second.WideEmission.Svg);
        var changed = renderer.PrepareSection(chart.WithTitle("A new title"), configure: value => value.IdScope = "first-font-scope");
        using var payload = JsonDocument.Parse(renderer.PrepareNativeTransitionJson(changed, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade, 3)));
        var aliases = changed.WideEmission!.Fonts!.Faces.Select(face => face.Alias).ToHashSet(StringComparer.Ordinal);
        foreach (var frame in payload.RootElement.GetProperty("drawings").GetProperty("wide").GetProperty("frames").EnumerateArray()) {
            var prefix = frame.GetProperty("prefix").GetString()!;
            var svg = XDocument.Parse(frame.GetProperty("svg").GetString()!);
            var delivered = svg.Descendants().Where(node => node.Attribute("data-cfx-owned-font-text") != null).ToArray();
            Assert.NotEmpty(delivered);
            Assert.All(delivered, node => Assert.Contains((string)node.Attribute("data-cfx-owned-font-text")!, aliases));
            Assert.All(svg.Descendants().Where(node => node.Attribute("id") != null), node =>
                Assert.StartsWith(prefix + "-", (string)node.Attribute("id")!, StringComparison.Ordinal));
        }
    }
}
