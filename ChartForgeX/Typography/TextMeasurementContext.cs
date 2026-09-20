using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>Resolves a font once for a sequence of layout measurements.</summary>
internal sealed class TextMeasurementContext {
    private readonly TrueTypeFont? _font;
    private readonly TrueTypeFont? _boldFont;
    private readonly string _family;
    private readonly TextMeasurementMode _mode;
    private readonly Dictionary<(string Text, double Size, bool Bold), double> _widths = new();
    private readonly object _gate = new();
    private const int MaximumCachedWidths = 4096;

    internal TextMeasurementContext(string family, TextMeasurementMode mode = TextMeasurementMode.PortableEstimate) {
        if (mode != TextMeasurementMode.PortableEstimate && mode != TextMeasurementMode.InstalledFonts)
            throw new ArgumentOutOfRangeException(nameof(mode));
        _family = family;
        _mode = mode;
        if (mode == TextMeasurementMode.PortableEstimate) return;
        _font = TypographyFontResolver.Resolve(new FontSpec { Family = family });
        _boldFont = TypographyFontResolver.ResolveBoldMeasurementFace(family);
    }

    internal double Measure(string value, double size, bool bold) {
        if (value.Length == 0) return 0;
        // Preserve portable layout's historical estimate unless font-dependent
        // geometry is explicitly requested. Do not probe the host in this mode.
        if (_mode == TextMeasurementMode.PortableEstimate) return value.Length * size * (bold ? 0.62 : 0.56);
        var key = (value, size, bold);
        lock (_gate) {
            if (_widths.TryGetValue(key, out var cached)) return cached;
            var width = TextLayoutEngine.MeasureWidth(value,
                new TextStyle { Font = new FontSpec { Family = _family, Weight = bold ? 700 : 400 }, FontSize = size }, _font);
            if (bold && _boldFont != null) {
                width = Math.Max(width, TextLayoutEngine.MeasureWidth(value,
                    new TextStyle { Font = new FontSpec { Family = _family, Weight = 400 }, FontSize = size }, _boldFont));
            }
            // A render owns this cache; cap it for scenes with many unique labels.
            if (_widths.Count < MaximumCachedWidths) _widths.Add(key, width);
            return width;
        }
    }
}
