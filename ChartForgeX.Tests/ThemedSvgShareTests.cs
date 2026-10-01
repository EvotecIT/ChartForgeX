using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// A host that writes token colours as CSS custom properties can show one SVG for its light and dark themes when the
/// light drawing, with the dark values of its properties, paints exactly what the dark drawing paints. This is the check
/// a report host makes; these families pass it with the Graphite tokens.
/// </summary>
public sealed class ThemedSvgShareTests {
    private static readonly string Json = File.ReadAllText(FixturePath("tokens", "palette-v1-graphite.json"));
    private static readonly VisualDesignTokens Light = VisualDesignTokens.FromJson(Json, VisualThemeMode.Light);
    private static readonly VisualDesignTokens Dark = VisualDesignTokens.FromJson(Json, VisualThemeMode.Dark);
    private static readonly Regex Variable = new(@"var\((?<name>--[A-Za-z0-9_-]+), #[0-9A-Fa-f]{6}(?:[0-9A-Fa-f]{2})?\)", RegexOptions.CultureInvariant);
    private static readonly Regex Ids = new(@"\bcfx[0-9a-f]{8}", RegexOptions.CultureInvariant);
    private static readonly Regex Definition = new(@"<(?<tag>linearGradient|radialGradient|pattern|filter|clipPath|mask|marker)\b[^>]*(?<![\w:-])id=""(?<id>[^""]+)""[^>]*?(?:/>|>.*?</\k<tag>>)", RegexOptions.CultureInvariant | RegexOptions.Singleline);

    public static IEnumerable<object[]> Families() => new[] {
        new object[] { "line" }, new object[] { "bars" }, new object[] { "flat-histogram" }, new object[] { "calendar" },
        new object[] { "heatmap" }, new object[] { "state-timeline" }, new object[] { "donut" }
    };

    [Theory]
    [MemberData(nameof(Families))]
    public void Chart_LightDrawingWithDarkProperties_PaintsTheDarkDrawing(string family) {
        var light = Build(family, Light).WithSvgColorVariables(Light.ToSvgColorVariables()).ToSvg();
        var dark = Build(family, Dark).WithSvgColorVariables(Dark.ToSvgColorVariables()).ToSvg();
        AssertShared(light, dark);
    }

    [Fact]
    public void Topology_TintsFollowTheTokensAndContrastWhiteStaysLiteral() {
        // Marker ids still carry the edge colour, so a topology is not yet one SVG for both themes; its tints and
        // contrast strokes no longer stand in the way.
        var svg = Diagram().WithDesignTokens(Light).ToSvg(new TopologyRenderOptions { SvgColorVariables = Light.ToSvgColorVariables() });
        Assert.Contains("color-mix(in srgb, var(--cfx-surface-page, #F2F3F4)", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("\uFDD0", svg, StringComparison.Ordinal);
        Assert.Equal(Diagram().WithDesignTokens(Light).ToPng(), Diagram().WithDesignTokens(Light).ToPng(new TopologyRenderOptions { SvgColorVariables = Light.ToSvgColorVariables() }));
    }

    [Fact]
    public void DerivedWhite_StaysLiteralWhereTheLightCardIsWhite() {
        // The white sheen of a line equals the light card colour; it must not take the card's property.
        var svg = Build("line", Light).WithLineVisualStyle(ChartLineVisualStyle.Premium()).WithSvgColorVariables(Light.ToSvgColorVariables()).ToSvg();
        Assert.Matches("data-cfx-role=\"line-highlight\"[^>]*stroke=\"#FFFFFF\"", svg);
        Assert.DoesNotMatch("data-cfx-role=\"line-highlight\"[^>]*stroke=\"var\\(", svg);
        // Applied to finished markup, the variables still match by value.
        Assert.Matches("data-cfx-role=\"line-highlight\"[^>]*stroke=\"var\\(--cfx-surface-card", Light.ToSvgColorVariables().Apply(Build("line", Light).ToSvg()));
    }

    [Fact]
    public void RampStep_TakesTheRampPropertyWhereASeriesColourIsTheSame() {
        // Graphite's light first series colour is also the middle sequential step; a calendar day takes the ramp's property.
        var svg = Build("calendar", Light).WithSvgColorVariables(Light.ToSvgColorVariables()).ToSvg();
        Assert.Contains("--cfx-ramps-sequential-3", svg, StringComparison.Ordinal);
        Assert.Contains("color-mix(in srgb, var(--cfx-ramps-sequential-", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("﷐", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutVariables_DerivedPaintsAreTheSameLiteralsAsBefore() {
        var svg = Build("calendar", Light).ToSvg();
        Assert.DoesNotContain("=\"var(", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("color-mix", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("﷐", svg, StringComparison.Ordinal);
        Assert.Equal(Build("calendar", Light).ToPng(), Build("calendar", Light).WithSvgColorVariables(Light.ToSvgColorVariables()).ToPng());
    }

    [Fact]
    public void PaintTokens_CannotBeForgedFromChartText_AndIdsIgnoreVariables() {
        var label = "\uFDD0L\uFDD1 and \uFDD0LFFFFFFFF\uFDD1";
        var chart = Build("line", Light).AddHorizontalLine(3, label).WithTitle(label);
        var svg = chart.ToSvg();
        AssertNotResolved(svg);
        var themed = Build("line", Light).AddHorizontalLine(3, label).WithTitle(label).WithSvgColorVariables(Light.ToSvgColorVariables()).ToSvg();
        AssertNotResolved(themed);
        string Id(string markup) => Regex.Match(markup, "<g id=\"(cfx[0-9a-f]{8})\"").Groups[1].Value;
        Assert.Equal(Id(svg), Id(themed));

        var grid = new ChartGrid().Add(Build("line", Light)).WithTitle(Forged).WithSvgColorVariables(Light.ToSvgColorVariables());
        AssertNotResolved(grid.ToSvg());

        var icon = new TopologyIconDefinition("vendor", "service", "Service", TopologyNodeKind.Service)
            .WithArtwork(TopologyIconArtwork.InlineSvg("<path d='M0 0h24v24H0z'/><desc>" + Forged + "</desc>", "0 0 24 24"));
        var catalog = new TopologyIconCatalog().AddPack(new TopologyIconPack("vendor", "Vendor").AddIcon(icon));
        var topology = TopologyChart.Create().AddIconNode("a", Forged, "vendor:service", 100, 100, catalog: catalog);
        AssertNotResolved(topology.ToSvg(new TopologyRenderOptions { IconCatalog = catalog, SvgColorVariables = Light.ToSvgColorVariables() }));
    }

    private const string Forged = "\uFDD0LFFFFFFFF\uFDD1 and";

    // A forged token is escaped to replacement characters; resolving it would have written the white it names.
    private static void AssertNotResolved(string svg) {
        Assert.DoesNotContain("\uFDD0", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("\uFDD1", svg, StringComparison.Ordinal);
        Assert.Contains("\uFFFDLFFFFFFFF\uFFFD", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("#FFFFFF and", svg, StringComparison.Ordinal);
    }

    private static void AssertShared(string light, string dark) {
        var dictionary = Dark.ToSvgColorVariables().Variables.GroupBy(variable => variable.Name).ToDictionary(group => group.Key, group => Hex(group.First().Color));
        // As the host compares: definitions nothing references are left out (the card and plot surface gradients of a
        // chart that draws neither), each property takes its dark value, and the ids of the rendering are made equal.
        string Canonical(string svg) {
            var used = Definition.Replace(svg, match => IsReferenced(svg, match.Groups["id"].Value) ? match.Value : string.Empty);
            return Ids.Replace(Variable.Replace(used, match => dictionary.TryGetValue(match.Groups["name"].Value, out var value) ? value : match.Value), "cfx");
        }

        var expected = Canonical(dark);
        var actual = Canonical(light);
        if (expected == actual) return;
        var index = 0;
        while (index < expected.Length && index < actual.Length && expected[index] == actual[index]) index++;
        var start = Math.Max(0, index - 40);
        Assert.Fail("The light drawing differs from the dark one at " + index.ToString(CultureInfo.InvariantCulture) + ":\nlight: " + actual.Substring(start, Math.Min(320, actual.Length - start)) + "\ndark:  " + expected.Substring(start, Math.Min(320, expected.Length - start)));
    }

    private static bool IsReferenced(string svg, string id) {
        var reference = "#" + id;
        var index = svg.IndexOf(reference, StringComparison.Ordinal);
        while (index >= 0) {
            var end = index + reference.Length;
            if (end < svg.Length && svg[end] is ')' or '"' or '\'') return true;
            index = svg.IndexOf(reference, end, StringComparison.Ordinal);
        }

        return false;
    }

    private static string Hex(ChartColor color) => color.A == 255 ? color.ToHex() : color.ToHexRgba();

    /// <summary>A chart as a report host creates it: token colours and font, transparent, no card, plot surface, or header.</summary>
    private static Chart Host(VisualDesignTokens tokens, int width = 640, int height = 320) => Chart.Create()
        .WithSize(width, height).WithDesignTokens(tokens).WithTransparentBackground().WithCard(false).WithPlotBackground(false).WithHeader(false)
        .WithMarkBackdrop(ChartMarkBackdrop.Card);

    private static Chart Build(string family, VisualDesignTokens tokens) {
        var day = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        switch (family) {
            case "line":
                return Host(tokens).AddLine("Inbound", Points(1, 3, 2, 5)).AddLine("Outbound", Points(2, 1, 3, 2));
            case "bars":
                return Host(tokens).AddBar("Changes", Points(3, 7, 5, 2)).AddBar("Deletes", Points(1, 2, 4, 3));
            case "flat-histogram":
                return Host(tokens).WithBarStyle(ChartBarStyle.Flat).AddHistogram("Latency", new[] { 1d, 2, 2, 3, 3, 3, 4, 5, 5, 6 }, 5);
            case "calendar":
                return Host(tokens, 760, 230).AddCalendarHeatmap("Changes", Enumerable.Range(0, 60).Select(i => new ChartCalendarHeatmapItem(new DateTime(2026, 7, 1).AddDays(i), i % 6 == 0 ? 0 : i % 9)).ToArray());
            case "heatmap":
                var heatmap = Host(tokens).WithXLabels("A", "B", "C", "D")
                    .AddHeatmapRow("DC01", new[] { 0d, 3, 7, 12 }).AddHeatmapRow("DC02", new[] { 5d, 0, 2, 9 });
                heatmap.Options.HeatmapRelativeScale = true;
                return heatmap;
            case "state-timeline":
                var states = tokens.Status.OperationalStateCategories();
                return Host(tokens, 720, 280).WithStateCategories(states.ToArray())
                    .AddStateTimelineLane("DC01", new[] { new ChartStateTimelineSegment(day, day.AddHours(6), "up"), new ChartStateTimelineSegment(day.AddHours(6), day.AddHours(8), "down"), new ChartStateTimelineSegment(day.AddHours(8), day.AddHours(12), "notObservable") })
                    .AddStateTimelineLane("DC02", new[] { new ChartStateTimelineSegment(day, day.AddHours(5), "unknown"), new ChartStateTimelineSegment(day.AddHours(5), day.AddHours(12), "maintenance") });
            default:
                return Host(tokens).AddDonut("Outcomes", new[] { new ChartPoint(0, 5), new ChartPoint(1, 3), new ChartPoint(2, 2) });
        }
    }

    private static TopologyChart Diagram() => TopologyChart.Create().WithId("sites").WithViewport(640, 360)
        .AddAutoGroup("waw", "Warsaw", TopologyHealthStatus.Healthy)
        .AddAutoNode("dc1", "DC01", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, groupId: "waw")
        .AddAutoNode("dc2", "DC02", TopologyNodeKind.Server, TopologyHealthStatus.Warning, groupId: "waw")
        .AddAutoNode("dc3", "DC03", TopologyNodeKind.Server, TopologyHealthStatus.Critical)
        .AddEdge("a", "dc1", "dc2").AddEdge("b", "dc2", "dc3");

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();

    private static string FixturePath(params string[] parts) {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ChartForgeX.sln"))) directory = directory.Parent;
        if (directory == null) throw new InvalidOperationException("Repository root was not found.");
        return Path.Combine(new[] { directory.FullName, "ChartForgeX.Tests", "Fixtures" }.Concat(parts).ToArray());
    }
}
