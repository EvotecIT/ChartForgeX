using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Svg;
using ChartForgeX.SvgRaster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Stories;

/// <summary>Displays a resolved raster artifact with an optional vector representation.</summary>
public sealed class VisualStoryMediaSurface : VisualStorySurface {
    private const string SvgNamespace = "http://www.w3.org/2000/svg";
    private static readonly string[] UnsafeCssResourceFunctions = {
        "image", "image-set", "-webkit-image-set", "cross-fade", "-webkit-cross-fade",
        "element", "-moz-element", "paint", "src"
    };
    private static readonly string[] SystemColorIdentifiers = {
        "AccentColor", "AccentColorText", "ActiveText", "ButtonBorder", "ButtonFace", "ButtonText",
        "Canvas", "CanvasText", "Field", "FieldText", "GrayText", "Highlight", "HighlightText",
        "LinkText", "Mark", "MarkText", "SelectedItem", "SelectedItemText", "VisitedText",
        "ActiveBorder", "ActiveCaption", "AppWorkspace", "Background", "ButtonHighlight", "ButtonShadow",
        "CaptionText", "InactiveBorder", "InactiveCaption", "InactiveCaptionText", "InfoBackground", "InfoText",
        "Menu", "MenuText", "Scrollbar", "ThreeDDarkShadow", "ThreeDFace", "ThreeDHighlight",
        "ThreeDLightShadow", "ThreeDShadow", "Window", "WindowFrame", "WindowText"
    };

    /// <summary>Captures a common static source without referencing its optional producer package.</summary>
    public VisualStoryMediaSurface(IStaticVisualSource source, string accessibleText)
        : this((source ?? throw new ArgumentNullException(nameof(source))).RenderRgba(), accessibleText,
            source.RenderSvg("story-media")) { }

    /// <summary>Captures native prepared geometry as the story's static vector and raster media.</summary>
    public VisualStoryMediaSurface(PreparedVisual prepared, string accessibleText)
        : base(VisualStorySurfaceKind.Media, accessibleText) {
        if (prepared == null) throw new ArgumentNullException(nameof(prepared));
        Raster = RequireRaster(prepared.ToRgba());
        var svg = prepared.ToSvg("story-media");
        // Native raster rounds fractional logical dimensions up to whole pixels. Keep that same
        // padded viewport in the vector representation rather than stretching its geometry.
        if (prepared.Size.Width != Raster.Width || prepared.Size.Height != Raster.Height) {
            var document = XDocument.Parse(svg);
            var root = document.Root!;
            root.SetAttributeValue("width", Raster.Width);
            root.SetAttributeValue("height", Raster.Height);
            root.SetAttributeValue("viewBox", "0 0 " + Raster.Width.ToString(CultureInfo.InvariantCulture)
                + " " + Raster.Height.ToString(CultureInfo.InvariantCulture));
            svg = document.ToString(SaveOptions.DisableFormatting);
        }
        Svg = RequireSvg(svg, Raster.Width, Raster.Height);
    }

    /// <summary>Initializes a raster artifact with an optional static SVG representation that has the same intrinsic aspect ratio.</summary>
    public VisualStoryMediaSurface(byte[] rasterBytes, string accessibleText, string? svg = null)
        : base(VisualStorySurfaceKind.Media, accessibleText) {
        if (rasterBytes == null) throw new ArgumentNullException(nameof(rasterBytes));
        Raster = RasterImageDecoder.Decode(rasterBytes);
        Svg = RequireSvg(svg, Raster.Width, Raster.Height);
    }

    /// <summary>Initializes an RGBA artifact with an optional static SVG representation that has the same intrinsic aspect ratio.</summary>
    public VisualStoryMediaSurface(RgbaImage raster, string accessibleText, string? svg = null)
        : base(VisualStorySurfaceKind.Media, accessibleText) {
        Raster = RequireRaster(raster);
        Svg = RequireSvg(svg, Raster.Width, Raster.Height);
    }

    /// <summary>Gets the resolved raster representation used by PNG, GIF, and APNG output.</summary>
    public RgbaImage Raster { get; }

    /// <summary>Gets an optional resolved SVG representation used by SVG output.</summary>
    public string Svg { get; }

    private static RgbaImage RequireRaster(RgbaImage raster) {
        if (raster.Width <= 0 ||
            raster.Height <= 0 ||
            raster.Pixels == null ||
            raster.Pixels.LongLength < checked((long)raster.Width * raster.Height * 4)) {
            throw new ArgumentException(
                "The raster representation must contain valid RGBA dimensions and pixels.",
                nameof(raster));
        }
        return raster;
    }

    private static string RequireSvg(string? svg, int rasterWidth, int rasterHeight) {
        if (string.IsNullOrWhiteSpace(svg)) return string.Empty;
        try {
            var settings = new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 64L * 1024 * 1024
            };
            using var text = new StringReader(svg);
            using var reader = XmlReader.Create(text, settings);
            var foundRoot = false;
            while (reader.Read()) {
                if (reader.NodeType == XmlNodeType.ProcessingInstruction) {
                    throw new ArgumentException("The vector representation must not contain processing instructions.", nameof(svg));
                }
                if (reader.NodeType != XmlNodeType.Element) continue;
                foundRoot = true;
                break;
            }
            if (!foundRoot ||
                !string.Equals(reader.LocalName, "svg", StringComparison.Ordinal) ||
                !string.Equals(reader.NamespaceURI, SvgNamespace, StringComparison.Ordinal)) {
                throw new ArgumentException("The vector representation must be a valid SVG document.", nameof(svg));
            }
            ValidateCompatibleAspectRatio(reader, rasterWidth, rasterHeight);
            StringBuilder? styleText = null;
            ValidateSvgNode(reader);
            while (reader.Read()) {
                if (reader.NodeType == XmlNodeType.ProcessingInstruction) {
                    throw new ArgumentException("The vector representation must not contain processing instructions.", nameof(svg));
                }
                ValidateSvgNode(reader);
                if (reader.NodeType == XmlNodeType.Element &&
                    string.Equals(reader.LocalName, "style", StringComparison.OrdinalIgnoreCase)) {
                    if (styleText != null) {
                        throw new ArgumentException("The vector representation must not contain nested style elements.", nameof(svg));
                    }
                    styleText = reader.IsEmptyElement ? null : new StringBuilder();
                } else if (styleText != null &&
                           (reader.NodeType == XmlNodeType.Text ||
                            reader.NodeType == XmlNodeType.CDATA)) {
                    styleText.Append(reader.Value);
                } else if (styleText != null &&
                           reader.NodeType == XmlNodeType.EndElement &&
                           string.Equals(reader.LocalName, "style", StringComparison.OrdinalIgnoreCase)) {
                    if (ContainsActiveCss(styleText.ToString())) {
                        throw new ArgumentException("The vector representation must not contain active or external CSS.", nameof(svg));
                    }
                    styleText = null;
                }
            }
            return svg!;
        } catch (XmlException ex) {
            throw new ArgumentException("The vector representation must be a valid SVG document.", nameof(svg), ex);
        }
    }

    private static void ValidateSvgNode(XmlReader reader) {
        if (reader.NodeType != XmlNodeType.Element) return;
        if (!string.Equals(reader.NamespaceURI, SvgNamespace, StringComparison.Ordinal)) {
            throw new ArgumentException("The vector representation must contain SVG elements only.", "svg");
        }
        if (IsActiveSvgElement(reader.LocalName)) {
            throw new ArgumentException("The vector representation must be static and cannot contain scripts, animation, or foreign content.", "svg");
        }
        if (!reader.HasAttributes) return;
        var isStyleElement = string.Equals(reader.LocalName, "style", StringComparison.OrdinalIgnoreCase);
        while (reader.MoveToNextAttribute()) {
            if (IsConditionalProcessingAttribute(reader.LocalName)) {
                throw new ArgumentException("The vector representation must not contain locale- or capability-dependent conditional content.", "svg");
            }
            if (isStyleElement &&
                string.Equals(reader.LocalName, "media", StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("The vector representation must not contain environment-dependent stylesheets.", "svg");
            }
            if (string.Equals(reader.LocalName, "base", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(reader.NamespaceURI, "http://www.w3.org/XML/1998/namespace", StringComparison.Ordinal)) {
                throw new ArgumentException("The vector representation must not change the base URI.", "svg");
            }
            if (reader.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("The vector representation must not contain event-handler attributes.", "svg");
            }
            if (string.Equals(reader.LocalName, "style", StringComparison.OrdinalIgnoreCase) &&
                ContainsActiveCss(reader.Value)) {
                throw new ArgumentException("The vector representation must not contain CSS animation or transitions.", "svg");
            }
            if ((string.Equals(reader.LocalName, "href", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(reader.LocalName, "src", StringComparison.OrdinalIgnoreCase)) &&
                IsUnsafeResourceReference(reader.Value)) {
                throw new ArgumentException("The vector representation must not contain executable or external resource references.", "svg");
            }
            if (IsPresentationIriAttribute(reader.LocalName) &&
                ContainsUnsafeCssReference(reader.Value)) {
                throw new ArgumentException("The vector representation must not contain external presentation resources.", "svg");
            }
            if (string.Equals(reader.LocalName, "color-scheme", StringComparison.OrdinalIgnoreCase) ||
                IsPresentationPaintAttribute(reader.LocalName) && ContainsSystemColorIdentifier(reader.Value)) {
                throw new ArgumentException("The vector representation must not depend on viewer system colors or color schemes.", "svg");
            }
        }
        reader.MoveToElement();
    }

    private static void ValidateCompatibleAspectRatio(XmlReader reader, int rasterWidth, int rasterHeight) {
        SvgRasterViewBox viewport;
        var viewBox = reader.GetAttribute("viewBox");
        try {
            if (!string.IsNullOrWhiteSpace(viewBox)) {
                viewport = SvgRasterViewBox.Parse(viewBox);
            } else if (!SvgRasterViewBox.TryFromDimensions(
                           reader.GetAttribute("width"),
                           reader.GetAttribute("height"),
                           out viewport)) {
                throw new ArgumentException(
                    "The vector representation must declare a viewBox or positive intrinsic width and height.",
                    "svg");
            }
        } catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is ArgumentOutOfRangeException) {
            throw new ArgumentException("The vector representation must declare a valid intrinsic viewport.", "svg", ex);
        }

        var rasterAspect = (double)rasterWidth / rasterHeight;
        var vectorAspect = viewport.Width / viewport.Height;
        var tolerance = Math.Max(0.000001, rasterAspect * 0.001);
        if (Math.Abs(rasterAspect - vectorAspect) > tolerance) {
            throw new ArgumentException(
                "The raster and vector representations must use the same intrinsic aspect ratio.",
                "svg");
        }
    }

    private static bool IsConditionalProcessingAttribute(string localName) =>
        string.Equals(localName, "systemLanguage", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "requiredFeatures", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "requiredExtensions", StringComparison.OrdinalIgnoreCase);

    private static bool IsPresentationIriAttribute(string localName) =>
        string.Equals(localName, "filter", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "fill", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "stroke", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "mask", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "clip-path", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "marker", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "marker-start", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "marker-mid", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "marker-end", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "cursor", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "color-profile", StringComparison.OrdinalIgnoreCase);

    private static bool IsPresentationPaintAttribute(string localName) =>
        string.Equals(localName, "color", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "fill", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "stroke", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "flood-color", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "lighting-color", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "stop-color", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "text-decoration-color", StringComparison.OrdinalIgnoreCase);

    private static bool IsActiveSvgElement(string localName) =>
        string.Equals(localName, "script", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "animate", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "animateMotion", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "animateTransform", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "set", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "discard", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "foreignObject", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsActiveCss(string value) {
        var withoutComments = StripCssComments(value);
        var normalized = DecodeCssEscapes(withoutComments);
        return ContainsCssAtRule(withoutComments) ||
               ContainsActiveCssProperty(normalized) ||
               ContainsUnsafeCssReferenceCore(normalized) ||
               ContainsSystemColorIdentifier(normalized);
    }

    private static bool ContainsCssAtRule(string value) {
        var quote = '\0';
        for (var index = 0; index < value.Length; index++) {
            var current = value[index];
            if (current == '\\') {
                if (quote == '\0' && TryDecodeCssEscape(value, ref index, out var decoded)) {
                    if (decoded == '@') return true;
                    continue;
                }
                SkipCssEscape(value, ref index);
                continue;
            }
            if (quote != '\0') {
                if (current == quote) quote = '\0';
                continue;
            }
            if (current == '\'' || current == '"') {
                quote = current;
                continue;
            }
            if (current == '@') return true;
        }
        return false;
    }

    private static bool TryDecodeCssEscape(string value, ref int index, out char decoded) {
        decoded = '\0';
        var cursor = index + 1;
        var scalar = 0;
        var digits = 0;
        while (cursor < value.Length && digits < 6) {
            var hex = HexValue(value[cursor]);
            if (hex < 0) break;
            scalar = checked(scalar * 16 + hex);
            cursor++;
            digits++;
        }
        if (digits == 0 || scalar > char.MaxValue) return false;
        index = cursor - 1;
        if (cursor < value.Length && char.IsWhiteSpace(value[cursor])) index++;
        decoded = (char)scalar;
        return true;
    }

    private static void SkipCssEscape(string value, ref int index) {
        var cursor = index + 1;
        var digits = 0;
        while (cursor < value.Length && digits < 6 && HexValue(value[cursor]) >= 0) {
            cursor++;
            digits++;
        }
        if (digits == 0 && cursor < value.Length) cursor++;
        if (digits > 0 && cursor < value.Length && char.IsWhiteSpace(value[cursor])) cursor++;
        index = cursor - 1;
    }

    private static bool ContainsActiveCssProperty(string value) {
        var segmentStart = 0;
        var quote = '\0';
        for (var index = 0; index < value.Length; index++) {
            var current = value[index];
            if (quote != '\0') {
                if (current == '\\') {
                    index++;
                } else if (current == quote) {
                    quote = '\0';
                }
                continue;
            }
            if (current == '\'' || current == '"') {
                quote = current;
                continue;
            }
            if (current == '{' || current == ';') {
                segmentStart = index + 1;
                continue;
            }
            if (current != ':') {
                continue;
            }
            var property = value.Substring(segmentStart, index - segmentStart).Trim();
            if (IsActiveCssPropertyName(property)) {
                return true;
            }
        }
        return false;
    }

    private static bool IsActiveCssPropertyName(string property) {
        if (property.Length == 0 || property.StartsWith("--", StringComparison.Ordinal)) {
            return false;
        }
        for (var index = 0; index < property.Length; index++) {
            var character = property[index];
            if (!char.IsLetterOrDigit(character) && character != '-') {
                return false;
            }
        }
        foreach (var prefix in new[] { "-webkit-", "-moz-", "-ms-", "-o-" }) {
            if (property.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                property = property.Substring(prefix.Length);
                break;
            }
        }
        return string.Equals(property, "animation", StringComparison.OrdinalIgnoreCase) ||
               property.StartsWith("animation-", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(property, "transition", StringComparison.OrdinalIgnoreCase) ||
               property.StartsWith("transition-", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(property, "behavior", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(property, "color-scheme", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsSystemColorIdentifier(string value) {
        var normalized = DecodeCssEscapes(StripCssComments(value));
        var quote = '\0';
        for (var index = 0; index < normalized.Length;) {
            if (quote != '\0') {
                if (normalized[index] == '\\' && index + 1 < normalized.Length) {
                    index += 2;
                } else {
                    if (normalized[index] == quote) quote = '\0';
                    index++;
                }
                continue;
            }
            if (normalized[index] == '\'' || normalized[index] == '"') {
                quote = normalized[index++];
                continue;
            }
            if (!IsCssIdentifierCharacter(normalized[index])) {
                index++;
                continue;
            }
            var start = index;
            while (index < normalized.Length && IsCssIdentifierCharacter(normalized[index])) index++;
            if (IsSystemColorIdentifier(normalized.Substring(start, index - start))) return true;
        }
        return false;
    }

    private static bool IsCssIdentifierCharacter(char value) =>
        char.IsLetterOrDigit(value) || value == '-' || value == '_';

    private static bool IsSystemColorIdentifier(string value) {
        foreach (var identifier in SystemColorIdentifiers) {
            if (string.Equals(value, identifier, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string StripCssComments(string value) {
        if (value.IndexOf("/*", StringComparison.Ordinal) < 0) {
            return value;
        }
        var output = new StringBuilder(value.Length);
        var quote = '\0';
        for (var index = 0; index < value.Length; index++) {
            if (quote != '\0') {
                output.Append(value[index]);
                if (value[index] == '\\' && index + 1 < value.Length) {
                    output.Append(value[++index]);
                } else if (value[index] == quote) {
                    quote = '\0';
                }
                continue;
            }
            if (value[index] == '\'' || value[index] == '"') {
                quote = value[index];
                output.Append(value[index]);
                continue;
            }
            if (value[index] == '/' && index + 1 < value.Length && value[index + 1] == '*') {
                var end = value.IndexOf("*/", index + 2, StringComparison.Ordinal);
                if (end < 0) {
                    break;
                }
                index = end + 1;
                continue;
            }
            output.Append(value[index]);
        }
        return output.ToString();
    }

    private static bool ContainsUnsafeCssReference(string value) =>
        ContainsUnsafeCssReferenceCore(DecodeCssEscapes(StripCssComments(value)));

    private static bool ContainsUnsafeCssReferenceCore(string value) {
        foreach (var function in UnsafeCssResourceFunctions) {
            if (ContainsCssFunction(value, function)) return true;
        }
        var cursor = 0;
        while (cursor < value.Length) {
            var start = FindCssFunction(value, "url", cursor);
            if (start < 0) return false;
            var contentStart = start + 4;
            var end = FindCssFunctionEnd(value, contentStart);
            if (end < 0) return true;
            var target = value.Substring(contentStart, end - contentStart)
                .Trim()
                .Trim('\'', '"');
            if (!target.StartsWith("#", StringComparison.Ordinal)) return true;
            cursor = end + 1;
        }
        return false;
    }

    private static bool ContainsCssFunction(string value, string function) =>
        FindCssFunction(value, function, 0) >= 0;

    private static int FindCssFunction(string value, string function, int cursor) {
        var quote = '\0';
        for (var index = cursor; index < value.Length; index++) {
            var current = value[index];
            if (quote != '\0') {
                if (current == '\\' && index + 1 < value.Length) {
                    index++;
                } else if (current == quote) {
                    quote = '\0';
                }
                continue;
            }
            if (current == '\'' || current == '"') {
                quote = current;
                continue;
            }
            if (current == '\\' && index + 1 < value.Length) {
                index++;
                continue;
            }
            if (index + function.Length >= value.Length ||
                string.Compare(value, index, function, 0, function.Length, StringComparison.OrdinalIgnoreCase) != 0) {
                continue;
            }
            var end = index + function.Length;
            var startsAtIdentifierBoundary = index == 0 || !IsCssIdentifierCharacter(value[index - 1]);
            if (startsAtIdentifierBoundary && value[end] == '(') return index;
        }
        return -1;
    }

    private static int FindCssFunctionEnd(string value, int contentStart) {
        var quote = '\0';
        for (var index = contentStart; index < value.Length; index++) {
            var current = value[index];
            if (quote != '\0') {
                if (current == '\\' && index + 1 < value.Length) {
                    index++;
                } else if (current == quote) {
                    quote = '\0';
                }
                continue;
            }
            if (current == '\'' || current == '"') {
                quote = current;
            } else if (current == ')') {
                return index;
            }
        }
        return -1;
    }

    private static string DecodeCssEscapes(string value) {
        if (value.IndexOf('\\') < 0) return value;
        var output = new StringBuilder(value.Length);
        var quote = '\0';
        for (var index = 0; index < value.Length; index++) {
            var current = value[index];
            if (current != '\\' || index + 1 >= value.Length) {
                if (current == '\'' || current == '"') {
                    if (quote == '\0') {
                        quote = current;
                    } else if (current == quote) {
                        quote = '\0';
                    }
                }
                output.Append(current);
                continue;
            }

            var next = value[index + 1];
            if (next == '\r' || next == '\n' || next == '\f') {
                index++;
                if (next == '\r' && index + 1 < value.Length && value[index + 1] == '\n') index++;
                continue;
            }

            var scalar = 0;
            var digits = 0;
            while (index + 1 < value.Length && digits < 6) {
                var hex = HexValue(value[index + 1]);
                if (hex < 0) break;
                scalar = checked(scalar * 16 + hex);
                index++;
                digits++;
            }
            if (digits == 0) {
                AppendDecodedCssScalar(output, next);
                index++;
                continue;
            }
            if (index + 1 < value.Length && char.IsWhiteSpace(value[index + 1])) {
                index++;
                if (value[index] == '\r' && index + 1 < value.Length && value[index + 1] == '\n') index++;
            }
            AppendDecodedCssScalar(output, scalar);
        }
        return output.ToString();
    }

    private static void AppendDecodedCssScalar(StringBuilder output, int scalar) {
        if (scalar == 0 ||
            scalar > 0x10FFFF ||
            scalar >= 0xD800 && scalar <= 0xDFFF ||
            scalar == '\\' ||
            scalar == '\'' ||
            scalar == '"') {
            output.Append('\uFFFD');
            return;
        }
        output.Append(char.ConvertFromUtf32(scalar));
    }

    private static int HexValue(char value) {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'a' && value <= 'f') return value - 'a' + 10;
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        return -1;
    }

    private static bool IsUnsafeResourceReference(string value) {
        var reference = value.Trim();
        if (reference.Length == 0 || reference[0] == '#') {
            return false;
        }
        if (reference.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) {
            return !IsSafeStaticRasterDataUri(reference);
        }
        return true;
    }

    private static bool IsSafeStaticRasterDataUri(string reference) {
        var comma = reference.IndexOf(',');
        if (comma <= 5) return false;
        var metadata = reference.Substring(5, comma - 5);
        var tokens = metadata.Split(';');
        var mediaType = tokens[0].Trim();
        if (string.Equals(mediaType, "image/jpeg", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mediaType, "image/bmp", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }
        if (!string.Equals(mediaType, "image/png", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }
        var base64 = false;
        for (var index = 1; index < tokens.Length; index++) {
            if (string.Equals(tokens[index].Trim(), "base64", StringComparison.OrdinalIgnoreCase)) {
                base64 = true;
                break;
            }
        }
        if (!base64) return false;
        try {
            var bytes = Convert.FromBase64String(reference.Substring(comma + 1));
            return PngReader.IsPng(bytes) && !ContainsPngAnimationControl(bytes);
        } catch (FormatException) {
            return false;
        }
    }

    private static bool ContainsPngAnimationControl(byte[] bytes) {
        var offset = 8;
        var chunkIndex = 0;
        var sawEnd = false;
        while (offset + 12 <= bytes.Length) {
            var length = ((uint)bytes[offset] << 24) |
                         ((uint)bytes[offset + 1] << 16) |
                         ((uint)bytes[offset + 2] << 8) |
                         bytes[offset + 3];
            if (length > int.MaxValue || length > bytes.Length - offset - 12) return true;
            if (chunkIndex == 0 &&
                !(bytes[offset + 4] == 'I' &&
                  bytes[offset + 5] == 'H' &&
                  bytes[offset + 6] == 'D' &&
                  bytes[offset + 7] == 'R')) {
                return true;
            }
            if (bytes[offset + 4] == 'a' &&
                bytes[offset + 5] == 'c' &&
                bytes[offset + 6] == 'T' &&
                bytes[offset + 7] == 'L') {
                return true;
            }
            if (bytes[offset + 4] == 'I' &&
                bytes[offset + 5] == 'E' &&
                bytes[offset + 6] == 'N' &&
                bytes[offset + 7] == 'D') {
                if (length != 0) return true;
                sawEnd = true;
            }
            offset += (int)length + 12;
            chunkIndex++;
            if (sawEnd) break;
        }
        return !sawEnd || offset != bytes.Length;
    }
}
