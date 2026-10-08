using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyThemePaintTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThemeVariablesRetainDistinctTokenIdentityAndTheSameNativeFallbackPixels(bool dark) {
        var theme = dark ? TopologyTheme.Dark() : TopologyTheme.Light();
        // Equal native values must still follow different host properties after a theme switch.
        theme.Card = theme.Background;
        theme.MutedForeground = theme.Foreground;
        theme.Warning = theme.Healthy;
        var literal = Diagram(theme);
        var variableTheme = Variables(theme);
        var variable = Diagram(variableTheme);
        var options = new TopologyRenderOptions { IncludeLegend = false, CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.PanelGrid, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Card };
        var prepared = variable.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg());
        XElement Mark(string role) => svg.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == role)
            .DescendantsAndSelf().First(element => element.Attribute("fill") != null || element.Attribute("stroke") != null);
        Assert.Contains("var(--host-Background,", (string?)Mark("background").Attribute("fill"));
        Assert.Contains("var(--host-Card,", (string?)Mark("topology-node-surface").Attribute("fill"));
        Assert.Contains("var(--host-Border,", (string?)Mark("topology-grid").Attribute("stroke"));
        Assert.Contains("var(--host-Foreground,", (string?)Mark("topology-node-label").Attribute("fill"));
        Assert.Contains("var(--host-MutedForeground,", (string?)Mark("topology-node-subtitle").Attribute("fill"));
        foreach (var name in new[] { "Healthy", "Warning", "Critical", "Unknown", "Disabled" }) Assert.Contains("var(--host-" + name + ",", prepared.ToSvg());
        Assert.Equal(literal.ToPng(options), prepared.ToPng());
        var original = prepared.ToSvg();
        variableTheme.Card = "var(--mutated, #FF0000)";
        variableTheme.Foreground = "#00FF00";
        Assert.Equal(original, prepared.ToSvg());
        Assert.Equal(literal.Prepare(options).WithOutputSize(prepared.Width, prepared.Height + 20).ToPng(), prepared.WithOutputSize(prepared.Width, prepared.Height + 20).ToPng());
        Assert.Contains("var(--host-Card,", prepared.WithOutputSize(prepared.Width, prepared.Height + 20).ToSvg());
    }

    [Theory]
    [InlineData("#11223380", 32)]
    [InlineData("rgba(17,34,51,0.5)", 32)]
    [InlineData("#11223300", 0)]
    public void ThemeVariableHighlightKeepsTheAuthoredFactorAndFallbackAlpha(string fallback, int expectedGridAlpha) {
        var theme = TopologyTheme.Light();
        theme.Card = "var(--card, " + fallback + ")";
        theme.Foreground = "var(--text, " + fallback + ")";
        theme.Border = "var(--border, " + fallback + ")";
        var chart = Diagram(theme);
        var options = new TopologyRenderOptions { IncludeLegend = false, DimmedOpacity = .5, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Card, CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.PanelGrid };
        options.HighlightNodeIds.Add("node-4");
        var prepared = chart.Prepare(options);
        var svg = prepared.ToSvg();
        Assert.Contains("color-mix(in srgb, var(--card,", svg);
        Assert.Contains("50%, transparent)", svg);
        Assert.All(prepared.Visual.Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneLine>().Where(line => line.Role == "topology-grid"), line => Assert.Equal(expectedGridAlpha, line.Stroke!.Value.A));
        theme.Card = fallback; theme.Foreground = fallback; theme.Border = fallback;
        Assert.Equal(chart.ToPng(options), prepared.ToPng());
    }

    [Fact]
    public void AttachingThemePaintsKeepsDefaultStatusGlyphHighlightPixels() {
        var chart = Diagram(Variables(TopologyTheme.Light()));
        var options = new TopologyRenderOptions { IncludeLegend = false, DimmedOpacity = .5 };
        options.HighlightNodeIds.Add("node-4");
        var pixels = chart.ToPng(options);
        chart.Theme = null;
        Assert.Equal(chart.ToPng(options), pixels);
    }

    [Fact]
    public void ThemeVariableWithoutALiteralFallbackUsesItsCorrespondingDefaultToken() {
        var theme = TopologyTheme.Light();
        theme.Background = "var(--background)";
        theme.Foreground = "var(--foreground)";
        var chart = Diagram(theme);
        var options = new TopologyRenderOptions { IncludeLegend = false };
        var prepared = chart.Prepare(options);
        Assert.Contains("var(--background, #FFFFFF)", prepared.ToSvg());
        Assert.Equal(Diagram(TopologyTheme.Light()).ToPng(options), prepared.ToPng());
    }

    [Fact]
    public void ThemeVariablesReachEveryLegendMarkerAndPreserveExplicitOverrides() {
        TopologyLegend Legend(bool variables) => TopologyLegend.Default()
            .AddNodeKind("Server", TopologyNodeKind.Server)
            .AddEdgeKind("Connection", TopologyEdgeKind.Generic)
            .AddStatus("Override", TopologyHealthStatus.Healthy, variables ? "var(--override, #8833AA)" : "#8833AA")
            .AddNodeKind("Override node", TopologyNodeKind.Database, variables ? "var(--override, #8833AA)" : "#8833AA", backgroundColor: variables ? "var(--override-card, #AABBCC)" : "#AABBCC");
        var literal = Diagram(TopologyTheme.Light()).WithLegend(Legend(false));
        var variable = Diagram(Variables(TopologyTheme.Light())).WithLegend(Legend(true));
        var options = new TopologyRenderOptions { IncludeLegend = true };
        var prepared = variable.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg());
        var items = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-legend-item").ToArray();
        foreach (var name in new[] { "Healthy", "Warning", "Critical", "Unknown", "Disabled" }) {
            var item = items.First(element => (string?)element.Attribute("data-cfx-status") == name);
            Assert.Contains("var(--host-" + name + ",", (string?)item.Descendants().First(element => element.Attribute("fill") != null).Attribute("fill"));
        }
        var server = items.First(element => (string?)element.Attribute("data-legend-kind") == "node");
        var surface = server.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == "topology-legend-node");
        Assert.Contains("var(--host-Card,", (string?)surface.Attribute("fill"));
        Assert.Contains("var(--host-Accent,", (string?)surface.Attribute("stroke"));
        var glyphs = server.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-node-icon").ToArray();
        Assert.NotEmpty(glyphs);
        Assert.All(glyphs, element => Assert.Contains("var(--host-Accent,", (string?)element.Attribute("fill") == "none" ? (string?)element.Attribute("stroke") : (string?)element.Attribute("fill")));
        Assert.Contains("var(--host-Accent,", items.First(element => (string?)element.Attribute("data-legend-kind") == "edge").ToString());
        Assert.Contains("var(--override, #8833AA)", prepared.ToSvg());
        Assert.Contains("var(--override-card, #AABBCC)", prepared.ToSvg());
        Assert.Equal(literal.ToPng(options), prepared.ToPng());
    }

    [Fact]
    public void ThemeVariableIconCaptionUsesTheSameHighlightOpacityAsNativeText() {
        var theme = TopologyTheme.Light();
        var chart = Diagram(Variables(theme));
        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Icon, IncludeIconLabels = true, DimmedOpacity = .5 };
        options.HighlightNodeIds.Add("node-4");
        var prepared = chart.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg());
        var dimmed = svg.Descendants().First(element => (string?)element.Attribute("data-cfx-source-id") == "node-0-label");
        Assert.Contains("color-mix(in srgb, var(--host-Foreground, #0F172A) 50%, transparent)", dimmed.ToString());
        var active = svg.Descendants().First(element => (string?)element.Attribute("data-cfx-source-id") == "node-4-label");
        Assert.DoesNotContain("color-mix", active.ToString());
        Assert.Equal(128, prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().First(text => text.Id == "node-0-label").Color.A);
        Assert.Equal(Diagram(theme).ToPng(options), prepared.ToPng());
    }

    [Theory]
    [InlineData("#FF0000", 241, 224)]
    [InlineData("#FF000080", 248, 218)]
    [InlineData("#FF000000", 255, 212)]
    public void GroupVariableClearanceUsesTheAuthoredFallbackTintAndAlpha(string fallback, int expectedGreen, int expectedAlpha) {
        var chart = TopologyChart.Create().WithTheme(TopologyTheme.Light()).WithViewport(600, 360, 20).WithLegend(null)
            .AddGroup("services", "Services", 10, 10, 560, 320, color: "var(--group, " + fallback + ")")
            .AddNode("source", "Source", 70, 150, groupId: "services")
            .AddNode("target", "Target", 350, 150, groupId: "services")
            .AddEdge("connection", "source", "target", label: "Connection", routing: TopologyEdgeRouting.Straight);
        var options = new TopologyRenderOptions { IncludeLegend = false, VisualStyle = TopologyVisualStyle.MonitoringDashboard, IncludeEdgeLabelBackplates = false };
        var prepared = chart.Prepare(options);
        var clearance = prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>().Single(rectangle => rectangle.Role == "topology-edge-label-clearance");
        Assert.Equal(expectedGreen, clearance.Fill!.Value.G);
        Assert.Equal(expectedAlpha, clearance.Fill!.Value.A);
        var svgClearance = XDocument.Parse(prepared.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-label-clearance");
        Assert.Contains("var(--group,", (string?)svgClearance.Attribute("fill"));
        chart.Groups[0].Color = fallback;
        Assert.Equal(chart.ToPng(options), prepared.ToPng());
    }

    [Fact]
    public void GeographicThemeVariableTintPreservesFallbackAlpha() {
        var theme = TopologyTheme.Light();
        theme.Accent = "var(--map-accent, #FF000080)";
        var chart = Diagram(theme).WithLayout(TopologyLayoutMode.Geographic)
            .WithNodeCoordinates("node-0", -80, 30).WithNodeCoordinates("node-1", 50, 30);
        var options = new TopologyRenderOptions { IncludeLegend = false, MapBackgroundStyle = TopologyMapBackgroundStyle.SoftSilhouette };
        var prepared = chart.Prepare(options);
        var surface = prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>().Single(rectangle => rectangle.Role == "topology-map-surface");
        Assert.Equal(251, surface.Fill!.Value.A);
        var svgSurface = XDocument.Parse(prepared.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-map-surface");
        Assert.Contains("var(--map-accent,", (string?)svgSurface.Attribute("fill"));
        theme.Accent = "#FF000080";
        Assert.Equal(chart.ToPng(options), prepared.ToPng());
    }

    private static TopologyChart Diagram(TopologyTheme theme) {
        var chart = TopologyChart.Create().WithId("theme-paints").WithTheme(theme).WithTitle("Network").WithSubtitle("Host theme")
            .WithViewport(820, 240, 20).WithLegend(null);
        var statuses = new[] { TopologyHealthStatus.Healthy, TopologyHealthStatus.Warning, TopologyHealthStatus.Critical, TopologyHealthStatus.Unknown, TopologyHealthStatus.Disabled };
        for (var i = 0; i < statuses.Length; i++) chart.AddNode("node-" + i, statuses[i].ToString(), 30 + i * 150, 60, kind: TopologyNodeKind.Server, status: statuses[i], subtitle: "Status", width: 120, height: 85);
        chart.AddEdge("route", "node-0", "node-1", routing: TopologyEdgeRouting.Straight);
        return chart;
    }

    private static TopologyTheme Variables(TopologyTheme theme) {
        var result = theme.Clone();
        foreach (var property in typeof(TopologyTheme).GetProperties().Where(property => property.PropertyType == typeof(string) && property.Name != nameof(TopologyTheme.FontFamily)))
            property.SetValue(result, "var(--host-" + property.Name + ", " + property.GetValue(theme) + ")");
        return result;
    }
}
