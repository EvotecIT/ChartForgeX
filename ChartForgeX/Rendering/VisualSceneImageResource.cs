using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Detached host image metadata. Its owning group contains the deterministic native fallback geometry.</summary>
internal sealed class VisualSceneImageResource : VisualSceneNode {
    internal VisualSceneImageResource(string href, ChartRect bounds, string? aspect, double opacity, string? role, string? id) : base(role, id) {
        if (!IsSafeHref(href)) throw new ArgumentException("Image resources require a safe relative or HTTP(S) reference.", nameof(href));
        Href = href.Trim(); Bounds = bounds; PreserveAspectRatio = string.IsNullOrWhiteSpace(aspect) ? "xMidYMid meet" : aspect!.Trim(); Opacity = opacity;
    }
    internal string Href { get; }
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
