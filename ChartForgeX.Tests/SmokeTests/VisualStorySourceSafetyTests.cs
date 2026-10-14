using ChartForgeX.Themes;
using System;
using ChartForgeX.Stories;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualStorySourceRenderingValidatesThemesAndBoundsGraphemes() {
        var oversizedElementStory = VisualStory.Create("Bounded source grapheme")
            .WithSize(480, 320);
        oversizedElementStory.Scene("result", "Completed")
            .Panel(
                "result",
                new VisualStorySourceSurface(
                    StorySourceText.Create("a" + new string('\u0301', 1024 * 1024), "text")));
        oversizedElementStory.Outcome("ready", "Ready", "result");
        Assert(oversizedElementStory.ToPng().Length > 200,
            "Source fitting should bound grapheme materialization before rendering an oversized combining sequence.");
    }
}
