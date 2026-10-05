using System;
using System.Collections.Generic;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Typography;

/// <summary>Resolves a font once for a sequence of layout measurements.</summary>
internal sealed class TextMeasurementContext {
    private readonly string _family;
    private readonly TextMeasurementMode _mode;
    private static readonly LabelPlacementService Service = new();

    internal TextMeasurementContext(string family, TextMeasurementMode mode = TextMeasurementMode.InstalledFonts) {
        if (mode != TextMeasurementMode.PortableEstimate && mode != TextMeasurementMode.InstalledFonts)
            throw new ArgumentOutOfRangeException(nameof(mode));
        _family = family;
        _mode = mode;
    }

    internal double Measure(string value, double size, bool bold) {
        if (value.Length == 0) return 0;
        // Preserve portable layout's historical estimate unless font-dependent
        // geometry is explicitly requested. Do not probe the host in this mode.
        if (_mode == TextMeasurementMode.PortableEstimate) return value.Length * size * (bold ? 0.62 : 0.56);
        return Service.Measure(value, new TextStyle { Font = new FontSpec { Family = _family, Weight = bold ? 700 : 400 }, FontSize = size }).Width;
    }
}
