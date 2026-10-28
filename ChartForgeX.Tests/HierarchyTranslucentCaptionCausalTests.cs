using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Existing ordinal fill APIs must choose automatic ink against the actual painted surface, not uncomposited RGB.</summary>
public sealed class HierarchyTranslucentCaptionCausalTests {
    public static IEnumerable<object[]> Cases() {
        foreach (var kind in new[] { ChartSeriesKind.Treemap, ChartSeriesKind.Sunburst, ChartSeriesKind.Tree })
            foreach (var dark in new[] { false, true }) {
                yield return new object[] { kind, dark, false, false };
                yield return new object[] { kind, dark, true, false };
                yield return new object[] { kind, dark, true, true };
            }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void AutomaticHierarchyInkUsesTheActualFrameWhileOpaqueAndExplicitInkStayAuthoritative(
        ChartSeriesKind kind, bool dark, bool translucent, bool explicitInk) {
        var chart = Single(kind); var series = chart.Series[0];
        var raw = dark ? ChartColor.White : ChartColor.Black;
        series.WithPointColor(0, raw.WithAlpha(translucent ? (byte)24 : (byte)255));
        var authoredInk = ChartColor.FromHex("#7C3AED");
        if (explicitInk) series.ConfigurePointDataLabelStyle(0, style => style.Color = authoredInk);
        var context = Context(dark, 620); var prepared = chart.Prepare(context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Capture("hierarchy-ink-" + kind + "-" + dark + "-" + translucent + "-" + explicitInk, svg, png);
        var mark = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneMark>(), item => item.Role is "treemap-tile-mark" or "sunburst-segment-mark" or "tree-node-mark");
        var actual = ChartColorMath.CompositeOverOpaque(mark.Fill!.Value, context.Theme.Resolve(context.ThemeMode).Background);
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role is "treemap-label" or "sunburst-label" or "tree-node-label");
        if (explicitInk) Assert.Equal(authoredInk, caption.Color);
        else Assert.True(ChartColorMath.ContrastRatio(caption.Color, actual) >= 4.5,
            kind + " ink " + caption.Color.ToHex() + " against native-composed " + actual.ToHex() + " has contrast " + ChartColorMath.ContrastRatio(caption.Color, actual));
        if (!translucent && !explicitInk) Assert.Equal(ChartColorMath.AccessibleTextOnBackground(mark.Fill.Value), caption.Color);
        Assert.Equal(raw.WithAlpha(translucent ? (byte)24 : (byte)255), series.PointColors[0]);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedTreemapInkIncludesAllActuallyPaintedAncestorSurfaces(bool dark) {
        var chart = Chart.Create().WithDataLabels().AddTreemap("Allocation", new[] {
            new ChartHierarchyItem("root", "Root"), new ChartHierarchyItem("section", "Section", "root"),
            new ChartHierarchyItem("target", "Target", "section", 4)
        });
        var series = chart.Series[0];
        series.WithPointColor(0, dark ? ChartColor.White : ChartColor.FromHex("#111827"));
        series.WithPointColor(1, (dark ? ChartColor.White : ChartColor.Black).WithAlpha(24));
        series.WithPointColor(2, (dark ? ChartColor.Black : ChartColor.White).WithAlpha(24));
        var context = Context(dark, 620); var prepared = chart.Prepare(context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); Capture("treemap-nested-ink-" + dark, svg, png);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(item => item.Role is "treemap-group-mark" or "treemap-tile-mark").ToArray();
        Assert.Equal(3, marks.Length);
        var actual = context.Theme.Resolve(context.ThemeMode).Background;
        foreach (var mark in marks) actual = ChartColorMath.CompositeOverOpaque(mark.Fill!.Value, actual);
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "treemap-label");
        Assert.True(ChartColorMath.ContrastRatio(caption.Color, actual) >= 4.5,
            "Nested ink " + caption.Color.ToHex() + " against native-composed " + actual.ToHex());
        Assert.Equal(new[] { "root", "section", "target" }, series.HierarchyItems.Select(item => item.Id));
    }

    internal static Chart Single(ChartSeriesKind kind) {
        var chart = Chart.Create().WithDataLabels();
        if (kind == ChartSeriesKind.Tree) return chart.AddTree("One", new[] { new ChartNode("target", "Target") }, Array.Empty<ChartTreeLink>());
        var items = new[] { new ChartHierarchyItem("target", "Target", value: 4) };
        return kind == ChartSeriesKind.Treemap ? chart.AddTreemap("One", items) : chart.AddSunburst("One", items);
    }
    internal static VisualRenderContext Context(bool dark, int width) => new(new VisualLayoutOptions(new VisualSize(width, 460)),
        VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light, new VisualFrame(showLegend: false),
        FontSpec.FromFamily(BrowserProofFonts.Family));
    private static void Capture(string name, string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), png);
    }
}
