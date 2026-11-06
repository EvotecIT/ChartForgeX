using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class HtmlOwnedFontDeliveryTests {
    // ICU culture data puts U+202F into en-US clock captions ("1:00 AM") and U+00A0 into grouped numbers. Archivo has
    // U+00A0 but not U+202F; a missing space must not pull the caption out of the product face, or the owned browser
    // text is withheld and the caption depends on whichever fallback fonts the host has installed.
    [Fact]
    public void CaptionsWithUnicodeSpacesStayInTheProductFaceForNativeInkAndOwnedBrowserText() {
        var archivo = ChartFontAssets.Find("Archivo", 400, false)!.MemoryFont!;
        Assert.False(archivo.HasGlyph(0x202F)); Assert.True(archivo.HasGlyph(0x00A0));
        var captions = new[] { "1:00 AM", "1 234" };
        var chart = Chart.Create().WithSize(720, 420).WithTitle("").WithXLabels(captions)
            .AddBar("Observed", new[] { new ChartPoint(1, 12), new ChartPoint(2, 18) });
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart);

        // The scene glyphs are what both PNG and SVG native ink paint.
        var labels = section.PreparedVisual.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "axis-x-label").ToArray();
        Assert.Equal(captions, labels.Select(node => node.Text.Lines.Single().Text));
        foreach (var label in labels) {
            var glyphs = label.Text.Lines.Single().Glyphs;
            Assert.All(glyphs, glyph => { Assert.Same(archivo.Root, glyph.Face.Root); Assert.NotEqual(0, glyph.Glyph); });
            Assert.True(label.Text.Metrics.Width > 0);
        }
        // The narrow no-break space takes Archivo's thin-space width; the no-break space keeps its own glyph.
        var narrow = TextShaper.Shape(archivo, captions[0])[4];
        Assert.Equal(archivo.MapGlyph(0x20), narrow.Glyph);
        Assert.Equal(archivo.AdvanceWidth(archivo.MapGlyph(0x2009)), narrow.Advance!.Value, 6);
        Assert.Equal(archivo.MapGlyph(0x00A0), TextShaper.Shape(archivo, captions[1])[1].Glyph);

        var svg = XDocument.Parse(section.WideEmission!.Svg);
        var browserText = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "axis-x-label")
            .Select(node => node.Descendants().Single(text => text.Attribute("data-cfx-owned-font-text") != null).Value);
        Assert.Equal(captions, browserText);
    }
}
