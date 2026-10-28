using System.Text;
using System.Xml.Linq;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Header/routing allocations must include the tallest face rather than the regular face alone.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyRemainingCaptionMetricsTests : IDisposable {
    private const string Family = "CFX Remaining Caption Metrics";
    [Theory]
    [InlineData("group", TextInlineEmphasis.Bold, false)]
    [InlineData("group", TextInlineEmphasis.Italic, false)]
    [InlineData("edge", TextInlineEmphasis.Bold, false)]
    [InlineData("edge", TextInlineEmphasis.Italic, false)]
    [InlineData("group", TextInlineEmphasis.Bold, true)]
    [InlineData("group", TextInlineEmphasis.Italic, true)]
    [InlineData("edge", TextInlineEmphasis.Bold, true)]
    [InlineData("edge", TextInlineEmphasis.Italic, true)]
    public void TallerSiblingFacesSurviveHeaderAndRelationshipAllocationAndDetachedCapture(string role, TextInlineEmphasis emphasis, bool tallFirst) {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        var weight = emphasis == TextInlineEmphasis.Bold ? 700 : 400;
        var tall = WithAscent(OpenTypeTestFonts.NameKeyed(weight: weight, italic: emphasis == TextInlineEmphasis.Italic), 1100);
        FontRegistry.Register(Family, tall, weight, italic: emphasis == TextInlineEmphasis.Italic);
        var style = new TextStyle { Font = FontSpec.FromFamily(Family), FontSize = 18 };
        Assert.True(TextLayoutEngine.Measure(TextContent.FromRuns(new[] { new TextContentRun("H", emphasis) }), style).LineHeight
            > TextLayoutEngine.Measure(TextContent.FromRuns(new[] { new TextContentRun("H") }), style).LineHeight);
        var content = TextContent.FromRuns(new[] { new TextContentRun("H", tallFirst ? emphasis : TextInlineEmphasis.Normal), new TextContentRun("H", tallFirst ? TextInlineEmphasis.Normal : emphasis) });
        var chart = MermaidRemainingCaptionRolesTests.Model().WithTheme(VisualTheme.Report().WithFontFamily(Family));
        if (role == "group") { chart.Groups[0].LabelContent = content; chart.Groups[0].TextStyle = new TextStyleOverride { FontSize = 18 }; }
        else { chart.Edges[0].LabelContent = content; chart.Edges[0].TextStyle = new TextStyleOverride { FontSize = 18 }; }
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var json = prepared.ToInterchangeEnvelope().ToJson();
        var xml = XDocument.Parse(svg); var caption = Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-caption") == "HH");
        Assert.Equal(role == "group" ? "topology-group-label" : "topology-edge-label-text", (string?)caption.Attribute("data-cfx-role"));
        Assert.Equal(2, caption.Descendants().Count(element => element.Attribute("data-cfx-native-text-ink") != null)); NativeInlineContentTests.AssertResources(svg);
        chart.Groups[0].Label = "Changed"; chart.Edges[0].Label = null;
        if (chart.Groups[0].TextStyle != null) chart.Groups[0].TextStyle!.FontSize = 9;
        if (chart.Edges[0].TextStyle != null) chart.Edges[0].TextStyle!.FontSize = 9;
        FontRegistry.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(json, prepared.ToInterchangeEnvelope().ToJson());
    }
    private static byte[] WithAscent(byte[] source, short ascent) {
        var bytes = (byte[])source.Clone(); var count = (bytes[4] << 8) | bytes[5];
        for (var table = 0; table < count; table++) {
            var at = 12 + table * 16; if (Encoding.ASCII.GetString(bytes, at, 4) != "hhea") continue;
            var offset = (bytes[at + 8] << 24) | (bytes[at + 9] << 16) | (bytes[at + 10] << 8) | bytes[at + 11];
            bytes[offset + 4] = (byte)(ascent >> 8); bytes[offset + 5] = (byte)ascent; return bytes;
        }
        throw new InvalidOperationException("The synthetic font must retain its hhea table.");
    }
    public void Dispose() => FontRegistry.Clear();
}
