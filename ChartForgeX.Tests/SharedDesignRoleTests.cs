using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

/// <summary>
/// The shared design roles (<c>--hfx-*</c>) reach every painting family, the categorical order is the validated one,
/// and no renderer writes a colour outside the token set except the derived literals allowed below.
/// </summary>
public sealed class SharedDesignRoleTests {
    private readonly ITestOutputHelper _output;
    public SharedDesignRoleTests(ITestOutputHelper output) => _output = output;

    private static readonly string[] LightOrder = { "#2A78D6", "#0F9F8C", "#7B61E8", "#A0522D", "#C24FA8", "#5B7F00" };
    private static readonly string[] DarkOrder = { "#3987E5", "#179C89", "#8A74EE", "#C06A42", "#C9579F", "#6F9120" };

    [Fact]
    public void DefaultCategoricalOrder_IsTheValidatedOrderInBothModes_AndHoldsNoStatusColour() {
        Assert.Equal(LightOrder, ChartPalettes.Categorical.Select(color => color.ToHex()));
        Assert.Equal(DarkOrder, ChartPalettes.CategoricalDark.Select(color => color.ToHex()));
        Assert.Equal(LightOrder, VisualTheme.Graphite().Resolve(VisualThemeMode.Light).Palette.Select(color => color.ToHex()));
        Assert.Equal(DarkOrder, VisualTheme.Graphite().Resolve(VisualThemeMode.Dark).Palette.Select(color => color.ToHex()));
        Assert.Equal(LightOrder, new VisualDesignTokens().Palette.Select(color => color.ToHex()));
        Assert.Equal(DarkOrder, VisualDesignTokens.Dark().Palette.Select(color => color.ToHex()));
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var colors = VisualTheme.Graphite().Resolve(mode);
            var status = colors.Status;
            var reserved = new[] { status.Critical.Fill, status.High.Fill, status.Medium.Fill, status.Low.Fill, status.Info.Fill, status.Pass.Fill, status.Neutral.Fill, status.Maintenance.Fill };
            Assert.All(colors.Palette, color => Assert.DoesNotContain(color, reserved));
            Assert.Equal(colors.Palette.Count, colors.Palette.Distinct().Count());
        }
    }

    [Theory]
    [InlineData(VisualThemeMode.Light, "#ABAEB2")]
    [InlineData(VisualThemeMode.Dark, "#595B61")]
    public void OtherSlice_IsNeutralGrayAndLast_EvenWithSourceOrder(VisualThemeMode mode, string gray) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        Assert.Equal(gray, colors.Other.ToHex());
        Assert.DoesNotContain(colors.Other, colors.Palette);
        foreach (var preserve in new[] { false, true }) {
            var chart = Chart.Create().WithThemeMode(mode).AddPie("Share", Enumerable.Range(0, 9).Select(i => new ChartPoint(i, 9 - i)));
            chart.Options.PiePreserveSourceOrder = preserve;
            var prepared = chart.Prepare(new VisualRenderContext(theme: chart.Options.Theme, themeMode: mode));
            var legend = prepared.Regions.Where(region => region.Role == "legend").Select(region => region.Label).ToArray();
            Assert.Equal(chart.Options.MaximumPieSlices, legend.Length);
            Assert.StartsWith("Other", legend[legend.Length - 1]);
            var svg = chart.WithSvgColorVariables(SvgColorVariables.SharedRoles(mode: mode)).ToSvg();
            var fill = XDocument.Parse(svg).Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "pie-slice"
                && (string?)e.Attribute("data-cfx-source-id") == "series-0-point-other").Attribute("fill")!.Value;
            Assert.Contains("var(--hfx-color-text-secondary,", fill, StringComparison.Ordinal);
            Assert.Contains("45%", fill, StringComparison.Ordinal);
            Assert.DoesNotContain(prepared.Diagnostics, d => d.Code == "palette.reused");
        }
    }

    [Fact]
    public void SeventhAutomaticSeries_ReportsPaletteReuse_InsteadOfAGeneratedHue() {
        var chart = Chart.Create();
        for (var i = 0; i < 7; i++) chart.AddLine("S" + i, new[] { new ChartPoint(0, i), new ChartPoint(1, i + 1) });
        var prepared = chart.Prepare(new VisualRenderContext());
        var reuse = Assert.Single(prepared.Diagnostics, d => d.Code == "palette.reused");
        Assert.Equal(ChartForgeX.Diagnostics.VisualDiagnosticSeverity.Information, reuse.Severity);
        var six = Chart.Create();
        for (var i = 0; i < 6; i++) six.AddLine("S" + i, new[] { new ChartPoint(0, i), new ChartPoint(1, i + 1) });
        Assert.DoesNotContain(six.Prepare(new VisualRenderContext()).Diagnostics, d => d.Code == "palette.reused");
    }

    [Theory]
    [InlineData("series.1", "--hfx-color-series-1")]
    [InlineData("surface.card", "--hfx-color-surface-card")]
    [InlineData("surface.cardAlt", "--hfx-color-surface-card-alt")]
    [InlineData("surface.line", "--hfx-color-surface-line")]
    [InlineData("text.primary", "--hfx-color-text-primary")]
    [InlineData("text.secondary", "--hfx-color-text-secondary")]
    [InlineData("accent.base", "--hfx-color-accent-base")]
    [InlineData("severity.critical.fill", "--hfx-color-severity-critical-fill")]
    [InlineData("ramps.sequential.3", "--hfx-ramp-sequential-3")]
    [InlineData("ramps.diverging.negative.1", "--hfx-ramp-diverging-negative-1")]
    public void SharedRoleName_FollowsTheHfxRoles(string path, string expected) => Assert.Equal(expected, SvgColorVariables.SharedRoleName(path));

    [Fact]
    public void SharedRoles_WriteHfxPropertiesWithTheThemeAsFallback_AndFontsInheritThePage() {
        var chart = Chart.Create().AddBar("A", Points(3, 5)).AddBar("B", Points(4, 2)).WithTitle("Roles");
        var svg = chart.Prepare(new VisualRenderContext()).ToSvg(new VisualSvgOptions("roles").WithSharedDesignRoles());
        Assert.Contains("var(--hfx-color-series-1, #2A78D6)", svg, StringComparison.Ordinal);
        Assert.Contains("var(--hfx-color-series-2, #0F9F8C)", svg, StringComparison.Ordinal);
        Assert.Contains("var(--hfx-color-text-primary, ", svg, StringComparison.Ordinal);
        Assert.Contains("font-family=\"var(--hfx-font-sans, inherit)\"", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("font-size=\"inherit\"", svg, StringComparison.Ordinal);
        Assert.Contains("font-size=", svg, StringComparison.Ordinal);
        // A page that defines nothing: PNG and the literal SVG are unchanged by the opt-in.
        Assert.Equal(chart.ToPng(), chart.WithSvgColorVariables(SvgColorVariables.SharedRoles()).ToPng());
    }

    [Fact]
    public void EqualHexInTwoSlots_KeepsEachSlotsProperty_AndASeriesEqualToARampStepKeepsItsSeriesRole() {
        var twin = ChartColor.FromHex("#336699");
        var tokens = VisualDesignTokens.GraphiteLight();
        tokens.Palette = new[] { twin, ChartColor.FromHex("#0F9F8C"), twin };
        var theme = new VisualTheme(tokens, tokens);
        var chart = Chart.Create().WithTheme(theme).AddBar("One", Points(1, 2)).AddBar("Two", Points(2, 3)).AddBar("Three", Points(3, 4))
            .WithSvgColorVariables(SvgColorVariables.SharedRoles(theme));
        // Bars and legend swatches draw series in order; by colour alone the third would name slot 1.
        var fills = XDocument.Parse(chart.ToSvg()).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") is "bar" or "legend-swatch")
            .Select(e => (string)e.Attribute("fill")!).ToArray();
        Assert.Contains("var(--hfx-color-series-1, #336699)", fills);
        Assert.Contains("var(--hfx-color-series-2, #0F9F8C)", fills);
        Assert.Contains("var(--hfx-color-series-3, #336699)", fills);
        Assert.Equal(fills.Count(fill => fill.Contains("series-1,", StringComparison.Ordinal)), fills.Count(fill => fill.Contains("series-3,", StringComparison.Ordinal)));
        // Graphite light series 1 equals sequential step 3: the bar still names the series role, the ramp keeps its own.
        var graphite = Chart.Create().AddBar("A", Points(1, 2)).WithSvgColorVariables(SvgColorVariables.SharedRoles()).ToSvg();
        Assert.Contains("var(--hfx-color-series-1, #2A78D6)", graphite, StringComparison.Ordinal);
        Assert.DoesNotContain("--hfx-ramp-sequential-3", graphite, StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> Families() => FamilyNames.Select(name => new object[] { name });

    private static readonly string[] FamilyNames = {
        "line", "area", "stacked-area", "bars", "stacked-bars", "scatter", "waterfall", "box-plot", "candlestick",
        "pie", "donut", "polar-area", "radar", "radial-bar", "gauge", "progress-ring", "bullet", "progress-bar",
        "treemap", "sunburst", "tree", "sankey", "chord", "funnel", "pyramid", "word-cloud",
        "heatmap", "calendar", "state-timeline", "gantt", "timeline", "topology", "sparkline", "venn", "git-graph"
    };

    /// <summary>
    /// Every paint is a shared role (or a <c>color-mix()</c>/relative colour of roles) or one of the derived literals
    /// below. A new family, or a renderer that hard-codes a colour, fails here until it reads a token or the exception
    /// is listed with its reason.
    /// </summary>
    [Theory]
    [MemberData(nameof(Families))]
    public void EveryPaint_IsASharedRoleOrAnAllowedDerivedLiteral(string family) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var svg = Render(family, mode);
            Assert.Contains("var(--hfx-", svg, StringComparison.Ordinal);
            var leaks = Literals(svg).Where(leak => !AllowedLiteral(leak.Value)).ToArray();
            foreach (var leak in leaks.Distinct()) _output.WriteLine($"{family} {mode}: {leak.Role} {leak.Attribute}={leak.Value}");
            Assert.True(leaks.Length == 0, family + " " + mode + " writes literal colours outside the token set: "
                + string.Join("; ", leaks.Distinct().Take(8).Select(leak => leak.Role + " " + leak.Attribute + "=" + leak.Value)));
        }
    }

    /// <summary>Renderer-derived colours that must stay literal (they are not theme roles): white sheens and black shades.</summary>
    private static bool AllowedLiteral(string value) {
        var color = PreparedSvgTestExtensions.ParseRenderedColor(value);
        return color.R == color.G && color.G == color.B && color.R is 0 or 255;
    }

    private static string Render(string family, VisualThemeMode mode) {
        var theme = VisualTheme.Graphite();
        var variables = SvgColorVariables.SharedRoles(theme, mode);
        if (family == "topology")
            return TopologyChart.Create().WithId("roles").WithViewport(640, 360).WithTheme(theme, mode)
                .AddAutoGroup("waw", "Warsaw", TopologyHealthStatus.Healthy)
                .AddAutoNode("dc1", "DC01", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, groupId: "waw")
                .AddAutoNode("dc2", "DC02", TopologyNodeKind.Server, TopologyHealthStatus.Warning, groupId: "waw")
                .AddAutoNode("dc3", "DC03", TopologyNodeKind.Server, TopologyHealthStatus.Critical)
                .AddEdge("a", "dc1", "dc2").AddEdge("b", "dc2", "dc3")
                .ToSvg(new TopologyRenderOptions { IdScope = "roles", SvgColorVariables = variables });
        if (family == "sparkline")
            return new Sparkline(SparklineData.FromValues(new[] { 1d, 3, 2, 5, 4 })).Prepare(new VisualRenderContext(theme: theme, themeMode: mode))
                .ToSvg(new VisualSvgOptions("roles").WithSharedDesignRoles(theme, mode));
        if (family == "venn")
            return ChartForgeX.VisualBlocks.VennDiagramBlock.Create().WithTheme(theme, mode).AddSet("A", "Alpha", 60).AddSet("B", "Beta", 50)
                .AddIntersection(new[] { "A", "B" }, "Both", 10).WithSvgColorVariables(variables).ToSvg();
        if (family == "git-graph")
            return ChartForgeX.VisualBlocks.GitGraphBlock.Create().WithTheme(theme, mode).AddBranch("main").AddCommit("base", "main", tag: "v1")
                .WithSvgColorVariables(variables).ToSvg();
        return Build(family, mode).WithTheme(theme).WithThemeMode(mode).WithSvgColorVariables(variables).ToSvg();
    }

    private static Chart Build(string family, VisualThemeMode mode) {
        var start = new DateTime(2026, 9, 1);
        var chart = Chart.Create().WithSize(640, 360).WithTitle("Roles");
        switch (family) {
            case "line": return chart.AddLine("A", Points(1, 3, 2)).AddLine("B", Points(2, 1, 3));
            case "area": return chart.AddArea("A", Points(1, 3, 2)).AddArea("B", Points(2, 1, 3));
            case "stacked-area": return chart.AddStackedArea("A", Points(1, 3, 2)).AddStackedArea("B", Points(2, 1, 3));
            case "bars": return chart.AddBar("A", Points(3, 5)).AddBar("B", Points(4, 2));
            case "stacked-bars": return chart.AddBar("A", Points(3, 5)).AddBar("B", Points(4, 2)).AddBar("C", Points(1, 2)).WithStackedBars();
            case "scatter": return chart.AddScatter("A", Points(1, 3, 2)).AddScatter("B", Points(2, 1, 3));
            case "waterfall": return chart.AddWaterfall("Bridge", Points(10, -3, 5, -2));
            case "box-plot": return chart.AddBoxPlot("Latency", 1, new[] { 1d, 2, 3, 4, 5, 9 });
            case "candlestick": return chart.AddCandlestick("Price", new[] { new ChartCandlestick(1, 10, 12, 9, 11), new ChartCandlestick(2, 11, 13, 8, 9) });
            case "pie": return chart.AddPie("Share", Points(5, 3, 2, 1, 1, 1, 1, 1));
            case "donut": return chart.AddDonut("Share", Points(5, 3, 2));
            case "polar-area": return chart.AddPolarArea("Share", Points(5, 3, 2, 4));
            case "radar": return chart.AddRadar("A", Points(3, 4, 2, 5)).AddRadar("B", Points(2, 3, 4, 1));
            case "radial-bar": return chart.AddRadialBar("A", Points(3, 4, 2));
            case "gauge": return chart.AddGauge("Readiness", 74);
            case "progress-ring": return chart.AddProgressRing("Done", Points(70, 40));
            case "bullet": return chart.AddBullet("Coverage", 74, 90).AddBullet("TLS", 92, 80);
            case "progress-bar": return chart.AddProgressBars("Done", new[] { new ChartProgressItem("Build", 64), new ChartProgressItem("Ship", 30) });
            case "treemap": return chart.AddTreemap("Files", new[] { new ChartHierarchyItem("One", "One", value: 50), new ChartHierarchyItem("Two", "Two", value: 30), new ChartHierarchyItem("Three", "Three", value: 20) });
            case "sunburst": return chart.AddSunburst("Files", new[] { new ChartHierarchyItem("root", "All"), new ChartHierarchyItem("One", "One", "root", 50), new ChartHierarchyItem("Two", "Two", "root", 30), new ChartHierarchyItem("Three", "Three", "root", 20) });
            case "tree": return chart.AddTree("Org", new[] { new ChartNode("a", "A"), new ChartNode("b", "B"), new ChartNode("c", "C") }, new[] { new ChartTreeLink("a", "b"), new ChartTreeLink("a", "c") });
            case "sankey": return chart.AddSankey("Flow", new[] { new ChartNode("a", "A"), new ChartNode("b", "B"), new ChartNode("c", "C") }, new[] { new ChartFlowLink("l1", "a", "b", 5), new ChartFlowLink("l2", "c", "b", 2) });
            case "chord": return chart.AddChord("Flow", new[] { new ChartNode("a", "A"), new ChartNode("b", "B"), new ChartNode("c", "C") }, new[] { new ChartFlowLink("l1", "a", "b", 5), new ChartFlowLink("l2", "c", "b", 2) });
            case "funnel": return chart.WithXLabels("A", "B", "C").AddFunnel("Steps", Points(100, 70, 40));
            case "pyramid": return chart.WithXLabels("A", "B", "C").AddPyramid("Steps", Points(100, 70, 40));
            case "word-cloud": return chart.AddWordCloud("Words", new[] { new ChartWordCloudItem("charts", 9), new ChartWordCloudItem("tokens", 5), new ChartWordCloudItem("roles", 3) });
            case "heatmap": return chart.WithXLabels("A", "B", "C").AddHeatmapRow("R1", new[] { 0d, 3, 7 }).AddHeatmapRow("R2", new[] { 5d, 1, 2 });
            case "calendar": return chart.AddCalendarHeatmap("Days", Enumerable.Range(0, 40).Select(i => new ChartCalendarHeatmapItem(start.AddDays(i), i % 5)).ToArray());
            case "state-timeline":
                var states = VisualTheme.Graphite().Resolve(mode).Status.OperationalStateCategories().ToArray();
                return chart.WithStateCategories(states).AddStateTimelineLane("DC01", new[] {
                    new ChartStateTimelineSegment(start, start.AddHours(6), "up"), new ChartStateTimelineSegment(start.AddHours(6), start.AddHours(8), "down") });
            case "gantt": return chart.AddGanttTask("Build", start, start.AddDays(3), 0.5).AddGanttTask("Ship", start.AddDays(3), start.AddDays(5));
            case "timeline": return chart.AddTimelineItem("Preview", start, start.AddDays(10)).AddTimelineItem("GA", start.AddDays(10), start.AddDays(30));
            default: throw new ArgumentOutOfRangeException(nameof(family));
        }
    }

    private static readonly Regex Paint = new(@"#[0-9A-Fa-f]{3,8}\b|rgba?\((?!from)[^)]*\)", RegexOptions.CultureInvariant);
    private static readonly Regex Declaration = new(@"(?<![\w-])(fill|stroke|stop-color|flood-color|lighting-color|color)\s*:\s*(?<value>[^;}""]+)", RegexOptions.CultureInvariant);
    private static readonly HashSet<string> PaintAttributes = new(StringComparer.Ordinal) { "fill", "stroke", "stop-color", "flood-color", "lighting-color", "color" };

    private sealed record Leak(string Role, string Attribute, string Value);

    /// <summary>Literal colours outside a <c>var(--hfx-*)</c> reference, with the role of the nearest marked ancestor.</summary>
    private static IEnumerable<Leak> Literals(string svg) {
        var document = XDocument.Parse(svg);
        foreach (var element in document.Descendants()) {
            var role = element.AncestorsAndSelf().Select(e => (string?)e.Attribute("data-cfx-role")).FirstOrDefault(r => r != null) ?? element.Name.LocalName;
            foreach (var attribute in element.Attributes()) {
                var name = attribute.Name.LocalName;
                if (PaintAttributes.Contains(name)) foreach (var value in Outside(attribute.Value)) yield return new Leak(role, name, value);
                if (name == "style") foreach (Match declaration in Declaration.Matches(attribute.Value))
                    foreach (var value in Outside(declaration.Groups["value"].Value)) yield return new Leak(role, "style:" + declaration.Groups[1].Value, value);
            }
            if (element.Name.LocalName == "style") foreach (Match declaration in Declaration.Matches(element.Value))
                foreach (var value in Outside(declaration.Groups["value"].Value)) yield return new Leak("<style>", declaration.Groups[1].Value, value);
        }
    }

    /// <summary>The literal colours of a paint that are not fallbacks of a role property.</summary>
    private static IEnumerable<string> Outside(string paint) {
        var stripped = Regex.Replace(paint, @"var\(--hfx-[A-Za-z0-9_-]+,\s*(?:#[0-9A-Fa-f]{3,8}|rgba?\([^)]*\))\)", "var()");
        return Paint.Matches(stripped).Cast<Match>().Select(match => match.Value);
    }

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
}
