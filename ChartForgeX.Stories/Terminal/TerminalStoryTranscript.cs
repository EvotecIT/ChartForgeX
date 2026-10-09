using System;
using System.Collections.Generic;
using ChartForgeX.Stories;
using ChartForgeX.Typography;

namespace ChartForgeX.Terminal;

/// <summary>Bounds expanded prompts and table rows before allocating transcript lines or display geometry.</summary>
internal static class TerminalStoryTranscript {
    internal static IReadOnlyList<string> Build(TerminalStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        story.Validate();
        var lines = new List<string>();
        var length = 0L;
        var activeTabId = story.Tabs[0].Id;
        void Reserve(long characters) => length = VisualStoryTranscriptRenderer.Reserve(
            length, characters + (lines.Count > 0 ? Environment.NewLine.Length : 0));
        void Add(params string[] parts) {
            var characters = 0L;
            foreach (var part in parts) characters += part.Length;
            Reserve(characters);
            lines.Add(string.Concat(parts));
        }
        void AddRow(string prefix, IReadOnlyList<string> cells) {
            var characters = (long)prefix.Length + Math.Max(0, cells.Count - 1) * 3L;
            foreach (var cell in cells) characters += cell.Length;
            Reserve(characters);
            lines.Add(prefix + string.Join(" | ", cells));
        }
        foreach (var step in story.Steps) {
            var tab = story.GetTab(step.TabId);
            var prefix = "[" + tab.Title + "] ";
            switch (step.Kind) {
                case TerminalStoryStepKind.DeclareTab:
                    Add("[Tab added: ", tab.Title, "]");
                    break;
                case TerminalStoryStepKind.OpenTab:
                case TerminalStoryStepKind.SelectTab:
                    Add("[Tab: ", tab.Title, "]");
                    activeTabId = tab.Id;
                    break;
                case TerminalStoryStepKind.Command:
                    Add(prefix, tab.Prompt(), step.Text);
                    break;
                case TerminalStoryStepKind.Output:
                    foreach (var line in TextLineScanner.Enumerate(step.Text)) {
                        Reserve((long)prefix.Length + line.Length);
                        lines.Add(prefix + line.Read(step.Text));
                    }
                    break;
                case TerminalStoryStepKind.Blank:
                    Add("[", tab.Title, "]");
                    break;
                case TerminalStoryStepKind.Table:
                    step.Table!.Validate();
                    AddRow(prefix, step.Table.Columns);
                    foreach (var row in step.Table.Rows) AddRow(prefix, row);
                    break;
                case TerminalStoryStepKind.Pause:
                    break;
                default:
                    throw new InvalidOperationException("Unknown terminal story step.");
            }
        }
        if (story.ShowFinalPrompt) {
            var tab = story.GetTab(activeTabId);
            Add("[", tab.Title, "] ", tab.Prompt());
        }
        return lines;
    }
}
