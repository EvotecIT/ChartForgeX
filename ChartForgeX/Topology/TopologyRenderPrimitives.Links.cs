using System;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static string? SafeHref(string? href) {
        if (string.IsNullOrWhiteSpace(href)) return null;
        var value = href!.Trim();
        for (var index = 0; index < value.Length; index++) {
            if (char.IsControl(value[index])) return null;
        }

        if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri? uri)) return null;
        if (!uri.IsAbsoluteUri) return value;
        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals("tel", StringComparison.OrdinalIgnoreCase)
            ? value
            : null;
    }
}
