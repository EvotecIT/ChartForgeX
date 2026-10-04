using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal sealed partial class BitmapFontData {
    /// <summary>Decodes horizontal EBDT images: small/big metrics, byte-aligned or continuous packed rows.</summary>
    private BitmapGlyph? ReadEmbedded(FontTableReader index, FontTableReader data, BitmapStrike strike,
        int format, int metrics, int record, int length) {
        var metricTable = data; var payload = record; var metricBytes = 0;
        if (format == 1 || format == 2) metricBytes = 5;
        else if (format == 6 || format == 7) metricBytes = 8;
        else if (format == 5 && metrics >= 0) metricTable = index;
        else return null; // Component records need a separate reference-backed implementation.
        if (metricBytes != 0) {
            if (length < metricBytes) throw new FontLayoutException();
            metrics = record; payload += metricBytes;
        }
        var height = metricTable.U8(metrics); var width = metricTable.U8(metrics + 1);
        if (height == 0 || width == 0) return null;
        var rowBits = width * strike.Depth;
        var strideBits = format == 1 || format == 6 ? (rowBits + 7) / 8 * 8 : rowBits;
        var bytes = (strideBits * height + 7) / 8;
        if (bytes > record + length - payload) throw new FontLayoutException();
        data.Require(payload, bytes);
        var pixels = new byte[width * height * 4]; var mask = (1 << strike.Depth) - 1;
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var bit = y * strideBits + x * strike.Depth;
            var value = data.U8(payload + bit / 8) >> (8 - strike.Depth - bit % 8) & mask;
            var pixel = (y * width + x) * 4;
            pixels[pixel] = pixels[pixel + 1] = pixels[pixel + 2] = 255;
            pixels[pixel + 3] = (byte)((value * 255 + mask / 2) / mask);
        }
        var xScale = _unitsPerEm / (double)strike.PpemX; var yScale = _unitsPerEm / (double)strike.PpemY;
        return new BitmapGlyph { Image = new RgbaImage(width, height, pixels),
            Bounds = new ChartRect(metricTable.I8(metrics + 2) * xScale,
                (metricTable.I8(metrics + 3) - height) * yScale, width * xScale, height * yScale) };
    }
}
