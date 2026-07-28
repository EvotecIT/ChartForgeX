using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Terminal;

internal sealed class TerminalStoryLayout {
    private const double HeaderHeight = 42;
    private const double HorizontalPadding = 28;
    private const double VerticalPadding = 24;

    public int Width { get; }
    public int Height { get; }
    public double ContentX => HorizontalPadding;
    public double ContentTop => HeaderHeight + VerticalPadding;
    public double HeaderHeightValue => HeaderHeight;
    public double DurationSeconds { get; }
    public IReadOnlyList<TerminalRenderedLine> Lines { get; }

    private TerminalStoryLayout(int width, int height, double durationSeconds, IReadOnlyList<TerminalRenderedLine> lines) {
        Width = width;
        Height = height;
        DurationSeconds = durationSeconds;
        Lines = lines;
    }

    public static TerminalStoryLayout Build(TerminalStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        story.Validate();
        var maxCharacters = Math.Max(24, (int)Math.Floor((story.Width - HorizontalPadding * 2) / (story.FontSize * 0.61)));
        var lines = new List<TerminalRenderedLine>();
        var clock = story.InitialDelaySeconds;
        foreach (var step in story.Steps) {
            switch (step.Kind) {
                case TerminalStoryStepKind.Command:
                    var prompt = story.Prompt();
                    var commandText = prompt + step.Text;
                    var typingDuration = step.DurationSeconds > 0
                        ? step.DurationSeconds
                        : Math.Max(0.35, Math.Min(4.5, step.Text.Length / story.CharactersPerSecond));
                    var commandElements = Math.Max(1, TextElementCount(commandText));
                    var remainingPromptLength = prompt.Length;
                    foreach (var wrappedCommandLine in Wrap(commandText, maxCharacters)) {
                        var promptLength = Math.Min(remainingPromptLength, wrappedCommandLine.Length);
                        var lineDuration = typingDuration * TextElementCount(wrappedCommandLine) / commandElements;
                        lines.Add(new TerminalRenderedLine(wrappedCommandLine, TerminalTextTone.Default, true, promptLength, clock, lineDuration));
                        remainingPromptLength -= promptLength;
                        clock += lineDuration + story.LineDelaySeconds;
                    }
                    break;
                case TerminalStoryStepKind.Output:
                    foreach (var outputLine in SplitLines(step.Text)) {
                        foreach (var wrappedLine in Wrap(outputLine, maxCharacters)) {
                            lines.Add(new TerminalRenderedLine(wrappedLine, step.Tone, false, 0, clock, 0.22));
                            clock += story.LineDelaySeconds;
                        }
                    }
                    break;
                case TerminalStoryStepKind.Blank:
                    lines.Add(new TerminalRenderedLine(string.Empty, TerminalTextTone.Default, false, 0, clock, 0));
                    break;
                case TerminalStoryStepKind.Pause:
                    clock += step.DurationSeconds;
                    break;
                case TerminalStoryStepKind.Table:
                    foreach (var tableLine in FormatTable(step.Table!, maxCharacters)) {
                        lines.Add(new TerminalRenderedLine(tableLine.Text, tableLine.Tone, false, 0, clock, 0.22));
                        clock += story.LineDelaySeconds;
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unknown terminal story step.");
            }

            if (lines.Count > 120) throw new InvalidOperationException("Terminal story content expands beyond the 120-line rendering limit.");
        }

        if (story.ShowFinalPrompt) {
            foreach (var promptLine in Wrap(story.Prompt(), maxCharacters)) {
                lines.Add(new TerminalRenderedLine(promptLine, TerminalTextTone.Default, true, promptLine.Length, clock + 0.08, 0));
            }
            clock += 0.08;
        }

        var completion = clock;
        foreach (var line in lines) {
            completion = Math.Max(completion, line.StartSeconds + line.DurationSeconds);
        }

        if (completion > 60) throw new InvalidOperationException("Terminal story animation must complete within 60 seconds.");
        var height = (int)Math.Ceiling(HeaderHeight + VerticalPadding * 2 + lines.Count * story.LineHeight);
        return new TerminalStoryLayout(story.Width, Math.Max(180, height), completion, lines);
    }

    private static IEnumerable<string> SplitLines(string value) {
        return value.Replace("\r\n", "\n").Replace('\r', '\n').Split(new[] { '\n' }, StringSplitOptions.None);
    }

    private static IReadOnlyList<TableRenderedLine> FormatTable(TerminalTable table, int maxCharacters) {
        table.Validate();
        var widths = new int[table.Columns.Count];
        for (var column = 0; column < widths.Length; column++) {
            widths[column] = TextElementCount(table.Columns[column]);
            foreach (var row in table.Rows) widths[column] = Math.Max(widths[column], TextElementCount(row[column]));
        }

        var separator = maxCharacters >= widths.Length + (widths.Length - 1) * 3 ? " | " : "|";
        var divider = separator == " | " ? "-+-" : "+";
        var available = maxCharacters - (widths.Length - 1) * separator.Length;
        while (widths.Sum() > available) {
            var largest = 0;
            for (var index = 1; index < widths.Length; index++) {
                if (widths[index] > widths[largest]) largest = index;
            }

            if (widths[largest] <= 1) break;
            widths[largest]--;
        }

        var output = new List<TableRenderedLine> {
            new(RenderRow(table.Columns, widths, table.Alignments, separator), TerminalTextTone.Accent),
            new(string.Join(divider, widths.Select(value => new string('-', value))), TerminalTextTone.Muted)
        };
        foreach (var row in table.Rows) output.Add(new TableRenderedLine(RenderRow(row, widths, table.Alignments, separator), TerminalTextTone.Default));
        return output;
    }

    private static string RenderRow(IReadOnlyList<string> values, IReadOnlyList<int> widths, IReadOnlyList<TerminalColumnAlignment> alignments, string separator) {
        var cells = new string[values.Count];
        for (var index = 0; index < values.Count; index++) {
            var value = Fit(values[index], widths[index]);
            var padding = new string(' ', Math.Max(0, widths[index] - TextElementCount(value)));
            cells[index] = alignments[index] == TerminalColumnAlignment.Right ? padding + value : value + padding;
        }

        return string.Join(separator, cells);
    }

    internal static string FitTitle(string value, int width) {
        var available = Math.Max(12, width - 180);
        var maximum = Math.Max(1, available / 12);
        return Fit(value, maximum);
    }

    internal static int TextElementCount(string value) {
        return StringInfo.ParseCombiningCharacters(value).Length;
    }

    private static string Fit(string value, int maximum) {
        var boundaries = StringInfo.ParseCombiningCharacters(value);
        if (boundaries.Length <= maximum) return value;
        if (maximum <= 1) return "…";
        return value.Substring(0, boundaries[maximum - 1]) + "…";
    }

    private static IEnumerable<string> Wrap(string value, int maximum) {
        if (value.Length == 0) {
            yield return string.Empty;
            yield break;
        }

        var boundaries = StringInfo.ParseCombiningCharacters(value);
        for (var elementOffset = 0; elementOffset < boundaries.Length; elementOffset += maximum) {
            var start = boundaries[elementOffset];
            var nextElement = Math.Min(boundaries.Length, elementOffset + maximum);
            var end = nextElement < boundaries.Length ? boundaries[nextElement] : value.Length;
            yield return value.Substring(start, end - start);
        }
    }

    private readonly struct TableRenderedLine {
        public readonly string Text;
        public readonly TerminalTextTone Tone;

        public TableRenderedLine(string text, TerminalTextTone tone) {
            Text = text;
            Tone = tone;
        }
    }
}

internal sealed class TerminalRenderedLine {
    public string Text { get; }
    public TerminalTextTone Tone { get; }
    public bool IsCommand { get; }
    public int PromptLength { get; }
    public double StartSeconds { get; }
    public double DurationSeconds { get; }

    public TerminalRenderedLine(string text, TerminalTextTone tone, bool isCommand, int promptLength, double startSeconds, double durationSeconds) {
        Text = text;
        Tone = tone;
        IsCommand = isCommand;
        PromptLength = promptLength;
        StartSeconds = startSeconds;
        DurationSeconds = durationSeconds;
    }
}
