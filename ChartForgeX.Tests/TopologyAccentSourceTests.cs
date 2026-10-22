using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyAccentSourceTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void StatusAccentSurvivesIconAssignmentLateThemeAndVariableBinding(bool graphite, bool dark) {
        var chart = Chart();
        var theme = graphite ? VisualTheme.Graphite() : VisualTheme.Graphite().Configure(b => { b.Light.Status.Critical = new VisualTokenColor(ChartColor.Parse("#AF1328"), ChartColor.Black); b.Dark.Status.Critical = new VisualTokenColor(ChartColor.Parse("#FA5376"), ChartColor.White); });
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        chart.WithTheme(theme).WithThemeMode(mode);
        chart.Nodes[0].Color = "#713CA6";
        chart.Nodes[0].PaletteIndex = 1;
        var options = Options(TopologyAccentSource.Status);
        var expected = theme.Resolve(mode).Status.Critical.Fill;
        options.SvgColorVariables = new SvgColorVariables().Add("--host-status", expected, SvgColorRole.Status);
        var prepared = chart.Prepare(options);
        Assert.Contains("var(--host-status,", Surface(prepared.ToSvg()));
        var native = prepared.ToPng();
        options.SvgColorVariables = null;
        Assert.Equal(native, chart.ToPng(options));
        var reference = Chart().WithTheme(theme, mode);
        reference.Nodes[0].IconId = null;
        reference.Nodes[0].Color = null;
        Assert.Equal(Surface(reference.ToSvg(options)), Surface(chart.ToSvg(options)));
        Assert.Equal(TopologyAccentSource.Status, options.Clone().AccentSource);
    }

    [Theory]
    [InlineData(TopologyAccentSource.Auto, "#713CA6")]
    [InlineData(TopologyAccentSource.Explicit, "#713CA6")]
    [InlineData(TopologyAccentSource.Icon, "#2468AC")]
    public void AccentPoliciesChooseTheirDeclaredSource(TopologyAccentSource source, string expected) {
        var chart = Chart(); chart.Nodes[0].Color = "#713CA6";
        Assert.Equal(ChartColor.Parse(expected).ToCss(), Surface(chart.ToSvg(Options(source))));
    }

    [Fact]
    public void StatusSourceControlsTheWholeIconPaintAndIgnoresAuthoredVariablesAndPalette() {
        var chart = Chart(); chart.Nodes[0].Color = "var(--icon-accent, #713CA6)"; chart.Nodes[0].PaletteIndex = 1;
        var reference = Chart();
        var options = Options(TopologyAccentSource.Status);
        options.SvgColorVariables = new SvgColorVariables().Add("--status", chart.Theme.Resolve(chart.ThemeMode).Status.Critical.Fill, SvgColorRole.Status);
        var prepared = chart.Prepare(options);
        Assert.Equal(reference.ToPng(options), prepared.ToPng());
        var glyph = XDocument.Parse(prepared.ToSvg()).Descendants().First(n => (string?)n.Attribute("data-cfx-role") == "topology-icon-surface");
        Assert.Contains("var(--status,", (string?)glyph.Attribute("stroke"));
        Assert.DoesNotContain("--icon-accent", string.Join(";", glyph.Attributes().Where(a => a.Name.LocalName is "fill" or "stroke").Select(a => a.Value)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IconConstructorsKeepCatalogueColorSeparateFromExplicitPalettePaint(bool automatic) {
        var options = Options(TopologyAccentSource.Explicit);
        var chart = TopologyChart.Create().WithViewport(420, 200, 20).WithLegend(null);
        if (automatic) chart.AddAutoIconNode("node", "Service", "accent-proof:certificate", width:180,height:80,catalog:options.IconCatalog);
        else chart.AddIconNode("node", "Service", "accent-proof:certificate", 60,70,width:180,height:80,catalog:options.IconCatalog);
        Assert.Null(chart.Nodes[0].Color);
        chart.Nodes[0].PaletteIndex = 1;
        var reference = Chart(); reference.Nodes[0].PaletteIndex = 1;
        Assert.Equal(Surface(reference.ToSvg(options)), Surface(chart.ToSvg(options)));
        var graph = chart.ToGraphScene(o => { o.AccentSource = TopologyAccentSource.Explicit; o.IconCatalog = options.IconCatalog; });
        Assert.Equal(Surface(chart.ToSvg(options)), graph.Nodes[0].Style.BorderColor);
    }

    internal static TopologyChart Chart() {
        var chart = TopologyChart.Create().WithId("status-accent").WithViewport(420, 200, 20).WithLegend(null)
            .AddNode("node", "Service", 60, 70, width: 180, height: 80, status: TopologyHealthStatus.Critical);
        chart.WithNodeIcon("node", "accent-proof:certificate", Options(TopologyAccentSource.Auto).IconCatalog);
        return chart;
    }

    internal static TopologyRenderOptions Options(TopologyAccentSource source) {
        var icon = new TopologyIconDefinition("accent-proof", "certificate", "Certificate", TopologyNodeKind.Service, TopologyIconShape.Application) { Color = "#2468AC" };
        return new TopologyRenderOptions { AccentSource = source, IncludeLegend = false,
            NodeSurfaceStyle = TopologyNodeSurfaceStyle.Card,
            IconCatalog = TopologyIconCatalog.Default().AddPack(new TopologyIconPack("accent-proof", "Common").AddIcon(icon)) };
    }

    internal static string Surface(string svg) => (string)XDocument.Parse(svg).Descendants()
        .First(element => (string?)element.Attribute("data-cfx-role") == "topology-node-surface").Attribute("stroke")!;
}
