using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ArtifactHostHandoffTests {
    [Theory]
    [InlineData("alternate", null, "alternate")]
    [InlineData("node-only", null, "delivery")]
    [InlineData("missing", null, "delivery")]
    [InlineData("alternate", "delivery", "delivery")]
    public void InteractiveMotionRetainsRouteSelectionPreferenceAndNormalFallback(string preference, string? explicitScenario, string selected) {
        var chart = TopologyFixture()
            .AddNode("other", "Other", 260, 160, width: 100, height: 60)
            .AddEdge("alternate-route", "source", "other", routing: TopologyEdgeRouting.Straight)
            .AddScenario("alternate", "Alternate", scenario => scenario.AddEdgeStep("alternate-route"))
            .AddScenario("node-only", "Node only", scenario => scenario.AddNodeStep("source"));
        var motion = new TopologyMotionOptions { ScenarioId = explicitScenario };
        var options = new TopologyRenderOptions { ActiveScenarioId = preference };
        string? svg = null;
        chart.ToInteractiveHtmlPage(options, prepared => {
            svg = prepared.WithMotion(motion, preference).ToSvg();
            return svg;
        });
        Assert.Contains("data-cfx-motion-source=\"" + selected + "\"", svg);
        Assert.Equal(preference, options.ActiveScenarioId);
        Assert.Equal(explicitScenario, motion.ScenarioId);
        Assert.Empty(motion.EdgeIds);
    }

    [Fact]
    public void RepeatedImageWatermarkDefinitionsAreLocalAcrossScopesAndLayers() {
        var image = VisualWatermark.FromImage(PngWriter.WriteRgba(new RgbaImage(1, 1, new byte[] { 30, 80, 140, 255 })), "image/png");
        image.Repeat = true;
        var chart = ChartFixture();
        var artifact = chart.ToVisualArtifact().WithWatermarks(image, image).WithWatermarks(image);
        var first = XDocument.Parse(artifact.ToSvg(null, "image-first"));
        var second = XDocument.Parse(artifact.ToSvg(null, "image-second"));
        AssertLocalReferences(first);
        AssertLocalReferences(second);
        Assert.Equal(3, first.Descendants().Count(e => e.Name.LocalName == "symbol"));
        Assert.Empty(Ids(first).Intersect(Ids(second), StringComparer.Ordinal));
        Assert.Equal(artifact.ToSvg(null, "image-first"), artifact.ToSvg(null, "image-first"));
    }

    [Theory]
    [InlineData(1000, 1000)]
    [InlineData(-40, 25)]
    public void SvgWatermarksHonorTheAuthoredViewBoxOrigin(int x, int y) {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"" + x + " " + y + " 100 100\"></svg>";
        var decorated = XDocument.Parse(VisualWatermarkDecoration.ApplyToSvg(svg, VisualWatermark.FromText("CENTER")));
        var layer = decorated.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "watermarks");
        Assert.Equal("translate(" + x + " " + y + ")", (string?)layer.Attribute("transform"));
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("0 0 -100 100")]
    [InlineData("NaN 0 100 100")]
    [InlineData("0 0 Infinity 100")]
    [InlineData("0 0 100 100 100")]
    public void SvgWatermarksUseNumericDimensionsWhenViewBoxIsUnusable(string viewBox) {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\" viewBox=\"" + viewBox + "\"></svg>";
        var decorated = XDocument.Parse(VisualWatermarkDecoration.ApplyToSvg(svg, VisualWatermark.FromText("CENTER")));
        var layer = decorated.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "watermarks");
        Assert.Null(layer.Attribute("transform"));
        Assert.Contains(layer.Descendants(), e => e.Name.LocalName == "text" && e.Value == "CENTER");
    }

    [Fact]
    public void InteractiveMotionPreservesLabelsAndGroupsNeededByInitiallyDisabledControls() {
        var chart = TopologyFixture().WithLayout(TopologyLayoutMode.RelationshipRadial)
            .AddGroup("cluster", "Cluster", 0, 0, 400, 240);
        chart.Edges[0].Label = "Traffic label";
        var options = new TopologyRenderOptions {
            IncludeEdgeLabels = false, IncludeGroups = false, EnableHtmlForceGraphControls = true,
            ActiveScenarioId = "delivery"
        };
        var motion = new TopologyMotionOptions { ScenarioId = "delivery" };
        string? svg = null;
        var html = chart.ToInteractiveHtmlPage(options, prepared => {
            svg = prepared.WithMotion(motion, options.ActiveScenarioId).ToSvg();
            return svg;
        });
        var document = XDocument.Parse(svg!);
        Assert.Contains(document.Descendants(), e => e.Name.LocalName == "text" && e.Value.Contains("Traffic label", StringComparison.Ordinal));
        Assert.Contains(document.Descendants(), e => e.Name.LocalName == "text" && e.Value.Contains("Cluster", StringComparison.Ordinal));
        Assert.False(options.IncludeEdgeLabels);
        Assert.False(options.IncludeGroups);
        Assert.Equal("delivery", options.ActiveScenarioId);
    }

    [Fact]
    public void CloneSeparatesMutableHostStateAndDecorationFromCapturedSemantics() {
        var source = ChartFixture().Prepare(VisualExportRequest.ForChart(ChartFixture()).Context).ToArtifact("original");
        source.Metadata["owner"] = "source";
        source.Regions[0].Metadata["detail"] = "source region";
        source.Legend.Add(new VisualArtifactLegendItem { Id = "cpu", Label = "CPU", Color = "#0891b2" });
        var svg = source.ToSvg();
        var png = source.ToPng();
        var copy = source.Clone();
        Assert.Same(source.Model, copy.Model);
        Assert.Same(source.RenderSource, copy.RenderSource);
        Assert.Equal(source.ToInterchangeUtf8Json(), copy.ToInterchangeUtf8Json());
        copy.Metadata["owner"] = "copy";
        copy.Accessibility.Name = "Copy title";
        copy.Regions[0].Label = "Copy region";
        copy.Regions[0].Metadata["detail"] = "copy region";
        copy.Legend[0].Label = "Copy legend";
        copy.WithWatermarks(VisualWatermark.FromText("COPY"));
        Assert.Equal("source", source.Metadata["owner"]);
        Assert.Equal("source region", source.Regions[0].Metadata["detail"]);
        Assert.NotEqual("Copy region", source.Regions[0].Label);
        Assert.Equal("CPU", source.Legend[0].Label);
        Assert.NotEqual("Copy title", source.Accessibility.Name);
        Assert.Null(source.RenderSource);
        Assert.Equal(svg, source.ToSvg());
        Assert.Equal(png, source.ToPng());
        Assert.Contains("COPY", copy.ToSvg());
        Assert.Equal("copy", copy.ToInterchangeEnvelope().Extensions["owner"]);
        Assert.Equal("1", copy.ToInterchangeEnvelope().Extensions["presentation.watermarks"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WatermarkFactsFollowAppliedLayersAcrossPortableHandoff(bool prepared) {
        var topology = TopologyFixture();
        var artifact = prepared ? topology.Prepare().Visual.ToArtifact() : topology.ToVisualArtifact();
        var before = artifact.ToInterchangeEnvelope();
        artifact.WithWatermarks(VisualWatermark.FromText("FIRST"), VisualWatermark.FromText("SECOND"));
        artifact.Metadata["presentation.watermarks"] = "99";
        artifact.WithWatermarks(VisualWatermark.FromText("THIRD"));
        Assert.Equal("3", artifact.Metadata["presentation.watermarks"]);
        var portable = VisualArtifactInterchangeEnvelope.FromUtf8Json(artifact.ToInterchangeUtf8Json());
        Assert.Equal("3", portable.Extensions["presentation.watermarks"]);
        var detached = artifact.ToWatermarkedArtifact(VisualWatermark.FromText("FOURTH"));
        Assert.Equal("4", detached.ToInterchangeEnvelope().Extensions["presentation.watermarks"]);
        Assert.Equal("3", artifact.Metadata["presentation.watermarks"]);
        Assert.Equal(before.Nodes.Select(n => n.Id), portable.Nodes.Select(n => n.Id));
        Assert.Equal(before.Edges.Select(e => e.Id), portable.Edges.Select(e => e.Id));
        var rendered = artifact.ToSvg();
        Assert.True(rendered.IndexOf("FIRST", StringComparison.Ordinal) < rendered.IndexOf("SECOND", StringComparison.Ordinal));
        Assert.True(rendered.IndexOf("SECOND", StringComparison.Ordinal) < rendered.IndexOf("THIRD", StringComparison.Ordinal));
        var source = artifact.RenderSource;
        Assert.Throws<ArgumentException>(() => artifact.WithWatermarks(VisualWatermark.FromText("VALID"), null!));
        Assert.Same(source, artifact.RenderSource);
        Assert.Equal(rendered, artifact.ToSvg());
    }

    [Fact]
    public void PreparedIntakeInfersCapturedFamilyWithoutInferringNativeDataFromSceneCommands() {
        var topology = TopologyFixture().Prepare().Visual;
        var artifact = topology.ToArtifact();
        Assert.Equal(VisualArtifactKind.Topology, artifact.Kind);
        Assert.Equal("host-route", artifact.Id);
        Assert.Equal(2, artifact.ToInterchangeEnvelope().Nodes.Count);
        Assert.Equal("new-host", topology.ToArtifact("new-host").ToInterchangeEnvelope().Id);
        Assert.Equal("host-route", topology.ToArtifact().Id);
        Assert.Throws<ArgumentException>(() => topology.ToArtifact(" "));
        var plain = new PreparedVisual(topology.Scene);
        var unknown = plain.ToArtifact();
        Assert.Equal(VisualArtifactKind.Unknown, unknown.Kind);
        Assert.Equal("prepared-visual", unknown.Id);
        Assert.False(unknown.SupportsExport(VisualArtifactExportFormat.Json));
        Assert.Equal(plain.ToPng(), unknown.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScopedArtifactEmbedsKeepEveryReferenceLocal(bool prepared) {
        var chart = ChartFixture();
        var artifact = prepared ? chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToArtifact() : chart.ToVisualArtifact();
        artifact.WithWatermarks(VisualWatermark.FromText("INTERNAL"));
        var first = XDocument.Parse(artifact.ToSvg(null, "first"));
        var second = XDocument.Parse(artifact.ToSvg(null, "second"));
        AssertLocalReferences(first);
        AssertLocalReferences(second);
        Assert.Empty(Ids(first).Intersect(Ids(second), StringComparer.Ordinal));
        Assert.Throws<ArgumentException>(() => artifact.ToSvg(null, " "));
    }

    [Fact]
    public void OptionalSvgDecorationAndInteractiveHostPreserveDetachedMotion() {
        var chart = TopologyFixture();
        var options = new TopologyRenderOptions { FitContentToViewport = true, EnableHtmlViewportControls = true };
        var motion = chart.WithMotion(new TopologyMotionOptions { ScenarioId = "delivery" }, options);
        var svg = motion.ToSvg();
        var decorated = VisualWatermarkDecoration.ApplyToSvg(svg, VisualWatermark.FromText("INTERNAL"));
        var original = XDocument.Parse(svg);
        var result = XDocument.Parse(decorated);
        Assert.Equal(original.Descendants().Count(e => e.Name.LocalName == "animateMotion"),
            result.Descendants().Count(e => e.Name.LocalName == "animateMotion"));
        Assert.Contains(result.Descendants(), e => e.Name.LocalName == "animateMotion");
        Assert.Contains("INTERNAL", result.Root!.Value);
        AssertLocalReferences(result);
        var html = motion.ToHtmlPage(value => VisualWatermarkDecoration.ApplyToSvg(value, VisualWatermark.FromText("INTERNAL")));
        Assert.Contains(decorated, html);
        Assert.DoesNotContain("<script", html);
        string? interactiveSvg = null;
        var interactive = chart.ToInteractiveHtmlPage(options, prepared => {
            interactiveSvg = prepared.WithMotion(new TopologyMotionOptions { ScenarioId = "delivery" }).ToSvg();
            return interactiveSvg;
        });
        Assert.Contains(interactiveSvg!, interactive);
        Assert.Contains("data-cfx-topology-zoom", interactive);
        Assert.Contains("<script", interactive);
        Assert.False(options.EnableHtmlInteractions);
        Assert.Equal(svg, motion.ToSvg());
        Assert.Throws<InvalidOperationException>(() => motion.ToHtmlPage(_ => " "));
        Assert.Throws<InvalidOperationException>(() => chart.ToInteractiveHtmlPage(options, _ => " "));
        Assert.Throws<InvalidOperationException>(() => VisualWatermarkDecoration.ApplyToSvg("<svg/>", VisualWatermark.FromText("BAD")));
    }

    private static Chart ChartFixture() => Chart.Create().WithTitle("CPU")
        .AddLine("CPU", new[] { new ChartPoint(0, 20), new ChartPoint(1, 45) });

    private static TopologyChart TopologyFixture() => TopologyChart.Create().WithId("host-route")
        .WithViewport(420, 260).WithLayout(TopologyLayoutMode.Manual)
        .AddNode("source", "Source", 30, 80, width: 100, height: 60)
        .AddNode("target", "Target", 260, 80, width: 100, height: 60)
        .AddEdge("route", "source", "target", routing: TopologyEdgeRouting.Straight)
        .AddScenario("delivery", "Delivery", scenario => scenario.AddEdgeStep("route"));

    private static HashSet<string> Ids(XDocument document) => new(document.Descendants().Attributes("id").Select(a => a.Value), StringComparer.Ordinal);

    private static void AssertLocalReferences(XDocument document) {
        var ids = Ids(document);
        Assert.NotEmpty(ids);
        Assert.Equal(document.Descendants().Attributes("id").Count(), ids.Count);
        foreach (var attribute in document.Descendants().Attributes()) {
            if (attribute.Name.LocalName == "href" && attribute.Value.StartsWith("#", StringComparison.Ordinal))
                Assert.Contains(attribute.Value.Substring(1), ids);
            if (attribute.Name.LocalName == "aria-labelledby" || attribute.Name.LocalName == "aria-describedby")
                foreach (var id in attribute.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)) Assert.Contains(id, ids);
            foreach (Match match in Regex.Matches(attribute.Value, @"url\(#([^\)]+)\)")) Assert.Contains(match.Groups[1].Value, ids);
        }
    }
}
