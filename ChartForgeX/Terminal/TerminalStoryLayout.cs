using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal sealed class TerminalStoryLayout {
    private const double HorizontalPadding = 28;
    private const double VerticalPadding = 24;

    public int Width { get; }
    public int Height { get; }
    public double ContentX => HorizontalPadding;
    public double ContentTop => HeaderHeightValue + VerticalPadding;
    public double HeaderHeightValue { get; }
    public double ColumnWidth { get; }
    public double DurationSeconds { get; }
    public IReadOnlyList<TerminalRenderedLine> Lines { get; }
    public IReadOnlyList<TerminalRenderedTab> Tabs { get; }
    public IReadOnlyList<TerminalTabTransition> Transitions { get; }
    public string FinalTabId { get; }
    public IReadOnlyList<string> TranscriptLines { get; }

    private TerminalStoryLayout(
        int width,
        int height,
        double headerHeight,
        double columnWidth,
        double durationSeconds,
        IReadOnlyList<TerminalRenderedLine> lines,
        IReadOnlyList<TerminalRenderedTab> tabs,
        IReadOnlyList<TerminalTabTransition> transitions,
        string finalTabId,
        IReadOnlyList<string> transcriptLines) {
        Width = width;
        Height = height;
        HeaderHeightValue = headerHeight;
        ColumnWidth = columnWidth;
        DurationSeconds = durationSeconds;
        Lines = lines;
        Tabs = tabs;
        Transitions = transitions;
        FinalTabId = finalTabId;
        TranscriptLines = transcriptLines;
    }

    public static TerminalStoryLayout Build(TerminalStory story) =>
        Build(story, null, null);

    internal static TerminalStoryLayout Build(TerminalStory story, Func<string, string>? transformText) =>
        Build(story, transformText, null);

    internal static TerminalStoryLayout Build(TerminalStory story, Func<string, string>? transformText, TrueTypeFont? outlineFont) {
        return Build(story, transformText, outlineFont, transformText);
    }

    internal static TerminalStoryLayout Build(TerminalStory story, Func<string, string>? transformText, TrueTypeFont? outlineFont, Func<string, string>? transformTableText) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        story.Validate();
        var transform = transformText ?? Identity;
        var tableTransform = transformTableText ?? transform;
        var columnWidth = MeasureColumnWidth(story, outlineFont);
        var maxColumns = Math.Max(1, (int)Math.Floor((story.Width - HorizontalPadding * 2) / columnWidth));
        var lines = new List<TerminalRenderedLine>();
        var linesByTab = story.Tabs.ToDictionary(tab => tab.Id, _ => new List<TerminalRenderedLine>(), StringComparer.OrdinalIgnoreCase);
        var openSecondsByTab = story.Tabs.ToDictionary(tab => tab.Id, _ => 0d, StringComparer.OrdinalIgnoreCase);
        var revealEndSecondsByTab = story.Tabs.ToDictionary(tab => tab.Id, _ => 0d, StringComparer.OrdinalIgnoreCase);
        var transitions = new List<TerminalTabTransition>();
        var transcriptLines = new List<string>();
        var clock = story.InitialDelaySeconds;
        var activeTabId = story.Tabs[0].Id;
        foreach (var step in story.Steps) {
            if (step.Kind == TerminalStoryStepKind.OpenTab || step.Kind == TerminalStoryStepKind.SelectTab) {
                clock = Math.Max(clock, revealEndSecondsByTab[activeTabId]);
                transcriptLines.Add("[Tab: " + story.GetTab(step.TabId).Title + "]");
                if (step.Kind == TerminalStoryStepKind.OpenTab) openSecondsByTab[step.TabId] = clock;
                transitions.Add(new TerminalTabTransition(activeTabId, step.TabId, clock, step.DurationSeconds));
                activeTabId = step.TabId;
                clock += step.DurationSeconds;
                continue;
            }

            var tab = story.GetTab(step.TabId);
            var tabLines = linesByTab[tab.Id];
            switch (step.Kind) {
                case TerminalStoryStepKind.Command:
                    transcriptLines.Add("[" + tab.Title + "] " + tab.Prompt() + step.Text);
                    var prompt = transform(tab.Prompt());
                    var commandText = prompt + transform(step.Text);
                    var typingDuration = step.DurationSeconds > 0
                        ? step.DurationSeconds
                        : Math.Max(0.35, Math.Min(4.5, VisibleTextElementCount(step.Text) / story.CharactersPerSecond));
                    var commandElements = Math.Max(1, VisibleTextElementCount(commandText));
                    var remainingPromptLength = prompt.Length;
                    foreach (var wrappedCommandLine in Wrap(commandText, maxColumns)) {
                        var promptLength = Math.Min(remainingPromptLength, wrappedCommandLine.Length);
                        var lineDuration = typingDuration * VisibleTextElementCount(wrappedCommandLine) / commandElements;
                        AddLine(lines, tabLines, new TerminalRenderedLine(tab.Id, tabLines.Count, wrappedCommandLine, TerminalTextTone.Default, true, promptLength, clock, lineDuration));
                        remainingPromptLength -= promptLength;
                        clock += lineDuration + story.LineDelaySeconds;
                    }
                    break;
                case TerminalStoryStepKind.Output:
                    foreach (var outputLine in SplitLines(step.Text)) {
                        transcriptLines.Add("[" + tab.Title + "] " + outputLine);
                        foreach (var wrappedLine in Wrap(transform(outputLine), maxColumns)) {
                            AddLine(lines, tabLines, new TerminalRenderedLine(tab.Id, tabLines.Count, wrappedLine, step.Tone, false, 0, clock, 0.22));
                            revealEndSecondsByTab[tab.Id] = Math.Max(revealEndSecondsByTab[tab.Id], clock + 0.22);
                            clock += story.LineDelaySeconds;
                        }
                    }
                    break;
                case TerminalStoryStepKind.Blank:
                    transcriptLines.Add("[" + tab.Title + "]");
                    AddLine(lines, tabLines, new TerminalRenderedLine(tab.Id, tabLines.Count, string.Empty, TerminalTextTone.Default, false, 0, clock, 0));
                    break;
                case TerminalStoryStepKind.Pause:
                    clock += step.DurationSeconds;
                    break;
                case TerminalStoryStepKind.Table:
                    AddTableTranscript(transcriptLines, step.Table!, tab.Title);
                    foreach (var tableLine in FormatTable(step.Table!, maxColumns, tableTransform)) {
                        AddLine(lines, tabLines, new TerminalRenderedLine(tab.Id, tabLines.Count, tableLine.Text, tableLine.Tone, false, 0, clock, 0.22, true));
                        revealEndSecondsByTab[tab.Id] = Math.Max(revealEndSecondsByTab[tab.Id], clock + 0.22);
                        clock += story.LineDelaySeconds;
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unknown terminal story step.");
            }
        }

        if (story.ShowFinalPrompt) {
            var finalTab = story.GetTab(activeTabId);
            var finalTabLines = linesByTab[activeTabId];
            transcriptLines.Add("[" + finalTab.Title + "] " + finalTab.Prompt());
            foreach (var promptLine in Wrap(transform(finalTab.Prompt()), maxColumns)) {
                AddLine(lines, finalTabLines, new TerminalRenderedLine(finalTab.Id, finalTabLines.Count, promptLine, TerminalTextTone.Default, true, promptLine.Length, clock + 0.08, 0, isFinalPrompt: true));
            }
            clock += 0.08;
        }

        var completion = clock;
        foreach (var line in lines) {
            completion = Math.Max(completion, line.StartSeconds + line.DurationSeconds);
        }

        if (completion > 60) throw new InvalidOperationException("Terminal story animation must complete within 60 seconds.");
        var renderedTabs = story.Tabs
            .Select(tab => new TerminalRenderedTab(tab, linesByTab[tab.Id], openSecondsByTab[tab.Id]))
            .ToArray();
        var maximumLineCount = renderedTabs.Max(tab => tab.Lines.Count);
        var headerHeight = TerminalWindowChrome.HeaderHeight(story.WindowStyle);
        var height = (int)Math.Ceiling(headerHeight + VerticalPadding * 2 + maximumLineCount * story.LineHeight);
        return new TerminalStoryLayout(story.Width, Math.Max(180, height), headerHeight, columnWidth, completion, lines, renderedTabs, transitions, activeTabId, transcriptLines);
    }

    internal double TabOpacity(string tabId, double? elapsedSeconds) {
        if (!elapsedSeconds.HasValue) {
            return string.Equals(tabId, FinalTabId, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        var activeTabId = Tabs[0].Tab.Id;
        var elapsed = elapsedSeconds.Value;
        foreach (var transition in Transitions) {
            if (elapsed < transition.StartSeconds) {
                break;
            }

            var transitionEnd = transition.StartSeconds + transition.DurationSeconds;
            if (transition.DurationSeconds > 0 && elapsed < transitionEnd) {
                var progress = Math.Max(0, Math.Min(1, (elapsed - transition.StartSeconds) / transition.DurationSeconds));
                if (string.Equals(tabId, transition.FromTabId, StringComparison.OrdinalIgnoreCase)) return 1 - progress;
                if (string.Equals(tabId, transition.ToTabId, StringComparison.OrdinalIgnoreCase)) return progress;
                return 0;
            }

            activeTabId = transition.ToTabId;
        }

        return string.Equals(tabId, activeTabId, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    internal ChartColor TabBackground(double? elapsedSeconds) {
        double red = 0;
        double green = 0;
        double blue = 0;
        double alpha = 0;
        double weight = 0;
        foreach (var renderedTab in Tabs) {
            var opacity = TabOpacity(renderedTab.Tab.Id, elapsedSeconds);
            if (opacity <= 0) continue;
            var color = renderedTab.Tab.Theme.Background;
            red += color.R * opacity;
            green += color.G * opacity;
            blue += color.B * opacity;
            alpha += color.A * opacity;
            weight += opacity;
        }

        if (weight <= 0) return Tabs[0].Tab.Theme.Background;
        return new ChartColor(
            (byte)Math.Round(red / weight),
            (byte)Math.Round(green / weight),
            (byte)Math.Round(blue / weight),
            (byte)Math.Round(alpha / weight));
    }

    internal bool TabVisible(string tabId, double? elapsedSeconds) {
        if (!elapsedSeconds.HasValue) return true;
        var tab = Tabs.First(item => string.Equals(item.Tab.Id, tabId, StringComparison.OrdinalIgnoreCase));
        return elapsedSeconds.Value >= tab.OpenSeconds;
    }

    private static IEnumerable<string> SplitLines(string value) {
        var start = 0;
        for (var index = 0; index < value.Length; index++) {
            if (value[index] != '\r' && value[index] != '\n') {
                continue;
            }

            yield return value.Substring(start, index - start);
            if (value[index] == '\r' && index + 1 < value.Length && value[index + 1] == '\n') {
                index++;
            }
            start = index + 1;
        }

        yield return value.Substring(start);
    }

    private static IReadOnlyList<TableRenderedLine> FormatTable(TerminalTable table, int maxColumns, Func<string, string> transform) {
        table.Validate();
        var columns = table.Columns.Select(transform).ToArray();
        var rows = table.Rows.Select(row => (IReadOnlyList<string>)row.Select(transform).ToArray()).ToArray();
        var widths = new int[table.Columns.Count];
        for (var column = 0; column < widths.Length; column++) {
            widths[column] = DisplayWidth(columns[column]);
            foreach (var row in rows) widths[column] = Math.Max(widths[column], DisplayWidth(row[column]));
        }

        var separator = maxColumns >= widths.Length + (widths.Length - 1) * 3 ? " | " : "|";
        var divider = separator == " | " ? "-+-" : "+";
        var available = maxColumns - (widths.Length - 1) * separator.Length;
        while (widths.Sum() > available) {
            var largest = 0;
            for (var index = 1; index < widths.Length; index++) {
                if (widths[index] > widths[largest]) largest = index;
            }

            if (widths[largest] <= 1) break;
            widths[largest]--;
        }

        var output = new List<TableRenderedLine> {
            new(RenderRow(columns, widths, table.Alignments, separator), TerminalTextTone.Accent),
            new(string.Join(divider, widths.Select(value => new string('-', value))), TerminalTextTone.Muted)
        };
        foreach (var row in rows) output.Add(new TableRenderedLine(RenderRow(row, widths, table.Alignments, separator), TerminalTextTone.Default));
        return output;
    }

    private static void AddTableTranscript(ICollection<string> transcriptLines, TerminalTable table, string tabTitle) {
        transcriptLines.Add("[" + tabTitle + "] " + string.Join(" | ", table.Columns));
        foreach (var row in table.Rows) {
            transcriptLines.Add("[" + tabTitle + "] " + string.Join(" | ", row));
        }
    }

    private static string RenderRow(IReadOnlyList<string> values, IReadOnlyList<int> widths, IReadOnlyList<TerminalColumnAlignment> alignments, string separator) {
        var cells = new string[values.Count];
        for (var index = 0; index < values.Count; index++) {
            var value = Fit(values[index], widths[index]);
            var padding = new string(' ', Math.Max(0, widths[index] - DisplayWidth(value)));
            cells[index] = alignments[index] == TerminalColumnAlignment.Right ? padding + value : value + padding;
        }

        return string.Join(separator, cells);
    }

    internal static string FitTitle(string value, int width, TerminalWindowStyle style) => TerminalWindowChrome.FitTitle(value, width, style);

    internal static int TextElementCount(string value) {
        return TerminalTextWidth.ElementCount(value);
    }

    internal static int VisibleTextElementCount(string value) {
        return TerminalTextWidth.VisibleElementCount(value);
    }

    internal static int DisplayWidth(string value) {
        return TerminalTextWidth.Measure(value);
    }

    private static string Fit(string value, int maximum) => TerminalTextWidth.Fit(value, maximum);

    private static IEnumerable<string> Wrap(string value, int maximum) => TerminalTextWidth.Wrap(value, maximum);

    private static void AddLine(ICollection<TerminalRenderedLine> lines, ICollection<TerminalRenderedLine> tabLines, TerminalRenderedLine line) {
        if (lines.Count >= 120) throw new InvalidOperationException("Terminal story content expands beyond the 120-line rendering limit.");
        lines.Add(line);
        tabLines.Add(line);
    }

    private static string Identity(string value) => value;

    private static double MeasureColumnWidth(TerminalStory story, TrueTypeFont? outlineFont) {
        var factor = story.Tabs.All(tab => TrueTypeFont.IsMonospaceFamily(tab.Theme.FontFamily)) ? 0.61 : 1.0;
        if (outlineFont != null) {
            factor = Math.Max(factor, outlineFont.Measure("W", 1));
            factor = Math.Max(factor, outlineFont.Measure("M", 1));
            factor = Math.Max(factor, outlineFont.Measure("@", 1));
        }
        return story.FontSize * factor;
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
    public string TabId { get; }
    public int RowIndex { get; }
    public string Text { get; }
    public TerminalTextTone Tone { get; }
    public bool IsCommand { get; }
    public int PromptLength { get; }
    public double StartSeconds { get; }
    public double DurationSeconds { get; }
    public bool IsTable { get; }
    public bool IsFinalPrompt { get; }

    public TerminalRenderedLine(string tabId, int rowIndex, string text, TerminalTextTone tone, bool isCommand, int promptLength, double startSeconds, double durationSeconds, bool isTable = false, bool isFinalPrompt = false) {
        TabId = tabId;
        RowIndex = rowIndex;
        Text = text;
        Tone = tone;
        IsCommand = isCommand;
        PromptLength = promptLength;
        StartSeconds = startSeconds;
        DurationSeconds = durationSeconds;
        IsTable = isTable;
        IsFinalPrompt = isFinalPrompt;
    }
}

internal sealed class TerminalRenderedTab {
    public TerminalTab Tab { get; }
    public IReadOnlyList<TerminalRenderedLine> Lines { get; }
    public double OpenSeconds { get; }

    public TerminalRenderedTab(TerminalTab tab, IReadOnlyList<TerminalRenderedLine> lines, double openSeconds) {
        Tab = tab ?? throw new ArgumentNullException(nameof(tab));
        Lines = lines ?? throw new ArgumentNullException(nameof(lines));
        OpenSeconds = openSeconds;
    }
}

internal readonly struct TerminalTabTransition {
    public string FromTabId { get; }
    public string ToTabId { get; }
    public double StartSeconds { get; }
    public double DurationSeconds { get; }

    public TerminalTabTransition(string fromTabId, string toTabId, double startSeconds, double durationSeconds) {
        FromTabId = fromTabId;
        ToTabId = toTabId;
        StartSeconds = startSeconds;
        DurationSeconds = durationSeconds;
    }
}
