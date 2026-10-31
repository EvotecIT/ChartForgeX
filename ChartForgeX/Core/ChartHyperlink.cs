using System;

namespace ChartForgeX.Core;

/// <summary>One navigation policy for authored chart links and existing topology navigation.</summary>
internal static class ChartHyperlink {
    internal static string? SafeHref(string? href, bool allowTelephone = false, bool allowWhitespace = false) {
        if (string.IsNullOrWhiteSpace(href)) return null;
        var value = href!.Trim();
        foreach (var c in value) if (char.IsControl(c) || !allowWhitespace && char.IsWhiteSpace(c) || c == '\\') return null;
        if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri)) return null;
        if (!uri.IsAbsoluteUri) return value;
        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase)
            || allowTelephone && uri.Scheme.Equals("tel", StringComparison.OrdinalIgnoreCase) ? value : null;
    }
}
