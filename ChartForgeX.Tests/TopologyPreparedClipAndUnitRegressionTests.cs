using System.Text.Json;
using System.Text.Json.Nodes;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyPreparedClipAndUnitRegressionTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void NaturalHorizontalClipExtensionKeepsExactPixelsAndTypedClipUntilTheFrameChanges(bool left) {
        var chart = Natural(left); var before = chart.Prepare(); var saved = before.ToInterchangeEnvelope(); var json = saved.ToJson();
        Capture("natural-clip-" + left + "-direct", before); var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var after = restored.Prepare(); Capture("natural-clip-" + left + "-restored", after);
        Assert.NotNull(saved.Topology!.ContentClip); Assert.Equal(2, saved.Version);
        var clip = saved.Topology.ContentClip!; Assert.True(clip.Clip.Left < clip.Plot.Left || clip.Clip.Right > clip.Plot.Right);
        Assert.Equal(before.ToPng(), after.ToPng()); Assert.Equal(Clips(before), Clips(after));
        var oldPadding = restored.Viewport.Padding; restored.Viewport.Padding += 3;
        var edited = restored.Prepare(); Capture("natural-clip-" + left + "-edited", edited);
        Assert.NotEqual(oldPadding, restored.Viewport.Padding); Assert.Null(edited.ToInterchangeEnvelope().Topology!.ContentClip);
        Assert.Equal(json, before.ToInterchangeEnvelope().ToJson());
    }

    [Theory] [InlineData("heading")] [InlineData("legend")] [InlineData("context")]
    [InlineData("context-theme")] [InlineData("context-mode")] [InlineData("context-legend-flags")]
    public void ChangedFrameRequestsCannotReplayAnOldOutwardContentClip(string edit) {
        var before = Natural(true).Prepare(); var chart = before.ToInterchangeEnvelope().ToTopologyChart();
        if (edit == "heading") chart.Title = "A changed heading";
        if (edit == "legend") { chart.Legend = TopologyLegend.Create("Changed legend").AddStatus("Healthy", TopologyHealthStatus.Healthy); (chart.DefaultRenderOptions ??= new TopologyRenderOptions()).IncludeLegend = true; }
        PreparedVisual visual;
        if (edit == "context") visual = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(chart.Viewport.Width, chart.Viewport.Height), chart.Viewport.Padding),
            chart.Theme, frame: new VisualFrame(title: "Explicit frame", showLegend: false), font: new FontSpec { Family = BrowserProofFonts.Family }));
        else if (edit.StartsWith("context-", StringComparison.Ordinal)) {
            var options = chart.ResolveRenderOptions(null); var request = VisualExportRequest.ForTopology(chart, options).Context;
            var context = VisualTopologyCompiler.ResolveLegendFrame(VisualTopologyCompiler.ResolveFrame(request, chart, options),
                options.IncludeLegend ? TopologyLegend.Resolve(chart, options.LegendMode) : null);
            var frame = context.Frame;
            if (edit == "context-legend-flags") frame = new VisualFrame(frame.Title, frame.Subtitle, showLegend: null,
                showSurface: frame.ShowSurface, transparentBackground: frame.TransparentBackground, titleStyle: frame.TitleStyle,
                subtitleStyle: frame.SubtitleStyle, legendStyle: frame.LegendStyle, legendMaximumRows: frame.LegendMaximumRows,
                legendMaximumHeightFraction: frame.LegendMaximumHeightFraction, showCard: frame.ShowCard, legendTitle: frame.LegendTitle);
            visual = chart.Prepare(new VisualRenderContext(context.Layout,
                edit == "context-theme" ? context.Theme.WithPalette(ChartColor.FromHex("#ef4444"), ChartColor.FromHex("#22c55e")) : context.Theme,
                edit == "context-mode" ? VisualThemeMode.Dark : context.ThemeMode, frame, context.Font), options);
        }
        else visual = chart.Prepare().Visual;
        Capture("clip-frame-edit-" + edit, visual);
        Assert.Null(visual.ToArtifact().ToInterchangeEnvelope().Topology!.ContentClip);
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Card,false)] [InlineData(TopologyNodeDisplayMode.Card,true)]
    [InlineData(TopologyNodeDisplayMode.CompactCard,false)] [InlineData(TopologyNodeDisplayMode.CompactCard,true)]
    [InlineData(TopologyNodeDisplayMode.Pill,false)] [InlineData(TopologyNodeDisplayMode.Pill,true)]
    [InlineData(TopologyNodeDisplayMode.Dot,false)] [InlineData(TopologyNodeDisplayMode.Dot,true)]
    public void GenericNativeConstantsShareTheOriginalNodeProjectionForEnlargementAndShrink(TopologyNodeDisplayMode mode, bool shrink) {
        var chart = Generic(mode, shrink); var before = chart.Prepare(); var json = before.ToInterchangeEnvelope().ToJson(); Capture("generic-unit-" + mode + "-" + shrink + "-direct", before);
        var imported = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); var after = imported.Prepare(); Capture("generic-unit-" + mode + "-" + shrink + "-restored", after);
        var basis = imported.Nodes[0].PreparedNodeBasis; Assert.NotNull(basis); Assert.True(shrink ? basis!.Factor < 1 : basis!.Factor > 1);
        Assert.Equal(before.ToPng(), after.ToPng()); Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(after.ToSvg()));
        imported.Nodes[0].X += 9; imported.Nodes[0].Y += 7; Assert.Same(basis, TopologyPreparedNodeBasis.Resolve(imported.Nodes[0]));
        imported.Nodes[0].Width += 1; Assert.Null(TopologyPreparedNodeBasis.Resolve(imported.Nodes[0]));
        imported.LayoutMode = TopologyLayoutMode.Layered; var automatic = TopologyLayoutEngine.Prepare(imported, options: imported.ResolveRenderOptions(null)); Assert.Null(automatic.Nodes[0].PreparedNodeBasis);
        Assert.Equal(json, before.ToInterchangeEnvelope().ToJson());
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void FittedNotesProjectTheirMeasuredGutterLeaderPaddingAndInkExactlyOnce(bool left) {
        var chart = Generic(TopologyNodeDisplayMode.Card, false); chart.Annotations.Add(new TopologyAnnotation("note", "Actual measured note", "node", left ? TopologyAnnotationSide.Left : TopologyAnnotationSide.Right));
        var before = chart.Prepare(); var saved = before.ToInterchangeEnvelope(); Capture("note-units-" + left + "-direct", before);
        var restored = saved.ToTopologyChart(); var after = restored.Prepare(); Capture("note-units-" + left + "-restored", after);
        var units = saved.Annotations[0].TopologyDefaultUnits; Assert.NotNull(units); Assert.NotEqual(1, units!.Value);
        Assert.NotNull(saved.Annotations[0].TopologyRoutingViewport);
        Assert.Equal(before.ToPng(), after.ToPng()); Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(after.ToSvg()));
        restored.WithViewport(restored.Viewport.Width + 100, restored.Viewport.Height + 80, restored.Viewport.Padding);
        var changed = restored.Prepare(); Capture("note-units-" + left + "-frame-edited", changed); Assert.Null(changed.ToInterchangeEnvelope().Annotations[0].TopologyDefaultUnits); Assert.Null(changed.ToInterchangeEnvelope().Annotations[0].TopologyRoutingViewport);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExplicitContextThemeOrModeChangeClearsCapturedNoteUnitsEvenWhenTheChartAndFontStayTheSame(bool modeOnly) {
        var source = Generic(TopologyNodeDisplayMode.Card, false); source.Annotations.Add(new TopologyAnnotation("note", "Measured note", "node"));
        var before = source.Prepare(); var chart = before.ToInterchangeEnvelope().ToTopologyChart();
        Assert.NotEqual(1, chart.Annotations[0].PreparedDefaultUnits);
        var options = chart.ResolveRenderOptions(null); var request = VisualExportRequest.ForTopology(chart, options).Context;
        var context = VisualTopologyCompiler.ResolveLegendFrame(VisualTopologyCompiler.ResolveFrame(request, chart, options),
            options.IncludeLegend ? TopologyLegend.Resolve(chart, options.LegendMode) : null);
        var theme = chart.Theme; var mode = chart.ThemeMode;
        var after = chart.Prepare(new VisualRenderContext(context.Layout,
            modeOnly ? context.Theme : context.Theme.WithPalette(ChartColor.FromHex("#ef4444"), ChartColor.FromHex("#22c55e")),
            modeOnly ? VisualThemeMode.Dark : context.ThemeMode, context.Frame, context.Font), options);
        Capture("note-explicit-context-witness-" + modeOnly, after);
        Assert.Same(theme, chart.Theme); Assert.Equal(mode, chart.ThemeMode);
        Assert.Null(after.ToArtifact().ToInterchangeEnvelope().Annotations[0].TopologyDefaultUnits);
        Assert.Null(after.ToArtifact().ToInterchangeEnvelope().Annotations[0].TopologyRoutingViewport);
    }

    [Fact]
    public void RawUnitNullAndFactorOneStillOmitAllNewFacts() {
        var chart = Generic(TopologyNodeDisplayMode.Card, false); chart.LayoutMode = TopologyLayoutMode.Manual;
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false, FitContentToViewport = false });
        chart.Nodes[0].Symbol = null; chart.Annotations.Add(new TopologyAnnotation("note", "Ordinary note", "node"));
        var before = chart.Prepare(); var saved = before.ToInterchangeEnvelope(); Capture("generic-note-null-control", before);
        Assert.Null(saved.Topology!.ContentClip); Assert.Null(saved.Nodes[0].Topology!.PreparedNodeBasis); Assert.Null(saved.Annotations[0].TopologyDefaultUnits); Assert.Null(saved.Annotations[0].TopologyRoutingViewport);
        Assert.DoesNotContain("topologyDefaultUnits", saved.ToJson(), StringComparison.Ordinal); Assert.DoesNotContain("contentClip", saved.ToJson(), StringComparison.Ordinal);
        Assert.Equal(before.ToPng(), saved.ToTopologyChart().Prepare().ToPng());
    }

    [Fact]
    public void MalformedClipAndNoteUnitFactsRejectAtTheExistingPortableBoundary() {
        var json = Natural(true).Prepare().ToInterchangeEnvelope().ToJson(); var raw = JsonNode.Parse(json)!;
        raw["topology"]!["contentClip"]!["clip"]!["x"] = -1;
        Assert.Throws<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(raw.ToJsonString()));
        var chart = Generic(TopologyNodeDisplayMode.Card, false); chart.Annotations.Add(new TopologyAnnotation("note", "Note", "node"));
        var saved = chart.Prepare().ToInterchangeEnvelope(); saved.Annotations[0].TopologyDefaultUnits = double.PositiveInfinity;
        Assert.Throws<ArgumentException>(() => saved.Validate()); saved.Annotations[0].TopologyDefaultUnits = -1;
        Assert.Throws<ArgumentException>(() => saved.Validate());
    }

    private static TopologyChart Natural(bool left) {
        var chart = TopologyChart.Create().WithId("actual-natural-clip").WithTitle("").WithViewport(640,400,24).WithLayout(TopologyLayoutMode.Manual)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .AddNode("source","Start",40,130,width:100,height:60).AddNode("target","Finish",380,130,width:100,height:60)
            .AddEdge("flow","source","target",routing:TopologyEdgeRouting.Orthogonal,direction:VisualLinkDirection.None);
        foreach(var node in chart.Nodes) { node.ShowStatusBadge=false; node.PreserveDisplayModeSize=true; node.Shape=TopologyNodeShape.Rectangle; }
        chart.Edges[0].Waypoints.AddRange(new[] { new ChartPoint(left?-12:660,160), new ChartPoint(left?-12:660,230) });
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeTitle=false,IncludeLegend=false,FitContentToViewport=false,EdgeVisualStyle=ChartLineVisualStyle.Plain() }); return chart;
    }
    private static TopologyChart Generic(TopologyNodeDisplayMode mode,bool shrink) {
        var chart = TopologyChart.Create().WithId("generic-default-units").WithTitle("").WithViewport(shrink?320:640,shrink?240:400,24).WithLayout(TopologyLayoutMode.Layered)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).AddNode("node","Literal native label",50,60,width:shrink?920:150,height:shrink?720:80);
        var node=chart.Nodes[0];node.DisplayMode=mode;node.Kind=TopologyNodeKind.Generic;node.PreserveDisplayModeSize=true;node.ShowStatusBadge=false;node.Symbol="Q";
        chart.WithRenderOptions(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,FitContentToViewport=true,IncludeNodeLabels=true});return chart;
    }
    private static ChartRect[] Clips(PreparedTopology value) => value.Visual.Scene.Nodes.OfType<VisualSceneGroup>().Where(group=>group.Clip.HasValue).Select(group=>group.Clip!.Value).ToArray();
    private static void Capture(string name,PreparedTopology value) => Capture(name,value.Visual);
    private static void Capture(string name,PreparedVisual value) {
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var path=Path.Combine(output,"prepared-clip-unit-"+name);
        File.WriteAllText(path+".svg",value.ToSvg());File.WriteAllBytes(path+".png",value.ToPng());File.WriteAllText(path+".json",value.ToArtifact().ToInterchangeEnvelope().ToJson());
        File.WriteAllText(path+".clips.json",JsonSerializer.Serialize(value.Scene.Nodes.OfType<VisualSceneGroup>().Where(group=>group.Clip.HasValue).Select(group=>group.Clip!.Value)));
    }
}
