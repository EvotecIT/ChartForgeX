using System;
using System.IO;
using System.Linq;
using System.Xml;

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

    /// <summary>Sets the preserveAspectRatio value used when rendering SVG or image artwork.</summary>
    public TopologyIconArtwork WithPreserveAspectRatio(string preserveAspectRatio) {
        PreserveAspectRatio = RequiredText(preserveAspectRatio, nameof(preserveAspectRatio));
        return this;
    }

    /// <summary>
    /// Returns whether an inline SVG fragment is safe enough for report embedding.
    /// </summary>
    public static bool IsSafeSvgFragment(string? svgBody) {
        if (string.IsNullOrWhiteSpace(svgBody)) return true;
        try {
            var settings = new XmlReaderSettings {
                ConformanceLevel = ConformanceLevel.Document,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using var input = new StringReader("<cfx-root xmlns:xlink=\"http://www.w3.org/1999/xlink\">" + svgBody + "</cfx-root>");
            using var reader = XmlReader.Create(input, settings);
            var styleDepth = -1;
            while (reader.Read()) {
                if (reader.NodeType == XmlNodeType.Element) {
                    if (IsUnsafeSvgElement(reader.LocalName)) return false;
                    if (string.Equals(reader.LocalName, "style", StringComparison.OrdinalIgnoreCase) && !reader.IsEmptyElement) styleDepth = reader.Depth;
                    if (!reader.HasAttributes) continue;
                    while (reader.MoveToNextAttribute()) {
                        if (IsEventHandlerAttribute(reader.LocalName) || HasUnsafeScriptScheme(reader.Value)) return false;
                        if ((string.Equals(reader.LocalName, "href", StringComparison.OrdinalIgnoreCase) || string.Equals(reader.LocalName, "style", StringComparison.OrdinalIgnoreCase)) && reader.Value.IndexOf('\\') >= 0) return false;
                    }
                    reader.MoveToElement();
                } else if ((reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA) && styleDepth >= 0 && reader.Depth > styleDepth) {
                    if (reader.Value.IndexOf('\\') >= 0 || reader.Value.IndexOf("@import", StringComparison.OrdinalIgnoreCase) >= 0 || HasUnsafeScriptScheme(reader.Value)) return false;
                } else if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == styleDepth && string.Equals(reader.LocalName, "style", StringComparison.OrdinalIgnoreCase)) {
                    styleDepth = -1;
                } else if (reader.NodeType == XmlNodeType.ProcessingInstruction || reader.NodeType == XmlNodeType.DocumentType) {
                    return false;
                }
            }
            return true;
        } catch (XmlException) {
            return false;
        }
    }

    /// <summary>
    /// Returns whether an image href is safe enough for report embedding.
    /// </summary>
    public static bool IsSafeImageHref(string? href) {
        if (string.IsNullOrWhiteSpace(href)) return true;
        return !HasUnsafeScriptScheme(href!);
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

    private static bool IsUnsafeSvgElement(string localName) =>
        string.Equals(localName, "script", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "foreignObject", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "iframe", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "object", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "embed", StringComparison.OrdinalIgnoreCase);

    private static bool IsEventHandlerAttribute(string localName) =>
        localName.Length > 2 && localName.StartsWith("on", StringComparison.OrdinalIgnoreCase) && char.IsLetter(localName[2]);

    private static bool HasUnsafeScriptScheme(string value) {
        var normalized = new char[value.Length];
        var length = 0;
        for (var index = 0; index < value.Length; index++) {
            if (!char.IsWhiteSpace(value[index]) && !char.IsControl(value[index])) normalized[length++] = value[index];
        }
        var compact = new string(normalized, 0, length);
        return compact.IndexOf("javascript:", StringComparison.OrdinalIgnoreCase) >= 0
            || compact.IndexOf("vbscript:", StringComparison.OrdinalIgnoreCase) >= 0
            || compact.IndexOf("data:text/html", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool PathLooksRooted(string value) {
        return value.StartsWith("/", StringComparison.Ordinal)
            || value.StartsWith("\\", StringComparison.Ordinal)
            || (value.Length > 1 && value[1] == ':');
    }
}
