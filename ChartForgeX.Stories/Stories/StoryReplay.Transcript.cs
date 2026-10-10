using System;
using System.Text;

namespace ChartForgeX.Stories;

public sealed partial class StoryReplay {
    /// <summary>Gets sanitized chronological output, including cleared lines and both timestamps.</summary>
    /// <exception cref="InvalidOperationException">The expanded transcript exceeds 16 Mi UTF-16 characters.</exception>
    public string ToTranscript() {
        var state = new ReplayState(_initialTab, 1);
        var output = new StringBuilder();
        void Append(string value) {
            VisualStoryTranscriptRenderer.Reserve(output.Length, value.Length);
            output.Append(value);
        }
        Append("Replay: "); Append(_initialTab.Title); Append(Environment.NewLine);
        foreach (var item in _events) {
            Append("["); Append(item.Timestamp.ToString("c")); Append("; ");
            Append(item.OriginalTimestamp.HasValue ? "recorded " + item.OriginalTimestamp.Value.ToString("c") : "authored explanation");
            Append("] ");
            if (item.Kind == StoryReplayEventKind.Command) Append(state.Active.Tab.Prompt());
            Append(item.Kind.ToString()); Append(": ");
            if (item.Kind == StoryReplayEventKind.OpenTab || item.Kind == StoryReplayEventKind.SelectTab) {
                state.Apply(item);
                Append(state.Active.Tab.Title); Append(" ["); Append(item.TabId); Append("]");
            } else {
                Append(item.Text);
                state.Apply(item);
            }
            Append(Environment.NewLine);
        }
        return output.ToString();
    }
}
