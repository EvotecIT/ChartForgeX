using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TableArtifactDeclaresNativeHostCapabilities() {
        var table = TableArtifact.Create("services")
            .WithTitle("Service Inventory")
            .WithSubtitle("Native-host table contract")
            .WithCapabilities(
                TableArtifactCapabilities.Search |
                TableArtifactCapabilities.Sort |
                TableArtifactCapabilities.Filter |
                TableArtifactCapabilities.MultiSelection |
                TableArtifactCapabilities.Copy |
                TableArtifactCapabilities.Export |
                TableArtifactCapabilities.Virtualization)
            .AddColumn("name", "Name")
            .AddColumn("status", "Status", TableArtifactColumnType.Status)
            .AddColumn("latency", "Latency", TableArtifactColumnType.Number, TextAlignment.Right)
            .AddRow("api", "API", "Healthy", 24)
            .AddRow("worker", "Worker", "Warning", 91);

        table.Columns[1].Metadata["facet"] = "health";
        table.WithRow(1, row => {
            row.Status = VisualStatus.Warning;
            row.Metadata["source"] = "probe";
            row.Cells[1].Status = VisualStatus.Warning;
        });

        Assert(table.Supports(TableArtifactCapabilities.Search), "TableArtifact should declare full-table search capability.");
        Assert(table.Supports(TableArtifactCapabilities.Virtualization), "TableArtifact should declare virtualization capability.");
        Assert(table.SupportsExport(VisualArtifactExportFormat.Csv), "TableArtifact should declare data export formats independently from static previews.");
        Assert(table.Columns[2].Alignment == TextAlignment.Right, "TableArtifact columns should preserve static preview alignment hints.");
        Assert(table.Columns[1].Metadata["facet"] == "health", "TableArtifact columns should preserve host metadata.");
        Assert(table.Rows[1].Status == VisualStatus.Warning, "TableArtifact rows should carry status for selection and preview hosts.");
        Assert(table.Rows[1].Cells[1].Status == VisualStatus.Warning, "TableArtifact cells should carry status for selection and preview hosts.");

        var artifact = table.ToVisualArtifact();
        Assert(artifact.Kind == VisualArtifactKind.Table, "TableArtifact should wrap into a product-neutral visual artifact envelope.");
        Assert(artifact.Model == table, "VisualArtifact should keep the typed table model for native hosts.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Png), "VisualArtifact should expose table preview export capabilities.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Html), "VisualArtifact should expose table HTML preview export support.");
        Assert(artifact.NaturalSize.HasValue && artifact.NaturalSize.Value.Width == 760 && artifact.NaturalSize.Value.Height == 360, "VisualArtifact should expose the table preview's known natural size.");
        Assert(artifact.Metadata["table.capabilities"].Contains("Virtualization", StringComparison.Ordinal), "VisualArtifact metadata should expose declared table capabilities.");
    }

    private static void TableArtifactRendersStaticPreviewThroughVisualBlocks() {
        var table = TableArtifact.Create("accounts")
            .WithTitle("Accounts")
            .WithSubtitle("Static table preview")
            .AddColumn("displayName", "Display name")
            .AddColumn("state", "State", TableArtifactColumnType.Status)
            .AddRow("a", "Ada Lovelace", "Enabled")
            .AddRow("g", "Grace Hopper", "Disabled")
            .WithRow(1, row => {
                row.Status = VisualStatus.Negative;
                row.Cells[1].Status = VisualStatus.Negative;
            });

        var block = table.ToPreviewBlock();
        var svg = table.ToSvg();
        var png = table.ToPng();

        Assert(block.Title == "Accounts", "TableArtifact preview should preserve the table title.");
        Assert(svg.Contains("data-cfx-role=\"table-header\"", StringComparison.Ordinal), "TableArtifact static preview should reuse ChartTable SVG rendering.");
        Assert(svg.Contains("data-cfx-role=\"table-status\"", StringComparison.Ordinal), "TableArtifact static preview should surface row and cell status.");
        Assert(png.Length > 64, "TableArtifact static preview should render PNG output.");

        var artifact = table.ToVisualArtifact();
        Assert(artifact.ToSvg().Contains("data-cfx-role=\"table-header\"", StringComparison.Ordinal), "VisualArtifact SVG rendering should reuse the shared artifact renderer.");
        Assert(artifact.ToHtmlPage().Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase), "VisualArtifact HTML rendering should emit a standalone table preview page.");
        Assert(artifact.ToPng().Length > 64, "VisualArtifact PNG rendering should reuse the shared artifact renderer.");
        using var temp = new TemporaryDirectory();
        artifact.SaveSvg(Path.Combine(temp.Path, "table.svg"));
        artifact.SaveHtml(Path.Combine(temp.Path, "table.html"));
        artifact.SavePng(Path.Combine(temp.Path, "table.png"));
        Assert(File.Exists(Path.Combine(temp.Path, "table.svg")) && File.Exists(Path.Combine(temp.Path, "table.html")) && File.Exists(Path.Combine(temp.Path, "table.png")), "VisualArtifact save helpers should write static output files.");
    }

    private static void VisualArtifactWatermarksStayAlignedAcrossStaticFormats() {
        var table = TableArtifact.Create("watermarked-table")
            .WithTitle("Quarterly Review")
            .AddColumn("name", "Name")
            .AddColumn("state", "State")
            .AddRow("api", "API", "Healthy")
            .AddRow("worker", "Worker", "Warning");
        var artifact = table.ToVisualArtifact();
        artifact.Accessibility.Language = "pl-PL";
        var watermark = VisualWatermark.FromText("CONFIDENTIAL");
        watermark.Anchor = ChartForgeX.Composition.VisualCanvasAnchor.Center;
        watermark.RotationDegrees = -28;
        watermark.Opacity = 0.24;
        watermark.Scale = 1.15;
        var options = new VisualArtifactRenderOptions {
            Raster = new RasterImageOptions { Dpi = 144 }
        };
        options.Watermarks.Add(watermark);

        var svg = artifact.ToSvg(options);
        var html = artifact.ToHtmlPage(options);
        var png = artifact.ToPng(options);
        var plain = RasterImageDecoder.Decode(artifact.ToPng());
        var decorated = RasterImageDecoder.Decode(png);

        Assert(svg.Contains("data-cfx-role=\"watermark\"", StringComparison.Ordinal), "Artifact SVG should expose a host-inspectable watermark layer.");
        Assert(svg.Contains("CONFIDENTIAL", StringComparison.Ordinal) && svg.Contains("rotate(-28", StringComparison.Ordinal), "Artifact SVG should preserve text and rotation.");
        Assert(html.Contains("data-cfx-role=\"watermark\"", StringComparison.Ordinal), "Artifact HTML should embed the same decorated SVG contract.");
        Assert(html.Contains(".chartforgex-visual-artifact svg{display:block;max-width:100%;height:auto;overflow:hidden}", StringComparison.Ordinal), "Watermarked artifact HTML should clip rotated and repeated SVG marks to the root viewport.");
        Assert(html.Contains("<html lang=\"pl-PL\">", StringComparison.Ordinal), "Watermarked artifact HTML should preserve the envelope language.");
        Assert(Encoding.ASCII.GetString(png).Contains("pHYs", StringComparison.Ordinal), "Artifact PNG should encode requested physical DPI metadata.");
        Assert(!plain.Pixels.SequenceEqual(decorated.Pixels), "Artifact PNG watermarking should modify visible pixels.");

        var interactiveTopologyArtifact = TopologyChart.Create()
            .WithViewport(320, 180)
            .WithLegend(null)
            .AddNode("a", "A", 30, 50)
            .AddNode("b", "B", 210, 50)
            .AddEdge("a-b", "a", "b")
            .ToVisualArtifact();
        var interactiveWatermarkOptions = new VisualArtifactRenderOptions {
            Topology = new TopologyRenderOptions { EnableHtmlInteractions = true }
        };
        interactiveWatermarkOptions.Watermarks.Add(VisualWatermark.FromText("STATIC"));
        AssertThrows<InvalidOperationException>(() => interactiveTopologyArtifact.ToHtmlPage(interactiveWatermarkOptions), "Watermarked topology HTML should reject interaction requests through the same adapter ownership boundary as ordinary topology HTML.");

        AssertThrows<ArgumentException>(() => VisualWatermark.FromText(" "), "Text watermarks should reject empty content.");
        AssertThrows<ArgumentOutOfRangeException>(() => watermark.Opacity = 1.1, "Watermarks should reject opacity outside the unit interval.");
        AssertThrows<ArgumentOutOfRangeException>(() => watermark.OffsetX = double.NaN, "Watermarks should reject non-finite horizontal offsets at assignment time.");
        AssertThrows<ArgumentOutOfRangeException>(() => watermark.OffsetY = double.PositiveInfinity, "Watermarks should reject non-finite vertical offsets at assignment time.");
        AssertThrows<ArgumentOutOfRangeException>(() => watermark.RepeatSpacingX = 0.000001, "Repeated watermarks should reject sub-pixel spacing that can create unbounded render loops.");
        AssertThrows<ArgumentOutOfRangeException>(() => new RasterImageOptions { Dpi = 0 }, "Raster options should reject non-positive DPI metadata.");
        AssertThrows<ArgumentOutOfRangeException>(() => new RasterImageOptions { Dpi = 0.001 }, "Raster options should reject DPI values that round to zero PNG pixels per meter.");
        AssertThrows<ArgumentOutOfRangeException>(() => new RasterImageOptions { Dpi = double.MaxValue }, "Raster options should reject DPI values above the PNG density range at assignment time.");
        var maximumDensityOptions = new RasterImageOptions { Dpi = uint.MaxValue * 0.0254D };

        var denseWatermark = VisualWatermark.FromText("DENSE");
        denseWatermark.Repeat = true;
        denseWatermark.RepeatSpacingX = 1;
        denseWatermark.RepeatSpacingY = 1;
        var denseOptions = new VisualArtifactRenderOptions();
        denseOptions.Watermarks.Add(denseWatermark);
        AssertThrows<InvalidOperationException>(() => artifact.ToSvg(denseOptions), "Repeated watermark rendering should reject configurations that exceed the bounded mark count.");

        var pixel = new RgbaImage(1, 1, new byte[] { 10, 20, 30, 255 });
        var pngBytes = PngWriter.WriteRgba(pixel);
        Assert(PngWriter.WriteRgba(pixel, maximumDensityOptions).Length > pngBytes.Length, "The maximum accepted DPI should encode successfully as PNG density metadata.");
        AssertThrows<ArgumentOutOfRangeException>(() => VisualWatermarkRendering.CalculateRotatedWatermarkAllocation(8192, 8192, 45), "Rotated watermark intermediates should honor the deterministic per-canvas allocation ceiling.");
        var oversizedWatermark = VisualWatermark.FromImage(pngBytes, "image/png");
        oversizedWatermark.Width = double.MaxValue;
        AssertThrows<ArgumentOutOfRangeException>(() => VisualWatermarkRendering.ApplyToImage(pixel, new[] { oversizedWatermark }), "Watermark dimensions should be range-checked before integer conversion or allocation.");
        Assert(VisualWatermark.FromImage(pngBytes, "image/png").ImageMimeType == "image/png", "Image watermarks should preserve a validated canonical media type.");
        var repeatedImage = VisualWatermark.FromImage(pngBytes, "image/png");
        repeatedImage.Repeat = true;
        repeatedImage.RepeatSpacingX = 80;
        repeatedImage.RepeatSpacingY = 60;
        repeatedImage.Width = 18;
        repeatedImage.Height = 18;
        var repeatedImageOptions = new VisualArtifactRenderOptions();
        repeatedImageOptions.Watermarks.Add(repeatedImage);
        var repeatedImageSvg = artifact.ToSvg(repeatedImageOptions);
        Assert(CountOccurrences(repeatedImageSvg, ";base64,") == 1 && CountOccurrences(repeatedImageSvg, "<use data-cfx-role=\"watermark\"") > 1, "Repeated SVG image watermarks should define their payload once and reuse it for every placement.");
        Assert(artifact.ToPng(repeatedImageOptions).Length > 64, "Repeated image watermarks should retain PNG parity.");

        var widePixels = new byte[20 * 10 * 4];
        for (var index = 0; index < widePixels.Length; index += 4) {
            widePixels[index] = 255;
            widePixels[index + 3] = 255;
        }
        var wideImage = new RgbaImage(20, 10, widePixels);
        var portableWatermark = VisualWatermark.FromImage(PpmWriter.WriteRgba(wideImage), "image/x-portable-pixmap");
        portableWatermark.Anchor = VisualCanvasAnchor.Center;
        portableWatermark.Width = 80;
        portableWatermark.Height = 80;
        portableWatermark.Opacity = 1;
        var portableOptions = new VisualArtifactRenderOptions();
        portableOptions.Watermarks.Add(portableWatermark);
        var portableSvg = artifact.ToSvg(portableOptions);
        Assert(portableSvg.Contains("data:image/png;base64,", StringComparison.Ordinal) && !portableSvg.Contains("image/x-portable-pixmap", StringComparison.Ordinal), "SVG watermarks should transcode accepted non-web raster inputs to browser-safe PNG data URIs.");

        var contained = VisualWatermarkRendering.ApplyToImage(new RgbaImage(100, 100, new byte[100 * 100 * 4]), new[] { portableWatermark });
        Assert(contained.Pixels[(15 * contained.Width + 50) * 4 + 3] == 0, "PNG watermark rendering should preserve wide-image aspect ratio inside a square target box.");
        Assert(contained.Pixels[(50 * contained.Width + 50) * 4] >= 250 && contained.Pixels[(50 * contained.Width + 50) * 4 + 3] == 255, "PNG watermark rendering should center contained image pixels in the target box.");

        var topology = TopologyChart.Create()
            .WithViewport(240, 140, 16)
            .WithLegend(null)
            .AddNode("left", "Left", 24, 44)
            .AddNode("right", "Right", 152, 44)
            .AddEdge("left-right", "left", "right");
        var topologyArtifact = topology.ToVisualArtifact();
        var scaleWatermark = VisualWatermark.FromText("SCALE");
        scaleWatermark.Anchor = VisualCanvasAnchor.Center;
        scaleWatermark.FontSize = 20;
        scaleWatermark.Opacity = 1;
        var scaleOneOptions = new VisualArtifactRenderOptions { Topology = new TopologyRenderOptions { IncludeLegend = false, PngOutputScale = 1 } };
        var scaleTwoOptions = new VisualArtifactRenderOptions { Topology = new TopologyRenderOptions { IncludeLegend = false, PngOutputScale = 2 } };
        scaleOneOptions.Watermarks.Add(scaleWatermark);
        scaleTwoOptions.Watermarks.Add(scaleWatermark);
        var scaleOnePlain = RasterImageDecoder.Decode(topology.ToPng(new TopologyRenderOptions { IncludeLegend = false, PngOutputScale = 1 }));
        var scaleTwoPlain = RasterImageDecoder.Decode(topology.ToPng(new TopologyRenderOptions { IncludeLegend = false, PngOutputScale = 2 }));
        var scaleOneDecorated = RasterImageDecoder.Decode(topologyArtifact.ToPng(scaleOneOptions));
        var scaleTwoDecorated = RasterImageDecoder.Decode(topologyArtifact.ToPng(scaleTwoOptions));
        var scaleOneChanged = CountChangedPixels(scaleOnePlain, scaleOneDecorated);
        var scaleTwoChanged = CountChangedPixels(scaleTwoPlain, scaleTwoDecorated);
        Assert(scaleTwoChanged > scaleOneChanged * 3, "PNG watermark geometry should scale with topology output scale instead of shrinking relative to the rendered chart.");

        var wideTopology = TopologyChart.Create()
            .WithViewport(320, 180, 16)
            .WithLegend(null)
            .AddGroup("wide", "Wide", 20, 40, 620, 80)
            .AddNode("wide-left", "Left", 40, 58, groupId: "wide")
            .AddNode("wide-right", "Right", 540, 58, groupId: "wide")
            .AddEdge("wide-edge", "wide-left", "wide-right");
        var fittedTopologyOptions = new TopologyRenderOptions { IncludeLegend = false }.WithFitContentToViewport();
        var preparedWideTopology = TopologyLayoutEngine.Prepare(wideTopology, fittedTopologyOptions.View, fittedTopologyOptions);
        var fittedWatermark = VisualWatermark.FromImage(PngWriter.WriteRgba(new RgbaImage(1, 1, new byte[] { 255, 0, 255, 255 })), "image/png");
        fittedWatermark.Anchor = VisualCanvasAnchor.BottomRight;
        fittedWatermark.Padding = 4;
        fittedWatermark.Width = 16;
        fittedWatermark.Height = 16;
        fittedWatermark.RotationDegrees = 45;
        fittedWatermark.Opacity = 1;
        var fittedArtifactOptions = new VisualArtifactRenderOptions { Topology = fittedTopologyOptions };
        fittedArtifactOptions.Watermarks.Add(fittedWatermark);
        var widePlain = RasterImageDecoder.Decode(wideTopology.ToPng(fittedTopologyOptions));
        var wideArtifact = wideTopology.ToVisualArtifact();
        wideArtifact.PreserveNaturalSize = true;
        var wideDecorated = RasterImageDecoder.Decode(wideArtifact.ToPng(fittedArtifactOptions));
        var fittedScale = Math.Min(wideDecorated.Width / preparedWideTopology.Viewport.Width, wideDecorated.Height / preparedWideTopology.Viewport.Height);
        var fittedRight = preparedWideTopology.Viewport.Width * fittedScale;
        var fittedBottom = preparedWideTopology.Viewport.Height * fittedScale;
        var fittedCenterX = (int)Math.Round(fittedRight - (fittedWatermark.Padding + fittedWatermark.Width.Value / 2) * fittedScale);
        var fittedCenterY = (int)Math.Round(fittedBottom - (fittedWatermark.Padding + fittedWatermark.Height.Value / 2) * fittedScale);
        Assert(fittedBottom < wideDecorated.Height - 20, "The fitted topology fixture should expose a visible bottom letterbox for watermark alignment coverage.");
        Assert(IsPixelNear(wideDecorated.Pixels, wideDecorated.Width, fittedCenterX, fittedCenterY, 255, 0, 255), "PNG watermarks should anchor inside the fitted SVG content extent rather than the destination letterbox.");
        var justBelowFittedFrame = ((int)Math.Ceiling(fittedBottom + 2) * wideDecorated.Width + fittedCenterX) * 4;
        Assert(wideDecorated.Pixels[justBelowFittedFrame] == widePlain.Pixels[justBelowFittedFrame] && wideDecorated.Pixels[justBelowFittedFrame + 1] == widePlain.Pixels[justBelowFittedFrame + 1] && wideDecorated.Pixels[justBelowFittedFrame + 2] == widePlain.Pixels[justBelowFittedFrame + 2] && wideDecorated.Pixels[justBelowFittedFrame + 3] == widePlain.Pixels[justBelowFittedFrame + 3], "Rotated PNG watermarks should be clipped at the fitted SVG content frame.");
        var destinationBottom = ((wideDecorated.Height - 8) * wideDecorated.Width + wideDecorated.Width - 8) * 4;
        Assert(wideDecorated.Pixels[destinationBottom] == widePlain.Pixels[destinationBottom] && wideDecorated.Pixels[destinationBottom + 1] == widePlain.Pixels[destinationBottom + 1] && wideDecorated.Pixels[destinationBottom + 2] == widePlain.Pixels[destinationBottom + 2] && wideDecorated.Pixels[destinationBottom + 3] == widePlain.Pixels[destinationBottom + 3], "Bottom-right watermark anchoring should not paint into fitted-content letterboxing.");

        AssertThrows<ArgumentException>(() => VisualWatermark.FromImage(pngBytes, "image/png\" onload=\"alert(1)"), "Image watermarks should reject attribute-breaking media types.");
        AssertThrows<ArgumentException>(() => VisualWatermark.FromImage(pngBytes, "image/jpeg"), "Image watermarks should reject media types that do not match the bytes.");
        AssertThrows<ArgumentException>(() => VisualWatermark.FromImage(new byte[] { 0x52, 0x49, 0x46, 0x46 }, "image/png"), "Image watermarks should reject unsupported image bytes at construction.");

        var mutableChart = Chart.Create().WithSize(240, 140).WithTitle("Mutable");
        mutableChart.WithAccessibility(accessibility => accessibility.WithTextAlternative("Mutable chart", "A mutable size chart.", "en"));
        mutableChart.AddBar("Value", new[] { new ChartPoint(1, 2) });
        var mutableArtifact = mutableChart.ToVisualArtifact();
        Assert(mutableArtifact.Accessibility.Name == "Mutable chart" && mutableArtifact.Accessibility.Description == "A mutable size chart." && mutableArtifact.Accessibility.Language == "en", "Chart artifacts should preserve accessibility metadata for host adapters.");
        mutableChart.WithSize(480, 280);
        var mutableOptions = new VisualArtifactRenderOptions();
        mutableOptions.Watermarks.Add(VisualWatermark.FromText("CURRENT"));
        var resizedSvg = XDocument.Parse(mutableArtifact.ToSvg(mutableOptions));
        var renderedMark = resizedSvg.Descendants().Single(element => string.Equals((string?)element.Attribute("data-cfx-role"), "watermark", StringComparison.Ordinal));
        var renderedX = double.Parse(renderedMark.Attribute("x")!.Value, CultureInfo.InvariantCulture);
        Assert(renderedX > 300, "Watermark placement should follow current rendered dimensions when natural-size preservation is disabled.");

        static int CountChangedPixels(RgbaImage plainImage, RgbaImage decoratedImage) {
            Assert(plainImage.Width == decoratedImage.Width && plainImage.Height == decoratedImage.Height, "Compared watermark images should have matching dimensions.");
            var changed = 0;
            for (var index = 0; index < plainImage.Pixels.Length; index += 4) {
                if (plainImage.Pixels[index] != decoratedImage.Pixels[index] ||
                    plainImage.Pixels[index + 1] != decoratedImage.Pixels[index + 1] ||
                    plainImage.Pixels[index + 2] != decoratedImage.Pixels[index + 2] ||
                    plainImage.Pixels[index + 3] != decoratedImage.Pixels[index + 3]) changed++;
            }
            return changed;
        }
    }

    private static void CompositeCfxSurfacesShareTheOfficeArtifactHandoff() {
        var chart = Chart.Create().WithSize(240, 140).WithTitle("Requests");
        chart.AddBar("API", new[] { new ChartPoint(1, 4), new ChartPoint(2, 7) });
        var grid = ChartGrid.Create().WithTitle("Service overview").WithColumns(1).Add(chart);
        var canvas = VisualCanvas.Create(320, 180).WithTitle("Release overview");
        canvas.Accessibility.WithTextAlternative("Release overview", "A fixed-size release summary.");
        var story = VisualStory.Create("Deployment story").WithDescription("A deployment reaches ready state.").WithSize(480, 320);
        story.Scene("ready", "Ready").Panel("result", new VisualStoryTextSurface("Ready", emphasized: true));
        story.Outcome("ready", "The deployment is ready", "result");
        var block = MetricCard.Create().WithMetric("Ready", "Yes").WithSize(240, 140);

        VisualArtifact[] artifacts = {
            grid.ToVisualArtifact("grid"),
            canvas.ToVisualArtifact("canvas"),
            story.ToVisualArtifact("story"),
            block.ToVisualArtifact("block")
        };
        var kinds = new[] { VisualArtifactKind.ChartGrid, VisualArtifactKind.VisualCanvas, VisualArtifactKind.Story, VisualArtifactKind.VisualBlock };

        for (var index = 0; index < artifacts.Length; index++) {
            VisualArtifact artifact = artifacts[index];
            Assert(artifact.Kind == kinds[index], "Composite artifacts should preserve their specific CFX surface kind.");
            Assert(artifact.SupportsExport(VisualArtifactExportFormat.Office), "Every static composite CFX surface should declare the Office handoff.");
            Assert(artifact.ToSvg().Contains("<svg", StringComparison.Ordinal), "Composite artifacts should render SVG through the shared artifact pipeline.");
            Assert(artifact.ToHtmlPage().Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase), "Composite artifacts should render standalone HTML through the shared artifact pipeline.");
            Assert(artifact.ToPng().Length > 64, "Composite artifacts should render PNG through the shared artifact pipeline.");
        }
        Assert(canvas.ToVisualArtifact().Accessibility.Description == "A fixed-size release summary.", "Canvas artifacts should preserve accessibility metadata for Office placement.");
    }

    private static void TableArtifactRejectsInvalidContractShapes() {
        AssertThrows<ArgumentNullException>(() => TableArtifact.Create(null!), "TableArtifact should reject null ids.");
        AssertThrows<ArgumentException>(() => TableArtifact.Create("bad").AddColumn("", "Bad"), "TableArtifact should reject empty column ids.");
        AssertThrows<ArgumentException>(() => TableArtifact.Create("bad").AddColumn("id", "ID").AddColumn("id", "Duplicate"), "TableArtifact should reject duplicate column ids.");
        AssertThrows<ArgumentException>(() => TableArtifact.Create("bad").AddColumn("id", "ID").AddRow("row", "a", "b"), "TableArtifact should reject row value count mismatches.");
        AssertThrows<InvalidOperationException>(() => TableArtifact.Create("bad").AddColumn("id", "ID").AddRow("row", "a").AddColumn("next", "Next"), "TableArtifact should reject adding columns after rows.");
        AssertThrows<ArgumentOutOfRangeException>(() => TableArtifact.Create("bad").WithCapabilities((TableArtifactCapabilities)1024), "TableArtifact should reject undefined capability flags.");
        AssertThrows<ArgumentOutOfRangeException>(() => TableArtifact.Create("bad").Capabilities = (TableArtifactCapabilities)1024, "TableArtifact should reject undefined capability flags through the public setter.");
        AssertThrows<ArgumentOutOfRangeException>(() => TableArtifact.Create("bad").ExportFormats = (VisualArtifactExportFormat)1024, "TableArtifact should reject undefined export format flags.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactColumn("bad", "Bad", (TableArtifactColumnType)999), "TableArtifactColumn should reject unknown data types.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactColumn("bad", "Bad", width: 0), "TableArtifactColumn should reject invalid width hints.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactColumn("bad", "Bad").Width = -1, "TableArtifactColumn should reject invalid width hints through the public setter.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactRow("bad").Status = (VisualStatus)999, "TableArtifactRow should reject unknown status values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactCell("bad").Status = (VisualStatus)999, "TableArtifactCell should reject unknown status values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new VisualArtifact().ExportFormats = (VisualArtifactExportFormat)1024, "VisualArtifact should reject undefined export format flags.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactQuery { Offset = -1 }, "TableArtifactQuery should reject negative virtual offsets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactQuery { Limit = 0 }, "TableArtifactQuery should reject empty virtual windows.");
    }

    private static void TableArtifactVirtualQueryContractIsHostNeutral() {
        var query = new TableArtifactQuery {
            SearchText = "warning",
            Offset = 25,
            Limit = 50
        };
        query.Sorts.Add(new TableArtifactSort("latency", descending: true));
        query.Filters.Add(new TableArtifactFilter("status", "warning") { Operator = "equals" });

        var rows = new List<TableArtifactRow> {
            new("worker") {
                Cells = {
                    new TableArtifactCell("Worker"),
                    new TableArtifactCell("Warning") { Status = VisualStatus.Warning }
                },
                Status = VisualStatus.Warning
            }
        };
        var result = new TableArtifactQueryResult(rows, totalRowCount: 125);

        Assert(query.SearchText == "warning", "TableArtifactQuery should carry host-neutral search text.");
        Assert(query.Sorts[0].ColumnId == "latency" && query.Sorts[0].Descending, "TableArtifactQuery should carry host-neutral sort descriptors.");
        Assert(query.Filters[0].ColumnId == "status" && query.Filters[0].Operator == "equals", "TableArtifactQuery should carry host-neutral filter descriptors.");
        Assert(result.Rows.Count == 1 && result.TotalRowCount == 125, "TableArtifactQueryResult should carry a virtualized row window and total count.");
    }

    private static void FlowArtifactRendersStaticPreviewAndEnvelope() {
        var flow = FlowArtifact.Create("approval")
            .WithTitle("Approval Flow")
            .WithSize(720, 420)
            .AddLane("ops", "Operations")
            .AddStep("start", "Start", FlowArtifactStepKind.Start, "ops", VisualStatus.Positive)
            .AddStep("review", "Review", FlowArtifactStepKind.Decision, "ops", VisualStatus.Warning)
            .AddConnector("start", "review", "handoff", FlowArtifactConnectorKind.Flow, VisualLinkDirection.Forward, VisualStatus.Positive, "#EF4444");

        var artifact = flow.ToVisualArtifact();
        var svg = flow.ToSvg();
        var png = flow.ToPng();

        Assert(artifact.Kind == VisualArtifactKind.Flow, "FlowArtifact should wrap into a product-neutral flow artifact envelope.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Office), "FlowArtifact should declare the documented Office handoff.");
        Assert(artifact.Model == flow, "FlowArtifact envelope should keep the typed flow model.");
        Assert(artifact.Metadata["render.model"] == nameof(FlowArtifact), "FlowArtifact envelope should expose the flow model.");
        Assert(artifact.Metadata["render.previewModel"] == nameof(TopologyChart), "FlowArtifact envelope should identify the static preview projection.");
        Assert(artifact.Metadata["flow.steps"] == "2" && artifact.Metadata["flow.connectors"] == "1", "FlowArtifact envelope should expose flow counts.");
        Assert(svg.Contains("data-cfx-role=\"topology\"", StringComparison.Ordinal), "FlowArtifact static preview should reuse deterministic topology SVG rendering.");
        Assert(svg.Contains("#EF4444", StringComparison.OrdinalIgnoreCase), "FlowArtifact static preview should preserve explicit connector colors.");
        Assert(flow.ToHtmlPage().Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase), "FlowArtifact HTML preview should reuse deterministic topology HTML rendering.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "FlowArtifact PNG preview should emit a valid PNG.");
    }

    private static void SequenceArtifactRendersStaticPreviewAndEnvelope() {
        var sequence = SequenceArtifact.Create("incident")
            .WithTitle("Incident Flow")
            .WithSubtitle("Native sequence preview")
            .WithSize(760, 420)
            .AddParticipant("user", "User", SequenceArtifactParticipantKind.Actor)
            .AddParticipant("api", "API")
            .AddParticipant("db", "Database", SequenceArtifactParticipantKind.Database)
            .AddMessage("user", "api", "Request")
            .AddMessage("api", "db", "Store", SequenceArtifactMessageLineStyle.Dashed)
            .AddNote(SequenceArtifactNotePlacement.RightOf, new[] { "api" }, "Processing")
            .AddBlock(SequenceArtifactBlockKind.Loop, "Retry", 0, 1);

        var artifact = sequence.ToVisualArtifact();
        var svg = sequence.ToSvg();
        var png = sequence.ToPng();

        Assert(artifact.Kind == VisualArtifactKind.Sequence, "SequenceArtifact should wrap into a product-neutral sequence artifact envelope.");
        Assert(artifact.Model == sequence, "SequenceArtifact envelope should keep the typed sequence model.");
        Assert(artifact.Metadata["sequence.participants"] == "3", "SequenceArtifact envelope should expose participant counts.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Svg), "SequenceArtifact should declare SVG export support.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Office), "SequenceArtifact should declare the documented Office handoff.");
        Assert(svg.Contains("data-cfx-role=\"sequence-message\"", StringComparison.Ordinal), "SequenceArtifact SVG should expose message regions.");
        Assert(svg.Contains("data-cfx-role=\"sequence-note\"", StringComparison.Ordinal), "SequenceArtifact SVG should expose note regions.");
        Assert(artifact.ToHtmlPage().Contains("chartforgex-visual-artifact", StringComparison.Ordinal), "VisualArtifact HTML rendering should wrap sequence previews in a standalone artifact page.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "SequenceArtifact PNG should emit a valid PNG.");
    }

    private sealed class TemporaryDirectory : IDisposable {
        public TemporaryDirectory() {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ChartForgeX-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}
