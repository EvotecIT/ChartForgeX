using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>SVG output can write token colours as CSS custom properties with the literal colour as fallback.</summary>
public sealed class SvgColorVariablesTests {
    private static readonly VisualDesignTokens Graphite = VisualDesignTokens.FromJsonFile(FixturePath("tokens", "palette-v1-graphite.json"));

    [Fact]
    public void Chart_WritesTokenColoursAsVariablesAndFallsBackToTheSameLiterals() {
        var literal = Lines().ToSvg();
        var themed = Lines().WithSvgColorVariables(Graphite.ToSvgColorVariables()).ToSvg();

        Assert.DoesNotContain("var(--cfx-series", literal, StringComparison.Ordinal);
        Assert.Contains("stroke=\"var(--cfx-series-1, #2A78D6)\"", themed, StringComparison.Ordinal);
        Assert.Contains("fill=\"var(--cfx-text-primary, #16181C)\"", themed, StringComparison.Ordinal);
        Assert.Contains("stop-color=\"var(--cfx-surface-card, #FFFFFF)\"", themed, StringComparison.Ordinal);
        Assert.Contains("color-mix(in srgb, var(--cfx-surface-line, #E2E4E7) 54.9%, transparent)", themed, StringComparison.Ordinal);
        Assert.Contains("fill=\"var(--cfx-text-secondary, #4D525B)\"", themed, StringComparison.Ordinal);
        Assert.Equal(literal, Strip(themed));
        Assert.Equal(Lines().ToPng(), Lines().WithSvgColorVariables(Graphite.ToSvgColorVariables()).ToPng());
    }

    [Fact]
    public void NameFunction_NamesOrSkipsEachTokenPath() {
        string? Host(string path) => path.StartsWith("ramps.", StringComparison.Ordinal) ? "--host-ramp-" + path.Substring(6).Replace('.', '-')
            : path == "surface.card" ? null
            : "--host-color-" + path.Replace('.', '-');

        var variables = Graphite.ToSvgColorVariables(Host);
        var names = variables.Variables.Select(variable => variable.Name).ToArray();
        Assert.Contains("--host-color-series-1", names);
        Assert.Contains("--host-color-severity-high-fill", names);
        Assert.Contains("--host-ramp-sequential-5", names);
        Assert.Contains("--host-ramp-diverging-neutral", names);
        Assert.DoesNotContain("--host-color-surface-card", names);
        Assert.Contains("--host-color-surface-cardAlt", names);

        var svg = Lines().WithSvgColorVariables(variables).ToSvg();
        Assert.Contains("var(--host-color-series-1, #2A78D6)", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("--host-color-surface-card,", svg, StringComparison.Ordinal);
        Assert.Contains("--cfx-surface-card-alt", Graphite.ToSvgColorVariables().Variables.Select(variable => variable.Name));
    }

    [Fact]
    public void SharedColour_TakesTheVariableAddedFirst() {
        // Graphite's first series colour is also the middle sequential step and the middle positive diverging step.
        var svg = new SvgColorVariables().Add("--series-1", ChartColor.FromHex("#2A78D6")).Add("--ramp-3", ChartColor.FromHex("#2a78d6"))
            .Apply("<rect fill=\"#2A78D6\"/>");
        Assert.Equal("<rect fill=\"var(--series-1, #2A78D6)\"/>", svg);
    }

    [Fact]
    public void Apply_ChangesOnlyPaintValues() {
        var variables = new SvgColorVariables().Add("--ink", ChartColor.FromHex("#112233"));
        const string svg = "<svg><style>.a{fill:#112233;stroke:#112233 !important}</style>"
            + "<rect fill=\"#112233\" stroke=\"rgba(17,34,51,0.5)\" stop-color=\"#112233\" data-cfx-color=\"#112233\" data-fill=\"#112233\" style=\"fill:#112233\"/>"
            + "<text fill=\"#445566\">fill:#112233; #112233</text><rect fill=\"rgba(17,34,51,0)\" flood-color=\"#1122334\"/></svg>";
        var result = variables.Apply(svg);

        Assert.Equal("<svg><style>.a{fill:var(--ink, #112233);stroke:var(--ink, #112233) !important}</style>"
            + "<rect fill=\"var(--ink, #112233)\" stroke=\"color-mix(in srgb, var(--ink, #112233) 50%, transparent)\" stop-color=\"var(--ink, #112233)\" data-cfx-color=\"#112233\" data-fill=\"#112233\" style=\"fill:var(--ink, #112233)\"/>"
            + "<text fill=\"#445566\">fill:#112233; #112233</text><rect fill=\"rgba(17,34,51,0)\" flood-color=\"#1122334\"/></svg>", result);
        Assert.Equal(result, variables.Apply(result));
    }

    [Fact]
    public void SurfaceVariables_AreNotUsedForTextFills() {
        var variables = new SvgColorVariables().Add("--card", ChartColor.White, appliesToText: false).Add("--ink", ChartColor.FromHex("#112233"));
        var result = variables.Apply("<rect fill=\"#FFFFFF\"/><text fill=\"#FFFFFF\" stroke=\"#FFFFFF\">a</text><tspan style=\"fill:#FFFFFF;stroke:#FFFFFF\"/><text fill=\"#112233\">b</text>");
        Assert.Equal("<rect fill=\"var(--card, #FFFFFF)\"/><text fill=\"#FFFFFF\" stroke=\"var(--card, #FFFFFF)\">a</text><tspan style=\"fill:#FFFFFF;stroke:var(--card, #FFFFFF)\"/><text fill=\"var(--ink, #112233)\">b</text>", result);

        // A tile code on a dark tile is white contrast text; with Graphite the card is white too, and the code stays literal.
        var bars = Chart.Create().WithSize(640, 360).WithDesignTokens(Graphite)
            .AddTileMap("Sites", ChartTileMapCatalog.Get("us-states"), new[] { new ChartRegionMapItem("CA", 10), new ChartRegionMapItem("NY", 0) })
            .WithMapColorScale(ChartMapColorScale.Sequential(ChartColor.FromHex("#EEF2F8"), ChartColor.FromHex("#1D4F9E")));
        var svg = bars.WithSvgColorVariables(Graphite.ToSvgColorVariables()).ToSvg();
        Assert.Matches("<text[^>]*fill=\"#FFFFFF\"", svg);
        Assert.DoesNotMatch("<text[^>]*fill=\"var\\(--cfx-surface", svg);
        Assert.Contains("var(--cfx-surface-card, #FFFFFF)", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void TranslucentVariable_ScalesOnlyTheRemainingAlpha() {
        var variables = new SvgColorVariables().Add("--veil", ChartColor.FromRgba(17, 34, 51, 128));
        Assert.Equal("<rect fill=\"var(--veil, #11223380)\"/>", variables.Apply("<rect fill=\"rgba(17,34,51,0.502)\"/>"));
        Assert.Equal("<rect fill=\"color-mix(in srgb, var(--veil, #11223380) 49.8%, transparent)\"/>", variables.Apply("<rect fill=\"rgba(17,34,51,0.25)\"/>"));
        Assert.Equal("<rect fill=\"#112233\"/>", variables.Apply("<rect fill=\"#112233\"/>"));
        Assert.Equal("<rect fill=\"var(--ink, #112233)\"/>", new SvgColorVariables().Add("--ink", ChartColor.FromHex("#112233")).Apply("<rect fill=\"rgb(17, 34, 51)\"/>"));
    }

    [Fact]
    public void GridAndPreparedTopology_ApplyTheirVariables() {
        var grid = new ChartGrid().Add(Lines()).Add(Lines()).WithTitle("Two").WithSvgColorVariables(Graphite.ToSvgColorVariables());
        var svg = grid.ToSvg();
        Assert.Contains("var(--cfx-series-1, #2A78D6)", svg, StringComparison.Ordinal);
        Assert.Equal(grid.ToSvg(), Graphite.ToSvgColorVariables().Apply(svg));

        var chart = TopologyChart.Create().WithId("sites").AddAutoNode("dc1", "DC1");
        chart.Theme = Graphite.ApplyTo(TopologyTheme.Light());
        var prepared = chart.Prepare(new TopologyRenderOptions { SvgColorVariables = Graphite.ToSvgColorVariables() }).ToSvg();
        Assert.Contains("var(--cfx-surface-card, #FFFFFF)", prepared, StringComparison.Ordinal);
    }

    [Fact]
    public void Names_AreValidatedAndCopiesAreIndependent() {
        Assert.Throws<ArgumentException>(() => new SvgColorVariables().Add("ink", ChartColor.White));
        Assert.Throws<ArgumentException>(() => new SvgColorVariables().Add("--ink color", ChartColor.White));
        Assert.Throws<ArgumentNullException>(() => new SvgColorVariables().Apply(null!));
        var original = new SvgColorVariables().Add("--ink", ChartColor.White);
        var copy = original.Clone().Add("--other", ChartColor.Black);
        Assert.Single(original.Variables);
        Assert.Equal(2, copy.Variables.Count);
    }

    [Fact]
    public void Topology_WritesTokenColoursAsVariablesAndKeepsPngLiteral() {
        TopologyChart Map() {
            var chart = TopologyChart.Create().WithId("sites").AddAutoNode("dc1", "DC1").AddAutoNode("dc2", "DC2").AddEdge("a", "dc1", "dc2");
            chart.Theme = Graphite.ApplyTo(TopologyTheme.Light());
            return chart;
        }

        var options = new TopologyRenderOptions { SvgColorVariables = Graphite.ToSvgColorVariables() };
        var svg = Map().ToSvg(options);
        Assert.Contains("var(--cfx-surface-card, #FFFFFF)", svg, StringComparison.Ordinal);
        Assert.Contains("var(--cfx-text-primary, #16181C)", svg, StringComparison.Ordinal);
        Assert.Equal(Map().ToSvg(), Strip(svg));
        Assert.Equal(Map().ToPng(), Map().ToPng(options));
    }

    private static Chart Lines() => Chart.Create().WithTitle("Traffic").WithSize(640, 320).WithDesignTokens(Graphite)
        .AddLine("Inbound", new[] { new ChartPoint(0, 1), new ChartPoint(1, 3), new ChartPoint(2, 2) })
        .AddLine("Outbound", new[] { new ChartPoint(0, 2), new ChartPoint(1, 1), new ChartPoint(2, 3) });

    // Undoes the variables: var(--x, #hex) becomes #hex and color-mix(... N%, transparent) becomes rgba(r,g,b,N/100).
    private static string Strip(string svg) {
        var plain = Regex.Replace(svg, @"color-mix\(in srgb, var\(--[\w-]+, #([0-9A-Fa-f]{6})\) ([0-9.]+)%, transparent\)", match => {
            var hex = match.Groups[1].Value;
            var alpha = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) / 100;
            return string.Format(CultureInfo.InvariantCulture, "rgba({0},{1},{2},{3:0.###})", Convert.ToInt32(hex.Substring(0, 2), 16), Convert.ToInt32(hex.Substring(2, 2), 16), Convert.ToInt32(hex.Substring(4, 2), 16), alpha);
        });
        return Regex.Replace(plain, @"var\(--[\w-]+, (#[0-9A-Fa-f]{6})\)", "$1");
    }

    private static string FixturePath(params string[] parts) {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ChartForgeX.sln"))) directory = directory.Parent;
        if (directory == null) throw new InvalidOperationException("Repository root was not found.");
        return Path.Combine(new[] { directory.FullName, "ChartForgeX.Tests", "Fixtures" }.Concat(parts).ToArray());
    }
}
