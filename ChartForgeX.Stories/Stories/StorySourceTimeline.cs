using System;
using System.Collections.Generic;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

/// <summary>Authors resolved source edits without running a script or depending on a language parser.</summary>
public sealed class StorySourceTimeline {
    private readonly List<SourceEdit> _edits = new();
    private readonly StorySourceText _initial;
    private StorySourceText _current;
    private int _caret;
    private long _retainedCharacters;
    private StorySourceTimeline(StorySourceText initial) {
        _initial = Copy(initial ?? throw new ArgumentNullException(nameof(initial))); _current = _initial; _caret = initial.Text.Length;
        _retainedCharacters = initial.Text.Length;
        if (_retainedCharacters > 8L * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(initial), "Source timelines support at most 8 MiB of retained characters.");
    }
    /// <summary>Creates an editor timeline from exact initial source, which may be empty.</summary>
    public static StorySourceTimeline Create(StorySourceText initial) => new(initial);
    /// <summary>Gets the completed document as an independent source observation.</summary>
    public StorySourceText Source => Copy(_current);
    /// <summary>Gets the authored editing duration.</summary>
    public TimeSpan Duration { get; private set; }
    internal long RetainedCharacters => _retainedCharacters;

    /// <summary>Replaces a UTF-16 range with resolved source. A zero duration pastes instantly; positive duration deletes then types whole Unicode elements.</summary>
    public StorySourceTimeline Edit(int start, int removeLength, StorySourceText insertion, TimeSpan duration) {
        if (insertion == null) throw new ArgumentNullException(nameof(insertion));
        CheckRange(_current.Text, start, removeLength); CheckDuration(duration);
        insertion.Validate();
        var after = Replace(_current, start, removeLength, insertion);
        Add(new SourceEdit(_current, after, start, removeLength, Copy(insertion), duration, 0));
        _current = after; _caret = CaretBoundary(after.Text, start + insertion.Text.Length);
        return this;
    }

    /// <summary>Types text at the current caret.</summary>
    public StorySourceTimeline Type(string text, TimeSpan duration) => Edit(_caret, 0, StorySourceText.Create(text, _current.Language), duration);
    /// <summary>Types resolved source, revealing its syntax colors together with whole Unicode text elements.</summary>
    /// <remarks>Tokenize the source before authoring. Playback uses captured spans and never calls a parser or executes code.</remarks>
    public StorySourceTimeline Type(StorySourceText source, TimeSpan duration) => Edit(_caret, 0, source, duration);
    /// <summary>Pastes resolved source at the current caret.</summary>
    public StorySourceTimeline Paste(StorySourceText source) => Edit(_caret, 0, source, TimeSpan.Zero);
    /// <summary>Deletes a range, optionally over an authored duration.</summary>
    public StorySourceTimeline Delete(int start, int length, TimeSpan duration) => Edit(start, length, StorySourceText.Create(string.Empty, _current.Language), duration);
    /// <summary>Shows a selection for the specified duration and moves the caret to its end.</summary>
    public StorySourceTimeline Select(int start, int length, TimeSpan duration) {
        CheckRange(_current.Text, start, length); CheckDuration(duration);
        Add(new SourceEdit(_current, _current, start, 0, StorySourceText.Create(string.Empty), duration, length));
        _caret = start + length; return this;
    }
    /// <summary>Holds the current editor state for reading.</summary>
    public StorySourceTimeline Pause(TimeSpan duration) => Select(_caret, 0, duration);

    internal SourceEditorState At(double? seconds) {
        if (!seconds.HasValue) return new SourceEditorState(_current, _caret, 0, 0, false);
        var remaining = StoryPlaybackClock.Ticks(Math.Max(0, Math.Min(Duration.TotalSeconds, seconds.Value)));
        var state = new SourceEditorState(_initial, _initial.Text.Length, 0, 0, true);
        foreach (var edit in _edits) {
            if (remaining >= edit.Duration.Ticks) {
                remaining -= edit.Duration.Ticks;
                state = new SourceEditorState(edit.After, CaretBoundary(edit.After.Text, edit.Start + edit.Insertion.Text.Length + edit.SelectionLength), 0, 0, true);
                continue;
            }
            if (edit.SelectionLength > 0 || edit.Before == edit.After) return new SourceEditorState(edit.Before, edit.Start + edit.SelectionLength, edit.Start, edit.SelectionLength, true);
            var deleting = edit.RemoveLength > 0;
            var typing = edit.Insertion.Text.Length > 0;
            var deleteTicks = deleting && typing ? Math.Min(edit.Duration.Ticks, remaining * 2) : deleting ? remaining : edit.Duration.Ticks;
            var removed = PrefixBoundary(edit.Before.Text.Substring(edit.Start, edit.RemoveLength), deleteTicks, edit.Duration.Ticks);
            var typingTicks = deleting && typing ? Math.Max(0, remaining * 2 - edit.Duration.Ticks) : typing ? remaining : 0;
            var inserted = PrefixBoundary(edit.Insertion.Text, typingTicks, edit.Duration.Ticks);
            var partial = Prefix(edit.Insertion, inserted);
            var source = Replace(edit.Before, edit.Start, removed, partial);
            return new SourceEditorState(source, CaretBoundary(source.Text, edit.Start + inserted), 0, 0, true);
        }
        return state;
    }

    internal StorySourceTimeline Capture() {
        var copy = new StorySourceTimeline(_initial);
        foreach (var edit in _edits) copy._edits.Add(edit); // Source observations are private and never returned directly.
        copy._current = _current; copy._caret = _caret; copy.Duration = Duration; copy._retainedCharacters = _retainedCharacters;
        return copy;
    }

    private void Add(SourceEdit edit) {
        var retained = checked(_retainedCharacters + edit.After.Text.Length + edit.Insertion.Text.Length);
        if (_edits.Count >= 512 || retained > 8L * 1024 * 1024) throw new InvalidOperationException("Source timeline exceeds its 512-event or 8 MiB character budget.");
        if (Duration + edit.Duration > TimeSpan.FromMinutes(10)) throw new InvalidOperationException("Source timelines support at most ten authored minutes.");
        _edits.Add(edit); _retainedCharacters = retained; Duration += edit.Duration;
    }
    private static void CheckDuration(TimeSpan duration) {
        if (duration < TimeSpan.Zero || duration > TimeSpan.FromSeconds(60)) throw new ArgumentOutOfRangeException(nameof(duration));
    }
    private static void CheckRange(string text, int start, int length) {
        if (start < 0 || length < 0 || start > text.Length || length > text.Length - start) throw new ArgumentOutOfRangeException(nameof(start));
        var offset = 0;
        while (offset < start) offset = TerminalTextWidth.NextElementBoundary(text, offset);
        if (offset != start) throw new ArgumentException("An edit cannot split a Unicode text element.", nameof(start));
        while (offset < start + length) offset = TerminalTextWidth.NextElementBoundary(text, offset);
        if (offset != start + length) throw new ArgumentException("An edit cannot split a Unicode text element.", nameof(length));
    }
    private static int PrefixBoundary(string text, long elapsedTicks, long durationTicks) {
        var boundaries = new List<int> { 0 };
        for (var offset = 0; offset < text.Length;) { offset = TerminalTextWidth.NextElementBoundary(text, offset); boundaries.Add(offset); }
        return boundaries[StoryPlaybackClock.Elements(boundaries.Count - 1, elapsedTicks, durationTicks)];
    }
    private static StorySourceText Copy(StorySourceText source) {
        var copy = StorySourceText.Create(source.Text, source.Language);
        foreach (var span in source.Spans) copy.AddSpan(span);
        return copy;
    }
    private static StorySourceText Prefix(StorySourceText source, int length) {
        var result = StorySourceText.Create(source.Text.Substring(0, length), source.Language);
        foreach (var span in source.Spans) {
            if (span.Start >= length) break;
            result.AddSpan(span.Start, Math.Min(span.Length, length - span.Start), span.Kind);
        }
        return result;
    }
    private static StorySourceText Replace(StorySourceText source, int start, int length, StorySourceText insertion) {
        var result = StorySourceText.Create(source.Text.Substring(0, start) + insertion.Text + source.Text.Substring(start + length), source.Language);
        var spans = new List<StorySourceSpan>();
        foreach (var span in source.Spans) if (span.Start < start) spans.Add(new StorySourceSpan(span.Start, Math.Min(span.End, start) - span.Start, span.Kind));
        foreach (var span in insertion.Spans) spans.Add(new StorySourceSpan(start + span.Start, span.Length, span.Kind));
        foreach (var span in source.Spans) if (span.End > start + length) {
            var retainedStart = Math.Max(span.Start, start + length);
            spans.Add(new StorySourceSpan(retainedStart - length + insertion.Text.Length, span.End - retainedStart, span.Kind));
        }
        // An insertion can join its neighbours into one grapheme. The category at the element's start owns the whole element.
        var spanIndex = 0; var runStart = 0; var kind = StorySyntaxKind.Plain;
        for (var offset = 0; offset < result.Text.Length;) {
            while (spanIndex < spans.Count && spans[spanIndex].End <= offset) spanIndex++;
            var nextKind = spanIndex < spans.Count && spans[spanIndex].Start <= offset ? spans[spanIndex].Kind : StorySyntaxKind.Plain;
            if (nextKind != kind) {
                if (kind != StorySyntaxKind.Plain) result.AddSpan(runStart, offset - runStart, kind);
                runStart = offset; kind = nextKind;
            }
            offset = TerminalTextWidth.NextElementBoundary(result.Text, offset);
        }
        if (kind != StorySyntaxKind.Plain) result.AddSpan(runStart, result.Text.Length - runStart, kind);
        return result;
    }
    private static int CaretBoundary(string text, int position) {
        var offset = 0;
        while (offset < position) offset = TerminalTextWidth.NextElementBoundary(text, offset);
        return offset;
    }
    private sealed class SourceEdit {
        internal SourceEdit(StorySourceText before, StorySourceText after, int start, int removeLength, StorySourceText insertion, TimeSpan duration, int selectionLength) {
            Before = before; After = after; Start = start; RemoveLength = removeLength; Insertion = insertion; Duration = duration; SelectionLength = selectionLength;
        }
        internal StorySourceText Before { get; }
        internal StorySourceText After { get; }
        internal int Start { get; }
        internal int RemoveLength { get; }
        internal StorySourceText Insertion { get; }
        internal TimeSpan Duration { get; }
        internal int SelectionLength { get; }
    }
}

internal readonly struct SourceEditorState {
    internal SourceEditorState(StorySourceText source, int caret, int selectionStart, int selectionLength, bool showCaret) {
        Source = source; Caret = caret; SelectionStart = selectionStart; SelectionLength = selectionLength; ShowCaret = showCaret;
    }
    internal StorySourceText Source { get; }
    internal int Caret { get; }
    internal int SelectionStart { get; }
    internal int SelectionLength { get; }
    internal bool ShowCaret { get; }
}
