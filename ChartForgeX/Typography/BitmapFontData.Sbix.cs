using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Typography;

internal sealed partial class BitmapFontData {
    private BitmapGlyph? ReadSbix(BitmapStrike strike, ushort glyph) {
        var table = _sbix!.Value; var original = SbixRecord(table, strike.At, glyph, out var originalLength);
        if (originalLength < 8) return null;
        var left = table.I16(original); var bottom = table.I16(original + 2);
        var at = original; var length = originalLength; var active = new HashSet<ushort>();
        while (table.Tag(at + 4) == "dupe") {
            if (length < 10 || active.Count >= 32 || !active.Add(glyph)) throw new FontLayoutException();
            glyph = (ushort)table.U16(at + 8); if (glyph >= _glyphCount) throw new FontLayoutException();
            at = SbixRecord(table, strike.At, glyph, out length); if (length < 8) return null;
        }
        var tag = table.Tag(at + 4); if (tag != "png " && tag != "jpg " && tag != "tiff") return null;
        if (!Decode(table, at + 8, length - 8, out var image, pngOnly: tag == "png ")) return null;
        var scale = _unitsPerEm / (double)strike.PpemY;
        return new BitmapGlyph { Image = image, Bounds = new ChartRect(left * scale, bottom * scale, image.Width * scale, image.Height * scale),
            OutlineAnchor = true, DrawOutline = (table.U16(2) & 2) != 0 };
    }
    private int SbixRecord(FontTableReader table, int strike, ushort glyph, out int length) {
        var start = table.U32(strike + 4 + glyph * 4); var end = table.U32(strike + 8 + glyph * 4);
        if (end < start || end > table.Length - strike || start < 4 + (_glyphCount + 1) * 4) throw new FontLayoutException();
        length = (int)(end - start); var at = strike + (int)start; table.Require(at, length); return at;
    }
}
