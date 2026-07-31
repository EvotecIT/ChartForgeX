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

    internal static string RequireContent(string value, string name) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty value is required.", name);
        return value;
    }
}

/// <summary>Displays exact source text with optional renderer-neutral syntax spans.</summary>
public sealed class VisualStorySourceSurface : VisualStorySurface {
    /// <summary>Initializes a source surface.</summary>
    public VisualStorySourceSurface(StorySourceText source, string? caption = null)
        : base(VisualStorySurfaceKind.Source, AccessibleSourceText(source, caption), preserveAccessibleWhitespace: true) {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>Gets the exact source and semantic syntax spans.</summary>
    public StorySourceText Source { get; }

    private static string AccessibleSourceText(StorySourceText source, string? caption) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var accessibleHeading = caption == null ? string.Empty : RequireText(caption, nameof(caption));
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
            while (reader.Read()) {
            }
            return svg!;
        } catch (XmlException ex) {
            throw new ArgumentException("The vector representation must be a valid SVG document.", nameof(svg), ex);
        }
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
