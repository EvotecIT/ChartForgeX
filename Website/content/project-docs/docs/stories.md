---
title: "Stories and recorded replays"
description: "Present source edits, terminal sessions, and visible results with a shared playback clock."
layout: docs
---

`ChartForgeX.Stories` turns resolved source, terminal output, images, and charts into timed scenes. Source timelines show typing, selections, and corrections. Recorded replays show timestamped commands and output, progress updates, persistent tabs, and directory changes. Construction and rendering consume observations supplied by the caller.

Prepare a story once to share its clock across static posters, animated SVG, GIF, APNG, and HTML. Landscape, square, and portrait formats keep source and terminal text at a fixed readable size while their viewports scroll. Complete transcripts retain the text that falls outside the viewport.

Use `StoryReplay.Trim` and `CompressPauses` to shorten a recorded session while keeping original timestamps. Add explanations with `Explain`. Trimming retains earlier events needed to establish the selected interval's state; review the transcript before sharing a recording that contains sensitive material.

The optional `ChartForgeX.Interactivity.Html` player adds play, seek, speed, chapter, restart, and transcript controls. Default rendering remains script-free. GIF and APNG provide animated file output; MP4 encoding is deferred.

See the [Stories API reference](https://github.com/EvotecIT/ChartForgeX/blob/main/docs/stories.md), [source-editing example](https://github.com/EvotecIT/ChartForgeX/blob/main/ChartForgeX.Examples/StoryPlaybackExamples.cs), and [recorded replay example](https://github.com/EvotecIT/ChartForgeX/blob/main/ChartForgeX.Examples/StoryReplayExamples.cs).
