using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PaletteV2CategoryTests {
    [Theory]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Treemap)]
    public void ExtendedHierarchyBranchesKeepTheirPaletteBinding(ChartSeriesKind kind) {
        var items = new List<ChartHierarchyItem>();
        if (kind == ChartSeriesKind.Sunburst) items.Add(new ChartHierarchyItem("all", "All", value: 12));
        items.AddRange(Enumerable.Range(0, 12).Select(index => new ChartHierarchyItem("branch-" + index, "Branch " + index,
            kind == ChartSeriesKind.Sunburst ? "all" : null, 1)));
        var chart = Chart.Create().WithSize(800, 600).WithLegend(false).WithDataLabels(false).WithSvgColorVariables(SvgColorVariables.SharedRoles().Clone());
        if (kind == ChartSeriesKind.Sunburst) chart.AddSunburst("Branches", items); else chart.AddTreemap("Branches", items);
        var document = XDocument.Parse(chart.ToSvg());
        var branch = document.Descendants().Single(element => element.Name.LocalName == "g" && (string?)element.Attribute("data-cfx-node") == "branch-10");
        var fill = branch.Descendants().First(element => element.Name.LocalName is "path" or "rect" && element.Attribute("fill") != null).Attribute("fill")!.Value;
        Assert.Contains("color-mix(", fill, StringComparison.Ordinal);
        Assert.Contains("var(--hfx-color-chart-series-1", fill, StringComparison.Ordinal);
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Treemap)]
    public void InheritedPaintStaysBoundedForADeepValidHierarchy(ChartSeriesKind kind) {
        var items = Enumerable.Range(0, 20).Select(index => new ChartHierarchyItem("node-" + index, "Node " + index,
            index == 0 ? null : "node-" + (index - 1), index == 19 ? 1 : null)).ToArray();
        var chart = Chart.Create().WithSize(800, 800).WithLegend(false).WithDataLabels(false);
        if (kind == ChartSeriesKind.Sunburst) chart.AddSunburst("Depth", items); else chart.AddTreemap("Depth", items);
        var literal = chart.ToSvg(); Assert.NotEmpty(chart.ToPng());
        chart.WithSvgColorVariables(SvgColorVariables.SharedRoles());
        var bound = chart.ToSvg();
        Assert.DoesNotContain('\uFDD0', bound);
        Assert.Contains("var(--hfx-color-chart-series-1", bound, StringComparison.Ordinal);
        Assert.True(bound.Length < literal.Length + 100_000);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void PartToWholeUsesValueRankAndKeepsOtherNeutralAndLast(VisualThemeMode mode) {
        var theme = VisualTheme.Graphite();
        var chart = Chart.Create().WithSize(800, 520).WithTheme(theme, mode)
            .AddPie("Allocation", new[] { new ChartPoint(1, 2), new ChartPoint(2, 20), new ChartPoint(3, 40),
                new ChartPoint(4, 10), new ChartPoint(5, 30), new ChartPoint(6, 6), new ChartPoint(7, 1) });
        var document = XDocument.Parse(chart.ToSvg());
        var slices = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "radial-point").ToArray();
        Assert.Equal(new[] { "2", "4", "1", "3", "5", "-1" }, slices.Select(element => (string?)element.Attribute("data-cfx-point")));
        var colors = slices.Select(Fill).ToArray();
        Assert.Equal(theme.Resolve(mode).Palette.Take(5), colors.Take(5));
        var other = colors[^1];
        Assert.Equal(other.R, other.G); Assert.Equal(other.G, other.B);
        Assert.DoesNotContain(other, colors.Take(5));
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Sunburst, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Sunburst, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Treemap, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Treemap, VisualThemeMode.Dark)]
    public void ChildSegmentsInheritHueWhileSiblingLightnessVaries(ChartSeriesKind kind, VisualThemeMode mode) {
        var items = new List<ChartHierarchyItem>();
        if (kind == ChartSeriesKind.Sunburst) items.Add(new ChartHierarchyItem("all", "All", value: 120));
        var parent = kind == ChartSeriesKind.Sunburst ? "all" : null;
        items.AddRange(new[] { new ChartHierarchyItem("north", "North", parent, kind == ChartSeriesKind.Sunburst ? 60 : null), new ChartHierarchyItem("south", "South", parent, kind == ChartSeriesKind.Sunburst ? 40 : null),
            new ChartHierarchyItem("north-a", "North A", "north", 25), new ChartHierarchyItem("north-b", "North B", "north", 35),
            new ChartHierarchyItem("south-a", "South A", "south", 15), new ChartHierarchyItem("south-b", "South B", "south", 25) });
        var chart = Chart.Create().WithSize(800, 520).WithTheme(VisualTheme.Graphite(), mode);
        if (kind == ChartSeriesKind.Sunburst) chart.AddSunburst("Allocation", items); else chart.AddTreemap("Allocation", items);
        var document = XDocument.Parse(chart.ToSvg());
        var nodes = document.Descendants().Where(element => element.Name.LocalName == "g" && element.Attribute("data-cfx-node") != null)
            .ToDictionary(element => element.Attribute("data-cfx-node")!.Value, Fill);
        foreach (var branch in new[] { "north", "south" }) foreach (var child in new[] { branch + "-a", branch + "-b" }) {
            var (parentHue, parentLightness) = HueAndLightness(nodes[branch]);
            var (childHue, childLightness) = HueAndLightness(nodes[child]);
            var delta = Math.Abs(parentHue - childHue);
            Assert.True(Math.Min(delta, 360 - delta) < 2);
            Assert.InRange(Math.Abs(parentLightness - childLightness), .001, .101);
        }
        Assert.NotEmpty(chart.ToPng());
    }

    private static ChartColor Fill(XElement group) => ChartColor.FromHex(group.Descendants()
        .First(element => element.Name.LocalName is "path" or "rect" or "circle" && ((string?)element.Attribute("fill"))?.StartsWith("#", StringComparison.Ordinal) == true)
        .Attribute("fill")!.Value);

    private static (double Hue, double Lightness) HueAndLightness(ChartColor color) {
        var r = color.R / 255d; var g = color.G / 255d; var b = color.B / 255d;
        var high = Math.Max(r, Math.Max(g, b)); var low = Math.Min(r, Math.Min(g, b)); var span = high - low;
        var hue = span == 0 ? 0 : high == r ? 60 * ((g - b) / span % 6) : high == g ? 60 * ((b - r) / span + 2) : 60 * ((r - g) / span + 4);
        return ((hue + 360) % 360, (high + low) / 2);
    }
}
