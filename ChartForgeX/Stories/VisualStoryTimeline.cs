using System;

namespace ChartForgeX.Stories;

internal readonly struct VisualStorySceneTiming {
    public VisualStorySceneTiming(double start, double end, double transitionStart) {
        Start = start;
        End = end;
        TransitionStart = transitionStart;
    }

    public double Start { get; }
    public double End { get; }
    public double TransitionStart { get; }
}

internal static class VisualStoryTimeline {
    internal static VisualStorySceneTiming Timing(
        VisualStory story,
        int sceneIndex,
        double transitionSeconds) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (sceneIndex < 0 || sceneIndex >= story.Scenes.Count) throw new ArgumentOutOfRangeException(nameof(sceneIndex));
        var start = 0d;
        for (var index = 0; index < sceneIndex; index++) {
            start += story.Scenes[index].DurationSeconds;
        }
        var duration = story.Scenes[sceneIndex].DurationSeconds;
        var end = start + duration;
        return new VisualStorySceneTiming(
            start,
            end,
            end - Math.Min(transitionSeconds, duration));
    }

    internal static int FindScene(VisualStory story, double elapsed, out VisualStorySceneTiming timing) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var start = 0d;
        for (var index = 0; index < story.Scenes.Count; index++) {
            var duration = story.Scenes[index].DurationSeconds;
            var end = start + duration;
            if (elapsed < end || index == story.Scenes.Count - 1) {
                timing = new VisualStorySceneTiming(start, end, end);
                return index;
            }
            start = end;
        }
        timing = new VisualStorySceneTiming(start, start, start);
        return story.Scenes.Count - 1;
    }
}
