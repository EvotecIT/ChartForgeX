using System;
using System.Collections.Generic;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

/// <summary>
/// Describes a renderer-neutral syntax category for source text.
/// </summary>
public enum StorySyntaxKind {
    /// <summary>Unclassified source text.</summary>
    Plain,
    /// <summary>Language keyword.</summary>
    Keyword,
    /// <summary>Type or class name.</summary>
    Type,
    /// <summary>Command, function, or method name.</summary>
    Command,
    /// <summary>Command or method parameter.</summary>
    Parameter,
    /// <summary>Variable or identifier.</summary>
    Variable,
    /// <summary>Property or member name.</summary>
    Property,
    /// <summary>Quoted string or character literal.</summary>
    String,
    /// <summary>Numeric literal.</summary>
    Number,
    /// <summary>Source comment.</summary>
    Comment,
    /// <summary>Language operator.</summary>
    Operator,
    /// <summary>Language punctuation.</summary>
    Punctuation
}

/// <summary>
/// Extension seam for optional language tokenizers. Implementations map parser-specific tokens
/// into exact, renderer-neutral story source spans without adding parser dependencies to ChartForgeX.
/// </summary>
public interface IStorySourceTokenizer {
    /// <summary>Gets the canonical language identifier handled by this tokenizer.</summary>
    string Language { get; }

    /// <summary>Tokenizes exact source text into renderer-neutral semantic spans.</summary>
    StorySourceText Tokenize(string source);
}

/// <summary>
/// Associates a UTF-16 source range with a renderer-neutral syntax category.
/// </summary>
public readonly struct StorySourceSpan {
    /// <summary>Initializes a syntax span.</summary>
    public StorySourceSpan(int start, int length, StorySyntaxKind kind) {
        if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (!Enum.IsDefined(typeof(StorySyntaxKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Start = start;
        Length = length;
        Kind = kind;
    }

    /// <summary>Gets the zero-based UTF-16 start offset.</summary>
    public int Start { get; }

    /// <summary>Gets the UTF-16 span length.</summary>
    public int Length { get; }

    /// <summary>Gets the semantic syntax category.</summary>
    public StorySyntaxKind Kind { get; }

    /// <summary>Gets the exclusive UTF-16 end offset.</summary>
    public int End => checked(Start + Length);
}

/// <summary>
/// Keeps exact source text together with optional dependency-free syntax spans.
/// Tokenizers live in adapters and map their language-specific tokens into this model.
/// </summary>
public sealed class StorySourceText {
    private readonly List<StorySourceSpan> _spans = new();
    private string _language = string.Empty;

    private StorySourceText(string text) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets the exact source text, including whitespace.</summary>
    public string Text { get; }

    /// <summary>Gets the optional language identifier.</summary>
    public string Language => _language;

    /// <summary>Gets ordered, non-overlapping syntax spans.</summary>
    public IReadOnlyList<StorySourceSpan> Spans => _spans;

    /// <summary>Creates source text without language-specific dependencies.</summary>
    public static StorySourceText Create(string text, string? language = null) =>
        new StorySourceText(text).WithLanguage(language);

    /// <summary>Sets an optional language identifier used by hosts and accessibility output.</summary>
    public StorySourceText WithLanguage(string? language) {
        _language = string.IsNullOrWhiteSpace(language) ? string.Empty : language!.Trim();
        return this;
    }

    /// <summary>Adds one ordered syntax span.</summary>
    public StorySourceText AddSpan(int start, int length, StorySyntaxKind kind) =>
        AddSpan(new StorySourceSpan(start, length, kind));

    /// <summary>Adds one ordered syntax span.</summary>
    public StorySourceText AddSpan(StorySourceSpan span) {
        if (span.End > Text.Length) throw new ArgumentOutOfRangeException(nameof(span), "Syntax spans must stay within the source text.");
        if (_spans.Count > 0 && span.Start < _spans[_spans.Count - 1].End) {
            throw new ArgumentException("Syntax spans must be ordered and cannot overlap.", nameof(span));
        }
        if (!TerminalTextWidth.IsElementBoundary(Text, span.Start) ||
            !TerminalTextWidth.IsElementBoundary(Text, span.End)) {
            throw new ArgumentException("Syntax spans cannot split a Unicode text element.", nameof(span));
        }
        if (_spans.Count >= 4096) throw new InvalidOperationException("Source text supports at most 4096 syntax spans.");
        _spans.Add(span);
        return this;
    }

    internal StorySyntaxKind KindAt(int sourceOffset) {
        if (sourceOffset < 0 || sourceOffset >= Text.Length) return StorySyntaxKind.Plain;
        foreach (var span in _spans) {
            if (sourceOffset < span.Start) break;
            if (sourceOffset < span.End) return span.Kind;
        }
        return StorySyntaxKind.Plain;
    }

    internal void Validate() {
        var previousEnd = 0;
        foreach (var span in _spans) {
            if (span.Start < previousEnd || span.End > Text.Length) throw new InvalidOperationException("Source syntax spans must be ordered, non-overlapping, and in range.");
            if (!TerminalTextWidth.IsElementBoundary(Text, span.Start) ||
                !TerminalTextWidth.IsElementBoundary(Text, span.End)) {
                throw new InvalidOperationException("Source syntax spans cannot split Unicode text elements.");
            }
            previousEnd = span.End;
        }
    }
}
