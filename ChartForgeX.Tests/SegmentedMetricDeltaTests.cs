using System.Xml.Linq;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SegmentedMetricDeltaTests {
    [Fact]
    public void ProgressRowsPreserveExactDeltaTextWithResolvedPlatformFonts() {
        var theme = VisualTheme.Graphite();
        var block = SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows).Theme(theme).Size(620, 260)
            .AddItem(new SegmentedMetricItem("Posts", 86).Maximum(100).Segments(44).DisplayValue("132,034").Delta("+4.3%").Status(Tone.Success))
            .AddItem(new SegmentedMetricItem("Replies", 62).Maximum(100).Segments(44).DisplayValue("56,234").Delta("-6.4%").Status(Tone.Danger));
        var svg = block.ToSvg();
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            File.WriteAllText(Path.Combine(captures, "metric-delta.svg"), svg);
            File.WriteAllBytes(Path.Combine(captures, "metric-delta.png"), block.ToPng());
        }
        var labels = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var caption in new[] { "Posts", "Replies", "132,034", "56,234", "+4.3%", "-6.4%" }) Assert.Contains(caption, labels);
    }
}
