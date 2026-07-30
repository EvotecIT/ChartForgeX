using System;
using System.Text;

namespace ChartForgeX.Stories;

/// <summary>Produces a portable text transcript for a visual story.</summary>
public sealed class VisualStoryTranscriptRenderer {
    /// <summary>Renders the complete story as plain text.</summary>
    public string Render(VisualStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        story.Validate();
        var text = new StringBuilder();
        text.AppendLine(story.Title);
        if (story.Description.Length > 0) text.AppendLine(story.Description);
        for (var sceneIndex = 0; sceneIndex < story.Scenes.Count; sceneIndex++) {
            var scene = story.Scenes[sceneIndex];
            text.AppendLine().Append(sceneIndex + 1).Append(". ").AppendLine(scene.Title);
            foreach (var panel in scene.Panels) {
                if (panel.Title.Length > 0) text.Append("   ").Append(panel.Title).AppendLine(":");
                foreach (var line in panel.Surface.AccessibleText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
                    text.Append("   ").AppendLine(line);
                }
            }
        }
        text.AppendLine().AppendLine("Outcomes:");
        foreach (var outcome in story.Outcomes) text.Append("- ").AppendLine(outcome.Label);
        return text.ToString().Replace("\r\n", "\n");
    }
}
