using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

/// <summary>Guards the opt-in SVG hooks the enhance runtime reads (docs/dom-contract.md, docs/enhance.md).</summary>
public sealed class EnhanceHookTests {
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private readonly ITestOutputHelper _output;
    public EnhanceHookTests(ITestOutputHelper output) => _output = output;

    private static VisualRenderContext Context(double width = 640, double height = 320) => new(new VisualLayoutOptions(new VisualSize(width, height), 8));

    private static Chart Activity() {
        var day = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);
        ChartPoint Point(int index, double value, string kind) => new ChartPoint(index + 1, value).WithMarkOptions(new ChartMarkOptions(
            keys: Enumerable.Range(0, (int)value).Select(n => kind + "-" + index + "-" + n),
            fields: new Dictionary<string, object> { ["day"] = day.AddDays(index) }));
        var chart = Chart.Create().WithStackedBars().WithLegend(true).WithXLabels("Mon", "Tue", "Wed")
            .AddBar("Updates", new[] { 3d, 2, 4 }.Select((v, i) => Point(i, v, "u")))
            .AddBar("Notes", new[] { 1d, 2, 1 }.Select((v, i) => Point(i, v, "n")));
        chart.Series[0].WithMarkOptions(new ChartMarkOptions(fields: new Dictionary<string, object> { ["kind"] = "Updates" }));
        chart.Series[1].WithMarkOptions(new ChartMarkOptions(fields: new Dictionary<string, object> { ["kind"] = "Notes", ["count"] = 2, ["final"] = true }));
        return chart;
    }

    private static Chart Trend() {
        var start = new DateTime(2026, 10, 1);
        return Chart.Create().WithLegend(true).ConfigureXAxis(axis => axis.Scale = ChartScaleKind.Time)
            .AddLine("A", Enumerable.Range(0, 6).Select(i => new ChartPoint(start.AddDays(i).ToOADate(), i % 4)))
            .AddLine("B", Enumerable.Range(0, 6).Select(i => new ChartPoint(start.AddDays(i).ToOADate(), (i * 3) % 5)));
    }

    [Fact]
    public void WithoutHooks_OutputIsByteIdenticalAndScriptFree() {
        foreach (var chart in new[] { Activity(), Trend() }) {
            var prepared = chart.Prepare(Context());
            var plain = prepared.ToSvg(new VisualSvgOptions("plain"));
            Assert.Equal(plain, prepared.ToSvg(new VisualSvgOptions("plain").WithEnhanceHooks(false)));
            Assert.DoesNotContain(ChartDomRoles.EnhanceAttribute, plain, StringComparison.Ordinal);
            Assert.DoesNotContain(ChartDomRoles.MarkAttribute, plain, StringComparison.Ordinal);
            Assert.DoesNotContain("<metadata", plain, StringComparison.Ordinal);
            var enhanced = prepared.ToSvg(new VisualSvgOptions("plain").WithEnhanceHooks(includeData: true));
            Assert.DoesNotContain("<script", enhanced, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(XDocument.Parse(enhanced).Descendants().SelectMany(e => e.Attributes()), a => a.Name.LocalName.StartsWith("on", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Hooks_DescribeTheRootForCrosshairAndBrush() {
        var root = XDocument.Parse(Trend().Prepare(Context()).ToSvg(new VisualSvgOptions("trend").WithEnhanceHooks())).Root!;
        Assert.Equal("1", (string?)root.Attribute(ChartDomRoles.EnhanceAttribute));
        Assert.Equal("640 320", (string?)root.Attribute(ChartDomRoles.SizeAttribute));
        Assert.Equal(4, ((string)root.Attribute(ChartDomRoles.PlotAttribute)!).Split(' ').Length);
        Assert.Equal("time", (string?)root.Attribute(ChartDomRoles.XKindAttribute));
        var domain = ((string)root.Attribute(ChartDomRoles.XDomainAttribute)!).Split(' ').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        Assert.True(domain[0] <= new DateTime(2026, 10, 1).ToOADate() && domain[1] >= new DateTime(2026, 10, 6).ToOADate());
        Assert.Equal("true", (string?)root.Attribute(ChartDomRoles.CrosshairAttribute));
        Assert.Equal("true", (string?)root.Attribute(ChartDomRoles.BrushAttribute));

        var bars = XDocument.Parse(Activity().Prepare(Context()).ToSvg(new VisualSvgOptions("bars").WithEnhanceHooks())).Root!;
        Assert.Null(bars.Attribute(ChartDomRoles.CrosshairAttribute));
        Assert.Null(bars.Attribute(ChartDomRoles.BrushAttribute));
    }

    [Fact]
    public void Hooks_MarkTargetsWithRecordsAndLinkLegendsToSeries() {
        var svg = XDocument.Parse(Activity().Prepare(Context()).ToSvg(new VisualSvgOptions("activity").WithEnhanceHooks()));
        var marks = svg.Descendants().Where(e => (string?)e.Attribute(ChartDomRoles.MarkAttribute) == "true").ToArray();
        Assert.Equal(6, marks.Length);
        Assert.All(marks, mark => Assert.NotNull(mark.Attribute(ChartDomRoles.SeriesAttribute)));
        var first = marks.Single(m => (string?)m.Attribute(ChartDomRoles.SeriesAttribute) == "1" && (string?)m.Attribute(ChartDomRoles.PointAttribute) == "1");
        Assert.Equal(new[] { "n-1-0", "n-1-1" }, JsonSerializer.Deserialize<string[]>((string)first.Attribute(ChartDomRoles.KeysAttribute)!));
        using var fields = JsonDocument.Parse((string)first.Attribute(ChartDomRoles.FieldsAttribute)!);
        // Item fields extend the series fields, with typed JSON values and UTC instants.
        Assert.Equal("Notes", fields.RootElement.GetProperty("kind").GetString());
        Assert.Equal("2026-07-14T00:00:00.000Z", fields.RootElement.GetProperty("day").GetString());
        Assert.Equal(2, fields.RootElement.GetProperty("count").GetDouble());
        Assert.True(fields.RootElement.GetProperty("final").GetBoolean());
        var legends = svg.Descendants().Where(e => (string?)e.Attribute(ChartDomRoles.RoleAttribute) == ChartDomRoles.LegendEntry).ToArray();
        Assert.Equal(new[] { "0", "1" }, legends.Select(l => (string?)l.Attribute(ChartDomRoles.LegendForAttribute)).ToArray());
        Assert.All(legends, legend => Assert.Null(legend.Attribute(ChartDomRoles.MarkAttribute)));
    }

    [Fact]
    public void DataIsland_ListsSeriesAndXSortedPoints() {
        var svg = XDocument.Parse(Trend().Prepare(Context()).ToSvg(new VisualSvgOptions("trend").WithEnhanceHooks(includeData: true)));
        var island = svg.Root!.Element(Svg + "metadata");
        Assert.NotNull(island);
        Assert.Equal("true", (string?)island!.Attribute(ChartDomRoles.EnhanceDataAttribute));
        using var json = JsonDocument.Parse(island.Value);
        var series = json.RootElement.GetProperty("series").EnumerateArray().ToArray();
        Assert.Equal(new[] { "A", "B" }, series.Select(s => s.GetProperty("name").GetString()).ToArray());
        Assert.All(series, s => Assert.False(string.IsNullOrEmpty(s.GetProperty("color").GetString())));
        var xs = json.RootElement.GetProperty("points").EnumerateArray().Select(p => p.GetProperty("x").GetDouble()).ToArray();
        Assert.Equal(12, xs.Length);
        Assert.Equal(xs.OrderBy(x => x).ToArray(), xs);
        Assert.All(json.RootElement.GetProperty("points").EnumerateArray(), p => Assert.EndsWith("Z", p.GetProperty("xi").GetString(), StringComparison.Ordinal));
    }

    [Fact]
    public void MarkOptions_ValidateRecordKeysAndFields() {
        Assert.Throws<ArgumentException>(() => new ChartMarkOptions(keys: new[] { "" }));
        Assert.Throws<ArgumentException>(() => new ChartMarkOptions(keys: new[] { "a\nb" }));
        Assert.Throws<ArgumentException>(() => new ChartMarkOptions(fields: new Dictionary<string, object> { ["day"] = new DateTime(2026, 1, 1) }));
        Assert.Throws<ArgumentException>(() => new ChartMarkOptions(fields: new Dictionary<string, object> { ["v"] = double.NaN }));
        Assert.Throws<ArgumentException>(() => new ChartMarkOptions(fields: new Dictionary<string, object> { ["v"] = new object() }));
        var options = new ChartMarkOptions(keys: new[] { "b", "a", "b" }, fields: new Dictionary<string, object> {
            ["when"] = new DateTimeOffset(2026, 7, 14, 2, 0, 0, TimeSpan.FromHours(2)), ["n"] = 5L
        });
        Assert.Equal(new[] { "b", "a" }, options.Keys);
        Assert.Equal("2026-07-14T00:00:00.000Z", options.Fields["when"]);
        Assert.Equal(5d, options.Fields["n"]);
    }

    [Fact]
    public void ContractAttributes_AreAllEmittedWithHooks() {
        var charts = new IVisualRenderable[] { Activity(), Trend(), ExampleRecipes.ForKind(ChartSeriesKind.HorizontalBar).Create("wide", VisualThemeMode.Light),
            ExampleRecipes.ForKind(ChartSeriesKind.Gantt).Create("wide", VisualThemeMode.Light), ExampleRecipes.ForKind(ChartSeriesKind.StateTimeline).Create("wide", VisualThemeMode.Light) };
        var emitted = charts.Select((chart, i) => chart.Prepare(Context()).ToSvg(new VisualSvgOptions("c" + i).WithEnhanceHooks(includeData: true)))
            .SelectMany(svg => XDocument.Parse(svg).Descendants()).SelectMany(e => e.Attributes()).Select(a => a.Name.LocalName).ToHashSet(StringComparer.Ordinal);
        var attributes = typeof(ChartDomRoles).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.Name.EndsWith("Attribute", StringComparison.Ordinal)).Select(field => (string)field.GetRawConstantValue()!).ToArray();
        Assert.All(attributes, attribute => Assert.Contains(attribute, emitted));
    }

    /// <summary>Coverage matrix, part 1: every generated chart family exposes interaction targets through the hooks.</summary>
    [Fact]
    public void EveryChartFamily_ExposesMarksThroughTheHooks() {
        var rows = new List<string>();
        foreach (var family in ExampleRecipes.ChartFamilies) {
            var visual = family.Value.Create("wide", VisualThemeMode.Light).Prepare(Context(720, 420));
            var svg = XDocument.Parse(visual.ToSvg(new VisualSvgOptions("f-" + family.Key.ToString().ToLowerInvariant()).WithEnhanceHooks(includeData: true)));
            var marks = svg.Descendants().Where(e => (string?)e.Attribute(ChartDomRoles.MarkAttribute) == "true").ToArray();
            Assert.True(marks.Length > 0, family.Key + " exposes no interaction targets.");
            Assert.All(marks, mark => {
                Assert.NotNull(mark.Attribute(ChartDomRoles.SeriesAttribute));
                // Targets are innermost: no mark contains another.
                Assert.DoesNotContain(mark.Descendants(), inner => inner.Attribute(ChartDomRoles.MarkAttribute) != null);
            });
            var roles = string.Join("|", marks.Select(m => (string?)m.Attribute(ChartDomRoles.RoleAttribute)).Distinct());
            var legends = svg.Descendants().Count(e => e.Attribute(ChartDomRoles.LegendForAttribute) != null);
            rows.Add(family.Key + ": " + marks.Length + " marks (" + roles + "), " + legends + " linked legend entries"
                + (svg.Root!.Attribute(ChartDomRoles.CrosshairAttribute) != null ? ", crosshair" : string.Empty));
        }
        foreach (var row in rows) _output.WriteLine(row);
    }
}
