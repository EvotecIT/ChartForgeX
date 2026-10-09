using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class DetachedVisualArtifactTests {
    [Fact]
    public void CloneSeparatesEditableHostStateAndBorrowsTheProducer() {
        var producer = new CountingSource();
        var source = VisualArtifact.Create("host", VisualArtifactKind.VisualBlock, producer);
        source.RenderSource = producer;
        source.SourceLanguage = VisualArtifactSourceLanguage.Markdown;
        source.Title = "Original";
        source.Subtitle = "Host subtitle";
        source.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        source.NaturalSize = new VisualArtifactSize(80, 40);
        source.PreserveNaturalSize = true;
        source.Accessibility.WithTextAlternative("Original accessible name", "Description", "pl-PL");
        source.Metadata.Add("owner", "source");
        var region = new VisualArtifactRegion {
            Id = "region", Kind = "node", Label = "Original region", Bounds = new ChartRect(1, 2, 3, 4),
            Href = "https://example.com/", AlternativeText = "Alternative"
        };
        region.Metadata.Add("state", "original");
        source.Regions.Add(region);
        source.Legend.Add(new VisualArtifactLegendItem { Id = "legend", Label = "Original legend", Color = "#123456" });

        var copy = source.Clone();
        Assert.NotSame(source, copy);
        Assert.Same(source.Model, copy.Model);
        Assert.Same(source.RenderSource, copy.RenderSource);
        Assert.Equal(source.Id, copy.Id);
        Assert.Equal(source.SourceLanguage, copy.SourceLanguage);
        Assert.Equal(source.Subtitle, copy.Subtitle);
        Assert.Equal(source.ExportFormats, copy.ExportFormats);
        Assert.Equal(source.NaturalSize, copy.NaturalSize);
        Assert.True(copy.PreserveNaturalSize);
        Assert.Equal(region.Bounds, copy.Regions[0].Bounds);
        Assert.Equal(region.Href, copy.Regions[0].Href);
        Assert.Equal(region.AlternativeText, copy.Regions[0].AlternativeText);
        Assert.Equal(source.Legend[0].Color, copy.Legend[0].Color);
        copy.Title = "Copy";
        copy.Accessibility.WithTextAlternative("Copy accessible name", "Copy description", "de-DE").AsDecorative();
        copy.Metadata["owner"] = "copy";
        copy.Regions[0].Label = "Copy region";
        copy.Regions[0].Metadata["state"] = "copy";
        copy.Legend[0].Label = "Copy legend";
        copy.NaturalSize = new VisualArtifactSize(160, 80);
        Assert.Equal("Original", source.Title);
        Assert.Equal("Original accessible name", source.Accessibility.Name);
        Assert.Equal("Description", source.Accessibility.Description);
        Assert.Equal("pl-PL", source.Accessibility.Language);
        Assert.False(source.Accessibility.IsDecorative);
        Assert.Equal("source", source.Metadata["owner"]);
        Assert.Equal("Original region", source.Regions[0].Label);
        Assert.Equal("original", source.Regions[0].Metadata["state"]);
        Assert.Equal("Original legend", source.Legend[0].Label);
        Assert.Equal(80, source.NaturalSize!.Value.Width);
        source.Metadata.Add("later", "source edit");
        source.Regions.Clear();
        source.Legend.Clear();
        Assert.False(copy.Metadata.ContainsKey("later"));
        Assert.Single(copy.Regions);
        Assert.Single(copy.Legend);
        Assert.Equal(0, producer.RenderCalls);
    }

    [Fact]
    public void CloneRetainsPreparedSemanticSnapshotAndViewportValidation() {
        var topology = CreateTopology();
        var semantics = topology.ToVisualArtifact().ToInterchangeEnvelope();
        var prepared = topology.Prepare(new VisualRenderContext(
            layout: new VisualLayoutOptions(new VisualSize(semantics.Width!.Value, semantics.Height!.Value)),
            frame: new VisualFrame(showLegend: false)));
        var source = prepared.ToArtifact(semantics.Id, VisualArtifactKind.Topology, semantics);
        var copy = source.Clone();
        string json = source.ToInterchangeJson();
        semantics.Nodes.Clear();
        Assert.Same(prepared, copy.Model);
        Assert.Equal(json, copy.ToInterchangeJson());
        Assert.Equal(source.ToPng(), copy.ToPng());
        copy.Metadata["host"] = "copy";
        Assert.Equal("copy", copy.ToInterchangeEnvelope().Extensions["host"]);
        Assert.False(source.Metadata.ContainsKey("host"));
        copy.NaturalSize = new VisualArtifactSize(640, 480);
        Assert.Throws<InvalidOperationException>(() => copy.ToSvg());
        Assert.Throws<InvalidOperationException>(() => copy.ToInterchangeEnvelope());
        Assert.Equal(json, source.ToInterchangeJson());
    }

    [Fact]
    public void DetachedWatermarksPreserveTopologySourceAcrossRepeatedExports() {
        var topology = CreateTopology();
        var source = topology.ToVisualArtifact();
        source.PreserveNaturalSize = true;
        source.Accessibility.Language = "pl-PL";
        // Updating the borrowed model after envelope creation must still resolve producer accessibility.
        topology.Accessibility.WithTextAlternative("Updated model name", "Updated model description", "pl-PL");
        var options = new VisualArtifactRenderOptions {
            Topology = new TopologyRenderOptions { IncludeLegend = false, PngOutputScale = 2 },
            Raster = new RasterImageOptions { Dpi = 144 }
        };
        string originalSvg = source.ToSvg(options);
        byte[] originalPng = source.ToPng(options);
        string originalJson = source.ToInterchangeJson();
        var originalSize = source.NaturalSize;
        var originalRegions = source.Regions.ToArray();
        var watermark = VisualWatermark.FromText("FIRST");
        watermark.Anchor = VisualCanvasAnchor.Center;
        watermark.Opacity = 0.6;
        var first = source.ToWatermarkedArtifact(options, watermark);
        var second = source.ToWatermarkedArtifact(options, VisualWatermark.FromText("SECOND"));
        string firstSvg = first.ToSvg(options);
        byte[] firstPng = first.ToPng(options);
        Assert.Contains("FIRST", firstSvg);
        Assert.DoesNotContain("SECOND", firstSvg);
        Assert.Contains("SECOND", second.ToSvg(options));
        Assert.DoesNotContain("FIRST", second.ToSvg(options));
        Assert.Contains("aria-label=\"Updated model name\"", firstSvg);
        Assert.Contains("Updated model description", firstSvg);
        Assert.Contains("<html lang=\"pl-PL\">", first.ToHtmlPage(options));
        Assert.Equal(PaintWithoutWatermarks(originalSvg), PaintWithoutWatermarks(firstSvg));
        Assert.NotEqual(originalPng, firstPng);
        var decoratedEnvelope = VisualArtifactInterchangeEnvelope.FromJson(originalJson);
        decoratedEnvelope.Extensions["presentation.watermarks"] = "1";
        Assert.Equal(decoratedEnvelope.ToJson(), first.ToInterchangeJson());
        Assert.Equal(decoratedEnvelope.ToJson(), second.ToInterchangeJson());
        watermark.OffsetX = 50;
        options.Topology.PngOutputScale = 1;
        Assert.Equal(firstSvg, first.ToSvg());
        Assert.Equal(firstPng, first.ToPng(new VisualArtifactRenderOptions { Raster = new RasterImageOptions { Dpi = 144 } }));
        Assert.Same(topology, source.Model);
        Assert.Null(source.RenderSource);
        Assert.Equal(originalSize, source.NaturalSize);
        Assert.Equal(originalRegions, source.Regions.ToArray());
        options.Topology.PngOutputScale = 2;
        Assert.Equal(originalSvg, source.ToSvg(options));
        Assert.Equal(originalPng, source.ToPng(options));
        Assert.Equal(originalJson, source.ToInterchangeJson());
        Assert.Equal(originalSize, first.NaturalSize);
        Assert.NotSame(source.Regions[0], first.Regions[0]);
        var originalPixels = RasterImageDecoder.Decode(originalPng);
        var decoratedPixels = RasterImageDecoder.Decode(firstPng);
        Assert.Equal(originalPixels.Width, decoratedPixels.Width);
        Assert.Equal(originalPixels.Height, decoratedPixels.Height);
    }

    [Fact]
    public void RenderedSvgDecorationRetainsNativeMotionAndWrapsTheSameMarkup() {
        var motionOptions = TopologyMotionOptions.RoutePulse();
        motionOptions.EdgeIds.Add("request");
        motionOptions.DurationSeconds = 2.5;
        motionOptions.Loop = false;
        var motion = CreateTopology().WithMotion(motionOptions);
        string svg = motion.ToSvg("detached-motion");
        var artifact = new VisualArtifact {
            Id = "motion", Title = "Motion <preview>", Kind = VisualArtifactKind.Topology,
            NaturalSize = new VisualArtifactSize(motion.Width, motion.Height)
        };
        artifact.Accessibility.Language = "pl-PL";
        var watermark = VisualWatermark.FromText("MOTION");
        string decorated = artifact.ToWatermarkedSvg(svg, watermark);
        var originalRoot = XDocument.Parse(svg);
        var decoratedRoot = XDocument.Parse(decorated);
        var animations = originalRoot.Descendants().Where(element => element.Name.LocalName.StartsWith("animate", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(animations);
        Assert.Equal(animations.Select(element => element.ToString()), decoratedRoot.Descendants()
            .Where(element => element.Name.LocalName.StartsWith("animate", StringComparison.Ordinal)).Select(element => element.ToString()));
        Assert.Equal(originalRoot.Descendants().Attributes("id").Select(attribute => attribute.Value),
            decoratedRoot.Descendants().Where(element => !element.AncestorsAndSelf().Any(parent => (string?)parent.Attribute("data-cfx-role") == "watermarks"))
                .Attributes("id").Select(attribute => attribute.Value));
        decoratedRoot.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "watermarks").Remove();
        Assert.Equal(originalRoot.ToString(), decoratedRoot.ToString());
        string html = artifact.ToWatermarkedHtmlPage(svg, watermark);
        Assert.Contains(decorated, html);
        Assert.Contains("<html lang=\"pl-PL\">", html);
        Assert.Contains("<title>Motion &lt;preview&gt;</title>", html);
        Assert.Contains("overflow:hidden", html);
        Assert.Equal(svg, artifact.ToWatermarkedSvg(svg));
        Assert.Null(artifact.Model);
        Assert.Null(artifact.RenderSource);
        Assert.Empty(artifact.Regions);
        Assert.Equal(svg, motion.ToSvg("detached-motion"));
    }

    [Fact]
    public void InvalidWatermarksFailBeforeProducerResolutionOrSourceChanges() {
        var source = new VisualArtifact { Title = "No producer" };
        var mark = VisualWatermark.FromText("VALID");
        var arrayError = Assert.Throws<ArgumentNullException>(() => source.ToWatermarkedArtifact((VisualWatermark[])null!));
        var itemError = Assert.Throws<ArgumentException>(() => source.ToWatermarkedArtifact(mark, null!));
        Assert.Equal("watermarks", arrayError.ParamName);
        Assert.Equal("watermarks", itemError.ParamName);
        Assert.Null(source.Model);
        Assert.Null(source.RenderSource);
        var copy = source.ToWatermarkedArtifact();
        Assert.NotSame(source, copy);
        copy.Metadata["host"] = "independent";
        Assert.Empty(source.Metadata);
        Assert.Throws<ArgumentNullException>(() => VisualWatermarkDecoration.ToWatermarkedArtifact(null!, mark));
        const string svg = "<svg width=\"80\" height=\"40\"></svg>";
        Assert.Throws<ArgumentNullException>(() => source.ToWatermarkedSvg(null!, mark));
        Assert.Throws<ArgumentException>(() => source.ToWatermarkedHtmlPage(svg, mark, null!));
        source.NaturalSize = default(VisualArtifactSize);
        Assert.Throws<ArgumentOutOfRangeException>(() => source.ToWatermarkedSvg(svg, mark));
    }

    [Fact]
    public void MutatingWatermarkApiStillReturnsAndAppendsToTheOriginalArtifact() {
        var source = CreateTopology().ToVisualArtifact();
        Assert.Same(source, source.WithWatermarks());
        Assert.Same(source, source.WithWatermarks(VisualWatermark.FromText("FIRST")));
        Assert.Same(source, source.WithWatermarks(VisualWatermark.FromText("SECOND")));
        string svg = source.ToSvg();
        Assert.Contains("FIRST", svg);
        Assert.Contains("SECOND", svg);
        Assert.Equal(2, XDocument.Parse(svg).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "watermarks"));
    }

    private static TopologyChart CreateTopology() => TopologyChart.Create().WithViewport(320, 180).WithLegend(null)
        .WithLayout(TopologyLayoutMode.Matrix).WithTitle("Detached topology")
        .AddAutoNode("api", "API").AddAutoNode("worker", "Worker").AddAutoNode("data", "Data")
        .AddEdge("request", "api", "worker").AddEdge("storage", "worker", "data");

    private static string PaintWithoutWatermarks(string svg) {
        var root = XDocument.Parse(svg);
        root.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "watermarks").Remove();
        foreach (var attribute in root.Descendants().Attributes().Where(attribute => attribute.Name.LocalName is "id" or "clip-path" or "aria-labelledby" or "aria-describedby").ToArray()) attribute.Remove();
        return root.ToString(SaveOptions.DisableFormatting);
    }

    private sealed class CountingSource : IStaticVisualSource {
        internal int RenderCalls { get; private set; }
        public string RenderSvg(string idScope) { RenderCalls++; return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"80\" height=\"40\"></svg>"; }
        public RgbaImage RenderRgba() { RenderCalls++; return new RgbaImage(80, 40, new byte[80 * 40 * 4]); }
    }
}
