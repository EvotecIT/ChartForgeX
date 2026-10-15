using System;
using System.Collections.Generic;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Typography;

/// <summary>Resolves a font once for a sequence of layout measurements.</summary>
internal sealed class TextMeasurementContext {
    private readonly FontSpec _font;
    private readonly TextMeasurementMode _mode;
    private readonly Dictionary<int, VisualSceneTextFace> _faces = new();
    private static readonly LabelPlacementService Service = new();

    internal TextMeasurementContext(string family, TextMeasurementMode mode = TextMeasurementMode.PortableEstimate)
        : this(new FontSpec { Family = family }, mode) { }

    internal TextMeasurementContext(FontSpec font, TextMeasurementMode mode = TextMeasurementMode.PortableEstimate) {
        if (mode != TextMeasurementMode.PortableEstimate && mode != TextMeasurementMode.InstalledFonts)
            throw new ArgumentOutOfRangeException(nameof(mode));
        _font = (font ?? throw new ArgumentNullException(nameof(font))).Clone();
        _mode = mode;
    }

    internal FontSpec Font => _font.Clone();
    internal TextMeasurementMode Mode => _mode;

    /// <summary>Measures topology allocations with their selected policy and emitted font weight.</summary>
    internal TextMetrics MeasureMetrics(string value, double size, int weight) {
        if (_mode == TextMeasurementMode.PortableEstimate) {
            var lineHeight = size * 1.5;
            return new TextMetrics(value.Length * size * (weight >= 600 ? .62 : .56), lineHeight, lineHeight);
        }
        if (!_faces.TryGetValue(weight, out var face)) {
            face = new VisualSceneTextFace(_font, weight);
            _faces.Add(weight, face);
        }
        return face.Prepare(value, size).Metrics;
    }

    internal double Measure(string value, double size, bool bold) {
        if (value.Length == 0) return 0;
        // Preserve portable layout's historical estimate unless font-dependent
        // geometry is explicitly requested. Do not probe the host in this mode.
        if (_mode == TextMeasurementMode.PortableEstimate) return value.Length * size * (bold ? 0.62 : 0.56);
        var font = _font.Clone(); font.Weight = bold ? 700 : 400;
        return Service.Measure(value, new TextStyle { Font = font, FontSize = size }).Width;
    }

    internal double MeasureLineHeight(double size, bool bold) => _mode == TextMeasurementMode.PortableEstimate
        ? size * 1.5 : new VisualSceneTextFace(_font, bold ? 700 : 400).Prepare("Ag", size).Metrics.LineHeight;
}
