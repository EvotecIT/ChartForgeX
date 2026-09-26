using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class DesignTokenJsonTests {
    // Fixture copied verbatim from TestimoX docs/roadmaps/evidence/palette-v1-graphite.json (decision D14): the shape
    // HtmlForgeX tokens v1 generates.
    private static readonly string GraphiteJson = File.ReadAllText(FixturePath("tokens", "palette-v1-graphite.json"));

    [Fact]
    public void FromJson_GraphiteLight_MapsSurfacesTextAccentsSeriesAndStatus() {
        var tokens = VisualDesignTokens.FromJson(GraphiteJson);
        Assert.Equal("#F2F3F4", tokens.Background.ToHex());
        Assert.Equal("#FFFFFF", tokens.ElevatedSurface.ToHex());
        Assert.Equal("#F8F9FA", tokens.Surface.ToHex());
        Assert.Equal("#E2E4E7", tokens.Border.ToHex());
        Assert.Equal("#16181C", tokens.Foreground.ToHex());
        Assert.Equal("#4D525B", tokens.MutedForeground.ToHex());
        Assert.Equal("#11776F", tokens.Accent.ToHex());
        Assert.Equal("#37C9B8", tokens.SecondaryAccent.ToHex());
        Assert.Equal(new[] { "#2A78D6", "#0F9F8C", "#7B61E8", "#C24FA8", "#5B7F00", "#1D4F9E" }, tokens.Palette.Select(color => color.ToHex()).ToArray());

        Assert.Equal("#D4302F", tokens.Status.Critical.Fill.ToHex());
        Assert.Equal("#B8292A", tokens.Status.Critical.Ink.ToHex());
        Assert.Equal("#8C5C05", tokens.Status.Medium.Ink.ToHex());
        Assert.Equal("#2B5FC0", tokens.Status.Info.Ink.ToHex());
        Assert.Equal("#1D8A52", tokens.Status.Pass.Fill.ToHex());
        Assert.Equal("#5B6069", tokens.Status.Neutral.Ink.ToHex());
        Assert.Equal("#6B5BD2", tokens.Status.Maintenance.Fill.ToHex());
        Assert.Equal(tokens.Status.Pass.Fill, tokens.Positive);
        Assert.Equal(tokens.Status.Medium.Fill, tokens.Warning);
        Assert.Equal(tokens.Status.Critical.Fill, tokens.Negative);
        Assert.Equal(tokens.Status.Neutral.Fill, tokens.Disabled);
    }

    [Fact]
    public void FromJson_GraphiteDark_LoadsTheDarkVariant() {
        var tokens = VisualDesignTokens.FromJson(GraphiteJson, VisualThemeMode.Dark);
        Assert.Equal("#0C0D10", tokens.Background.ToHex());
        Assert.Equal("#16181C", tokens.ElevatedSurface.ToHex());
        Assert.Equal("#F2F3F5", tokens.Foreground.ToHex());
        Assert.Equal("#37C9B8", tokens.Accent.ToHex());
        Assert.Equal("#F47171", tokens.Status.Critical.Fill.ToHex());
        Assert.Equal("#A0A5AE", tokens.Status.Neutral.Ink.ToHex());
        Assert.Equal("#3987E5", tokens.Palette[0].ToHex());
        Assert.Equal(6, tokens.Palette.Length);
    }

    [Fact]
    public void StatusCategories_FollowThePlanVocabularyAndHueSharing() {
        var status = VisualDesignTokens.FromJson(GraphiteJson).Status;
        var states = status.OperationalStateCategories();
        Assert.Equal(new[] { "up", "degraded", "down", "recovering", "maintenance", "notObservable", "unknown" }, states.Select(state => state.Key).ToArray());
        Assert.Equal(status.Pass.Fill, states[0].Color);
        Assert.Equal(status.Medium.Fill, states[1].Color);
        Assert.Equal(status.Critical.Fill, states[2].Color);
        Assert.Equal(status.Low.Fill, states[3].Color);
        Assert.True(states[5].Hatched && states[6].Hatched && !states[0].Hatched);
        Assert.Equal(new[] { "critical", "high", "medium", "low", "info" }, status.SeverityCategories().Select(state => state.Key).ToArray());
        Assert.Equal(new[] { "pass", "notEvaluated", "couldNotEvaluate" }, status.OutcomeCategories().Select(state => state.Key).ToArray());
    }

    [Fact]
    public void WithDesignTokens_AppliesThemeSeriesAndStateColoursToRenderedCharts() {
        var tokens = VisualDesignTokens.FromJson(GraphiteJson);
        var day = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc);
        var timeline = Chart.Create().WithSize(640, 240).WithDesignTokens(tokens)
            .WithStateCategories(tokens.Status.OperationalStateCategories())
            .AddStateTimelineLane("DC01", new[] { new ChartStateTimelineSegment(day, day.AddHours(2), "up"), new ChartStateTimelineSegment(day.AddHours(2), day.AddHours(3), "down") });
        var svg = XDocument.Parse(timeline.ToSvg());
        var fills = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "state-segment").Select(element => (string)element.Attribute("fill")!).ToArray();
        Assert.Equal(new[] { tokens.Status.Pass.Fill.ToCss(), tokens.Status.Critical.Fill.ToCss() }, fills);
        Assert.Equal(tokens.MutedForeground, timeline.Options.Theme.MutedText);

        var line = Chart.Create().WithDesignTokens(tokens).AddLine("A", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) }).AddLine("B", new[] { new ChartPoint(0, 2), new ChartPoint(1, 1) });
        var lineSvg = line.ToSvg();
        Assert.Contains(tokens.Palette[0].ToCss(), lineSvg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(tokens.Palette[1].ToCss(), lineSvg, StringComparison.OrdinalIgnoreCase);
        Assert.True(line.ToPng().Length > 200);
    }

    [Fact]
    public void FromJson_MissingOrInvalidMembers_NameThePath() {
        var missingInk = Regex.Replace(GraphiteJson, @"""fill"": ""#dd5a17"",\s*""ink"": ""#b64a13""", "\"fill\": \"#dd5a17\"");
        Assert.NotEqual(GraphiteJson, missingInk);
        var error = Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(missingInk));
        Assert.Contains("light.severity.high.ink", error.Message, StringComparison.Ordinal);
        Assert.NotNull(VisualDesignTokens.FromJson(missingInk, VisualThemeMode.Dark));

        var badColour = GraphiteJson.Replace("\"page\": \"#f2f3f4\"", "\"page\": \"grey\"");
        Assert.Contains("light.surface.page", Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(badColour)).Message, StringComparison.Ordinal);
        Assert.Contains("'dark'", Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson("{\"light\":{}}", VisualThemeMode.Dark)).Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson("not json"));
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualDesignTokens.FromJson(GraphiteJson, (VisualThemeMode)7));
    }

    [Fact]
    public void Clone_CopiesStatusIndependently() {
        var tokens = VisualDesignTokens.FromJson(GraphiteJson);
        var copy = tokens.Clone();
        copy.Status.Critical = new VisualTokenColor(ChartColor.FromHex("#000000"), ChartColor.FromHex("#000000"));
        Assert.Equal("#D4302F", tokens.Status.Critical.Fill.ToHex());
        Assert.Equal("#6B5BD2", copy.Status.Maintenance.Fill.ToHex());
        Assert.Equal("#F47171", VisualDesignTokens.Dark().Status.Critical.Fill.ToHex());
    }

    [Fact]
    public void FromJson_StrictContract_RejectsDuplicatesShortHexWrongCaseAndDeepNesting() {
        var duplicate = GraphiteJson.Replace("\"page\": \"#f2f3f4\",", "\"page\": \"#f2f3f4\", \"page\": \"#000000\",");
        Assert.NotEqual(GraphiteJson, duplicate);
        Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(duplicate));
        Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(GraphiteJson.Replace("\"card\": \"#ffffff\"", "\"card\": \"#fff\"")));
        Assert.Contains("light.surface.cardAlt", Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(GraphiteJson.Replace("\"cardAlt\": \"#f8f9fa\"", "\"CardAlt\": \"#f8f9fa\""))).Message, StringComparison.Ordinal);
        Assert.Contains("light.series[1]", Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(Regex.Replace(GraphiteJson, @"""#2a78d6"",\s*""#0f9f8c""","\"#2a78d6\", 7"))).Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(new string('[', 100_000) + new string(']', 100_000)));
        Assert.Equal(0x80, VisualDesignTokens.FromJson(GraphiteJson.Replace("\"page\": \"#f2f3f4\"", "\"page\": \"#f2f3f480\"")).Background.A);
        Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJsonFile(" "));
    }

    [Fact]
    public void Defaults_UseGraphiteInksThatMeetTextContrast() {
        var status = new VisualDesignTokens().Status;
        foreach (var pair in new[] { status.Critical, status.High, status.Medium, status.Low, status.Info, status.Pass, status.Neutral, status.Maintenance }) {
            Assert.True(Contrast(pair.Ink, ChartColor.FromHex("#FFFFFF")) >= 4.5, pair.Ink.ToHex());
        }

        var labels = new Dictionary<string, string> { ["down"] = "Niedostępny" };
        var states = status.OperationalStateCategories(labels);
        Assert.Equal("Niedostępny", states.Single(state => state.Key == "down").Label);
        Assert.Equal("Up", states.Single(state => state.Key == "up").Label);
    }

    [Fact]
    public void FromJson_Ramps_MapSequentialAndDivergingPerMode() {
        var light = VisualDesignTokens.FromJson(GraphiteJson);
        Assert.Equal(new[] { "#86B6EF", "#5598E7", "#2A78D6", "#1C5CAB", "#104281" }, light.SequentialRamp!.Select(color => color.ToHex()).ToArray());
        Assert.Equal(new[] { "#F0A39D", "#E0645B", "#B8292A" }, light.DivergingRamp!.Negative.Select(color => color.ToHex()).ToArray());
        Assert.Equal("#ECEEF0", light.DivergingRamp.Neutral.ToHex());
        Assert.Equal("#1C5CAB", light.DivergingRamp.Positive[2].ToHex());
        var dark = VisualDesignTokens.FromJson(GraphiteJson, VisualThemeMode.Dark);
        Assert.Equal("#184F95", dark.SequentialRamp![0].ToHex());
        Assert.Equal("#2B2E34", dark.DivergingRamp!.Neutral.ToHex());

        var scale = light.DivergingRamp.ToMapColorScale(0);
        Assert.Equal("#B8292A", scale.LowColor.ToHex());
        Assert.Equal("#1C5CAB", scale.HighColor.ToHex());
        Assert.Equal(0, scale.MidpointValue);
        Assert.Equal("#104281", light.ToSequentialMapColorScale()!.HighColor.ToHex());
        Assert.Equal(light.SequentialRamp![0], light.Clone().SequentialRamp![0]);
    }

    [Fact]
    public void FromJson_WithoutRamps_StillLoadsAndKeepsDefaultBlend() {
        var withoutRamps = Regex.Replace(GraphiteJson, @",\s*""ramps"": \{(?:[^{}]|\{[^{}]*\})*\}", string.Empty);
        Assert.DoesNotContain("\"ramps\"", withoutRamps, StringComparison.Ordinal);
        var tokens = VisualDesignTokens.FromJson(withoutRamps);
        Assert.Null(tokens.SequentialRamp);
        Assert.Null(tokens.DivergingRamp);
        Assert.Null(tokens.ToSequentialMapColorScale());
        Assert.Null(Chart.Create().WithDesignTokens(tokens).Options.Theme.SequentialRamp);
        var replaced = Chart.Create().WithDesignTokens(VisualDesignTokens.FromJson(GraphiteJson)).WithDesignTokens(tokens);
        Assert.Null(replaced.Options.Theme.SequentialRamp);
    }

    [Fact]
    public void FromJson_InvalidRamps_NameThePath() {
        var shortRamp = Regex.Replace(GraphiteJson, @"""sequential"": \[\s*""#86b6ef"",(?:\s*""#[0-9a-f]{6}"",?)*\s*\]","\"sequential\": [\"#86b6ef\"]");
        Assert.Contains("light.ramps.sequential", Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(shortRamp)).Message, StringComparison.Ordinal);
        var badArm = GraphiteJson.Replace("\"#e0645b\"", "\"red\"");
        Assert.Contains("light.ramps.diverging.negative[1]", Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(badArm)).Message, StringComparison.Ordinal);
        var noNeutral = GraphiteJson.Replace("\"neutral\": \"#eceef0\",", string.Empty);
        Assert.Contains("light.ramps.diverging.neutral", Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(noNeutral)).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithDesignTokens_SequentialRampColoursCountHeatmapsInSvgAndPng() {
        var tokens = VisualDesignTokens.FromJson(GraphiteJson);
        var chart = Chart.Create().WithSize(640, 280).WithDesignTokens(tokens).AddHeatmapRow("Logons", new[] { 0d, 200d, 400d });
        chart.Options.HeatmapRelativeScale = true;
        var fills = XDocument.Parse(chart.ToSvg()).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "heatmap-cell").Select(element => (string)element.Attribute("fill")!).ToArray();
        Assert.Equal(new[] { tokens.SequentialRamp![0].ToCss(), tokens.SequentialRamp[2].ToCss(), tokens.SequentialRamp[4].ToCss() }, fills);
        var explicitColour = Chart.Create().WithSize(640, 280).WithDesignTokens(tokens).AddHeatmapRow("Logons", new[] { 0d, 400d }, ChartColor.FromHex("#0f9f8c"));
        Assert.DoesNotContain(tokens.SequentialRamp[4].ToCss(), explicitColour.ToSvg(), StringComparison.OrdinalIgnoreCase);
        var withRamp = chart.ToPng();
        chart.Options.Theme.SequentialRamp = null;
        Assert.NotEqual(withRamp, chart.ToPng());
        chart.WithDesignTokens(tokens);
        Assert.Throws<ArgumentException>(() => chart.Options.Theme.SequentialRamp = new[] { ChartColor.White });
    }

    [Fact]
    public void FromJsonFile_ReadsTheFixture() {
        Assert.Equal("#F2F3F4", VisualDesignTokens.FromJsonFile(FixturePath("tokens", "palette-v1-graphite.json")).Background.ToHex());
    }

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

    private static string FixturePath(params string[] parts) {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ChartForgeX.sln"))) directory = directory.Parent;
        if (directory == null) throw new InvalidOperationException("Repository root was not found.");
        return Path.Combine(new[] { directory.FullName, "ChartForgeX.Tests", "Fixtures" }.Concat(parts).ToArray());
    }
}
