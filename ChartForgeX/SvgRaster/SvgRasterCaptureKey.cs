using System;

namespace ChartForgeX.SvgRaster;

/// <summary>Exact inputs to an embedded SVG capture, including its existing recursion budget and opt-in renderer context.</summary>
internal readonly struct SvgRasterCaptureKey : IEquatable<SvgRasterCaptureKey> {
    private readonly string _source;
    private readonly string? _viewBox, _aspect;
    private readonly bool _fragment;
    private readonly int _width, _height, _imageDepth;
    private readonly SvgRasterFilterContext? _filters;
    private readonly SvgRasterDiagnostics? _diagnostics;
    internal SvgRasterCaptureKey(string source, bool fragment, string? viewBox, string? aspect, int width, int height,
        int imageDepth, SvgRasterFilterContext? filters = null, SvgRasterDiagnostics? diagnostics = null) {
        _source = source; _fragment = fragment; _viewBox = viewBox; _aspect = aspect;
        _width = width; _height = height; _imageDepth = imageDepth; _filters = filters; _diagnostics = diagnostics;
    }
    public bool Equals(SvgRasterCaptureKey other) => _fragment == other._fragment && _width == other._width && _height == other._height
        && _imageDepth == other._imageDepth && string.Equals(_source, other._source, StringComparison.Ordinal)
        && string.Equals(_viewBox, other._viewBox, StringComparison.Ordinal) && string.Equals(_aspect, other._aspect, StringComparison.Ordinal)
        && ReferenceEquals(_filters, other._filters) && ReferenceEquals(_diagnostics, other._diagnostics);
    public override bool Equals(object? other) => other is SvgRasterCaptureKey key && Equals(key);
    public override int GetHashCode() {
        unchecked {
            var hash = StringComparer.Ordinal.GetHashCode(_source);
            hash = hash * 31 + (_viewBox == null ? 0 : StringComparer.Ordinal.GetHashCode(_viewBox));
            hash = hash * 31 + (_aspect == null ? 0 : StringComparer.Ordinal.GetHashCode(_aspect));
            hash = hash * 31 + _fragment.GetHashCode(); hash = hash * 31 + _width;
            hash = hash * 31 + _height; hash = hash * 31 + _imageDepth;
            hash = hash * 31 + (_filters?.GetHashCode() ?? 0); return hash * 31 + (_diagnostics?.GetHashCode() ?? 0);
        }
    }
}
