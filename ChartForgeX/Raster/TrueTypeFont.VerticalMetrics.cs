using System;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    /// <summary>Bounds the ascenders a native SVG text host can choose from this face's horizontal and Windows metrics.</summary>
    /// <remarks>The painters retain their existing baseline. Layouts use this envelope only when text must clear another mark.</remarks>
    internal double AscentEnvelope(double fontSize) {
        var maximum = Ascent(fontSize);
        if (_os2 < 0 || !_tableLengths.TryGetValue("OS/2", out var length)) return maximum;
        // Legacy version-zero tables can stop before the vertical fields.
        if (length >= 70 && InBounds(_os2 + 68, 2))
            maximum = Math.Max(maximum, (ReadInt16(_data, _os2 + 68) + MetricVariation("hasc")) * ScaleFor(fontSize));
        if (length >= 76 && InBounds(_os2 + 74, 2))
            maximum = Math.Max(maximum, (ReadUInt16(_data, _os2 + 74) + MetricVariation("hcla")) * ScaleFor(fontSize));
        return maximum;
    }
}
