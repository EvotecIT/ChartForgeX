using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidTreemapTranslucentClassTests {
    internal static string Source(bool dark) => "treemap-beta\n\"Shell\":::shell\n  \"Section\":::section\n    \"Target\": 4:::target\n"
        + "classDef shell fill:" + (dark ? "#FFFFFF" : "#111827") + "\n"
        + "classDef section fill:" + (dark ? "rgba(255,255,255,0.1)" : "rgba(0,0,0,0.1)") + "\n"
        + "classDef target fill:" + (dark ? "rgba(0,0,0,0.1)" : "rgba(255,255,255,0.1)");

    [Theory]
    [InlineData(390, false)]
    [InlineData(390, true)]
    [InlineData(1100, false)]
    [InlineData(1100, true)]
    public void SourceAlphaClassesComposeThroughExistingHierarchyAndDirectFenceExports(int width, bool dark) {
        var source = Source(dark); var options = MermaidTreemapStyleMappingTests.Options(width, dark);
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        MermaidTreemapStyleMappingTests.Capture("treemap-alpha-class-" + width + "-" + dark, svg, png);
        var chart = Assert.IsType<Chart>(artifact.Model); var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(context);
        var surface = HierarchyCaptionSurfaceTestFacts.FrameSurface(prepared);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(item => item.Role is "treemap-group-mark" or "treemap-tile-mark").ToArray();
        Assert.Equal(3, marks.Length);
        foreach (var mark in marks) surface = ChartColorMath.CompositeOverOpaque(mark.Fill!.Value, surface);
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "treemap-label");
        Assert.True(ChartColorMath.ContrastRatio(caption.Color, surface) >= 4.5);
        Assert.Equal(new double?[] { null, null, 4 }, chart.Series[0].HierarchyItems.Select(item => item.Value));
        Assert.Equal(new string?[] { null, "node-0", "node-1" }, chart.Series[0].HierarchyItems.Select(item => item.ParentId));
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fence.Diagnostics); Assert.Equal(svg, Assert.Single(fence.Artifacts).ToSvg()); Assert.Equal(png, fence.Artifacts[0].ToPng());
        MermaidTreemapStyleMappingTests.AssertDirectAndFencedFacts(artifact, fence.Artifacts[0]);
        var saved = prepared.ToSvg(); var savedPng = prepared.ToPng();
        chart.Series[0].ConfigurePointDataLabelStyle(2, style => style.Color = ChartColor.FromHex("#7C3AED"));
        var changed = chart.Prepare(context);
        Assert.Equal(ChartColor.FromHex("#7C3AED"), Assert.Single(changed.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "treemap-label").Color);
        Assert.Equal(saved, prepared.ToSvg()); Assert.Equal(savedPng, prepared.ToPng());
    }
}
