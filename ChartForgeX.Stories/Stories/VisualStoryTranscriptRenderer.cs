using System;
using System.Text;

namespace ChartForgeX.Stories;

/// <summary>Produces a portable text transcript for a visual story.</summary>
public sealed class VisualStoryTranscriptRenderer {
    internal const long MaximumTranscriptCharacters = 16L * 1024 * 1024;

    /// <summary>Renders the complete story as plain text.</summary>
    public string Render(VisualStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        story.Validate();
        var requiredCharacters = Measure(story);
        if (requiredCharacters > MaximumTranscriptCharacters) {
            throw new InvalidOperationException(
                "Visual-story transcript exceeds the " + MaximumTranscriptCharacters +
                "-character safety limit. Reduce source, terminal, scene, or panel content.");
        }
        var text = new StringBuilder((int)requiredCharacters);
        text.Append(story.Title).Append('\n');
        if (story.Description.Length > 0) text.Append(story.Description).Append('\n');
        for (var sceneIndex = 0; sceneIndex < story.Scenes.Count; sceneIndex++) {
            var scene = story.Scenes[sceneIndex];
            text.Append('\n').Append(sceneIndex + 1).Append(". ").Append(scene.Title).Append('\n');
            foreach (var panel in scene.Panels) {
                if (panel.Title.Length > 0) text.Append("   ").Append(panel.Title).Append(":\n");
                AppendAccessibleText(text, panel.Surface.AccessibleText);
            }
        }
        text.Append("\nOutcomes:\n");
        foreach (var outcome in story.Outcomes) text.Append("- ").Append(outcome.Label).Append('\n');
        return text.ToString();
    }

    private static long Measure(VisualStory story) {
        var length = checked((long)story.Title.Length + 1);
        if (story.Description.Length > 0) length = Reserve(length, story.Description.Length + 1L);
        for (var sceneIndex = 0; sceneIndex < story.Scenes.Count; sceneIndex++) {
            var scene = story.Scenes[sceneIndex];
            length = Reserve(length, 1L + (sceneIndex + 1).ToString().Length + 2L + scene.Title.Length + 1L);
            foreach (var panel in scene.Panels) {
                if (panel.Title.Length > 0) {
                    length = Reserve(length, 3L + panel.Title.Length + 2L);
                }
                length = Reserve(length, AccessibleOutputLength(panel.Surface.AccessibleText));
            }
        }
        length = Reserve(length, 11);
        foreach (var outcome in story.Outcomes) {
            length = Reserve(length, 2L + outcome.Label.Length + 1L);
        }
        return length;
    }

    internal static long Reserve(long currentCharacters, long additionalCharacters) {
        if (currentCharacters < 0) throw new ArgumentOutOfRangeException(nameof(currentCharacters));
        if (additionalCharacters < 0) throw new ArgumentOutOfRangeException(nameof(additionalCharacters));
        var total = checked(currentCharacters + additionalCharacters);
        if (total > MaximumTranscriptCharacters) {
            throw new InvalidOperationException(
                "Visual-story transcript exceeds the " + MaximumTranscriptCharacters +
                "-character safety limit. Reduce source, terminal, scene, or panel content.");
        }
        return total;
    }

    private static long AccessibleOutputLength(string value) {
        var characters = 0L;
        var lines = 1L;
        for (var index = 0; index < value.Length; index++) {
            var character = value[index];
            if (character == '\r') {
                if (index + 1 < value.Length && value[index + 1] == '\n') index++;
                lines++;
            } else if (character == '\n') {
                lines++;
            } else {
                characters++;
            }
        }
        return checked(characters + lines * 4);
    }

    private static void AppendAccessibleText(StringBuilder text, string value) {
        text.Append("   ");
        for (var index = 0; index < value.Length; index++) {
            var character = value[index];
            if (character == '\r') {
                if (index + 1 < value.Length && value[index + 1] == '\n') index++;
                text.Append("\n   ");
            } else if (character == '\n') {
                text.Append("\n   ");
            } else {
                text.Append(character);
            }
        }
        text.Append('\n');
    }
}
