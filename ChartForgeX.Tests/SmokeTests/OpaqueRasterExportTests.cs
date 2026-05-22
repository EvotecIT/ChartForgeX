using System;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void OpaqueRasterExportsEmitValidFlattenedImages() {
        var bmp = SampleChart().ToBmp();
        AssertBmpHeader(bmp, 640, 360);
        AssertPpmHeader(SampleChart().ToPpm(), 640, 360);
        AssertTiffHeader(SampleChart().ToTiff(), 640, 360);
        AssertBmpHeader(SampleChart().ToRasterImage(RasterImageFormat.Bmp), 640, 360);
        AssertPpmHeader(SampleChart().ToRasterImage(RasterImageFormat.Ppm), 640, 360);
        AssertTiffHeader(SampleChart().ToRasterImage(RasterImageFormat.Tiff), 640, 360);
        var supportedFormats = RasterImageFormatExtensions.GetSupportedFormats();
        Assert(supportedFormats.SequenceEqual(new[] { RasterImageFormat.Bmp, RasterImageFormat.Ppm, RasterImageFormat.Tiff }), "Supported raster formats should be discoverable in stable order.");
        Assert(RasterImageFormat.Bmp.IsSupported(), "BMP should be reported as a supported raster format.");
        Assert(RasterImageFormat.Ppm.IsSupported(), "PPM should be reported as a supported raster format.");
        Assert(RasterImageFormat.Tiff.IsSupported(), "TIFF should be reported as a supported raster format.");
        Assert(!((RasterImageFormat)999).IsSupported(), "Unknown raster formats should not be reported as supported.");
        Assert(RasterImageFormat.Bmp.GetFileExtension() == ".bmp", "BMP file extension metadata should be available.");
        Assert(RasterImageFormat.Ppm.GetFileExtension() == ".ppm", "PPM file extension metadata should be available.");
        Assert(RasterImageFormat.Tiff.GetFileExtension() == ".tiff", "TIFF file extension metadata should be available.");
        Assert(RasterImageFormat.Bmp.GetFileExtensions().SequenceEqual(new[] { ".bmp" }), "BMP extension aliases should be discoverable.");
        Assert(RasterImageFormat.Ppm.GetFileExtensions().SequenceEqual(new[] { ".ppm" }), "PPM extension aliases should be discoverable.");
        Assert(RasterImageFormat.Tiff.GetFileExtensions().SequenceEqual(new[] { ".tiff", ".tif" }), "TIFF extension aliases should be discoverable with the conventional extension first.");
        Assert(RasterImageFormat.Bmp.GetMimeType() == "image/bmp", "BMP MIME metadata should be available.");
        Assert(RasterImageFormat.Ppm.GetMimeType() == "image/x-portable-pixmap", "PPM MIME metadata should be available.");
        Assert(RasterImageFormat.Tiff.GetMimeType() == "image/tiff", "TIFF MIME metadata should be available.");
        Assert(RasterImageFormatExtensions.FromFileExtension("bmp") == RasterImageFormat.Bmp, "Bare BMP extension tokens should resolve to BMP format.");
        Assert(RasterImageFormatExtensions.FromFileExtension(".bmp") == RasterImageFormat.Bmp, "BMP extensions should resolve to BMP format.");
        Assert(RasterImageFormatExtensions.FromFileExtension("PPM") == RasterImageFormat.Ppm, "Bare PPM extension tokens should resolve case-insensitively.");
        Assert(RasterImageFormatExtensions.FromFileExtension("report.PPM") == RasterImageFormat.Ppm, "PPM paths should resolve case-insensitively.");
        Assert(RasterImageFormatExtensions.FromFileExtension("tif") == RasterImageFormat.Tiff, "Bare TIF extension tokens should resolve to TIFF format.");
        Assert(RasterImageFormatExtensions.FromFileExtension("report.tif") == RasterImageFormat.Tiff, "TIF paths should resolve to TIFF format.");
        Assert(RasterImageFormatExtensions.FromFileExtension("report.tiff") == RasterImageFormat.Tiff, "TIFF paths should resolve to TIFF format.");
        Assert(RasterImageFormatExtensions.FromFileExtension(".report.bmp") == RasterImageFormat.Bmp, "Dot-prefixed BMP filenames should resolve from the final extension.");
        Assert(RasterImageFormatExtensions.FromFileExtension(".report.tif") == RasterImageFormat.Tiff, "Dot-prefixed TIF filenames should resolve from the final extension.");
        Assert(RasterImageFormatExtensions.TryFromFileExtension("report.bmp", out var inferredBmp) && inferredBmp == RasterImageFormat.Bmp, "BMP extensions should resolve through the non-throwing helper.");
        Assert(RasterImageFormatExtensions.TryFromFileExtension("tiff", out var inferredBareTiff) && inferredBareTiff == RasterImageFormat.Tiff, "Bare TIFF extension tokens should resolve through the non-throwing helper.");
        Assert(RasterImageFormatExtensions.TryFromFileExtension("report.TIFF", out var inferredTiff) && inferredTiff == RasterImageFormat.Tiff, "TIFF extensions should resolve through the non-throwing helper.");
        Assert(RasterImageFormatExtensions.TryFromFileExtension(".report.PPM", out var inferredDotPrefixedPpm) && inferredDotPrefixedPpm == RasterImageFormat.Ppm, "Dot-prefixed filenames should resolve through the non-throwing helper.");
        Assert(!RasterImageFormatExtensions.TryFromFileExtension("report.gif", out _), "Unsupported raster extensions should not resolve through the non-throwing helper.");
        Assert(!RasterImageFormatExtensions.TryFromFileExtension(null, out _), "Null raster extensions should not resolve through the non-throwing helper.");
        Assert(!RasterImageFormatExtensions.TryFromFileExtension(" ", out _), "Empty raster extensions should not resolve through the non-throwing helper.");
        using var chartStream = new MemoryStream();
        SampleChart().WriteRasterImage(chartStream, RasterImageFormat.Bmp);
        Assert(chartStream.ToArray().SequenceEqual(SampleChart().ToRasterImage(RasterImageFormat.Bmp)), "Chart stream raster export should match byte-array export.");
        using var chartBmpStream = new MemoryStream();
        SampleChart().WriteBmp(chartBmpStream);
        Assert(chartBmpStream.ToArray().SequenceEqual(SampleChart().ToBmp()), "Chart BMP stream export should match byte-array export.");
        using var chartPpmStream = new MemoryStream();
        SampleChart().WritePpm(chartPpmStream);
        Assert(chartPpmStream.ToArray().SequenceEqual(SampleChart().ToPpm()), "Chart PPM stream export should match byte-array export.");
        using var chartTiffStream = new MemoryStream();
        SampleChart().WriteTiff(chartTiffStream);
        Assert(chartTiffStream.ToArray().SequenceEqual(SampleChart().ToTiff()), "Chart TIFF stream export should match byte-array export.");
        AssertThrows<ArgumentOutOfRangeException>(() => SampleChart().ToRasterImage((RasterImageFormat)999), "Generic raster export should reject unknown formats.");
        using var invalidFormatStream = new MemoryStream();
        AssertThrows<ArgumentOutOfRangeException>(() => SampleChart().WriteRasterImage(invalidFormatStream, (RasterImageFormat)999), "Generic raster stream export should reject unknown formats before writing.");
        Assert(invalidFormatStream.Length == 0, "Generic raster stream export should not write bytes for unknown formats.");
        AssertThrows<ArgumentOutOfRangeException>(() => ((RasterImageFormat)999).GetFileExtension(), "Raster format file extension metadata should reject unknown formats.");
        AssertThrows<ArgumentOutOfRangeException>(() => ((RasterImageFormat)999).GetFileExtensions(), "Raster format file extension alias metadata should reject unknown formats.");
        AssertThrows<ArgumentOutOfRangeException>(() => ((RasterImageFormat)999).GetMimeType(), "Raster format MIME metadata should reject unknown formats.");
        AssertThrows<ArgumentException>(() => RasterImageFormatExtensions.FromFileExtension("chart.gif"), "Raster format extension inference should reject unsupported extensions.");
        AssertThrows<ArgumentException>(() => RasterImageFormatExtensions.FromFileExtension(" "), "Raster format extension inference should reject empty extensions.");
        AssertThrows<ArgumentNullException>(() => SampleChart().WriteRasterImage(null!, RasterImageFormat.Bmp), "Generic raster stream export should reject null streams.");
        AssertThrows<ArgumentNullException>(() => SampleChart().WriteBmp(null!), "Format-specific raster stream export should reject null streams.");
        var invalidFormatPath = Path.Combine(Path.GetTempPath(), "chartforgex-invalid-raster-" + Guid.NewGuid().ToString("N") + ".bmp");
        try {
            AssertThrows<ArgumentOutOfRangeException>(() => SampleChart().SaveRasterImage(invalidFormatPath, (RasterImageFormat)999), "Generic raster file export should reject unknown formats before opening the destination.");
            Assert(!File.Exists(invalidFormatPath), "Generic raster file export should not create a destination file for unknown formats.");
        } finally {
            if (File.Exists(invalidFormatPath)) File.Delete(invalidFormatPath);
        }

        Chart nullChart = null!;
        AssertFailedSavePreservesExistingFile(path => nullChart.SaveRasterImage(path, RasterImageFormat.Bmp), "Chart raster file export should render before opening the destination.");
        ChartGrid nullGrid = null!;
        AssertFailedSavePreservesExistingFile(path => nullGrid.SaveRasterImage(path, RasterImageFormat.Bmp), "Chart grid raster file export should render before opening the destination.");
        IVisualBlock nullBlock = null!;
        AssertFailedSavePreservesExistingFile(path => nullBlock.SaveRasterImage(path, RasterImageFormat.Bmp), "Visual block raster file export should render before opening the destination.");
        VisualGrid nullVisualGrid = null!;
        AssertFailedSavePreservesExistingFile(path => nullVisualGrid.SaveRasterImage(path, RasterImageFormat.Bmp), "Visual grid raster file export should render before opening the destination.");

        var inferredPath = Path.Combine(Path.GetTempPath(), "chartforgex-raster-" + Guid.NewGuid().ToString("N") + ".tif");
        try {
            SampleChart().SaveRasterImage(inferredPath);
            AssertTiffHeader(File.ReadAllBytes(inferredPath), 640, 360);
        } finally {
            if (File.Exists(inferredPath)) File.Delete(inferredPath);
        }

        AssertExtensionInferredSave("chart", ".svg", path => SampleChart().Save(path), bytes => Assert(System.Text.Encoding.UTF8.GetString(bytes).Contains("<svg", StringComparison.Ordinal), "Save should infer SVG from the output extension."));
        AssertExtensionInferredSave("chart", ".html", path => SampleChart().Save(path), bytes => Assert(System.Text.Encoding.UTF8.GetString(bytes).Contains("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase), "Save should infer HTML from the output extension."));
        AssertExtensionInferredSave("chart", ".png", path => SampleChart().Save(path), bytes => AssertPngHeader(bytes));
        AssertExtensionInferredSave("chart", ".bmp", path => SampleChart().Save(path), bytes => AssertBmpHeader(bytes, 640, 360));
        AssertExtensionInferredSave("chart", ".tif", path => SampleChart().Save(path), bytes => AssertTiffHeader(bytes, 640, 360));
        AssertDotPrefixedExtensionInferredSave(path => SampleChart().Save(path), bytes => AssertBmpHeader(bytes, 640, 360));
        AssertExtensionInferredSave("grid", ".ppm", path => GridForInferredSave().Save(path), bytes => AssertPpmHeader(bytes, null, null));
        AssertExtensionInferredSave("block", ".png", path => MetricCard.Create().WithSize(180, 100).WithMetric("CPU", "42%").Save(path), bytes => AssertPngHeader(bytes));
        AssertExtensionInferredSave("visual-grid", ".html", path => VisualGrid.CreateMetricStrip("Endpoint", new[] { MetricCard.Create().WithMetric("CPU", "42%") }).Save(path), bytes => Assert(System.Text.Encoding.UTF8.GetString(bytes).Contains("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase), "Visual grid Save should infer HTML from the output extension."));
        AssertThrows<ArgumentException>(() => SampleChart().Save("chart.gif"), "Save should reject unsupported image extensions.");

        var transparent = Chart.Create()
            .WithSize(32, 24)
            .WithTheme(ChartTheme.TransparentOverlayDark())
            .WithTransparentBackground()
            .WithHeader(false)
            .WithCard(false)
            .WithPlotBackground(false)
            .AddLine("Signal", Points(1, 2, 3), ChartColor.FromRgb(96, 165, 250));
        transparent.Options.ShowAxes = false;
        transparent.Options.ShowLegend = false;
        var flattened = transparent.ToBmp(new RasterImageOptions { Background = ChartColors.Magenta });
        AssertBmpHeader(flattened, 32, 24);
        var topLeft = ReadBmpPixel(flattened, 0, 0);
        Assert(topLeft.R == ChartColors.Magenta.R && topLeft.G == ChartColors.Magenta.G && topLeft.B == ChartColors.Magenta.B, "BMP export should flatten transparent pixels against the requested background.");
        var flattenedPpm = transparent.ToPpm(new RasterImageOptions { Background = ChartColors.Magenta });
        AssertPpmHeader(flattenedPpm, 32, 24);
        var ppmTopLeft = ReadPpmPixel(flattenedPpm, 0, 0);
        Assert(ppmTopLeft.R == ChartColors.Magenta.R && ppmTopLeft.G == ChartColors.Magenta.G && ppmTopLeft.B == ChartColors.Magenta.B, "PPM export should flatten transparent pixels against the requested background.");
        var flattenedTiff = transparent.ToTiff(new RasterImageOptions { Background = ChartColors.Magenta });
        AssertTiffHeader(flattenedTiff, 32, 24);
        var tiffTopLeft = ReadTiffPixel(flattenedTiff, 0, 0);
        Assert(tiffTopLeft.R == ChartColors.Magenta.R && tiffTopLeft.G == ChartColors.Magenta.G && tiffTopLeft.B == ChartColors.Magenta.B, "TIFF export should flatten transparent pixels against the requested background.");

        var grid = ChartGrid.Create().WithPanelSize(180, 120).Add(Chart.Create().WithSize(180, 120).AddLine("Values", Points(1, 2, 3)));
        AssertBmpHeader(grid.ToBmp(), null, null);
        AssertPpmHeader(grid.ToPpm(), null, null);
        AssertTiffHeader(grid.ToTiff(), null, null);
        AssertTiffHeader(grid.ToRasterImage(RasterImageFormat.Tiff), null, null);
        using var gridStream = new MemoryStream();
        grid.WriteRasterImage(gridStream, RasterImageFormat.Tiff);
        Assert(gridStream.ToArray().SequenceEqual(grid.ToRasterImage(RasterImageFormat.Tiff)), "Grid stream raster export should match byte-array export.");
        using var gridTiffStream = new MemoryStream();
        grid.WriteTiff(gridTiffStream);
        Assert(gridTiffStream.ToArray().SequenceEqual(grid.ToTiff()), "Grid TIFF stream export should match byte-array export.");
        AssertBmpHeader(MetricCard.Create().WithSize(180, 100).WithMetric("CPU", "42%").ToBmp(), 180, 100);
        AssertPpmHeader(MetricCard.Create().WithSize(180, 100).WithMetric("CPU", "42%").ToPpm(), 180, 100);
        AssertTiffHeader(MetricCard.Create().WithSize(180, 100).WithMetric("CPU", "42%").ToTiff(), 180, 100);
        var card = MetricCard.Create().WithSize(180, 100).WithMetric("CPU", "42%");
        AssertPpmHeader(card.ToRasterImage(RasterImageFormat.Ppm), 180, 100);
        using var cardStream = new MemoryStream();
        card.WriteRasterImage(cardStream, RasterImageFormat.Ppm);
        Assert(cardStream.ToArray().SequenceEqual(card.ToRasterImage(RasterImageFormat.Ppm)), "Visual block stream raster export should match byte-array export.");
        using var cardPpmStream = new MemoryStream();
        card.WritePpm(cardPpmStream);
        Assert(cardPpmStream.ToArray().SequenceEqual(card.ToPpm()), "Visual block PPM stream export should match byte-array export.");
        var visualGrid = VisualGrid.CreateMetricStrip("Endpoint", new[] { MetricCard.Create().WithMetric("CPU", "42%") });
        AssertBmpHeader(visualGrid.ToBmp(), null, null);
        AssertPpmHeader(visualGrid.ToPpm(), null, null);
        AssertTiffHeader(visualGrid.ToTiff(), null, null);
        AssertBmpHeader(visualGrid.ToRasterImage(RasterImageFormat.Bmp), null, null);
        using var visualGridStream = new MemoryStream();
        visualGrid.WriteRasterImage(visualGridStream, RasterImageFormat.Bmp);
        Assert(visualGridStream.ToArray().SequenceEqual(visualGrid.ToRasterImage(RasterImageFormat.Bmp)), "Visual grid stream raster export should match byte-array export.");
        using var visualGridBmpStream = new MemoryStream();
        visualGrid.WriteBmp(visualGridBmpStream);
        Assert(visualGridBmpStream.ToArray().SequenceEqual(visualGrid.ToBmp()), "Visual grid BMP stream export should match byte-array export.");
        var topology = CreateSampleTopologyChart();
        var topologyOptions = new TopologyRenderOptions { IncludeLegend = false };
        AssertBmpHeader(topology.ToBmp(topologyOptions), null, null);
        AssertPpmHeader(topology.ToPpm(topologyOptions), null, null);
        AssertTiffHeader(topology.ToTiff(topologyOptions), null, null);
        AssertBmpHeader(topology.ToRasterImage(RasterImageFormat.Bmp, topologyOptions), null, null);
        using var topologyStream = new MemoryStream();
        topology.WriteRasterImage(topologyStream, RasterImageFormat.Bmp, topologyOptions);
        Assert(topologyStream.ToArray().SequenceEqual(topology.ToRasterImage(RasterImageFormat.Bmp, topologyOptions)), "Topology stream raster export should match byte-array export.");
        using var topologyBmpStream = new MemoryStream();
        topology.WriteBmp(topologyBmpStream, topologyOptions);
        Assert(topologyBmpStream.ToArray().SequenceEqual(topology.ToBmp(topologyOptions)), "Topology BMP stream export should match byte-array export.");
        var invalidTopologyPath = Path.Combine(Path.GetTempPath(), "chartforgex-invalid-topology-raster-" + Guid.NewGuid().ToString("N") + ".bmp");
        try {
            AssertThrows<ArgumentOutOfRangeException>(() => topology.SaveRasterImage(invalidTopologyPath, (RasterImageFormat)999, topologyOptions), "Topology raster file export should reject unknown formats before opening the destination.");
            Assert(!File.Exists(invalidTopologyPath), "Topology raster file export should not create a destination file for unknown formats.");
        } finally {
            if (File.Exists(invalidTopologyPath)) File.Delete(invalidTopologyPath);
        }

        TopologyChart nullTopology = null!;
        AssertFailedSavePreservesExistingFile(path => nullTopology.SaveRasterImage(path, RasterImageFormat.Bmp), "Topology raster file export should render before opening the destination.");

        var topologyPath = Path.Combine(Path.GetTempPath(), "chartforgex-topology-raster-" + Guid.NewGuid().ToString("N") + ".ppm");
        try {
            topology.SaveRasterImage(topologyPath, topologyOptions);
            AssertPpmHeader(File.ReadAllBytes(topologyPath), null, null);
        } finally {
            if (File.Exists(topologyPath)) File.Delete(topologyPath);
        }

        AssertExtensionInferredSave("topology", ".svg", path => topology.Save(path, topologyOptions), bytes => Assert(System.Text.Encoding.UTF8.GetString(bytes).Contains("<svg", StringComparison.Ordinal), "Topology Save should infer SVG from the output extension."));
        AssertExtensionInferredSave("topology", ".png", path => topology.Save(path, topologyOptions), bytes => AssertPngHeader(bytes));
        var topologyMotionOptions = new TopologyRenderOptions { IncludeLegend = false }
            .WithMotion(TopologyMotionOptions.RoutePulseForEdges("amer-emea"));
        AssertExtensionInferredSave("topology", ".gif", path => topology.Save(path, topologyMotionOptions), bytes => Assert(bytes.Length > 128 && bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F', "Topology Save should infer animated GIF from the output extension."));
        AssertExtensionInferredSave("topology", ".apng", path => topology.Save(path, topologyMotionOptions), bytes => Assert(bytes.Length > 128 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71 && System.Text.Encoding.ASCII.GetString(bytes).Contains("acTL", StringComparison.Ordinal), "Topology Save should infer animated PNG from the output extension."));
        AssertExtensionInferredSave("topology", ".tiff", path => topology.Save(path, topologyOptions), bytes => AssertTiffHeader(bytes, null, null));
    }

    private static ChartGrid GridForInferredSave() => ChartGrid.Create().WithPanelSize(180, 120).Add(Chart.Create().WithSize(180, 120).AddLine("Values", Points(1, 2, 3)));

    private static void AssertExtensionInferredSave(string prefix, string extension, Action<string> saveAction, Action<byte[]> assertOutput) {
        var path = Path.Combine(Path.GetTempPath(), "chartforgex-save-image-" + prefix + "-" + Guid.NewGuid().ToString("N") + extension);
        try {
            saveAction(path);
            assertOutput(File.ReadAllBytes(path));
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void AssertDotPrefixedExtensionInferredSave(Action<string> saveAction, Action<byte[]> assertOutput) {
        var path = Path.Combine(Path.GetTempPath(), ".chartforgex-save-image-" + Guid.NewGuid().ToString("N") + ".bmp");
        try {
            saveAction(path);
            assertOutput(File.ReadAllBytes(path));
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void AssertFailedSavePreservesExistingFile(Action<string> saveAction, string message) {
        var path = Path.Combine(Path.GetTempPath(), "chartforgex-preserve-raster-" + Guid.NewGuid().ToString("N") + ".bmp");
        var original = new byte[] { 1, 2, 3, 4 };
        File.WriteAllBytes(path, original);
        try {
            AssertThrows<Exception>(() => saveAction(path), message);
            Assert(File.ReadAllBytes(path).SequenceEqual(original), message + " Existing file contents should remain unchanged.");
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void AssertPngHeader(byte[] png) {
        Assert(png.Length > 64, "PNG output should contain an encoded image.");
        Assert(png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71, "PNG signature should be valid.");
    }

    private static void AssertBmpHeader(byte[] bmp, int? expectedWidth, int? expectedHeight) {
        Assert(bmp.Length > 54, "BMP output should include file and DIB headers.");
        Assert(bmp[0] == (byte)'B' && bmp[1] == (byte)'M', "BMP signature should be valid.");
        Assert(ReadLittleEndianInt32(bmp, 2) == bmp.Length, "BMP file size header should match the emitted byte count.");
        Assert(ReadLittleEndianInt32(bmp, 10) == 54, "BMP pixel data should start after the file and BITMAPINFOHEADER headers.");
        Assert(ReadLittleEndianInt32(bmp, 14) == 40, "BMP should use the baseline BITMAPINFOHEADER.");
        var width = ReadLittleEndianInt32(bmp, 18);
        var height = ReadLittleEndianInt32(bmp, 22);
        Assert(width > 0 && height > 0, "BMP dimensions should be positive.");
        if (expectedWidth.HasValue) Assert(width == expectedWidth.Value, "BMP width should match the rendered image width.");
        if (expectedHeight.HasValue) Assert(height == expectedHeight.Value, "BMP height should match the rendered image height.");
        Assert(ReadLittleEndianInt16(bmp, 26) == 1, "BMP should declare a single color plane.");
        Assert(ReadLittleEndianInt16(bmp, 28) == 24, "BMP should emit 24-bit flattened pixels.");
        Assert(ReadLittleEndianInt32(bmp, 30) == 0, "BMP should use uncompressed BI_RGB storage.");
    }

    private static (byte R, byte G, byte B) ReadBmpPixel(byte[] bmp, int x, int y) {
        var width = ReadLittleEndianInt32(bmp, 18);
        var height = ReadLittleEndianInt32(bmp, 22);
        if (x < 0 || x >= width || y < 0 || y >= height) throw new ArgumentOutOfRangeException(nameof(x));
        var pixelOffset = ReadLittleEndianInt32(bmp, 10);
        var rowStride = ((width * 3) + 3) / 4 * 4;
        var row = height - 1 - y;
        var index = pixelOffset + row * rowStride + x * 3;
        return (bmp[index + 2], bmp[index + 1], bmp[index]);
    }

    private static void AssertPpmHeader(byte[] ppm, int? expectedWidth, int? expectedHeight) {
        var header = ReadPpmHeader(ppm);
        Assert(header.Width > 0 && header.Height > 0, "PPM dimensions should be positive.");
        if (expectedWidth.HasValue) Assert(header.Width == expectedWidth.Value, "PPM width should match the rendered image width.");
        if (expectedHeight.HasValue) Assert(header.Height == expectedHeight.Value, "PPM height should match the rendered image height.");
        Assert(header.MaxValue == 255, "PPM should emit 8-bit color channels.");
        Assert(ppm.Length == header.PixelOffset + header.Width * header.Height * 3, "PPM byte count should match the binary RGB payload size.");
    }

    private static (byte R, byte G, byte B) ReadPpmPixel(byte[] ppm, int x, int y) {
        var header = ReadPpmHeader(ppm);
        if (x < 0 || x >= header.Width || y < 0 || y >= header.Height) throw new ArgumentOutOfRangeException(nameof(x));
        var index = header.PixelOffset + (y * header.Width + x) * 3;
        return (ppm[index], ppm[index + 1], ppm[index + 2]);
    }

    private static (int Width, int Height, int MaxValue, int PixelOffset) ReadPpmHeader(byte[] ppm) {
        var offset = 0;
        var magic = ReadPpmToken(ppm, ref offset);
        Assert(magic == "P6", "PPM signature should be binary P6.");
        var width = int.Parse(ReadPpmToken(ppm, ref offset), System.Globalization.CultureInfo.InvariantCulture);
        var height = int.Parse(ReadPpmToken(ppm, ref offset), System.Globalization.CultureInfo.InvariantCulture);
        var max = int.Parse(ReadPpmToken(ppm, ref offset), System.Globalization.CultureInfo.InvariantCulture);
        return (width, height, max, offset);
    }

    private static string ReadPpmToken(byte[] ppm, ref int offset) {
        while (offset < ppm.Length && ppm[offset] <= 32) offset++;
        var start = offset;
        while (offset < ppm.Length && ppm[offset] > 32) offset++;
        var token = System.Text.Encoding.ASCII.GetString(ppm, start, offset - start);
        if (offset < ppm.Length && ppm[offset] <= 32) offset++;
        return token;
    }

    private static void AssertTiffHeader(byte[] tiff, int? expectedWidth, int? expectedHeight) {
        Assert(tiff.Length > 192, "TIFF output should include a header, IFD, and pixel data.");
        Assert(tiff[0] == (byte)'I' && tiff[1] == (byte)'I', "TIFF should use little-endian byte order.");
        Assert(ReadLittleEndianInt16(tiff, 2) == 42, "TIFF magic number should be valid.");
        var ifdOffset = ReadLittleEndianInt32(tiff, 4);
        var width = TiffTagValue(tiff, ifdOffset, 256);
        var height = TiffTagValue(tiff, ifdOffset, 257);
        Assert(width > 0 && height > 0, "TIFF dimensions should be positive.");
        if (expectedWidth.HasValue) Assert(width == expectedWidth.Value, "TIFF width should match the rendered image width.");
        if (expectedHeight.HasValue) Assert(height == expectedHeight.Value, "TIFF height should match the rendered image height.");
        Assert(TiffTagValue(tiff, ifdOffset, 259) == 1, "TIFF should use uncompressed storage.");
        Assert(TiffTagValue(tiff, ifdOffset, 262) == 2, "TIFF should use RGB photometric interpretation.");
        Assert(TiffTagValue(tiff, ifdOffset, 277) == 3, "TIFF should emit RGB samples.");
        Assert(TiffTagValue(tiff, ifdOffset, 279) == width * height * 3, "TIFF strip byte count should match the RGB payload size.");
        var pixelOffset = TiffTagValue(tiff, ifdOffset, 273);
        Assert(tiff.Length == pixelOffset + width * height * 3, "TIFF byte count should match the RGB payload size.");
    }

    private static (byte R, byte G, byte B) ReadTiffPixel(byte[] tiff, int x, int y) {
        var ifdOffset = ReadLittleEndianInt32(tiff, 4);
        var width = TiffTagValue(tiff, ifdOffset, 256);
        var height = TiffTagValue(tiff, ifdOffset, 257);
        if (x < 0 || x >= width || y < 0 || y >= height) throw new ArgumentOutOfRangeException(nameof(x));
        var pixelOffset = TiffTagValue(tiff, ifdOffset, 273);
        var index = pixelOffset + (y * width + x) * 3;
        return (tiff[index], tiff[index + 1], tiff[index + 2]);
    }

    private static int TiffTagValue(byte[] tiff, int ifdOffset, int tag) {
        var entries = ReadLittleEndianInt16(tiff, ifdOffset);
        var offset = ifdOffset + 2;
        for (var i = 0; i < entries; i++) {
            var entry = offset + i * 12;
            if (ReadLittleEndianInt16(tiff, entry) == tag) return ReadLittleEndianInt32(tiff, entry + 8);
        }

        throw new InvalidOperationException("Missing TIFF tag " + tag.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
    }

    private static int ReadLittleEndianInt32(byte[] bytes, int offset) {
        return bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
    }

    private static int ReadLittleEndianInt16(byte[] bytes, int offset) {
        return bytes[offset] | (bytes[offset + 1] << 8);
    }
}
