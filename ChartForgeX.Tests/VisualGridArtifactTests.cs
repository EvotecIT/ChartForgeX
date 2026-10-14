using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualGridArtifactTests {
    [Fact]
    public void CreatingAnEnvelopeMeasuresLogicalSizeWithoutRenderingChildren() {
        var child = new DeferredBlock();
        var grid = VisualGrid.Create().WithColumns(1).WithPadding(10).WithPngOutputScale(3).Add(child);

        var artifact = grid.ToVisualArtifact("  report-grid  ", VisualArtifactSourceLanguage.Markdown);

        Assert.Same(grid, artifact.Model);
        Assert.Equal("report-grid", artifact.Id);
        Assert.Equal(VisualArtifactKind.ChartGrid, artifact.Kind);
        Assert.Equal(VisualArtifactSourceLanguage.Markdown, artifact.SourceLanguage);
        Assert.Equal(new VisualArtifactSize(140, 90), artifact.NaturalSize);
        Assert.Equal("1", artifact.Metadata["visual-grid.items"]);
        Assert.Equal("1", artifact.Metadata["visual-grid.columns"]);
        Assert.Equal("VisualGrid", artifact.Metadata["render.model"]);
        Assert.Equal("ChartForgeX visual grid", artifact.Accessibility.Name);
        Assert.True(artifact.SupportsExport(VisualArtifactExportFormat.Office));
        Assert.Equal(0, child.RenderCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MixedSpanningGridsPreserveLogicalSizeAndPortableExports(bool adaptiveRows) {
        var chart = Chart.Create().WithSize(180, 110).WithTitle("Requests");
        chart.AddLine("Completed", ChartPoints.FromValues(3, 7, 5));
        var grid = VisualGrid.Create().WithTitle("Service summary").WithSubtitle("Charts and facts")
            .WithColumns(2).WithPadding(12).WithGap(9).WithAdaptiveRowHeights(adaptiveRows)
            .WithTheme(VisualTheme.Graphite(), VisualThemeMode.Dark).WithPngOutputScale(2)
            .Add("requests", chart, columnSpan: 2)
            .Add("ready", MetricCard.Create().WithMetric("Ready", "12").WithSize(120, 70))
            .Add("waiting", MetricCard.Create().WithMetric("Waiting", "3").WithSize(150, 90));
        var artifact = grid.ToVisualArtifact();

        var svg = XDocument.Parse(artifact.ToSvg()).Root!;
        double width = double.Parse(svg.Attribute("width")!.Value, CultureInfo.InvariantCulture);
        double height = double.Parse(svg.Attribute("height")!.Value, CultureInfo.InvariantCulture);
        var image = RasterImageDecoder.Decode(artifact.ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());

        Assert.Equal("visual-grid", artifact.Id);
        Assert.Equal(grid.Title, artifact.Title);
        Assert.Equal(grid.Subtitle, artifact.Subtitle);
        Assert.Equal(grid.Title, artifact.Accessibility.Name);
        Assert.Equal("3", artifact.Metadata["visual-grid.items"]);
        Assert.Equal("2", artifact.Metadata["visual-grid.columns"]);
        Assert.Equal(new VisualArtifactSize(width, height), artifact.NaturalSize);
        Assert.Equal((int)width * 2, image.Width);
        Assert.Equal((int)height * 2, image.Height);
        Assert.Equal(width, envelope.Width);
        Assert.Equal(height, envelope.Height);
        Assert.Equal(VisualArtifactInterchangeFamily.None, envelope.Family);
        Assert.Equal(grid.Title, envelope.AccessibleName);
        Assert.Contains("Completed", svg.Value);
        Assert.Contains("Ready", svg.Value);
        Assert.Contains("Waiting", svg.Value);
        Assert.Contains("<!doctype html>", artifact.ToHtmlPage(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidGridsFailBeforeAnEnvelopeCanClaimANaturalSize() {
        Assert.Throws<ArgumentNullException>(() => VisualGridArtifactRendering.ToVisualArtifact(null!));
        Assert.Throws<InvalidOperationException>(() => VisualGrid.Create().ToVisualArtifact());
    }

    private sealed class DeferredBlock : IVisualBlock {
        public string Title => "Deferred";
        public string Subtitle => string.Empty;
        public string AccessibleName => Title;
        public VisualBlockOptions Options { get; } = new() { Size = new ChartSize(120, 70) };
        public int RenderCalls { get; private set; }
        public string RenderSvg(string idScope) { RenderCalls++; throw new InvalidOperationException("Rendering is deferred."); }
        public RgbaImage RenderRgba() { RenderCalls++; throw new InvalidOperationException("Rendering is deferred."); }
    }
}
