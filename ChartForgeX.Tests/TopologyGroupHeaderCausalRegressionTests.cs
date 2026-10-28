using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Uses only existing owners so failures can be reproduced before the production header patch.</summary>
public sealed class TopologyGroupHeaderCausalRegressionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredErMultilineHeaderRetainsRawSourceAndPaintsBothNativeRows(bool dark) {
        var result = Render(dark); Assert.False(result.HasErrors);
        var source = Assert.IsType<MermaidEntityRelationshipDocument>(result.Document);
        var authored = source.Groups.Single(group => group.Id == "Order group");
        Assert.Equal("Order<br>records", authored.Label); Assert.True(authored.Span.Line > 0);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var mapped = chart.Groups.Single(group => group.Id == authored.Id);
        Assert.Equal("Order\nrecords", mapped.Label);
        Assert.Equal(authored.Label, mapped.Metadata["mermaid.source.label"]);
        Assert.Equal("Domain: core", mapped.ParentGroupId);
        var prepared = chart.Prepare();
        var rows = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>()
            .Where(text => text.Role == "topology-group-label")
            .SelectMany(text => text.Text.Lines.Select(line => line.Text))
            .Where(text => text is "Order" or "records").ToArray();
        Assert.Equal(new[] { "Order", "records" }, rows);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultErChildHeaderInkContrastsWithTheActualParentAndChildFill(bool dark) {
        var result = Render(dark); Assert.False(result.HasErrors);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var parent = chart.Groups.Single(group => group.Id == "Domain: core");
        var child = chart.Groups.Single(group => group.Id == "Order group");
        Assert.Equal("#dcfce7", parent.BackgroundColor); Assert.Equal("#14532d", parent.LabelColor);
        Assert.Null(child.BackgroundColor); Assert.Null(child.LabelColor);
        var prepared = chart.Prepare();
        // The maintained source has exactly these two shells, painted parent first.
        var fills = prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>()
            .Where(mark => mark.Role == "topology-group-surface").Select(mark => mark.Fill!.Value).ToArray();
        Assert.Equal(2, fills.Length); Assert.Equal(255, fills[0].A); Assert.InRange(fills[1].A, 1, 254);
        var composed = ChartColorMath.CompositeOverOpaque(fills[1], fills[0]);
        var title = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>(),
            text => text.Role == "topology-group-label" && text.Text.Lines.Any(line => line.Text == "Order"));
        var ratio = ChartColorMath.ContrastRatio(title.Color, composed);
        Assert.True(ratio >= 4.5, "Default nested caption ink " + title.Color.ToCss() + " must contrast against composed shell " + composed.ToCss() + "; actual ratio " + ratio + ".");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitInkAndOrdinarySingleLineDefaultInkKeepTheirAuthoredAndThemeContracts(bool dark) {
        var chart = TopologyChart.Create().WithViewport(640, 280, 16).WithLegend(null)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddGroup("plain", "Plain control", 24, 40, 280, 180)
            .AddGroup("explicit", "Explicit control", 328, 40, 280, 180);
        chart.Groups[1].BackgroundColor = "#DCFCE7"; chart.Groups[1].LabelColor = "#7C3AED";
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false,
            IncludeGroupStatusDots = false, CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Plain });
        var rows = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "topology-group-label").ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(chart.Theme.Resolve(chart.ThemeMode).Foreground,
            Assert.Single(rows, text => text.Text.Lines.Any(line => line.Text == "Plain control")).Color);
        Assert.Equal(ChartColor.Parse("#7C3AED"),
            Assert.Single(rows, text => text.Text.Lines.Any(line => line.Text == "Explicit control")).Color);
    }

    private static MermaidRenderResult Render(bool dark) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "Mermaid", "er-native-captions.mmd"));
        return MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light,
                FontFamily = BrowserProofFonts.Family, TopologySizing = MermaidTopologySizing.Natural },
            EntityRelationship = new MermaidTopologyRenderOptions { Width = 1100, Height = 620 }
        });
    }
}
