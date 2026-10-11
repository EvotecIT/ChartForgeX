using System.Xml.Linq;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SegmentedMetricDeltaTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProgressRowsPreserveExactDeltaTextWithResolvedPlatformFonts(bool graphite) {
        var theme = graphite ? ChartTheme.GraphiteLight() : ChartTheme.ReportLight();
        var block = SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows).WithTheme(theme).WithSize(620, 260)
            .AddItem(new SegmentedMetricItem("Posts", 86).WithProgress(100, 44).WithDisplayValue("132,034").WithDelta("+4.3%").WithStatus(VisualStatus.Positive))
            .AddItem(new SegmentedMetricItem("Replies", 62).WithProgress(100, 44).WithDisplayValue("56,234").WithDelta("-6.4%").WithStatus(VisualStatus.Negative));
        var svg = block.ToSvg();
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            File.WriteAllText(Path.Combine(captures, "metric-delta-" + graphite + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(captures, "metric-delta-" + graphite + ".png"), block.ToPng());
        }
        var labels = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var caption in new[] { "Posts", "Replies", "132,034", "56,234", "+4.3%", "-6.4%" }) Assert.Contains(caption, labels);
    }
}
