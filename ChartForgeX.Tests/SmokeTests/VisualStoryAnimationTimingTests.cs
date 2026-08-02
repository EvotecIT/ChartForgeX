using System.Linq;
using ChartForgeX.Stories;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void AssertExactAnimatedRasterDuration() {
        var story = VisualStory.Create("Exact animation duration").WithSize(480, 320);
        story.Scene("result", "Completed", 0.25)
            .Panel("result", new VisualStoryTextSurface("ready", emphasized: true));
        story.Outcome("visible", "The result is visible", "result");
        var options = VisualStoryAnimationOptions.Create()
            .WithFramesPerSecond(6)
            .WithTransition(0)
            .WithEndHold(0)
            .WithLoop(false)
            .WithMaximumFrames(2);

        var gifControls = ReadGraphicsControls(story.ToGif(options));
        var apngControls = ReadApngFrameControls(story.ToApng(options));
        Assert(gifControls.Length == 2 &&
               gifControls[0].DelayCentiseconds == 17 &&
               gifControls[1].DelayCentiseconds == 8 &&
               gifControls.Sum(static control => control.DelayCentiseconds) == 25,
            "GIF visual stories should use a residual final-frame delay so encoded playback matches the requested duration.");
        Assert(apngControls.Length == 2 &&
               apngControls[0].DelayNumerator == 17 &&
               apngControls[1].DelayNumerator == 8 &&
               apngControls.Sum(static control => control.DelayNumerator) == 25,
            "APNG visual stories should use the same exact residual final-frame timing contract as GIF output.");
    }
}
