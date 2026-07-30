using System;
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
    private protected VisualStorySurface(VisualStorySurfaceKind kind, string accessibleText) {
        Kind = kind;
        AccessibleText = RequireText(accessibleText, nameof(accessibleText));
    }

    /// <summary>Gets the surface kind.</summary>
    public VisualStorySurfaceKind Kind { get; }

    /// <summary>Gets the text alternative included in transcripts and accessible output.</summary>
    public string AccessibleText { get; }

    internal static string RequireText(string value, string name) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty value is required.", name);
        return value.Trim();
    }
}

/// <summary>Displays exact source text with optional renderer-neutral syntax spans.</summary>
public sealed class VisualStorySourceSurface : VisualStorySurface {
    /// <summary>Initializes a source surface.</summary>
    public VisualStorySourceSurface(StorySourceText source, string? caption = null)
        : base(VisualStorySurfaceKind.Source, AccessibleSourceText(source, caption)) {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>Gets the exact source and semantic syntax spans.</summary>
    public StorySourceText Source { get; }

    private static string AccessibleSourceText(StorySourceText source, string? caption) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (caption == null) return source.Text;
        return RequireText(caption, nameof(caption)) + Environment.NewLine + source.Text;
    }
}

/// <summary>Displays a deterministic terminal story without executing its commands.</summary>
public sealed class VisualStoryTerminalSurface : VisualStorySurface {
    /// <summary>Initializes a terminal surface.</summary>
    public VisualStoryTerminalSurface(TerminalStory terminal, string accessibleText)
        : base(VisualStorySurfaceKind.Terminal, accessibleText) {
        Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    /// <summary>Gets the resolved terminal presentation.</summary>
    public TerminalStory Terminal { get; }
}

/// <summary>Displays a resolved raster artifact with an optional vector representation.</summary>
public sealed class VisualStoryMediaSurface : VisualStorySurface {
    /// <summary>Initializes a raster artifact.</summary>
    public VisualStoryMediaSurface(byte[] rasterBytes, string accessibleText, string? svg = null)
        : base(VisualStorySurfaceKind.Media, accessibleText) {
        if (rasterBytes == null) throw new ArgumentNullException(nameof(rasterBytes));
        Raster = RasterImageDecoder.Decode(rasterBytes);
        Svg = string.IsNullOrWhiteSpace(svg) ? string.Empty : svg!;
    }

    /// <summary>Initializes an RGBA artifact.</summary>
    public VisualStoryMediaSurface(RgbaImage raster, string accessibleText, string? svg = null)
        : base(VisualStorySurfaceKind.Media, accessibleText) {
        Raster = raster;
        Svg = string.IsNullOrWhiteSpace(svg) ? string.Empty : svg!;
    }

    /// <summary>Gets the resolved raster representation used by PNG, GIF, and APNG output.</summary>
    public RgbaImage Raster { get; }

    /// <summary>Gets an optional resolved SVG representation used by SVG output.</summary>
    public string Svg { get; }
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
