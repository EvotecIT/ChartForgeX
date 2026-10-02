using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Topology diagrams write their automatic name and description through <see cref="TopologyLabels"/>.</summary>
public sealed class TopologyLabelsTests {
    [Fact]
    public void Defaults_KeepTheEnglishNameAndDescription() {
        var svg = XDocument.Parse(Diagram(null).ToSvg());
        Assert.Equal("ChartForgeX topology", Element(svg, "title"));
        Assert.Equal("Topology chart with 1 groups, 3 nodes, and 2 edges.", Element(svg, "desc"));
    }

    [Fact]
    public void Formatter_ReceivesTheCountsAndWritesTheDescription() {
        ChartDescriptionFacts? seen = null;
        var chart = Diagram("Sieć").WithLabels(labels => {
            labels.UntitledTopology = "Topologia";
            labels.AccessibleTextFormatter = facts => {
                seen = facts;
                return facts.Title + ": " + facts.Count + " węzły, " + facts.EdgeCount + " połączenia, " + facts.GroupCount + " grupa";
            };
        });
        var svg = XDocument.Parse(chart.ToSvg());

        Assert.Equal("Sieć: 3 węzły, 2 połączenia, 1 grupa", Element(svg, "desc"));
        Assert.NotNull(seen);
        Assert.Equal(ChartDescriptionKind.Topology, seen!.Kind);
        Assert.Equal((3, 1, 2), (seen.Count, seen.GroupCount, seen.EdgeCount));
        Assert.Equal("Sieć", Element(svg, "title"));
        Assert.Equal("Topologia", Element(XDocument.Parse(Diagram(null).WithLabels(labels => labels.UntitledTopology = "Topologia").ToSvg()), "title"));
    }

    [Fact]
    public void Formatter_ReturningNothing_KeepsEnglish_AndAnExplicitDescriptionWins() {
        var blank = Diagram("Net").WithLabels(labels => labels.AccessibleTextFormatter = _ => " ");
        Assert.Equal("Net with 1 groups, 3 nodes, and 2 edges.", Element(XDocument.Parse(blank.ToSvg()), "desc"));
        var called = false;
        var described = Diagram("Net").WithAccessibility(a => a.Description = "Two sites.").WithLabels(labels => labels.AccessibleTextFormatter = _ => { called = true; return "x"; });
        Assert.Equal("Two sites.", Element(XDocument.Parse(described.ToSvg()), "desc"));
        Assert.False(called);
        Assert.Throws<ArgumentException>(() => new TopologyLabels().UntitledTopology = " ");
    }

    [Fact]
    public void HtmlPage_TakesTheUntitledNameAndLanguage() {
        var chart = Diagram(null).WithLabels(labels => labels.UntitledTopology = "Topologia").WithAccessibility(a => a.Language = "pl");
        var html = chart.ToHtmlPage();
        Assert.Contains("<title>Topologia</title>", html, StringComparison.Ordinal);
        Assert.Contains("lang=\"pl\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Report_UsesTheUntitledReportNameAndCarriesTheFormatterToItsPages() {
        var chart = Diagram(null).WithLabels(labels => {
            labels.UntitledReport = "Raport";
            labels.AccessibleTextFormatter = facts => "Opis: " + facts.Count;
        });
        var report = chart.PrepareReport();
        Assert.Equal("Raport", Element(XDocument.Parse(report.Overview.ToSvg()), "title"));
        var page = XDocument.Parse(report.Pages[0].ToSvg());
        Assert.StartsWith("Raport", Element(page, "title"), StringComparison.Ordinal);
        Assert.StartsWith("Opis: ", Element(page, "desc"), StringComparison.Ordinal);
        Assert.Equal("Topology report", Diagram(null).PrepareReport().Overview.ToSvg().Split(new[] { "<title" }, StringSplitOptions.None)[1].Split('>')[1].Split('<')[0]);
    }

    private static TopologyChart Diagram(string? title) {
        var chart = TopologyChart.Create().WithViewport(640, 360);
        if (title != null) chart.WithTitle(title);
        return chart
            .AddAutoGroup("site", "Site")
            .AddAutoNode("a", "DC01", groupId: "site")
            .AddAutoNode("b", "DC02", groupId: "site")
            .AddAutoNode("c", "DC03")
            .AddEdge("a-b", "a", "b")
            .AddEdge("b-c", "b", "c");
    }

    private static string Element(XDocument svg, string name) => svg.Root!.Elements().First(element => element.Name.LocalName == name).Value;
}
