using System;
using System.Collections.Generic;
using ChartForgeX.Motion;

namespace ChartForgeX.VisualBlocks;

internal static class VisualGridMotion {
    public const string TitleTarget = "title";
    public const string SubtitleTarget = "subtitle";

    public static void Validate(VisualGrid grid) {
        if (grid.Motion == null) return;
        grid.Motion.Validate();
        var targets = new HashSet<string>(StringComparer.Ordinal);
        if (grid.Title.Length > 0) targets.Add(TitleTarget);
        if (grid.Subtitle.Length > 0) targets.Add(SubtitleTarget);
        foreach (var item in grid.Items) {
            if (item.MotionTargetId != null) targets.Add(item.MotionTargetId);
        }

        foreach (var cue in grid.Motion.Cues) {
            if (!targets.Contains(cue.TargetId)) {
                throw new InvalidOperationException("Visual motion target '" + cue.TargetId + "' is not present in this visual grid.");
            }
        }
    }
}
