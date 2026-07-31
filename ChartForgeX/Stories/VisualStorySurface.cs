using System;
using System.IO;
using System.Xml;
using ChartForgeX.Raster;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

/// <summary>Identifies a renderer-neutral visual-story surface.</summary>
public enum VisualStorySurfaceKind {
    /// <summary>Formatted source code or other exact source text.</summary>
    Source,
    /// <summary>A deterministic terminal presentation.</summary>
    Terminal,
    /// <summary>A raster or vector artifact.</summary>
    Media,
    /// <summary>Explanatory prose, status, or a callout.</summary>
    Text
}

/// <summary>Base class for resolved visual-story surfaces.</summary>
public abstract class VisualStorySurface {
    private protected VisualStorySurface(VisualStorySurfaceKind kind, string accessibleText, bool preserveAccessibleWhitespace = false) {
        Kind = kind;
        AccessibleText = preserveAccessibleWhitespace
            ? RequireContent(accessibleText, nameof(accessibleText))
            : RequireText(accessibleText, nameof(accessibleText));
    }

    /// <summary>Gets the surface kind.</summary>
    public VisualStorySurfaceKind Kind { get; }

    /// <summary>Gets the text alternative included in transcripts and accessible output.</summary>
    public virtual string AccessibleText { get; }

    internal static string RequireText(string value, string name) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty value is required.", name);
        return value.Trim();
    }

    internal static string RequireSingleLineText(string value, string name) {
        var normalized = RequireText(value, name);
        if (normalized.IndexOfAny(new[] { '\r', '\n' }) >= 0) {
            throw new ArgumentException("A single-line value is required.", name);
        }
        return normalized;
    }

    internal static string OptionalSingleLineText(string value, string name) {
        if (value == null) throw new ArgumentNullException(name);
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return RequireSingleLineText(value, name);
    }

    internal static string RequireContent(string value, string name) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty value is required.", name);
        return value;
    }
}

/// <summary>Displays exact source text with optional renderer-neutral syntax spans.</summary>
public sealed class VisualStorySourceSurface : VisualStorySurface {
    private readonly string _caption;

    /// <summary>Initializes a source surface.</summary>
    public VisualStorySourceSurface(StorySourceText source, string? caption = null)
        : base(VisualStorySurfaceKind.Source, AccessibleSourceText(source, caption), preserveAccessibleWhitespace: true) {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        _caption = string.IsNullOrWhiteSpace(caption)
            ? string.Empty
            : RequireText(caption!, nameof(caption));
    }

    /// <summary>Gets the exact source and semantic syntax spans.</summary>
    public StorySourceText Source { get; }

    /// <summary>Gets accessibility text derived from the current source metadata.</summary>
    public override string AccessibleText => AccessibleSourceText(Source, _caption);

    private static string AccessibleSourceText(StorySourceText source, string? caption) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var accessibleHeading = string.IsNullOrWhiteSpace(caption)
            ? string.Empty
            : RequireText(caption!, nameof(caption));
        if (source.Language.Length > 0) {
            if (accessibleHeading.Length > 0) accessibleHeading += Environment.NewLine;
            accessibleHeading += "Language: " + source.Language;
        }
        return accessibleHeading.Length == 0
            ? source.Text
            : accessibleHeading + Environment.NewLine + source.Text;
    }
}

/// <summary>Displays a deterministic terminal story without executing its commands.</summary>
public sealed class VisualStoryTerminalSurface : VisualStorySurface {
    private readonly string _accessibleHeading;

    /// <summary>Initializes a terminal surface.</summary>
    public VisualStoryTerminalSurface(TerminalStory terminal, string? accessibleText = null)
        : base(
            VisualStorySurfaceKind.Terminal,
            AccessibleTerminalText(terminal, accessibleText),
            preserveAccessibleWhitespace: true) {
        Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _accessibleHeading = string.IsNullOrWhiteSpace(accessibleText)
            ? string.Empty
            : RequireText(accessibleText!, nameof(accessibleText));
    }

    /// <summary>Gets the resolved terminal presentation.</summary>
    public TerminalStory Terminal { get; }

    /// <summary>Gets an accessibility transcript derived from the current terminal state.</summary>
    public override string AccessibleText => AccessibleTerminalText(Terminal, _accessibleHeading);

    private static string AccessibleTerminalText(TerminalStory terminal, string? heading) {
        if (terminal == null) throw new ArgumentNullException(nameof(terminal));
        var transcript = string.Join(Environment.NewLine, TerminalStoryLayout.Build(terminal).TranscriptLines);
        if (string.IsNullOrWhiteSpace(heading)) return transcript;
        return RequireText(heading!, nameof(heading)) + Environment.NewLine + transcript;
    }
}

/// <summary>Displays a resolved raster artifact with an optional vector representation.</summary>
public sealed class VisualStoryMediaSurface : VisualStorySurface {
    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>Initializes a raster artifact.</summary>
    public VisualStoryMediaSurface(byte[] rasterBytes, string accessibleText, string? svg = null)
        : base(VisualStorySurfaceKind.Media, accessibleText) {
        if (rasterBytes == null) throw new ArgumentNullException(nameof(rasterBytes));
        Raster = RasterImageDecoder.Decode(rasterBytes);
        Svg = RequireSvg(svg);
    }

    /// <summary>Initializes an RGBA artifact.</summary>
    public VisualStoryMediaSurface(RgbaImage raster, string accessibleText, string? svg = null)
        : base(VisualStorySurfaceKind.Media, accessibleText) {
        Raster = raster;
        Svg = RequireSvg(svg);
    }

    /// <summary>Gets the resolved raster representation used by PNG, GIF, and APNG output.</summary>
    public RgbaImage Raster { get; }

    /// <summary>Gets an optional resolved SVG representation used by SVG output.</summary>
    public string Svg { get; }

    private static string RequireSvg(string? svg) {
        if (string.IsNullOrWhiteSpace(svg)) return string.Empty;
        try {
            var settings = new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 64L * 1024 * 1024
            };
            using var text = new StringReader(svg);
            using var reader = XmlReader.Create(text, settings);
            reader.MoveToContent();
            if (!string.Equals(reader.LocalName, "svg", StringComparison.Ordinal) ||
                !string.Equals(reader.NamespaceURI, SvgNamespace, StringComparison.Ordinal)) {
                throw new ArgumentException("The vector representation must be a valid SVG document.", nameof(svg));
            }
            var insideStyle = false;
            ValidateSvgNode(reader);
            while (reader.Read()) {
                ValidateSvgNode(reader);
                if (reader.NodeType == XmlNodeType.Element &&
                    string.Equals(reader.LocalName, "style", StringComparison.OrdinalIgnoreCase)) {
                    insideStyle = !reader.IsEmptyElement;
                } else if (insideStyle &&
                           (reader.NodeType == XmlNodeType.Text ||
                            reader.NodeType == XmlNodeType.CDATA) &&
                           ContainsActiveCss(reader.Value)) {
                    throw new ArgumentException("The vector representation must not contain active or external CSS.", nameof(svg));
                } else if (reader.NodeType == XmlNodeType.EndElement &&
                           string.Equals(reader.LocalName, "style", StringComparison.OrdinalIgnoreCase)) {
                    insideStyle = false;
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
        while (reader.MoveToNextAttribute()) {
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
        }
        reader.MoveToElement();
    }

    private static bool IsActiveSvgElement(string localName) =>
        string.Equals(localName, "script", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "animate", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "animateMotion", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "animateTransform", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "set", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "discard", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(localName, "foreignObject", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsActiveCss(string value) =>
        value.IndexOf("animation", StringComparison.OrdinalIgnoreCase) >= 0 ||
        value.IndexOf("transition", StringComparison.OrdinalIgnoreCase) >= 0 ||
        value.IndexOf("@keyframes", StringComparison.OrdinalIgnoreCase) >= 0 ||
        value.IndexOf("@import", StringComparison.OrdinalIgnoreCase) >= 0 ||
        value.IndexOf("behavior", StringComparison.OrdinalIgnoreCase) >= 0 ||
        ContainsUnsafeCssReference(value);

    private static bool ContainsUnsafeCssReference(string value) {
        var cursor = 0;
        while (cursor < value.Length) {
            var start = value.IndexOf("url(", cursor, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return false;
            var contentStart = start + 4;
            var end = value.IndexOf(')', contentStart);
            if (end < 0) return true;
            var target = value.Substring(contentStart, end - contentStart)
                .Trim()
                .Trim('\'', '"');
            if (!target.StartsWith("#", StringComparison.Ordinal)) return true;
            cursor = end + 1;
        }
        return false;
    }

    private static bool IsUnsafeResourceReference(string value) {
        var reference = value.Trim();
        if (reference.Length == 0 || reference[0] == '#') {
            return false;
        }
        if (reference.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) {
            return !reference.StartsWith("data:image/png", StringComparison.OrdinalIgnoreCase) &&
                   !reference.StartsWith("data:image/jpeg", StringComparison.OrdinalIgnoreCase) &&
                   !reference.StartsWith("data:image/gif", StringComparison.OrdinalIgnoreCase) &&
                   !reference.StartsWith("data:image/webp", StringComparison.OrdinalIgnoreCase) &&
                   !reference.StartsWith("data:image/bmp", StringComparison.OrdinalIgnoreCase);
        }
        return true;
    }
}

/// <summary>Displays explanatory prose, status, or a callout.</summary>
public sealed class VisualStoryTextSurface : VisualStorySurface {
    /// <summary>Initializes a text surface.</summary>
    public VisualStoryTextSurface(string text, bool emphasized = false)
        : base(VisualStorySurfaceKind.Text, text) {
        Text = text.Trim();
        Emphasized = emphasized;
    }

    /// <summary>Gets the text.</summary>
    public string Text { get; }

    /// <summary>Gets whether the text should receive stronger visual emphasis.</summary>
    public bool Emphasized { get; }
}
