using System.Text;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Every rendered face contributes to the allocated native shape-caption row.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyMixedCaptionLineMetricsTests : IDisposable {
    private const string Family = "CFX Mixed Caption Unequal Metrics";

    [Theory]
    [InlineData(TopologyNodeShape.Rectangle, TextInlineEmphasis.Bold, false)]
    [InlineData(TopologyNodeShape.Rectangle, TextInlineEmphasis.Italic, false)]
    [InlineData(TopologyNodeShape.Hexagon, TextInlineEmphasis.Bold, false)]
    [InlineData(TopologyNodeShape.Hexagon, TextInlineEmphasis.Italic, false)]
    [InlineData(TopologyNodeShape.Rectangle, TextInlineEmphasis.Bold, true)]
    [InlineData(TopologyNodeShape.Rectangle, TextInlineEmphasis.Italic, true)]
    public void LabelOnlyShapeKeepsEveryRunWhenASiblingFaceHasTallerMetrics(
        TopologyNodeShape shape, TextInlineEmphasis emphasis, bool tallestFirst) {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        var tall = WithAscent(OpenTypeTestFonts.NameKeyed(weight: emphasis == TextInlineEmphasis.Bold ? 700 : 400,
            italic: emphasis == TextInlineEmphasis.Italic), 1100);
        FontRegistry.Register(Family, tall, emphasis == TextInlineEmphasis.Bold ? 700 : 400,
            italic: emphasis == TextInlineEmphasis.Italic);
        var style = new TextStyle { Font = FontSpec.FromFamily(Family), FontSize = 18 };
        var regularHeight = TextLayoutEngine.Measure(TextContent.FromRuns(new[] { new TextContentRun("H") }), style).LineHeight;
        var tallHeight = TextLayoutEngine.Measure(TextContent.FromRuns(new[] { new TextContentRun("H", emphasis) }), style).LineHeight;
        Assert.True(tallHeight > regularHeight, "The synthetic registered sibling must actually have taller native metrics.");
        var content = TextContent.FromRuns(new[] {
            new TextContentRun("H", tallestFirst ? emphasis : TextInlineEmphasis.Normal),
            new TextContentRun("H", tallestFirst ? TextInlineEmphasis.Normal : emphasis)
        });
        var chart = TopologyChart.Create().WithViewport(420, 250)
            .WithTheme(VisualTheme.Report().WithFontFamily(Family));
        chart.Nodes.Add(new TopologyNode { Id = "caption", LabelContent = content, Shape = shape,
            X = 40, Y = 50, Width = 320, Height = 140, ShowStatusBadge = false,
            PreserveDisplayModeSize = true });
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "mixed-caption-unequal-" + shape + "-" + emphasis + "-" + tallestFirst);
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".png", png);
        }
        var xml = XDocument.Parse(svg);
        var caption = Assert.Single(xml.Descendants(), node => (string?)node.Attribute("data-cfx-caption") == "HH");
        Assert.Equal(2, caption.Descendants().Count(node => node.Attribute("data-cfx-native-text-ink") != null));
        NativeInlineContentTests.AssertResources(svg);
        Assert.Equal("HH", chart.Nodes[0].Label);
        Assert.Equal(content.Runs.Select(run => run.Emphasis), chart.Nodes[0].LabelContent.Runs.Select(run => run.Emphasis));
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Tile, true)]
    [InlineData(TopologyNodeDisplayMode.Card, true)]
    [InlineData(TopologyNodeDisplayMode.Icon, true)]
    [InlineData(TopologyNodeDisplayMode.Tile, false)]
    [InlineData(TopologyNodeDisplayMode.Card, false)]
    [InlineData(TopologyNodeDisplayMode.Icon, false)]
    public void CaptionModesKeepPaintedTextWithTallerSiblingFacesAndAuthoredSizes(
        TopologyNodeDisplayMode mode, bool structured) {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        FontRegistry.Register(Family, WithAscent(OpenTypeTestFonts.NameKeyed(italic: true), 1100), italic: true);
        var chart = TopologyChart.Create().WithViewport(420, 300)
            .WithTheme(VisualTheme.Report().WithFontFamily(Family))
            .WithRenderOptions(new TopologyRenderOptions {
                MaxNodeLabelLines = 1, IncludeIconLabels = true,
                TextMeasurementMode = mode == TopologyNodeDisplayMode.Icon
                    ? TextMeasurementMode.InstalledFonts : TextMeasurementMode.PortableEstimate
            });
        var node = new TopologyNode {
            Id = "caption", Label = "HH", DisplayMode = mode,
            X = 40, Y = 35, Width = 320, Height = 140,
            Shape = mode == TopologyNodeDisplayMode.Card ? TopologyNodeShape.Actor : null,
            ShowStatusBadge = false, PreserveDisplayModeSize = true,
            TextStyle = new TextStyleOverride { FontSize = mode == TopologyNodeDisplayMode.Tile ? 11
                : mode == TopologyNodeDisplayMode.Card ? 64 : 20 }
        };
        if (structured) node.LabelContent = TextContent.FromRuns(new[] {
            new TextContentRun("H"), new TextContentRun("H", TextInlineEmphasis.Italic)
        });
        chart.Nodes.Add(node);
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "mixed-caption-mode-" + mode + "-" + structured);
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".png", png);
        }
        var xml = XDocument.Parse(svg);
        if (structured) {
            var caption = Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-caption") == "HH");
            Assert.Equal(2, caption.Descendants().Count(element => element.Attribute("data-cfx-native-text-ink") != null));
            NativeInlineContentTests.AssertResources(svg);
        } else Assert.Contains(xml.Descendants(), element => element.Name.LocalName == "text" && element.Value == "HH");
        Assert.Equal("HH", node.Label);
    }

    private static byte[] WithAscent(byte[] source, short ascent) {
        var bytes = (byte[])source.Clone(); var count = (bytes[4] << 8) | bytes[5];
        for (var table = 0; table < count; table++) {
            var at = 12 + table * 16;
            if (Encoding.ASCII.GetString(bytes, at, 4) != "hhea") continue;
            var offset = (bytes[at + 8] << 24) | (bytes[at + 9] << 16) | (bytes[at + 10] << 8) | bytes[at + 11];
            bytes[offset + 4] = (byte)(ascent >> 8); bytes[offset + 5] = (byte)ascent;
            return bytes;
        }
        throw new InvalidOperationException("The synthetic font must retain its hhea table.");
    }

    public void Dispose() => FontRegistry.Clear();
}
