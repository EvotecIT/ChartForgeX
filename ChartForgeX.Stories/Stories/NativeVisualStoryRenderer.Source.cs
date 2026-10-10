using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Terminal;
using ChartForgeX.Typography;

namespace ChartForgeX.Stories;

internal static partial class NativeVisualStoryRenderer {
    private static void DrawSource(VisualSceneBuilder parent, VisualStory story, VisualStorySourceSurface surface, ChartRect bounds, double? elapsed) {
        var options = surface.Options ?? new VisualStorySourceOptions(lineNumbers: false);
        var state = surface.Timeline?.At(elapsed) ?? new SourceEditorState(surface.Source, surface.Source.Text.Length, 0, 0, false);
        var source = state.Source;
        var font = FontSpec.FromFamily(story.Theme.MonospaceFontFamily);
        var builder = new VisualSceneBuilder(new VisualSize(story.Width, story.Height), font);
        var size = options.FontSize; var lineHeight = size * 1.5;
        var style = story.Theme.WindowStyle;
        var chrome = style == TerminalWindowStyle.Minimal ? (options.FileName.Length == 0 ? 0 : 35) : TerminalWindowChrome.HeaderHeight(style);
        if (style != TerminalWindowStyle.Minimal && chrome > 0) {
            DrawStoryWindowHeader(builder, bounds, style, story.Theme.TerminalPalette(), options.FileName.Length == 0 ? "Source" : options.FileName);
        } else if (chrome > 0) {
            FitText(builder, options.FileName, bounds.X + 2, bounds.Y + 18, bounds.Width - 4, 14, story.Theme.Muted);
            builder.Line(bounds.X, bounds.Y + 27, bounds.X + bounds.Width, bounds.Y + 27, story.Theme.Border);
        }
        var content = new ChartRect(bounds.X, bounds.Y + chrome, bounds.Width, Math.Max(1, bounds.Height - chrome));
        var digitWidth = builder.MeasureText("0", size * 0.78).Width;
        var gutter = options.LineNumbers ? digitWidth * 5 + 12 : 0;
        if (content.Height < lineHeight + 14 || content.Width < gutter + builder.MeasureText("M", size).Width + 8)
            throw new InvalidOperationException("The source viewport cannot fit a readable line with the selected font, filename and line numbers. Enlarge the panel, rebalance its weight or reduce editor chrome.");
        var width = Math.Max(1, content.Width - gutter - 8);
        var capacity = Math.Max(1, (int)((content.Height - 14) / lineHeight));
        var focus = surface.Timeline != null ? state.Caret : 0;
        if (options.HighlightedLine > 0) {
            var lineNumber = 1;
            foreach (var line in TextLineScanner.Enumerate(source.Text)) {
                if (lineNumber++ == options.HighlightedLine) { focus = line.Start; break; }
            }
        }
        var rows = SourceRows(builder, source.Text, size, width, options.Wrap, focus, capacity, out var discarded, out var continues);
        var caretRow = 0;
        for (var i = 0; i < rows.Count; i++) {
            if (state.Caret >= rows[i].Start) caretRow = i;
            if (state.Caret < rows[i].End) break;
        }
        if (surface.Timeline == null && options.HighlightedLine == 0) caretRow = 0;
        if (options.HighlightedLine > 0) {
            for (var i = 0; i < rows.Count; i++) if (rows[i].Line == options.HighlightedLine) { caretRow = i; break; }
        }
        var first = Math.Max(0, caretRow - Math.Max(0, capacity - 2));
        first = Math.Min(first, Math.Max(0, rows.Count - capacity));
        using (builder.PushClip(content)) {
            for (var i = first; i < Math.Min(rows.Count, first + capacity); i++) {
                var row = rows[i]; var top = content.Y + (i - first) * lineHeight;
                var highlighted = options.HighlightedLine == 0 ? state.ShowCaret && i == caretRow : row.Line == options.HighlightedLine;
                if (highlighted) builder.Rect(new ChartRect(content.X, top, content.Width, lineHeight), story.Theme.Accent.WithOpacity(0.08), radius: 4, role: "source-active-line");
                if (options.LineNumbers && row.FirstInLine) builder.Text(row.Line.ToString(CultureInfo.InvariantCulture), content.X + gutter - 13, top + builder.TextAscent(size * 0.78),
                    size * 0.78, story.Theme.Muted, alignment: TextAlignment.Right, role: "source-line-number");
                var x = content.X + gutter;
                var run = new StringBuilder(); var runX = x; var runKind = StorySyntaxKind.Plain;
                foreach (var glyph in row.Glyphs) {
                    if (glyph.Offset >= state.SelectionStart && glyph.Offset < state.SelectionStart + state.SelectionLength) builder.Rect(new ChartRect(x, top, glyph.Width, lineHeight), story.Theme.Accent.WithOpacity(0.23), role: "source-selection");
                    if (state.ShowCaret && state.Caret == glyph.Offset) builder.Rect(new ChartRect(x, top + 2, 2, size + 3), story.Theme.Accent, role: "source-caret");
                    var kind = source.KindAt(glyph.Offset);
                    if (run.Length > 0 && kind != runKind) {
                        builder.Text(run.ToString(), runX, top + builder.TextAscent(size), size, story.Theme.Syntax.Resolve(runKind), role: "source-text");
                        run.Clear();
                    }
                    if (run.Length == 0) { runX = x; runKind = kind; }
                    run.Append(glyph.Text);
                    x += glyph.Width;
                }
                if (run.Length > 0) builder.Text(run.ToString(), runX, top + builder.TextAscent(size), size, story.Theme.Syntax.Resolve(runKind), role: "source-text");
                if (state.ShowCaret && i == caretRow && state.Caret >= row.End) builder.Rect(new ChartRect(x, top + 2, 2, size + 3), story.Theme.Accent, role: "source-caret");
            }
        }
        if (discarded > 0 || first > 0 || continues || first + capacity < rows.Count) {
            var label = "Lines " + rows[first].Line.ToString(CultureInfo.InvariantCulture) + "–" + rows[Math.Min(rows.Count - 1, first + capacity - 1)].Line.ToString(CultureInfo.InvariantCulture) + (continues ? " · continues" : "");
            var labelSize = Math.Min(11, size * 0.7);
            builder.Text(label, content.X + content.Width - 2, bounds.Y + bounds.Height - 2, labelSize, story.Theme.Muted, alignment: TextAlignment.Right, role: "source-scroll-position");
        }
        parent.Append(builder.Build());
    }

    private static List<SourceRow> SourceRows(VisualSceneBuilder builder, string text, double size, double width, bool wrap,
        int focus, int capacity, out int discarded, out bool continues) {
        var rows = new List<SourceRow>(); var lineNumber = 1;
        discarded = 0; continues = false;
        var reachedFocus = false;
        var measured = new Dictionary<string, double>(StringComparer.Ordinal);
        var spaceWidth = builder.MeasureText(" ", size).Width;
        foreach (var line in TextLineScanner.Enumerate(text)) {
            var row = new SourceRow(lineNumber, line.Start, true); rows.Add(row);
            if (focus <= line.Start) reachedFocus = true;
            var used = 0d;
            for (var offset = line.Start; offset < line.Start + line.Length;) {
                var next = TerminalTextWidth.NextElementBoundary(text, offset);
                // Keep transcript text exact while bounding the visual representation of pathological graphemes.
                var glyph = next - offset > 1024 ? "…" : text.Substring(offset, next - offset);
                if (glyph == "\t") glyph = new string(' ', Math.Max(1, 4 - (int)Math.Round(used / Math.Max(1, spaceWidth)) % 4));
                if (!measured.TryGetValue(glyph, out var advance)) {
                    advance = builder.MeasureText(glyph, size).Width;
                    if (measured.Count < 1024) measured.Add(glyph, advance);
                }
                if (wrap && used > 0 && used + advance > width) {
                    row.End = offset;
                    if (!reachedFocus && rows.Count >= capacity) { rows.RemoveAt(0); discarded++; }
                    if (reachedFocus && rows.Count >= capacity + 1) { continues = true; return rows; }
                    row = new SourceRow(lineNumber, offset, false); rows.Add(row); used = 0;
                }
                // Non-wrapping rows retain a visible prefix rather than expanding the entire line.
                if (used < width && row.Glyphs.Count < 4096) row.Glyphs.Add(new SourceGlyph(offset, glyph, advance));
                used += advance; row.End = next; offset = next;
                if (focus < next) reachedFocus = true;
                if (!wrap && used >= width) { row.End = line.Start + line.Length; reachedFocus |= focus <= row.End; break; }
            }
            if (focus <= line.Start + line.Length) reachedFocus = true;
            if (!reachedFocus && rows.Count >= capacity) { rows.RemoveAt(0); discarded++; }
            if (reachedFocus && rows.Count >= capacity + 1) { continues = true; return rows; }
            lineNumber++;
        }
        return rows;
    }
    private sealed class SourceRow {
        internal SourceRow(int line, int start, bool firstInLine) { Line = line; Start = start; End = start; FirstInLine = firstInLine; }
        internal int Line { get; }
        internal int Start { get; }
        internal int End { get; set; }
        internal bool FirstInLine { get; }
        internal List<SourceGlyph> Glyphs { get; } = new();
    }
    private readonly struct SourceGlyph {
        internal SourceGlyph(int offset, string text, double width) { Offset = offset; Text = text; Width = width; }
        internal int Offset { get; }
        internal string Text { get; }
        internal double Width { get; }
    }
}
