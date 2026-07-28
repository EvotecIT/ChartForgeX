using System;
using System.Linq;
using ChartForgeX.Motion;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualMotionTimelineRendersAccessibleScriptFreeStories() {
        var metric = MetricCard.Create().WithMetric("Open-source projects", 126);
        var activity = ActivityTimelineBlock.Create()
            .WithTitle("Recent work")
            .AddEvent("Released a reusable rendering improvement", "Today");
        var timeline = VisualMotionTimeline.Create()
            .Reveal("title", durationSeconds: 0.9)
            .Fade("subtitle", delaySeconds: 0.15)
            .Rise("metric", delaySeconds: 0.35, distancePixels: 10)
            .Scale("activity", delaySeconds: 0.55)
            .Pulse("activity-accent", delaySeconds: 1.4);
        var accent = MetricCard.Create().WithMetric("Maintained packages", 24);
        var grid = VisualGrid.Create()
            .WithTitle("Engineering signal")
            .WithSubtitle("A deterministic visual story")
            .WithTheme(ChartTheme.ReportDark())
            .WithColumns(2)
            .WithMotion(timeline)
            .Add("metric", metric)
            .Add("activity", activity)
            .Add("activity-accent", accent, columnSpan: 2);

        var svg = grid.ToSvg("motion-story");
        var svgId = SvgDocument.Parse(svg).Root.GetAttribute("id")!;
        Assert(svg.Contains("data-cfx-motion=\"timeline\"", StringComparison.Ordinal), "Animated visual grids should declare motion metadata.");
        Assert(svg.Contains("data-cfx-motion-duration=\"2.2\"", StringComparison.Ordinal), "Animated visual grids should expose deterministic total duration metadata.");
        Assert(svg.Contains("data-cfx-motion-target=\"title\"", StringComparison.Ordinal) && svg.Contains("data-cfx-motion-target=\"metric\"", StringComparison.Ordinal), "Visual motion should target built-in headings and stable panel ids.");
        Assert(svg.Contains("@keyframes " + svgId + "-motion-0", StringComparison.Ordinal) && svg.Contains("animation:" + svgId + "-motion-0 ", StringComparison.Ordinal), "Visual motion should scope keyframe definitions and references to the final rendered SVG identity.");
        Assert(svg.Contains("@media (prefers-reduced-motion:reduce)", StringComparison.Ordinal) && svg.Contains("@media print", StringComparison.Ordinal), "Visual motion should expose completed-state reduced-motion and print fallbacks.");
        Assert(svg.Contains("Motion is decorative and has a static reduced-motion fallback.", StringComparison.Ordinal), "Animated visual grids should describe the accessibility fallback.");
        Assert(!svg.Contains("<script", StringComparison.OrdinalIgnoreCase), "Visual motion should remain script-free.");

        var html = grid.ToHtmlPage();
        Assert(html.Contains("data-cfx-motion=\"timeline\"", StringComparison.Ordinal) && html.Contains("@keyframes cfx-visual-grid-motion-0", StringComparison.Ordinal), "Visual grid HTML pages should carry the same script-free motion timeline.");
        Assert(html.Contains("data-cfx-motion-target=\"activity\"", StringComparison.Ordinal), "Visual grid HTML panels should retain stable motion targets.");
        Assert(!html.Contains("<script", StringComparison.OrdinalIgnoreCase), "Visual grid HTML motion should remain script-free.");

        var staticGrid = VisualGrid.Create()
            .WithTitle("Engineering signal")
            .WithSubtitle("A deterministic visual story")
            .WithTheme(ChartTheme.ReportDark())
            .WithColumns(2)
            .Add(metric)
            .Add(activity)
            .Add(accent, columnSpan: 2);
        Assert(grid.ToPng().SequenceEqual(staticGrid.ToPng()), "Raster output should render the completed visual state without motion artifacts.");
    }

    private static void VisualMotionKeyframesUseFinalContentIdentity() {
        var metric = MetricCard.Create().WithMetric("Maintained packages", 24);
        var fadeGrid = VisualGrid.Create()
            .WithTitle("Engineering signal")
            .WithMotion(VisualMotionTimeline.Create().Fade("metric"))
            .Add("metric", metric);
        var riseGrid = VisualGrid.Create()
            .WithTitle("Engineering signal")
            .WithMotion(VisualMotionTimeline.Create().Rise("metric"))
            .Add("metric", metric);

        var fadeSvg = fadeGrid.ToSvg();
        var riseSvg = riseGrid.ToSvg();
        var fadeId = SvgDocument.Parse(fadeSvg).Root.GetAttribute("id")!;
        var riseId = SvgDocument.Parse(riseSvg).Root.GetAttribute("id")!;

        Assert(!string.Equals(fadeId, riseId, StringComparison.Ordinal), "Different motion content should produce different final SVG identities.");
        Assert(fadeSvg.Contains("@keyframes " + fadeId + "-motion-0", StringComparison.Ordinal) && fadeSvg.Contains("animation:" + fadeId + "-motion-0 ", StringComparison.Ordinal), "The first inline SVG should bind its keyframe definition and reference to its final identity.");
        Assert(riseSvg.Contains("@keyframes " + riseId + "-motion-0", StringComparison.Ordinal) && riseSvg.Contains("animation:" + riseId + "-motion-0 ", StringComparison.Ordinal), "The second inline SVG should bind its keyframe definition and reference to its final identity.");
        Assert(!fadeSvg.Contains("@keyframes cfx-visual-grid-seed-", StringComparison.Ordinal) && !riseSvg.Contains("@keyframes cfx-visual-grid-seed-", StringComparison.Ordinal), "Rendered SVGs should not retain provisional keyframe names that can collide in a shared document.");
    }

    private static void VisualMotionTimelineRejectsAmbiguousTargets() {
        AssertThrows<InvalidOperationException>(() => VisualGrid.Create()
            .WithTitle("Missing")
            .WithMotion(VisualMotionTimeline.Create().Fade("unknown"))
            .Add("known", MetricCard.Create().WithMetric("Known", 1))
            .ToSvg(), "Visual motion should reject targets that are not present in the grid.");
        AssertThrows<ArgumentException>(() => VisualGrid.Create()
            .Add("duplicate", MetricCard.Create().WithMetric("One", 1))
            .Add("duplicate", MetricCard.Create().WithMetric("Two", 2)), "Visual grid motion target ids should be unique.");
        AssertThrows<ArgumentException>(() => VisualGrid.Create()
            .Add("title", MetricCard.Create().WithMetric("Reserved", 1)), "Visual grid panel targets should not alias the built-in title target.");
        AssertThrows<ArgumentException>(() => VisualMotionTimeline.Create().Fade("bad target"), "Visual motion target ids should be safe stable tokens.");
        AssertThrows<ArgumentException>(() => VisualMotionTimeline.Create().Fade("same").Rise("same"), "A timeline should reject competing effects for the same target.");
        var first = new VisualMotionCue("first", VisualMotionEffect.Fade);
        var second = new VisualMotionCue("second", VisualMotionEffect.Rise);
        var retargeted = VisualMotionTimeline.Create().Add(first).Add(second);
        second.TargetId = "first";
        AssertThrows<InvalidOperationException>(() => VisualGrid.Create()
            .WithMotion(retargeted)
            .Add("first", MetricCard.Create().WithMetric("First", 1))
            .Add("second", MetricCard.Create().WithMetric("Second", 2))
            .ToSvg(), "Visual motion validation should reject duplicate ids introduced after cues are added.");
        AssertThrows<InvalidOperationException>(() => VisualGrid.Create()
            .WithTitle("Empty")
            .WithMotion(VisualMotionTimeline.Create())
            .Add(MetricCard.Create().WithMetric("One", 1))
            .ToSvg(), "Visual motion timelines should require at least one cue.");
        AssertThrows<ArgumentOutOfRangeException>(() => new VisualMotionCue("valid", (VisualMotionEffect)999), "Visual motion cues should reject unknown effects.");
        AssertThrows<ArgumentOutOfRangeException>(() => new VisualMotionCue("valid", VisualMotionEffect.Rise).WithDistance(81), "Visual motion cues should keep entrance distances restrained.");
        AssertThrows<InvalidOperationException>(() => VisualGrid.Create()
            .WithTitle("Long")
            .WithMotion(VisualMotionTimeline.Create().Fade("title", delaySeconds: 59.5, durationSeconds: 1))
            .Add(MetricCard.Create().WithMetric("One", 1))
            .ToSvg(), "Visual motion cues should complete within the bounded timeline.");
    }
}
