using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyRegionPreparedFitTests {
    [Theory]
    [InlineData(.45, false)]
    [InlineData(1.5, false)]
    [InlineData(.45, true)]
    [InlineData(1.5, true)]
    public void OutputFittingKeepsTheDetachedRegionLayoutAndUniformPrimitiveScale(double factor, bool fitContent) {
        var chart = TopologyHierarchyExamples.CreateTopology(1440, false);
        var policy = new TopologyRenderOptions { FitContentToViewport = fitContent };
        var prepared = chart.Prepare(policy);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var semantics = prepared.ToInterchangeEnvelope().ToJson();
        var before = prepared.ToInterchangeEnvelope();
        var fitted = prepared.WithOutputSize(prepared.Width * factor, prepared.Height * factor);
        var after = fitted.ToInterchangeEnvelope();
        Assert.Equal(prepared.Width * factor, fitted.Width, 5); Assert.Equal(prepared.Height * factor, fitted.Height, 5);
        Assert.Equal(before.Groups.Count, after.Groups.Count);
        var actualScale = fitContent ? factor : Math.Min(1, factor);
        for (var i = 0; i < before.Groups.Count; i++) {
            Assert.Equal(before.Groups[i].X!.Value * actualScale, after.Groups[i].X!.Value, 5);
            Assert.Equal(before.Groups[i].Y!.Value * actualScale, after.Groups[i].Y!.Value, 5);
            Assert.Equal(before.Groups[i].Width!.Value * actualScale, after.Groups[i].Width!.Value, 5);
        }
        Assert.DoesNotContain("Links to other regions", fitted.ToSvg());
        var firstLabel = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().First();
        var fittedLabel = fitted.Visual.Scene.Nodes.OfType<VisualSceneText>().First();
        Assert.Equal(firstLabel.Text.Face, fittedLabel.Text.Face);
        Assert.Equal(firstLabel.Text.Size * actualScale, fittedLabel.Text.Size, 5);
        var firstPath = prepared.Visual.Scene.Nodes.OfType<VisualScenePath>().First();
        var fittedPath = fitted.Visual.Scene.Nodes.OfType<VisualScenePath>().First();
        Assert.Equal(firstPath.StrokeWidth * actualScale, fittedPath.StrokeWidth, 5);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(semantics, prepared.ToInterchangeEnvelope().ToJson());
        Assert.True(fitted.ToPng().Length > 1000);
        chart.Groups[0].Width = 500;
        Assert.NotEqual(before.Groups[0].Width, chart.Prepare(policy).ToInterchangeEnvelope().Groups[0].Width);
    }
}
