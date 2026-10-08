using System;
using System.IO;
using System.Text;
using System.Xml;
using ChartForgeX.Raster;
using ChartForgeX.Svg;
using ChartForgeX.SvgRaster;

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
    internal const int MaximumHeadingLength = 512;

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
        foreach (var character in normalized) {
            if (!IsSemanticLineSeparator(character)) continue;
            throw new ArgumentException("A single-line value is required.", name);
        }
        return normalized;
    }

    internal static string RequireIdentifier(string value, string name) {
        var normalized = RequireSingleLineText(value, name);
        for (var index = 0; index < normalized.Length; index++) {
            var current = normalized[index];
            int scalar;
            if (char.IsHighSurrogate(current) &&
                index + 1 < normalized.Length &&
                char.IsLowSurrogate(normalized[index + 1])) {
                scalar = char.ConvertToUtf32(current, normalized[++index]);
            } else {
                scalar = current;
            }
            if (scalar == '\t' || !SvgMarkupWriter.IsMarkupScalar(scalar)) {
                throw new ArgumentException("A stable single-line markup identifier is required.", name);
            }
        }
        return normalized;
    }

    internal static string RequireHeading(string value, string name) {
        var normalized = RequireSingleLineText(value, name);
        if (normalized.Length > MaximumHeadingLength) {
            throw new ArgumentOutOfRangeException(name, "Visual-story headings support at most " + MaximumHeadingLength + " UTF-16 code units.");
        }
        return normalized;
    }

    private static bool IsSemanticLineSeparator(char value) =>
        value == '\r' ||
        value == '\n' ||
        value == '\u000B' ||
        value == '\u000C' ||
        value == '\u0085' ||
        value == '\u2028' ||
        value == '\u2029';

    internal static string OptionalSingleLineText(string value, string name) {
        if (value == null) throw new ArgumentNullException(name);
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return RequireSingleLineText(value, name);
    }

    internal static string OptionalHeading(string value, string name) {
        if (value == null) throw new ArgumentNullException(name);
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return RequireHeading(value, name);
    }

    internal static string RequireContent(string value, string name) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty value is required.", name);
        return value;
    }
}

/// <summary>Displays explanatory prose, status, or a callout.</summary>
public sealed class VisualStoryTextSurface : VisualStorySurface {
    private const int MaximumTextCharacters = 1024 * 1024;

    /// <summary>Initializes a text surface.</summary>
    public VisualStoryTextSurface(string text, bool emphasized = false)
        : base(VisualStorySurfaceKind.Text, RequireBoundedText(text)) {
        Text = AccessibleText;
        Emphasized = emphasized;
    }

    /// <summary>Gets the text.</summary>
    public string Text { get; }

    /// <summary>Gets whether the text should receive stronger visual emphasis.</summary>
    public bool Emphasized { get; }

    private static string RequireBoundedText(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (text.Length > MaximumTextCharacters) {
            throw new ArgumentOutOfRangeException(
                nameof(text),
                "Visual-story text surfaces support at most " + MaximumTextCharacters + " UTF-16 characters.");
        }
        return text;
    }
}
