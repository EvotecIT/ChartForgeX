using System.Xml.Linq;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FactTableLayoutOwnershipTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FittedTableHeaderAndLastCaptionRemainVisibleOnLargeLogicalCanvases(bool dark) {
        var table = ChartTable.Create().WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(6790, 3772).WithPadding(6).WithCard(false).WithTransparentBackground().WithColumns("Service");
        for (var i = 0; i < 120; i++) table.AddRow(i == 119 ? "XXXXXXXXXXXXXXXXXXXX" : "service " + i);

        var svg = table.ToSvg("fitted-table");
        foreach (var scene in new[] { svg, new SvgFactBlockRenderer().RenderLabelScene(table).ToSvg() }) {
            var labels = XDocument.Parse(scene).Descendants().Where(element => element.Name.LocalName == "text").ToArray();
            Assert.Equal(121, labels.Length);
            foreach (var caption in new[] { "Service", "XXXXXXXXXXXXXXXXXXXX" }) {
                var label = Assert.Single(labels, element => element.Value == caption);
                Assert.NotEqual("none", (string?)label.Attribute("display"));
                Assert.NotEqual("true", (string?)label.Attribute("aria-hidden"));
            }
        }
        var captureRoot = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (!string.IsNullOrWhiteSpace(captureRoot)) {
            var folder = Path.Combine(captureRoot, "fitted-table"); Directory.CreateDirectory(folder);
            var name = dark ? "dark" : "light";
            File.WriteAllText(Path.Combine(folder, name + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(folder, name + "-bounded-svg.png"),
                ChartForgeX.SvgRaster.SvgRasterizer.ToPng(svg, 720, 400, null));
            var detail = XDocument.Parse(svg);
            detail.Root!.SetAttributeValue("width", 720); detail.Root.SetAttributeValue("height", 110);
            detail.Root.SetAttributeValue("viewBox", "0 0 720 110");
            File.WriteAllBytes(Path.Combine(folder, name + "-header.png"),
                ChartForgeX.SvgRaster.SvgRasterizer.ToPng(detail.ToString(), 720, 110, null));
        }
    }
}
