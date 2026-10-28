using ChartForgeX.Primitives;
using System.Collections.Generic;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HierarchyPointStrokeColorTests {
    [Theory]
    [InlineData(ChartSeriesKind.Treemap)]
    [InlineData(ChartSeriesKind.Sunburst)]
    public void AuthoredNodeBorderUsesSharedSceneAndPreparedOutputIsDetached(ChartSeriesKind kind) {
        var items = new[] { new ChartHierarchyItem("root", "Root"), new ChartHierarchyItem("leaf", "Leaf", "root", 3) };
        var chart = Chart.Create().WithDataLabels(false);
        if (kind == ChartSeriesKind.Treemap) chart.AddTreemap("Work", items); else chart.AddSunburst("Work", items);
        var series = chart.Series[0]; var before = chart.ToSvg();
        series.WithPointStrokeColor(1, ChartColor.FromHex("#FC0349")).WithStrokeWidth(3);
        var prepared = TreemapHierarchyTests.Prepare(chart); var svg = prepared.ToSvg();
        var role = kind == ChartSeriesKind.Treemap ? "treemap-tile-mark" : "sunburst-segment-mark";
        var mark = Assert.Single(XDocument.Parse(svg).Descendants(), item => (string?)item.Attribute("data-cfx-role") == role
            && item.Ancestors().Any(parent => (string?)parent.Attribute("data-cfx-target-id") == "leaf"));
        Assert.Equal("#FC0349", ((string?)mark.Attribute("stroke"))?.ToUpperInvariant());
        Assert.Equal("3", (string?)mark.Attribute("stroke-width"));
        var png = prepared.ToPng(); Assert.NotEmpty(png);
        var pixels = RasterImageDecoder.Decode(png);
        var pink = ChartColor.FromHex("#FC0349"); var paintedBorder = false;
        for (var offset = 0; offset < pixels.Pixels.Length; offset += 4)
            if (pixels.Pixels[offset] == pink.R && pixels.Pixels[offset + 1] == pink.G && pixels.Pixels[offset + 2] == pink.B && pixels.Pixels[offset + 3] == pink.A) { paintedBorder = true; break; }
        Assert.True(paintedBorder, "The authored node border must reach native PNG paint.");
        Assert.Throws<NotSupportedException>(() => ((IList<ChartColor?>)series.PointStrokeColors)[1] = ChartColor.Black);
        series.UseSeriesStrokeColor(1);
        Assert.Null(series.PointStrokeColors[1]); Assert.Equal(before, chart.ToSvg());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        series.WithPointStrokeColor(1, ChartColor.White);
        series.ReplaceHierarchyItems(Enumerable.Reverse(items));
        Assert.Empty(series.PointStrokeColors);
        Assert.Equal(new[] { "leaf", "root" }, series.HierarchyItems.Select(item => item.Id));
    }

    [Fact]
    public void HierarchyStrokeIndicesAndUnsupportedFamilyRejectWithoutGrowingAnOrdinalList() {
        var chart = Chart.Create().AddTreemap("One", new[] { new ChartHierarchyItem("only", "Only", value: 1) });
        var series = chart.Series[0];
        Assert.Throws<ArgumentOutOfRangeException>(() => series.WithPointStrokeColor(-1, ChartColor.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.WithPointStrokeColor(1, ChartColor.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.UseSeriesStrokeColor(1));
        Assert.Empty(series.PointStrokeColors);
        var line = Chart.Create().AddLine("Line", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) }).Series[0];
        Assert.Throws<NotSupportedException>(() => line.WithPointStrokeColor(0, ChartColor.Black));
        Assert.Empty(line.PointStrokeColors);
    }
}
