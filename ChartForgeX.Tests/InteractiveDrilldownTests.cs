using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class InteractiveDrilldownTests {
    internal static Chart Quarters(ChartSeriesKind kind = ChartSeriesKind.Bar) {
        var chart = Chart.Create().Size(560, 320).Title("Sales by quarter").XLabels("Q1", "Q2", "Q3", "Q4");
        var points = new[] { new ChartPoint(1, 40), new ChartPoint(2, 55), new ChartPoint(3, 32), new ChartPoint(4, 61) };
        return kind == ChartSeriesKind.Line ? chart.AddLine("Sales", points) : chart.AddBar("Sales", points);
    }

    internal static Chart Regions(string quarter) => Chart.Create().Size(560, 320).Title(quarter + " by region")
        .XLabels("North", "South", "East", "West")
        .AddBar("Sales", new[] { new ChartPoint(1, 12), new ChartPoint(2, 9), new ChartPoint(3, 11), new ChartPoint(4, 8) });

    private static JsonElement DrillPayload(string html, string chartId) {
        var section = Regex.Match(html, "<section[^>]*data-cfx-chart-id=\"" + Regex.Escape(chartId) + "\"[^>]*>").Value;
        var encoded = Regex.Match(section, "data-cfx-drill=\"([^\"]*)\"").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(encoded), "The section names its drill-down targets.");
        return JsonDocument.Parse(System.Net.WebUtility.HtmlDecode(encoded)).RootElement;
    }

    [Fact]
    public void EmbeddedTargetsPrepareTheChildWithTheSameRendererAndKeepThePageScriptFree() {
        var html = Quarters().ToInteractiveHtmlPage(options => {
            options.Interaction.ChartId = "sales";
            options.Drilldown.Add("Q1", Regions("Q1")).AddOnDemand("Q3", "Q3 detail");
        });
        var payload = DrillPayload(html, "sales");
        Assert.Equal("Sales by quarter", payload.GetProperty("label").GetString());
        var targets = payload.GetProperty("targets").EnumerateArray().ToArray();
        Assert.Equal(new[] { "q1", "q3" }, targets.Select(target => target.GetProperty("id").GetString()));
        Assert.Equal(new[] { 1d, 3d }, targets.Select(target => target.GetProperty("x").GetDouble()));
        Assert.Equal(new[] { "Q1 by region", "Q3 detail" }, targets.Select(target => target.GetProperty("label").GetString()));
        Assert.Equal(new[] { true, false }, targets.Select(target => target.GetProperty("embedded").GetBoolean()));
        Assert.Equal("Opens {label}", payload.GetProperty("text").GetProperty("DrilldownAvailable").GetString());

        // One embedded child, inert until opened, rendered as an ordinary interactive section with its own identity.
        var template = Regex.Match(html, "<template data-cfx-drill-child=\"q1\">(.*?)</template>", RegexOptions.Singleline);
        Assert.True(template.Success);
        Assert.Contains("data-cfx-chart-id=\"sales-q1\"", template.Groups[1].Value);
        Assert.Contains("aria-label=\"Q1 by region\"", template.Groups[1].Value);
        Assert.DoesNotContain("<script", template.Groups[1].Value);
        Assert.DoesNotContain("data-cfx-drill-child=\"q3\"", html);
        Assert.Contains("data-cfx-feature=\"drilldown\"", html);

        // The parent drawing is the one a chart without targets renders.
        var plain = Quarters().ToInteractiveHtmlPage(options => options.Interaction.ChartId = "sales");
        Assert.Equal(Regex.Match(plain, "<svg.*?</svg>", RegexOptions.Singleline).Value, Regex.Match(html, "<svg.*?</svg>", RegexOptions.Singleline).Value);
        Assert.DoesNotContain("data-cfx-drill=", plain);
        Assert.DoesNotContain("data-cfx-drill-child", plain);
        Assert.DoesNotContain("data-cfx-feature=\"drilldown\"", plain);
    }

    [Fact]
    public void ChildrenInheritPresentationTakeTheirOwnTargetsAndStrictPagesKeepNoInlineStyles() {
        var html = Quarters().ToInteractiveHtmlPage(options => {
            options.Interaction.ChartId = "sales";
            options.Nonce = "drill-nonce";
            options.Localization[HtmlChartText.DrilldownNavigation] = "Pfad";
            options.Drilldown.RootLabel = "All quarters";
            options.Drilldown.Add("Q2", Regions("Q2"), "Q2 regions", child => child.Drilldown.Add("North", Regions("North"), "North sites"));
        });
        Assert.Equal("All quarters", DrillPayload(html, "sales").GetProperty("label").GetString());
        Assert.Equal("Pfad", DrillPayload(html, "sales").GetProperty("text").GetProperty("DrilldownNavigation").GetString());
        Assert.Equal("North sites", DrillPayload(html, "sales-q2").GetProperty("targets")[0].GetProperty("label").GetString());
        Assert.Equal("Pfad", DrillPayload(html, "sales-q2").GetProperty("text").GetProperty("DrilldownNavigation").GetString());
        Assert.Contains("data-cfx-chart-id=\"sales-q2-north\"", html);
        Assert.DoesNotContain(" style=\"", html);
        Assert.All(Regex.Matches(html, "<script[^>]*>").Select(match => match.Value), tag => Assert.Contains("nonce=\"drill-nonce\"", tag));
    }

    [Fact]
    public void ReaderOnlyStandalonePagesSelectTheStandaloneReaderWithTheDrilldownBundle() {
        var html = Quarters().ToInteractiveHtmlPage(options => {
            options.Runtime = HtmlChartRuntime.Standalone;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.KeyboardNavigation;
            options.Drilldown.AddOnDemand("Q4");
        });
        Assert.Contains("data-cfx-feature=\"drilldown\"", html);
        Assert.Contains(HtmlInteractiveAssets.ScriptFor(HtmlChartRuntime.Standalone, ChartInteractionFeatures.Tooltips, drilldown: true), html);
        Assert.NotEqual(HtmlInteractiveAssets.ScriptFor(HtmlChartRuntime.Standalone, ChartInteractionFeatures.Tooltips),
            HtmlInteractiveAssets.ScriptFor(HtmlChartRuntime.Standalone, ChartInteractionFeatures.Tooltips, drilldown: true));
    }

    [Fact]
    public void AdaptiveSectionsNameTheTargetsInBothDrawingsAndEmbedTheChildOnce() {
        var html = Quarters(ChartSeriesKind.Line).ToInteractiveHtmlFragment(options => {
            options.Interaction.ChartId = "sales";
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Drilldown.Add("Q1", Regions("Q1"));
        });
        Assert.Equal(2, Regex.Matches(html, "<section[^>]*data-cfx-chart-id=\"sales\"[^>]*data-cfx-drill=").Count);
        Assert.Single(Regex.Matches(html, "data-cfx-drill-child=\"q1\""));
    }

    [Fact]
    public void DrilldownExampleEmbedsTwoLevelsOfChildren() {
        var html = HtmlDrilldownExamples.CreatePage(dark: true);
        Assert.Single(Regex.Matches(html, "<template data-cfx-drill-child=\"london\">"));
        Assert.Single(Regex.Matches(html, "<template data-cfx-drill-child=\"warsaw\">"));
        Assert.Single(Regex.Matches(html, "<template data-cfx-drill-child=\"lon-dc01\">"));
        Assert.Contains("data-cfx-feature=\"drilldown\"", html);
    }

    [Fact]
    public void InvalidTargetsAreRejectedWhenConfiguredOrPrepared() {
        var options = new HtmlChartInteractionOptions();
        options.Drilldown.Add("Q1", Regions("Q1"));
        Assert.Throws<ArgumentException>(() => options.Drilldown.AddOnDemand("Q1"));
        Assert.Throws<ArgumentException>(() => options.Drilldown.AddOnDemand(" "));
        Assert.Throws<ArgumentNullException>(() => options.Drilldown.Add("Q2", null!));
        Assert.Throws<ArgumentException>(() => options.Drilldown.RootLabel = "");
        Assert.Throws<ArgumentException>(() => Quarters().ToInteractiveHtmlPage(o => o.Drilldown.AddOnDemand("Q9")));
        var pie = Chart.Create().XLabels("Q1").AddPie("Share", new[] { new ChartPoint(1, 3) });
        Assert.Throws<NotSupportedException>(() => pie.ToInteractiveHtmlPage(o => o.Drilldown.AddOnDemand("Q1")));
    }
}
