# Authoring and playing visual stories

`ChartForgeX.Stories` presents source, terminal output, text and resolved media on one deterministic timeline. Use it for a coding demonstration, a build replay, an API walkthrough or an image comparison. A story declares its outcomes, and its final scene must contain every referenced outcome panel.

Preparation captures the story, themes, source edits, terminal tables and media pixels. Later changes to those inputs do not change the prepared result. The same observation supplies timestamp frames, static posters, script-free SVG animation, GIF and APNG.

```csharp
using System;
using System.IO;
using ChartForgeX.Stories;
using ChartForgeX.Raster;

var editor = StorySourceTimeline.Create(StorySourceText.Create("", "csharp"))
    .Type("Console.WriteLine(\"Ready\");", TimeSpan.FromSeconds(2))
    .Pause(TimeSpan.FromSeconds(2));

var story = VisualStory.Create("Write and run")
    .WithTheme(VisualStoryTheme.GraphiteDark())
    .WithFormat(VisualStoryFormat.Widescreen);
story.Scene("write", "Write the source", 4)
    .Panel("source", new VisualStorySourceSurface(editor,
        options: new VisualStorySourceOptions("demo.cs", fontSize: 18)));
story.Scene("result", "Read the result", 2)
    .Panel("result", new VisualStoryTextSurface("Ready", emphasized: true));
story.Outcome("ready", "Ready", "result");

var prepared = story.Prepare(new VisualStoryPlaybackOptions(
    endHold: TimeSpan.FromSeconds(2), playCount: 1));
var frames = new VisualStoryFrameOptions(framesPerSecond: 6);

File.WriteAllBytes("poster.png", prepared.ToPng());
File.WriteAllText("poster.svg", prepared.ToSvg());
File.WriteAllText("story.svg", prepared.ToAnimatedSvg(frames));
File.WriteAllText("story.txt", prepared.ToTranscript());
using var gif = File.Create("story.gif");
prepared.WriteAnimation(gif, RasterAnimationFormat.Gif, frames);
```

`RenderAt(timestamp)` returns independently owned RGBA pixels. `PrepareFrame(timestamp)` retains native geometry for SVG and raster output. Timestamps beyond the authored content show the completed scene; negative timestamps fail. `Chapters` exposes scene start times for seeking. Repetition belongs to the player or encoded animation, so timestamp rendering always observes one play.

`VisualStoryPlaybackOptions` controls transitions, the final hold and total plays. A play count of zero repeats indefinitely; one plays once. `VisualStoryFrameOptions` controls cadence, pixel scale and the explicit frame budget. Frame durations sum to one play in .NET ticks. GIF rounds cumulative boundaries to 10 milliseconds to avoid drift at rates such as six frames per second. GIF accepts up to 50 frames per second and requires a final delay of at least 20 milliseconds; increase the hold or change cadence when the rounded remainder is shorter. APNG supports up to 60 frames per second and retains fractions within its 16-bit timing precision. Each chapter needs a sample at least half opaque, including its incoming cross-fade, and an opacity-weighted display duration of at least one sampling interval, capped by its authored duration. A cadence that misses this visibility or reveals the final chapter before its boundary fails before producing frames. Default SVG sampling increases from six up to sixty frames per second when needed, within the same explicit frame budget; supplied sampling stays explicit.

The convenience methods `story.ToSvg()`, `ToPng()`, `ToGif()` and `ToApng()` use this engine. `ToSvg()` produces animation; `prepared.ToSvg()` produces a static frame. Existing `VisualStoryAnimationOptions` configures convenience GIF/APNG exports. Prepare once when several outputs must share custom timing.

## Source and terminal panels

Source editing accepts exact text and resolved syntax spans. `Type`, `Paste`, `Delete`, `Select`, `Edit` and `Pause` author the demonstration without executing code. Ranges use UTF-16 offsets and must preserve Unicode text elements. Replacement first deletes and then types when it has a positive duration; a zero duration inserts immediately.

The editor uses a fixed font size, optional filename and line numbers. It wraps within its viewport and follows the caret vertically. Static source starts at the first line, or at `HighlightedLine` when specified. Long files retain their complete accessible transcript while the visible viewport stays bounded. Oversized combining sequences display a bounded marker. Use a focused excerpt for a small social image rather than reducing text until it becomes unreadable.

Embedded `TerminalStory` panels play commands, output and tab changes at the current scene time. Their completed poster retains the final terminal state. Each scene starts its surfaces at zero; use separate authored terminal sequences when each chapter shows a different command.

`WithFormat` supplies landscape, square and portrait sizes and stacks split panels in portrait. `WithSize` supplies exact dimensions. `WithPanelReflow` controls whether a tall viewport changes a split into a stack. Font size remains explicit, so choose a smaller readable size for a narrow source panel instead of relying on whole-image scaling.

## Streaming and browser controls

`FrameSource` creates a repeatable producer for `RasterAnimationEncoder`. GIF makes two passes to choose a stable global palette; APNG releases each frame as it proceeds. The story declares its retained assets and render working buffers so they share the encoder's 256 MiB payload budget, including terminal images held together during panel rendering and cross-fades. Streaming avoids retaining every scene, sampled frame or complete encoded file. Returned byte arrays also reserve space for the encoded result. The complete animated SVG document is limited to 64 MiB of characters, including escaped transcript text, frame payloads, metadata and CSS.

Caller-owned streams remain open. Cancellation, rendering or I/O failure can leave partial output; use a temporary destination and replace the final file after success when atomic saving matters. Caller-authored generic producers must return identical pixels and durations for each index and declare additional working memory when appropriate. See [RGBA animation encoding](raster-animation.md).

The optional `ChartForgeX.Interactivity.Html` adapter adds play/pause, restart, seeking, chapter buttons, playback rate and a readable transcript:

```csharp
using ChartForgeX.Interactivity.Html;

var page = new HtmlMotionPlayerRenderer().RenderPage(
    prepared.ToAnimatedSvg(frames), prepared.Title);
File.WriteAllText("player.html", page);
```

The player lists every declared chapter and seeks to its first sample at least half opaque. It starts paused, uses standard keyboard-accessible controls and pauses when the page becomes hidden. Static animation needs no script and shows its completed poster under reduced motion or print. The adapter accepts bounded, self-contained sampled story SVG and rejects external image references and active markup.

Run the examples with `dotnet run --project ChartForgeX.Examples -- --story-playback-only --output ./story-output`. They show a source typo, build error, selection and correction, successful output and a real chart in all three aspect ratios. The error transcript is authored; the example does not execute the displayed source. MP4 encoding and platform uploads are outside these exports.
