using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Typography;

/// <summary>Safe inline emphasis applied over a resolved native base text style.</summary>
[Flags]
public enum TextInlineEmphasis {
    /// <summary>The structured content's regular base face.</summary>
    Normal = 0,
    /// <summary>Emphasizes the resolved base at a weight of at least 700.</summary>
    Bold = 1,
    /// <summary>Adds italic to the resolved base face.</summary>
    Italic = 2
}

/// <summary>One immutable span of literal text and safe inline emphasis; it never contains executable markup.</summary>
public readonly struct TextContentRun {
    /// <summary>Creates an immutable literal run.</summary>
    public TextContentRun(string text, TextInlineEmphasis emphasis = TextInlineEmphasis.Normal) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        if ((emphasis & ~(TextInlineEmphasis.Bold | TextInlineEmphasis.Italic)) != 0) throw new ArgumentOutOfRangeException(nameof(emphasis));
        Emphasis = emphasis;
    }
    /// <summary>Gets literal text, including any authored line breaks.</summary>
    public string Text { get; }
    /// <summary>Gets safe native inline emphasis.</summary>
    public TextInlineEmphasis Emphasis { get; }
}

/// <summary>Immutable native caption content. Plain semantic text is derived from its runs, never authored independently.</summary>
public sealed class TextContent {
    private readonly IReadOnlyList<TextContentRun> _runs;
    private TextContent(IEnumerable<TextContentRun> runs, bool structured) {
        var copied = new List<TextContentRun>(); var text = new StringBuilder(); var pending = new StringBuilder();
        var previous = TextInlineEmphasis.Normal;
        foreach (var run in runs) {
            if (run.Text == null) throw new ArgumentException("Every text run must be initialized.", nameof(runs));
            if (run.Text.Length == 0) continue;
            if (pending.Length > 0 && previous != run.Emphasis) { copied.Add(new TextContentRun(pending.ToString(), previous)); pending.Clear(); }
            previous = run.Emphasis; pending.Append(run.Text); text.Append(run.Text);
        }
        if (pending.Length > 0) copied.Add(new TextContentRun(pending.ToString(), previous));
        _runs = Array.AsReadOnly(copied.ToArray()); PlainText = text.ToString(); IsStructured = structured;
    }
    /// <summary>Gets the immutable run sequence. Adjacent equal emphasis is canonicalized without changing text.</summary>
    public IReadOnlyList<TextContentRun> Runs => _runs;
    /// <summary>Gets the complete semantic text derived from Runs.</summary>
    public string PlainText { get; }
    /// <summary>Gets whether the caller deliberately supplied inline content rather than an ordinary plain string.</summary>
    /// <remarks>Topology structured captions use a regular base face unless a base font weight was explicitly authored. Plain strings retain incumbent semantic heading emphasis.</remarks>
    public bool IsStructured { get; }
    /// <summary>Creates ordinary plain content, preserving the incumbent native string rendering path.</summary>
    public static TextContent Plain(string text) => new(new[] { new TextContentRun(text) }, false);
    /// <summary>Creates explicitly structured content from detached immutable runs.</summary>
    public static TextContent FromRuns(IEnumerable<TextContentRun> runs) => new(runs ?? throw new ArgumentNullException(nameof(runs)), true);

    internal TextContent Transform(TextCaseTransform transform) {
        if (transform == TextCaseTransform.None) return this;
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var transformed = TextCaseTransformer.Apply(PlainText, transform, culture);
        var output = new List<TextContentRun>(); var sourceEnd = 0; var targetStart = 0;
        foreach (var run in _runs) {
            sourceEnd += run.Text.Length;
            var targetEnd = sourceEnd == PlainText.Length ? transformed.Length
                : TextCaseTransformer.Apply(PlainText.Substring(0, sourceEnd), transform, culture).Length;
            targetEnd = Math.Max(targetStart, Math.Min(transformed.Length, targetEnd));
            output.Add(new TextContentRun(transformed.Substring(targetStart, targetEnd - targetStart), run.Emphasis)); targetStart = targetEnd;
        }
        return new TextContent(output, IsStructured);
    }

    internal TextContent Slice(int start, int length) {
        if (start < 0 || length < 0 || start > PlainText.Length - length) throw new ArgumentOutOfRangeException(nameof(start));
        var selected = new List<TextContentRun>(); var offset = 0;
        foreach (var run in _runs) {
            var from = Math.Max(0, start - offset); var to = Math.Min(run.Text.Length, start + length - offset);
            if (to > from) selected.Add(new TextContentRun(run.Text.Substring(from, to - from), run.Emphasis));
            offset += run.Text.Length; if (offset >= start + length) break;
        }
        return new TextContent(selected, IsStructured);
    }
    internal static TextContent Join(TextContent left, TextContent right) {
        var runs = new List<TextContentRun>(left.Runs); runs.AddRange(right.Runs);
        return new TextContent(runs, left.IsStructured || right.IsStructured);
    }
    internal TextContent WithLiteral(string text, bool trailing = false) => new(new[] {
        new TextContentRun(text, _runs.Count == 0 ? TextInlineEmphasis.Normal : _runs[trailing ? _runs.Count - 1 : 0].Emphasis)
    }, IsStructured);
}
