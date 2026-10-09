using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Terminal;

public sealed partial class TerminalStory {
    internal TerminalStory Capture() {
        var copy = new TerminalStory {
            Width = Width, FontSize = FontSize, LineHeight = LineHeight,
            InitialDelaySeconds = InitialDelaySeconds, CharactersPerSecond = CharactersPerSecond,
            LineDelaySeconds = LineDelaySeconds, TabHoldSeconds = TabHoldSeconds,
            ShowFinalPrompt = ShowFinalPrompt, PngOutputScale = PngOutputScale,
            WindowStyle = WindowStyle, _activeTabId = _activeTabId
        };
        copy._tabs.Clear(); copy._tabsById.Clear();
        foreach (var tab in _tabs) {
            var detached = new TerminalTab(tab.Id, tab.Title, tab.Dialect, tab.WorkingDirectory,
                tab.CustomPrompt, tab.Theme.Copy(), tab.Icon);
            copy._tabs.Add(detached); copy._tabsById.Add(detached.Id, detached);
        }
        var tables = new Dictionary<TerminalTable, TerminalTable>();
        foreach (var step in _steps) {
            TerminalTable? table = null;
            if (step.Table != null && !tables.TryGetValue(step.Table, out table)) {
                table = TerminalTable.Create().WithColumns(step.Table.Columns.ToArray());
                for (var i = 0; i < step.Table.Alignments.Count; i++) table.AlignColumn(i, step.Table.Alignments[i]);
                foreach (var row in step.Table.Rows) table.AddRow(row.Cast<object>().ToArray());
                tables.Add(step.Table, table);
            }
            copy._steps.Add(new TerminalStoryStep(step.Kind, step.Text, step.Tone, step.DurationSeconds,
                table, step.TabId, step.Tab == null ? null : copy._tabsById[step.Tab.Id]));
        }
        return copy;
    }
}
