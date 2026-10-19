using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SegmentedMetricNumericIntegrityTests {
    [Theory]
    [InlineData(SegmentedMetricStyle.CompositionStrip)]
    [InlineData(SegmentedMetricStyle.DistributionRows)]
    [InlineData(SegmentedMetricStyle.CapsuleLoop)]
    public void FiniteLargePartsRetainSharesRawFactsAndNativeGeometry(SegmentedMetricStyle style) {
        foreach (var value in new[] { 1e307, 1e308 }) {
            var block = Create(style, value, false);
            var svg = XDocument.Parse(block.ToSvg());
            var parts = Parts(svg);
            Assert.Equal(2, parts.Length);
            Assert.All(parts, part => {
                Assert.Equal(value, Number(part, "data-cfx-value"));
                Assert.Equal(.5, Number(part, "data-cfx-share"));
            });
            var totals = svg.Descendants().Where(element => element.Attribute("data-cfx-total") != null || element.Attribute("data-cfx-total-overflow") != null).ToArray();
            Assert.NotEmpty(totals);
            Assert.All(totals, total => {
                if (value == 1e308) {
                    Assert.Null(total.Attribute("data-cfx-total"));
                    Assert.Equal("true", (string?)total.Attribute("data-cfx-total-overflow"));
                } else {
                    Assert.Equal(value * 2, Number(total, "data-cfx-total"));
                    Assert.Null(total.Attribute("data-cfx-total-overflow"));
                }
            });
            // Explicit display values isolate part geometry from unrelated formatted-number width.
            Assert.Equal(Create(style, 1, false).RenderRgba().Pixels, block.RenderRgba().Pixels);
        }
    }

    [Theory]
    [InlineData(SegmentedMetricStyle.CompositionStrip)]
    [InlineData(SegmentedMetricStyle.DistributionRows)]
    [InlineData(SegmentedMetricStyle.CapsuleLoop)]
    public void ZeroPartsReserveNoGeometryOrGapAndAnAllZeroWholeRemainsInvalid(SegmentedMetricStyle style) {
        var block = Create(style, 5, true);
        var parts = Parts(XDocument.Parse(block.ToSvg()));
        Assert.Equal(new[] { "First", "Second" }, parts.Select(part => (string?)part.Attribute("data-cfx-label")));
        Assert.All(parts, part => Assert.Equal(.5, Number(part, "data-cfx-share")));
        if (style != SegmentedMetricStyle.CapsuleLoop) {
            Assert.Equal(Number(parts[0], "width"), Number(parts[1], "width"), 3);
        } else {
            Assert.Equal(0, Number(parts[0], "data-cfx-start"));
            Assert.Equal(.5, Number(parts[0], "data-cfx-end"));
            Assert.Equal(.5, Number(parts[1], "data-cfx-start"));
            Assert.Equal(1, Number(parts[1], "data-cfx-end"));
        }
        var empty = Create(style, 0, true);
        Assert.Throws<InvalidOperationException>(() => empty.ToSvg());
        Assert.Throws<InvalidOperationException>(() => empty.RenderRgba());
    }

    private static SegmentedMetricBlock Create(SegmentedMetricStyle style, double value, bool includeZero) {
        var block = SegmentedMetricBlock.Create(style).WithSize(620, 360).WithMetric("Capacity", "Balanced");
        block.AddItem(new SegmentedMetricItem("First", value) { Color = ChartColor.FromHex("#D32F2F"), DisplayValue = "One half" });
        if (includeZero) block.AddItem(new SegmentedMetricItem("Empty", 0) { DisplayValue = "Empty" });
        block.AddItem(new SegmentedMetricItem("Second", value) { Color = ChartColor.FromHex("#1976D2"), DisplayValue = "One half" });
        if (includeZero) block.AddItem(new SegmentedMetricItem("Trailing empty", 0) { DisplayValue = "Empty" });
        return block;
    }

    private static XElement[] Parts(XDocument svg) => svg.Descendants().Where(element => {
        var role = (string?)element.Attribute("data-cfx-role");
        return role == "segmented-metric-composition-segment" || role == "segmented-metric-distribution-segment" || role == "segmented-metric-capsule-segment";
    }).ToArray();

    private static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
}
