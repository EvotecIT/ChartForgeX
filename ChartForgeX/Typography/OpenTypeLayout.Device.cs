using System;

namespace ChartForgeX.Typography;

internal sealed partial class OpenTypeLayout {
    /// <summary>Reads a font's signed pixel correction into design units at the logical layout size.
    /// Export scaling magnifies this same layout; it does not select another device-size correction.</summary>
    private static double DeviceAdjustment(FontTableReader table, int origin, int field, LayoutExecution execution) {
        if (execution.PixelSize <= 0) return 0;
        try {
            var at = table.Offset(origin, field, optional: true);
            if (at < 0) return 0;
            var first = table.U16(at); var last = table.U16(at + 2); var format = table.U16(at + 4);
            // VariationIndex records belong to the future non-default variation context, not pixel sizes.
            if (format == 0x8000) return 0;
            if (format < 1 || format > 3 || last < first) return 0;
            var bits = 1 << format; var perWord = 16 / bits;
            table.Require(at + 6, ((last - first + 1 + perWord - 1) / perWord) * 2);
            var ppem = (int)Math.Floor(execution.PixelSize + 0.5);
            if (ppem < first || ppem > last) return 0;
            var index = ppem - first;
            var word = table.U16(at + 6 + index / perWord * 2);
            var delta = (word >> (16 - bits - index % perWord * bits)) & ((1 << bits) - 1);
            if (delta >= 1 << (bits - 1)) delta -= 1 << bits;
            return delta * execution.UnitsPerEm / execution.PixelSize;
        } catch (FontLayoutException) {
            // An optional malformed correction must not discard the usable design-unit positioning.
            return 0;
        }
    }
}
