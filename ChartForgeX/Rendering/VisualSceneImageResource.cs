using System;
using System.Text;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Detached host image metadata. Its owning group contains the deterministic native fallback geometry.</summary>
internal sealed class VisualSceneImageResource : VisualSceneNode {
    private readonly Lazy<string> _href;
    internal VisualSceneImageResource(string href, ChartRect bounds, string? aspect, double opacity, string? role, string? id) : base(role, id) {
        if (!IsSafeHref(href)) throw new ArgumentException("Image resources require a safe relative or HTTP(S) reference.", nameof(href));
        _href = new Lazy<string>(() => href.Trim()); Bounds = bounds; PreserveAspectRatio = string.IsNullOrWhiteSpace(aspect) ? "xMidYMid meet" : aspect!.Trim(); Opacity = opacity;
    }
    private VisualSceneImageResource(string svg, ChartRect bounds, string? aspect, double opacity, string? role, string? id, bool embedded) : base(role, id) {
        // Only trusted, already resolved static SVG enters this internal seam; external host references use IsSafeHref.
        if (svg == null) throw new ArgumentNullException(nameof(svg));
        if (Encoding.UTF8.GetByteCount(svg) > 64L * 1024 * 1024) throw new ArgumentException("Embedded SVG exceeds the 64 MiB resource budget.", nameof(svg));
        // Raster backends use the native fallback. Encode and cache vector resources only when SVG needs them.
        _href = new Lazy<string>(() => "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg)));
        Bounds = bounds; PreserveAspectRatio = string.IsNullOrWhiteSpace(aspect) ? "xMidYMid meet" : aspect!.Trim(); Opacity = opacity;
    }
    internal static VisualSceneImageResource EmbeddedSvg(string resolvedSvg, ChartRect bounds, string? aspect, double opacity, string? role, string? id) =>
        new(resolvedSvg, bounds, aspect, opacity, role, id, embedded: true);
    internal string Href => _href.Value;
    internal ChartRect Bounds { get; }
    internal string PreserveAspectRatio { get; }
    internal double Opacity { get; }

    internal static bool IsSafeHref(string? href) {
        if (string.IsNullOrWhiteSpace(href) || href!.Length > 8192) return false;
        var value = href.Trim();
        foreach (var c in value) if (char.IsControl(c) || c == '\\') return false;
        if (value.StartsWith("/", StringComparison.Ordinal) && !value.StartsWith("//", StringComparison.Ordinal)) return true;
        if (value.StartsWith("//", StringComparison.Ordinal)) value = "https:" + value;
        if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri)) return false;
        return !uri.IsAbsoluteUri || uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
