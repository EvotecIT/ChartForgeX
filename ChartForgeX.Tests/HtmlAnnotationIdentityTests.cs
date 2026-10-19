using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlAnnotationIdentityTests {
    [Fact]
    public void HtmlCatalogMatchesTheDetachedNativeBindingsWithoutChangingAnonymousSections() {
        var chart = Create().AddHorizontalLine(new ChartAnnotationId("warning: β \"quoted\""), 4, "Limit")
            .AddVerticalBand(new ChartAnnotationId("range"), 1, 2, "Limit").AddVerticalLine(2, "Limit");
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart);
        using var metadata = PreparedMetadata(section.Html);
        var annotations = metadata.RootElement.GetProperty("annotations").EnumerateArray().ToArray();
        Assert.Equal(2, annotations.Length);
        var groups = XDocument.Parse(section.PreparedVisual.ToSvg()).Descendants()
            .Where(node => node.Attribute("data-cfx-annotation-id") != null).ToArray();
        foreach (var entry in annotations) {
            var group = Assert.Single(groups, node => (string?)node.Attribute("data-cfx-source-id") == entry.GetProperty("sourceId").GetString());
            Assert.Equal((string?)group.Attribute("data-cfx-annotation-id"), entry.GetProperty("id").GetString());
            Assert.Equal((string?)group.Attribute("data-cfx-kind"), entry.GetProperty("kind").GetString());
        }
        chart.Annotations.Clear();
        Assert.Equal(2, section.PreparedVisual.Scene.Annotations.Count);
        using var fresh = PreparedMetadata(new HtmlInteractiveChartRenderer().PrepareSection(Create().AddHorizontalLine(4, "Limit")).Html);
        Assert.False(fresh.RootElement.TryGetProperty("annotations", out _));
    }

    [Fact]
    public void TypedScenarioUsesTheNativeFormatterAndSurvivesDashboardCloning() {
        var id = new ChartAnnotationId("annotation-0");
        var chart = Create().AddHorizontalLine(id, 4, "Limit").AddVerticalLine(2, "Limit");
        var native = new HtmlInteractiveChartRenderer().PrepareSection(chart).PreparedVisual.Scene.Annotations.Single();
        var scenario = new ChartInteractionScenario { Id = "thresholds", Label = "Thresholds" };
        scenario.AddAnnotationStep(id).AddAnnotationStep("Limit");
        Assert.Equal(native.SourceId, scenario.Steps[0].TargetId);
        Assert.Equal("Limit", scenario.Steps[1].TargetId);
        Assert.Throws<InvalidOperationException>(() => scenario.AddAnnotationStep(default(ChartAnnotationId)));
        Assert.Equal(2, scenario.Steps.Count);
        var page = new[] { chart, Create().AddVerticalLine(id, 2, "Translated") }.ToInteractiveHtmlDashboardPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.Scenarios;
            options.Interaction.Scenarios.Add(scenario);
        });
        var routes = Regex.Matches(page, "data-cfx-scenario-steps=\"([^\"]+)\"");
        Assert.Equal(2, routes.Count);
        foreach (Match route in routes) {
            using var steps = JsonDocument.Parse(WebUtility.HtmlDecode(route.Groups[1].Value));
            Assert.Equal(native.SourceId, steps.RootElement[0].GetProperty("targetId").GetString());
            Assert.Equal("annotation", steps.RootElement[0].GetProperty("targetKind").GetString());
            Assert.Equal("Limit", steps.RootElement[1].GetProperty("targetId").GetString());
        }
    }

    private static Chart Create() => Chart.Create().AddLine("Readings", new[] { new ChartPoint(0, 2), new ChartPoint(3, 8) });
    private static JsonDocument PreparedMetadata(string html) => JsonDocument.Parse(WebUtility.HtmlDecode(
        Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]+)\"").Groups[1].Value));
}
