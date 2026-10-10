using System;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

/// <summary>Renders completed visual stories through the common native playback engine.</summary>
public sealed class PngVisualStoryRenderer {
    /// <summary>Renders the completed story state to PNG bytes.</summary>
    public byte[] Render(VisualStory story) => (story ?? throw new ArgumentNullException(nameof(story))).Prepare().ToPng();

    internal static long MaximumFittedTerminalWorkingBytes(VisualStory story, int outputScale, bool includeTransitions = true) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var maximum = 0L;
        var previousSceneRetained = 0L;
        foreach (var scene in story.Scenes) {
            var retained = 0L;
            var peak = 0L;
            var bounds = VisualStoryLayout.Panels(story, scene);
            for (var index = 0; index < scene.Panels.Count; index++) {
                if (!(scene.Panels[index].Surface is VisualStoryTerminalSurface terminal) || terminal.Options != null) continue;
                var content = VisualStoryLayout.PanelContent(scene.Panels[index], bounds[index]);
                var working = Terminal.PngTerminalStoryRenderer.EstimateFittedWorkingBytes(
                        terminal.Terminal,
                        content.Width,
                        content.Height,
                        outputScale, out var imageBytes);
                peak = Math.Max(peak, checked(retained + working));
                retained = checked(retained + imageBytes);
            }
            maximum = Math.Max(maximum, checked(peak + (includeTransitions ? previousSceneRetained : 0)));
            previousSceneRetained = retained;
        }
        return maximum;
    }

    internal static string ExpandSourceTabs(string value, ref int visualColumn) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (visualColumn < 0) throw new ArgumentOutOfRangeException(nameof(visualColumn));
        if (value.IndexOf('\t') < 0) {
            visualColumn = checked(visualColumn + TerminalTextWidth.Measure(value));
            return value;
        }

        var expanded = new StringBuilder(value.Length + 8);
        var start = 0;
        for (var index = 0; index < value.Length; index++) {
            if (value[index] != '\t') continue;
            if (index > start) {
                var preceding = value.Substring(start, index - start);
                expanded.Append(preceding);
                visualColumn = checked(visualColumn + TerminalTextWidth.Measure(preceding));
            }
            var spaces = 4 - visualColumn % 4;
            expanded.Append(' ', spaces);
            visualColumn = checked(visualColumn + spaces);
            start = index + 1;
        }
        if (start < value.Length) {
            var trailing = value.Substring(start);
            expanded.Append(trailing);
            visualColumn = checked(visualColumn + TerminalTextWidth.Measure(trailing));
        }
        return expanded.ToString();
    }

}
