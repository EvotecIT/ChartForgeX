using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
        new object[] { "heatmap" }, new object[] { "state-timeline" }, new object[] { "donut" },
        new object[] { "heatmap-values" }, new object[] { "categorical-text" }, new object[] { "hexbin-values" }, new object[] { "gantt-lanes" }, new object[] { "semantic-values" }
    };

    public static IEnumerable<object[]> MarkTextFamilies() => new[] {
        new object[] { "heatmap-values" }, new object[] { "categorical-text" }, new object[] { "hexbin-values" }, new object[] { "gantt-lanes" }, new object[] { "semantic-values" }
    };

    [Theory]
    [MemberData(nameof(Families))]
    public void Chart_LightDrawingWithDarkProperties_PaintsTheDarkDrawing(string family) {
        var light = Build(family, Light).WithSvgColorVariables(Light.ToSvgColorVariables()).ToSvg();
        var dark = Build(family, Dark).WithSvgColorVariables(Dark.ToSvgColorVariables()).ToSvg();
        AssertShared(light, dark);
    }

    [Theory]
    [MemberData(nameof(MarkTextFamilies))]
    public void MarkText_ReachesThreeToOneOnEveryFill_InBothThemes(string family) {
        foreach (var tokens in new[] { Light, Dark }) {
            // On the card, as report hosts draw marks, and on the page surface behind a chart without card (Layered).
            foreach (var (backdrop, surface) in new[] { (ChartMarkBackdrop.Card, Surface(tokens)), (ChartMarkBackdrop.Layered, tokens.Background) }) {
                var texts = MarkTexts(Build(family, tokens).WithMarkBackdrop(backdrop).ToSvg(), surface);
                Assert.NotEmpty(texts);
                foreach (var (fill, text, label) in texts) {
                    var contrast = Contrast(fill, text);
                    Assert.True(contrast >= 3, $"{family} on {backdrop} ({tokens.Foreground.ToHex()} text): '{label}' is {text.ToHex()} on {fill.ToHex()} at {contrast:0.00}:1.");
                }
            }
        }
    }

    [Fact]
    public void MarkText_OnAPaleCallerColour_StaysReadable() {
        // A strong cell of a pale series colour would get the white surface as text; the text colour contrasts more.
        var chart = Host(Light).WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always)
            .WithXLabels("A", "B", "C").AddHeatmapRow("Pale", new[] { 1d, 50, 100 }, ChartColor.FromHex("#FFE066"));
        var texts = MarkTexts(chart.ToSvg(), Surface(Light));
        Assert.Equal(3, texts.Count);
        foreach (var (fill, text, label) in texts) Assert.True(Contrast(fill, text) >= 3, $"'{label}' is {text.ToHex()} on {fill.ToHex()}.");
    }

    [Fact]
    public void MarkText_IsWrittenByRole_WithVariables() {
        var svg = Build("categorical-text", Light).WithSvgColorVariables(Light.ToSvgColorVariables()).ToSvg();
        // Solid marks carry the card surface as text, quiet and outlined tints the text colour.
        Assert.Matches("data-cfx-role=\"data-label\"[^>]*fill=\"var\\(--cfx-surface-card, #FFFFFF\\)\"", svg);
        Assert.Matches("data-cfx-role=\"data-label\"[^>]*fill=\"var\\(--cfx-text-primary, #16181C\\)\"", svg);
        Assert.DoesNotMatch("data-cfx-role=\"data-label\"[^>]*fill=\"#", svg);
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

    private static readonly HashSet<string> MarkRoles = new(StringComparer.Ordinal) { "heatmap-cell", "hexbin-cell", "gantt-lane-item" };

    /// <summary>Pairs every text drawn on a mark with the mark's fill as it appears on <paramref name="backdrop"/>.</summary>
    private static List<(ChartColor Fill, ChartColor Text, string Label)> MarkTexts(string svg, ChartColor backdrop) {
        var result = new List<(ChartColor, ChartColor, string)>();
        ChartColor? mark = null;
        foreach (var element in XDocument.Parse(svg).Descendants()) {
            var role = (string?)element.Attribute("data-cfx-role");
            if (role != null && MarkRoles.Contains(role)) {
                var opacity = double.Parse((string?)element.Attribute("fill-opacity") ?? "1", CultureInfo.InvariantCulture);
                mark = Over(ChartColor.FromHex((string)element.Attribute("fill")!), opacity, backdrop);
            } else if (mark.HasValue && role is "data-label" or "gantt-lane-item-label") {
                result.Add((mark.Value, ChartColor.FromHex((string)element.Attribute("fill")!), element.Value));
            }
        }

        return result;
    }

    private static ChartColor Over(ChartColor top, double opacity, ChartColor bottom) => ChartColor.FromRgb(
        (byte)Math.Round(top.R * opacity + bottom.R * (1 - opacity)),
        (byte)Math.Round(top.G * opacity + bottom.G * (1 - opacity)),
        (byte)Math.Round(top.B * opacity + bottom.B * (1 - opacity)));

    private static ChartColor Surface(VisualDesignTokens tokens) => tokens.ElevatedSurface;

    private static double Contrast(ChartColor first, ChartColor second) {
        static double Channel(byte value) {
            var c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        static double Luminance(ChartColor color) => 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        var a = Luminance(first);
        var b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

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
            case "heatmap-values":
                // Counts across the whole ramp, from a neutral zero to the strongest step, with the value in every cell.
                var counts = Host(tokens, 720, 280).WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always).WithXLabels("1", "2", "3", "4", "5", "6", "7", "8");
                for (var row = 0; row < 3; row++) counts.AddHeatmapRow("Row " + (row + 1).ToString(CultureInfo.InvariantCulture), Enumerable.Range(row * 8, 8).Select(value => (double)value).ToArray());
                counts.Options.HeatmapRelativeScale = true;
                return counts;
            case "semantic-values":
                // Status colours from negative through warning to positive, with the value in every cell.
                var semantic = Host(tokens, 720, 280).WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always).WithXLabels("1", "2", "3", "4", "5", "6", "7", "8");
                semantic.Options.HeatmapScale = ChartHeatmapScale.Semantic;
                for (var row = 0; row < 3; row++) semantic.AddHeatmapRow("Row " + (row + 1).ToString(CultureInfo.InvariantCulture), Enumerable.Range(row * 8, 8).Select(value => value * 100.0 / 23).ToArray());
                return semantic;
            case "categorical-text":
                // As report views draw them: up is quiet, and an outlined state stands for could not evaluate.
                var operational = tokens.Status.OperationalStateCategories().Select(state => state.Key switch {
                    "up" => new ChartStateCategory(state.Key, state.Label, state.Color, state.Pattern, ChartStateEmphasis.Quiet),
                    "unknown" => new ChartStateCategory(state.Key, state.Label, state.Color, ChartStatePattern.Outlined),
                    _ => state
                }).ToList();
                var keys = operational.Select(state => state.Key).ToArray();
                var matrix = Host(tokens, 760, 260).WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always).WithStateCategories(operational.ToArray()).WithXLabels(keys);
                for (var row = 0; row < 3; row++) matrix.AddHeatmapCategoryRow("Site " + (row + 1).ToString(CultureInfo.InvariantCulture), keys.Select((key, column) => (ChartHeatmapCell?)new ChartHeatmapCell(key, (row * 7 + column).ToString(CultureInfo.InvariantCulture))).ToArray());
                return matrix;
            case "hexbin-values":
                var hexbin = Host(tokens, 720, 320).WithDataLabels();
                for (var row = 0; row < 3; row++) hexbin.AddHexbinHeatmapRow("Row " + (row + 1).ToString(CultureInfo.InvariantCulture), Enumerable.Range(row * 6, 6).Select(value => (double)value).ToArray());
                hexbin.Options.HeatmapRelativeScale = true;
                return hexbin;
            case "gantt-lanes":
                var lanes = tokens.Status.OperationalStateCategories();
                var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
                return Host(tokens, 760, 420).WithStateCategories(lanes.ToArray())
                    .AddGanttLane("Changes", lanes.Take(4).Select((state, index) => new ChartGanttLaneItem(start.AddHours(index * 6), start.AddHours(index * 6 + 5), state.Key, state.Label)).ToArray())
                    .AddGanttLane("Patching", lanes.Skip(4).Select((state, index) => new ChartGanttLaneItem(start.AddHours(index * 8), start.AddHours(index * 8 + 7), state.Key, state.Label)).ToArray());
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
