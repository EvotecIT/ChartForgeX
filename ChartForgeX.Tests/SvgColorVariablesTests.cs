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
        Assert.Contains("fill=\"var(--cfx-surface-card, #FFFFFF)\"", themed, StringComparison.Ordinal);
        Assert.Contains("color-mix(in srgb, var(--cfx-surface-line, #E2E4E7)", themed, StringComparison.Ordinal);
        Assert.Contains("fill=\"var(--cfx-text-secondary, #4D525B)\"", themed, StringComparison.Ordinal);
        Assert.Contains("stroke=\"" + Lines().Options.Theme.Grid.ToCss() + "\"", literal, StringComparison.Ordinal);
        Assert.Equal(NormalizeIdentity(literal), NormalizeIdentity(Strip(themed)));
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
    public void TypedPaints_ResolveByRole_KeepDerivedLiterals_AndMixTokenColours() {
        var blue = ChartColor.FromHex("#2A78D6");
        var variables = new SvgColorVariables().Add("--series-1", blue, SvgColorRole.Series).Add("--ramp-3", blue, SvgColorRole.Ramp)
            .Add("--card", ChartColor.White, SvgColorRole.Surface);
        // A ramp step takes the ramp property, a paint without a role the one added first.
        Assert.Equal("var(--ramp-3, #2A78D6)", SvgPaint.Resolve(SvgPaint.Of(blue, SvgColorRole.Ramp).Value!, variables));
        Assert.Equal("var(--series-1, #2A78D6)", SvgPaint.Resolve(SvgPaint.Of(blue, SvgColorRole.Any).Value!, variables));
        // A derived white stays literal although the card is white; kept as a token for a host that applies more variables.
        var literal = SvgPaint.Literal(ChartColor.White).Value!;
        Assert.Equal("#FFFFFF", SvgPaint.Resolve(literal, variables));
        Assert.Equal(literal, SvgPaint.Resolve(literal, variables, keepLiterals: true));
        // A blend of a token and a derived colour mixes the property; without variables it is the blended literal.
        var mix = SvgPaint.Mix(ChartColor.FromHex("#3F85DA"), ChartColor.White, null, blue, SvgColorRole.Series, 0.9).Value!;
        Assert.Equal("color-mix(in srgb, #FFFFFF, var(--series-1, #2A78D6) 90%)", SvgPaint.Resolve(mix, variables));
        Assert.Equal("#3F85DA", SvgPaint.Resolve(mix, null));
        // A blend of colours nothing maps stays a protected literal.
        Assert.Equal("#808080", SvgPaint.Resolve(SvgPaint.Mix(ChartColor.FromHex("#808080"), ChartColor.Black, null, ChartColor.White, null, 0.5).Value!, variables));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SvgColorVariables().Add("--x", blue, (SvgColorRole)99));
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
        var variables = new SvgColorVariables().Add("--card", ChartColor.White, SvgColorRole.Surface).Add("--ink", ChartColor.FromHex("#112233"));
        var result = variables.Apply("<rect fill=\"#FFFFFF\"/><text fill=\"#FFFFFF\" stroke=\"#FFFFFF\">a</text><tspan style=\"fill:#FFFFFF;stroke:#FFFFFF\"/><text fill=\"#112233\">b</text>");
        Assert.Equal("<rect fill=\"var(--card, #FFFFFF)\"/><text fill=\"#FFFFFF\" stroke=\"var(--card, #FFFFFF)\">a</text><tspan style=\"fill:#FFFFFF;stroke:var(--card, #FFFFFF)\"/><text fill=\"var(--ink, #112233)\">b</text>", result);

        // A caller's white text and the white card share RGB values but have different paint roles.
        var bars = Chart.Create().WithSize(640, 360).WithDesignTokens(Graphite)
            .AddTileMap("Sites", ChartTileMapCatalog.Get("us-states"), new[] { new ChartRegionMapItem("CA", 10), new ChartRegionMapItem("NY", 0) })
            .WithMapColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#EEF2F8"), ChartColor.FromHex("#1D4F9E")))
            .WithTickLabelStyle(style => style.WithColor("#FFFFFF"));
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
        var grid = new ChartGrid().Add(Lines().WithLuminousLineStyle()).Add(Lines().WithLuminousLineStyle()).WithTitle("Two").WithSvgColorVariables(Graphite.ToSvgColorVariables());
        var svg = grid.ToSvg();
        Assert.Contains("var(--cfx-series-1, #2A78D6)", svg, StringComparison.Ordinal);
        // Panels without variables of their own resolve their typed paints with the grid's: the white sheen stays literal.
        var highlights = System.Xml.Linq.XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "line-highlight").ToArray();
        Assert.NotEmpty(highlights);
        Assert.All(highlights, element => Assert.StartsWith("rgba(255,255,255,", (string)element.Attribute("stroke")!));
        Assert.DoesNotContain("\uFDD0", svg, StringComparison.Ordinal);

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
        Assert.Equal(NormalizeIdentity(Map().ToSvg()), NormalizeIdentity(Strip(svg)));
        Assert.Equal(Map().ToPng(), Map().ToPng(options));
    }

    private static Chart Lines() => Chart.Create().WithTitle("Traffic").WithSize(640, 320).WithDesignTokens(Graphite)
        .AddLine("Inbound", new[] { new ChartPoint(0, 1), new ChartPoint(1, 3), new ChartPoint(2, 2) })
        .AddLine("Outbound", new[] { new ChartPoint(0, 2), new ChartPoint(1, 1), new ChartPoint(2, 3) });

    // Paint policy participates in definition identity. Normalize only that generated prefix
    // when comparing resolved visual content; source identities and reference suffixes remain.
    private static string NormalizeIdentity(string svg) => Regex.Replace(svg, @"\bcfx-v2-[0-9a-f]{64}", "cfx-normalized");

    // Undoes the variables: var(--x, #hex) becomes #hex, color-mix(... N%, transparent) becomes rgba(r,g,b,N/100), and a
    // color-mix of two colours becomes their blend, rounded as the renderers blend.
    private static string Strip(string svg) {
        svg = Regex.Replace(svg, @"var\(--[\w-]+, (#[0-9A-Fa-f]{6})\)", "$1");
        const string Operand = @"(?:var\(--[\w-]+, )?#([0-9A-Fa-f]{6})\)?";
        svg = Regex.Replace(svg, @"color-mix\(in srgb, " + Operand + @", " + Operand + @" ([0-9.]+)%\)", match => {
            var from = ChartColor.FromHex(match.Groups[1].Value);
            var to = ChartColor.FromHex(match.Groups[2].Value);
            var amount = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) / 100;
            byte Channel(byte a, byte b) => (byte)Math.Round(a + (b - a) * amount);
            return ChartColor.FromRgb(Channel(from.R, to.R), Channel(from.G, to.G), Channel(from.B, to.B)).ToHex();
        });
        // Resolve innermost opacity mixes first: token alpha and mark opacity can both apply.
        // Quantize each result through the same RGBA byte boundary as static/raster paint.
        string previous;
        do {
            previous = svg;
            svg = Regex.Replace(svg, @"color-mix\(in srgb, (?:(?:#([0-9A-Fa-f]{6}))|rgba\(([0-9]+),([0-9]+),([0-9]+),([0-9.]+)\)) ([0-9.]+)%, transparent\)", match => {
                var color = match.Groups[1].Success ? ChartColor.FromHex(match.Groups[1].Value)
                    : ChartColor.FromRgba(byte.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), byte.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                        byte.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture), (byte)Math.Round(double.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture) * 255));
                var multiplier = double.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture) / 100;
                return color.WithAlpha((byte)Math.Round(color.A * multiplier)).ToCss();
            });
        } while (svg != previous);
        return svg;
    }

    private static string FixturePath(params string[] parts) {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ChartForgeX.sln"))) directory = directory.Parent;
        if (directory == null) throw new InvalidOperationException("Repository root was not found.");
        return Path.Combine(new[] { directory.FullName, "ChartForgeX.Tests", "Fixtures" }.Concat(parts).ToArray());
    }
}
