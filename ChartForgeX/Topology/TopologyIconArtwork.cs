using System;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Describes optional external artwork for a topology icon.
/// </summary>
public sealed class TopologyIconArtwork {
    /// <summary>Gets or sets an SVG viewBox used for inline SVG artwork.</summary>
    public string SvgViewBox { get; set; } = "0 0 24 24";

    /// <summary>Gets or sets trusted inline SVG fragment markup, such as path, rect, circle, and linearGradient elements.</summary>
    public string? SvgBody { get; set; }

    /// <summary>Gets or sets a relative or host-managed path to an SVG file.</summary>
    public string? SvgPath { get; set; }

    /// <summary>Gets or sets a relative or host-managed path to a generated preview image.</summary>
    public string? PreviewPath { get; set; }

    /// <summary>Gets or sets an image href, usually a data URI or host-managed URL.</summary>
    public string? ImageHref { get; set; }

    /// <summary>Gets or sets the preserveAspectRatio value used when rendering SVG or image artwork.</summary>
    public string PreserveAspectRatio { get; set; } = "xMidYMid meet";

    /// <summary>Gets whether this artwork defines an inline SVG fragment.</summary>
    public bool HasSvgBody => !string.IsNullOrWhiteSpace(SvgBody);

    /// <summary>Gets whether this artwork references an SVG file.</summary>
    public bool HasSvgPath => !string.IsNullOrWhiteSpace(SvgPath);

    /// <summary>Gets whether this artwork references a preview image.</summary>
    public bool HasPreviewPath => !string.IsNullOrWhiteSpace(PreviewPath);

    /// <summary>Gets whether this artwork defines an image href.</summary>
    public bool HasImageHref => !string.IsNullOrWhiteSpace(ImageHref);

    /// <summary>Creates inline SVG artwork.</summary>
    public static TopologyIconArtwork InlineSvg(string svgBody, string svgViewBox = "0 0 24 24") {
        return new TopologyIconArtwork {
            SvgBody = RequiredText(svgBody, nameof(svgBody)),
            SvgViewBox = RequiredText(svgViewBox, nameof(svgViewBox))
        };
    }

    /// <summary>Creates image artwork.</summary>
    public static TopologyIconArtwork Image(string href, string svgViewBox = "0 0 24 24") {
        return new TopologyIconArtwork {
            ImageHref = RequiredText(href, nameof(href)),
            SvgViewBox = RequiredText(svgViewBox, nameof(svgViewBox))
        };
    }

    /// <summary>Creates SVG file artwork.</summary>
    public static TopologyIconArtwork SvgFile(string path, string svgViewBox = "0 0 24 24", string? previewPath = null) {
        return new TopologyIconArtwork {
            SvgPath = RequiredText(path, nameof(path)),
            SvgViewBox = RequiredText(svgViewBox, nameof(svgViewBox)),
            PreviewPath = string.IsNullOrWhiteSpace(previewPath) ? null : previewPath!.Trim()
        };
    }

    /// <summary>
    /// Returns whether an inline SVG fragment is safe enough for report embedding.
    /// </summary>
    public static bool IsSafeSvgFragment(string? svgBody) {
        if (string.IsNullOrWhiteSpace(svgBody)) return true;
        var body = svgBody!;
        return body.IndexOf("<script", StringComparison.OrdinalIgnoreCase) < 0
            && body.IndexOf("<foreignObject", StringComparison.OrdinalIgnoreCase) < 0
            && body.IndexOf("<iframe", StringComparison.OrdinalIgnoreCase) < 0
            && body.IndexOf("<object", StringComparison.OrdinalIgnoreCase) < 0
            && body.IndexOf("<embed", StringComparison.OrdinalIgnoreCase) < 0
            && body.IndexOf("javascript:", StringComparison.OrdinalIgnoreCase) < 0
            && !ContainsEventHandlerAttribute(body);
    }

    /// <summary>
    /// Returns whether an image href is safe enough for report embedding.
    /// </summary>
    public static bool IsSafeImageHref(string? href) {
        if (string.IsNullOrWhiteSpace(href)) return true;
        var value = href!.Trim();
        return value.IndexOf("javascript:", StringComparison.OrdinalIgnoreCase) < 0
            && value.IndexOf("vbscript:", StringComparison.OrdinalIgnoreCase) < 0
            && value.IndexOf("data:text/html", StringComparison.OrdinalIgnoreCase) < 0;
    }

    /// <summary>
    /// Returns whether an artwork path is safe enough for pack-local file resolution.
    /// </summary>
    public static bool IsSafeAssetPath(string? path) {
        if (string.IsNullOrWhiteSpace(path)) return true;
        var value = path!.Trim();
        return value.IndexOf("javascript:", StringComparison.OrdinalIgnoreCase) < 0
            && value.IndexOf("vbscript:", StringComparison.OrdinalIgnoreCase) < 0
            && value.IndexOf("://", StringComparison.OrdinalIgnoreCase) < 0
            && !PathLooksRooted(value)
            && !value.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).Contains("..", StringComparer.Ordinal);
    }

    /// <summary>Gets whether inline SVG artwork can be embedded safely.</summary>
    public bool IsSafe => IsSafeSvgFragment(SvgBody) && IsSafeImageHref(ImageHref) && IsSafeAssetPath(SvgPath) && IsSafeAssetPath(PreviewPath);

    private static string RequiredText(string? value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be empty.", parameterName);
        return value!.Trim();
    }

    private static bool ContainsEventHandlerAttribute(string value) {
        for (var index = 0; index < value.Length - 3; index++) {
            if (!char.IsWhiteSpace(value[index])) continue;
            if (char.ToLowerInvariant(value[index + 1]) != 'o' || char.ToLowerInvariant(value[index + 2]) != 'n') continue;
            var cursor = index + 3;
            if (cursor >= value.Length || !char.IsLetter(value[cursor])) continue;
            while (cursor < value.Length && (char.IsLetterOrDigit(value[cursor]) || value[cursor] == '-' || value[cursor] == '_' || value[cursor] == ':')) cursor++;
            while (cursor < value.Length && char.IsWhiteSpace(value[cursor])) cursor++;
            if (cursor < value.Length && value[cursor] == '=') return true;
        }

        return false;
    }

    private static bool PathLooksRooted(string value) {
        return value.StartsWith("/", StringComparison.Ordinal)
            || value.StartsWith("\\", StringComparison.Ordinal)
            || (value.Length > 1 && value[1] == ':');
    }
}
