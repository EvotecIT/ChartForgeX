using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Svg;
using ChartForgeX.Terminal;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TerminalStoriesRenderAccessibleScriptFreePresentations() {
        var table = TerminalTable.Create()
            .WithColumns("Project", "Stack", "Stars")
            .AlignColumn(2, TerminalColumnAlignment.Right)
            .AddRow("OfficeIMO", ".NET", 1200)
            .AddRow("PSWriteHTML", "PowerShell", 9800);
        var story = TerminalStory.Create()
            .WithTitle(@"pwsh — C:\OpenSource")
            .WithDialect(TerminalDialect.PowerShell)
            .WithWorkingDirectory(@"C:\OpenSource")
            .Command("Get-EvotecPortfolio -Active")
            .Table(table)
            .Blank()
            .Command(@".\Invoke-EnvironmentAudit.ps1")
            .Progress("Validation complete", 1)
            .Output("0 critical findings", TerminalTextTone.Success);

        var svg = story.ToSvg("profile");
        var id = SvgDocument.Parse(svg).Root.GetAttribute("id")!;
        Assert(svg.Contains("data-cfx-terminal=\"PowerShell\"", StringComparison.Ordinal), "Terminal stories should expose their presentation dialect.");
        Assert(svg.Contains("data-cfx-role=\"terminal-command\"", StringComparison.Ordinal) && svg.Contains("PS C:\\OpenSource&gt; ", StringComparison.Ordinal), "PowerShell terminal stories should render authentic prompts and typed commands.");
        Assert(svg.Contains("OfficeIMO", StringComparison.Ordinal) && svg.Contains("0 critical findings", StringComparison.Ordinal), "Terminal stories should retain formatted table and semantic output content.");
        Assert(svg.Contains("Terminal transcript:", StringComparison.Ordinal) && svg.Contains("Get-EvotecPortfolio -Active", StringComparison.Ordinal), "Accessible terminal descriptions should expose the completed transcript.");
        Assert(svg.Contains("@keyframes " + id + "-motion-type", StringComparison.Ordinal) && svg.Contains("animation:" + id + "-motion-type ", StringComparison.Ordinal), "Terminal typing keyframes should use the final rendered identity.");
        var firstCommandElements = TerminalStoryLayout.TextElementCount(TerminalStoryLayout.Build(story).Lines.First(line => line.IsCommand).Text);
        Assert(svg.Contains("--cfx-steps:" + firstCommandElements.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) &&
               svg.Contains("steps(var(--cfx-steps),end)", StringComparison.Ordinal) &&
               !svg.Contains("steps(24,end)", StringComparison.Ordinal),
            "SVG terminal typing should step through the actual grapheme count used by animated raster output.");
        Assert(!svg.Contains(".cfx-terminal-line{opacity:0}", StringComparison.Ordinal) &&
               !svg.Contains(".cfx-terminal-type{opacity:1;clip-path:", StringComparison.Ordinal) &&
               !svg.Contains(".cfx-terminal-cursor{opacity:0", StringComparison.Ordinal),
            "Terminal content should remain visible when CSS animation is unsupported.");
        Assert(svg.Contains("@media (prefers-reduced-motion:reduce)", StringComparison.Ordinal) && svg.Contains("@media print", StringComparison.Ordinal), "Terminal stories should expose completed reduced-motion and print states.");
        Assert(!svg.Contains("<script", StringComparison.OrdinalIgnoreCase), "Terminal stories should remain script-free.");

        var html = story.ToHtmlPage();
        Assert(html.Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase) && html.Contains("chartforgex-terminal-story", StringComparison.Ordinal), "Terminal stories should render responsive complete HTML pages.");
        Assert(html.Contains("linear-gradient(180deg", StringComparison.Ordinal) && html.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal), "Terminal HTML should use the shared premium surface and text-polish contract.");
        Assert(html.Contains("text-rendering:geometricPrecision", StringComparison.Ordinal) && html.Contains("overflow:visible", StringComparison.Ordinal), "Terminal HTML should preserve crisp text and visible SVG overflow.");
        Assert(!html.Contains("<script", StringComparison.OrdinalIgnoreCase), "Terminal HTML should remain script-free.");

        var png = story.ToPng();
        Assert(png.Length > 8 && png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71, "Terminal PNG output should render the completed terminal state.");

        var animatedStory = TerminalStory.Create()
            .WithWidth(480)
            .WithPngOutputScale(1)
            .WithTiming(0, 200, 0)
            .WithFinalPrompt(false)
            .Command("dotnet run", 0.05)
            .Output("Chart saved", TerminalTextTone.Success);
        var animationOptions = TerminalStoryAnimationOptions.Create()
            .WithFramesPerSecond(4)
            .WithEndHold(0.1)
            .WithLoop(false);
        var gif = animatedStory.ToGif(animationOptions);
        var apng = animatedStory.ToApng(animationOptions);
        Assert(gif.Length > 800 && gif[0] == (byte)'G' && gif[1] == (byte)'I' && gif[2] == (byte)'F' &&
               ReadImageDescriptors(gif).Length >= 2 &&
               !System.Text.Encoding.ASCII.GetString(gif).Contains("NETSCAPE2.0", StringComparison.Ordinal),
            "Terminal GIF output should preserve multiple timeline frames and non-looping options.");
        Assert(!GifReader.Decode(gif).Pixels.SequenceEqual(PngReader.Decode(animatedStory.ToPng()).Pixels),
            "Terminal GIF output should begin before the completed transcript instead of repeating a static final frame.");
        Assert(apng.Length > 128 && apng[0] == 137 && apng[1] == 80 && apng[2] == 78 && apng[3] == 71 &&
               ReadApngFrameControls(apng).Length >= 2,
            "Terminal APNG output should preserve multiple full-color timeline frames.");
        AssertThrows<InvalidOperationException>(
            () => animatedStory.ToGif(TerminalStoryAnimationOptions.Create().WithFramesPerSecond(30).WithMaximumFrames(2)),
            "Animated terminal export should enforce its explicit frame budget.");

        var captured = "prefix " + new string('x', 120) + " suffix";
        var transcriptStory = TerminalStory.Create()
            .WithWidth(480)
            .WithFinalPrompt(false)
            .Output("\u001B[31m" + captured + "\u001B[0m\u0001");
        var transcriptLayout = TerminalStoryLayout.Build(transcriptStory);
        Assert(string.Concat(transcriptLayout.Lines.Select(line => line.Text)) == captured, "Captured output should strip ANSI/XML-invalid controls and wrap without losing line content.");
        SvgDocument.Parse(transcriptStory.ToSvg());

        var unicodeTranscript = new string('x', 48) + "😀tail";
        var unicodeStory = TerminalStory.Create().WithWidth(480).WithFinalPrompt(false).Output(unicodeTranscript);
        Assert(string.Concat(TerminalStoryLayout.Build(unicodeStory).Lines.Select(line => line.Text)) == unicodeTranscript, "Transcript wrapping should preserve supplementary Unicode characters at line boundaries.");
        SvgDocument.Parse(unicodeStory.ToSvg());

        var wideTranscript = new string('界', 28) + "😀";
        var wideLayout = TerminalStoryLayout.Build(TerminalStory.Create().WithWidth(480).WithTypography(24, 30).WithFinalPrompt(false).Output(wideTranscript));
        Assert(wideLayout.Lines.Count == 3 &&
               string.Concat(wideLayout.Lines.Select(line => line.Text)) == wideTranscript &&
               wideLayout.Lines.All(line => TerminalStoryLayout.DisplayWidth(line.Text) <= 28),
            "Full-width and emoji transcript content should wrap by rendered display columns without clipping or data loss.");

        var longCommand = "Invoke-LongRunningAudit -" + new string('x', 96);
        var longCommandLayout = TerminalStoryLayout.Build(TerminalStory.Create().WithWidth(480).WithTypography(24, 30).WithFinalPrompt(false).Command(longCommand));
        Assert(longCommandLayout.Lines.Count > 1 &&
               string.Concat(longCommandLayout.Lines.Select(line => line.Text)) == @"PS C:\> " + longCommand &&
               longCommandLayout.Lines.All(line => !line.Text.Contains("…", StringComparison.Ordinal)),
            "Long terminal commands should wrap without losing transcript content.");

        var longTitle = "PowerShell terminal title that is deliberately much wider than the minimum terminal chrome";
        var longTitleStory = TerminalStory.Create().WithWidth(480).WithTitle(longTitle).Output("ready");
        var fittedTitle = TerminalStoryLayout.FitTitle(longTitle, 480);
        var longTitleSvg = longTitleStory.ToSvg();
        Assert(fittedTitle.EndsWith("…", StringComparison.Ordinal) &&
               longTitleSvg.Contains(">" + fittedTitle + "</text>", StringComparison.Ordinal) &&
               longTitleSvg.Contains(">" + longTitle + "</title>", StringComparison.Ordinal),
            "Visible terminal titles should fit the chrome while retaining the full accessible title.");
        Assert(longTitleStory.ToPng().Length > 8, "PNG terminal titles should use the same fitted display text.");

        var timedLayout = TerminalStoryLayout.Build(TerminalStory.Create().WithTiming(0, 42, 0).WithFinalPrompt(false).Output("ready"));
        Assert(Math.Abs(timedLayout.DurationSeconds - 0.22) < 0.001, "Terminal duration metadata should include the final output reveal.");

        var progressStory = TerminalStory.Create().Progress("Ready", 0.5, 8);
        Assert(progressStory.Steps[0].Text.Contains("####----", StringComparison.Ordinal) &&
               !progressStory.Steps[0].Text.Contains("█", StringComparison.Ordinal) &&
               !progressStory.Steps[0].Text.Contains("░", StringComparison.Ordinal),
            "Terminal progress should use glyphs supported by the dependency-free raster font.");

        var narrowTable = TerminalTable.Create()
            .WithColumns("ColumnOne", "ColumnTwo", "ColumnThree", "ColumnFour", "ColumnFive", "ColumnSix", "ColumnSeven", "ColumnEight")
            .AddRow("abcdefghij", "abcdefghij", "abcdefghij", "abcdefghij", "abcdefghij", "abcdefghij", "abcdefghij", "abcdefghij");
        var narrowLayout = TerminalStoryLayout.Build(TerminalStory.Create().WithWidth(480).WithTypography(24, 30).WithFinalPrompt(false).Table(narrowTable));
        Assert(narrowLayout.Lines.All(line => line.Text.Length <= 28), "Tables should compact separators and columns enough to remain inside the narrowest supported terminal at maximum font size.");

        var unicodeTable = TerminalTable.Create()
            .WithColumns("N", "V")
            .AddRow("é", "one")
            .AddRow("e\u0301", "two");
        var unicodeTableRows = TerminalStoryLayout.Build(TerminalStory.Create().WithFinalPrompt(false).Table(unicodeTable)).Lines.Skip(2).ToArray();
        var firstSeparator = unicodeTableRows[0].Text.IndexOf('|');
        var secondSeparator = unicodeTableRows[1].Text.IndexOf('|');
        Assert(TerminalStoryLayout.DisplayWidth(unicodeTableRows[0].Text.Substring(0, firstSeparator)) ==
               TerminalStoryLayout.DisplayWidth(unicodeTableRows[1].Text.Substring(0, secondSeparator)),
            "Table columns should align equivalent precomposed and combining-character cells by rendered text elements.");

        var completeCell = "complete-table-value-" + new string('z', 72);
        var losslessTableStory = TerminalStory.Create()
            .WithWidth(480)
            .WithTypography(24, 30)
            .WithFinalPrompt(false)
            .Table(TerminalTable.Create().WithColumns("Name", "Value").AddRow("row", completeCell));
        var losslessTableLayout = TerminalStoryLayout.Build(losslessTableStory);
        Assert(losslessTableLayout.Lines.Any(line => line.Text.Contains("…", StringComparison.Ordinal)) &&
               losslessTableLayout.TranscriptLines.Any(line => line.Contains(completeCell, StringComparison.Ordinal)) &&
               losslessTableStory.ToSvg().Contains(completeCell, StringComparison.Ordinal),
            "Compacted visual tables should retain complete cell values in the accessible transcript.");
        var fontlessTableLayout = TerminalStoryLayout.Build(losslessTableStory, value => TerminalPngTextPreserver.Preserve(value, null));
        Assert(fontlessTableLayout.Lines.Any(line => line.Text.Contains("…", StringComparison.Ordinal)) &&
               TerminalPngTextPreserver.Preserve("…", null) == "…" &&
               new PngTerminalStoryRenderer().Render(losslessTableStory, null).Length > 8,
            "Fontless terminal tables should preserve generated ellipses instead of rendering fallback question marks.");

        var controlStrings = "before\u001BPgraphics payload\u001B\\middle\u001B_status payload\u009Cafter";
        Assert(TerminalTextSanitizer.Transcript(controlStrings) == "beforemiddleafter", "Captured transcripts should consume complete ST-terminated ANSI control strings.");
        var c1Controls = "before\u009B31mred\u009B0m\u009Dtitle\u009Cmiddle\u0090graphics\u009Cafter";
        Assert(TerminalTextSanitizer.Transcript(c1Controls) == "beforeredmiddleafter", "Captured transcripts should consume complete eight-bit C1 ANSI sequences.");
        var intermediateEscapes = "before\u001B(Bmiddle\u001B#8after";
        Assert(TerminalTextSanitizer.Transcript(intermediateEscapes) == "beforemiddleafter", "Captured transcripts should consume complete ESC sequences with intermediate bytes.");

        var fontlessText = TerminalPngTextPreserver.Preserve("demo » 😀", null);
        Assert(fontlessText.Contains("[U+00BB]", StringComparison.Ordinal) &&
               fontlessText.Contains("[U+1F600]", StringComparison.Ordinal) &&
               !fontlessText.Contains("?", StringComparison.Ordinal),
            "Fontless PNG text should retain unsupported Unicode as explicit scalar escapes.");
        var fontlessStory = TerminalStory.Create()
            .WithTitle("pwsh 😀")
            .WithDialect(TerminalDialect.Custom, "demo » ")
            .WithFinalPrompt(false)
            .Command("ship 😀")
            .Output("ready » 😀");
        var fontlessLayout = TerminalStoryLayout.Build(fontlessStory, value => TerminalPngTextPreserver.Preserve(value, null));
        Assert(fontlessLayout.Lines.Count(line => line.Text.Contains("[U+00BB]", StringComparison.Ordinal)) == 2 &&
               fontlessLayout.Lines.Count(line => line.Text.Contains("[U+1F600]", StringComparison.Ordinal)) == 2 &&
               TerminalStoryLayout.DisplayWidth(TerminalPngTextPreserver.Preserve("»", null)) == TerminalStoryLayout.DisplayWidth("»") &&
               TerminalStoryLayout.DisplayWidth(TerminalPngTextPreserver.Preserve("😀", null)) == TerminalStoryLayout.DisplayWidth("😀") &&
               TerminalStoryLayout.DisplayWidth("[U+1F600]") == 9 &&
               fontlessLayout.Lines.All(line => line.Text.Count(character => character == '[') == line.Text.Count(character => character == ']')),
            "Fontless terminal layout should preserve unsupported prompt, command, and output scalars instead of replacing them with question marks.");
        Assert(new PngTerminalStoryRenderer().Render(fontlessStory, null).Length > 8, "Fontless terminal PNG output should render the preserved scalar escapes.");

        var fallbackHeavyStory = TerminalStory.Create()
            .WithWidth(480)
            .WithTypography(24, 30)
            .WithFinalPrompt(false)
            .Output(string.Join("\n", Enumerable.Repeat("😀😀😀😀", 61)));
        var fallbackHeavyLayout = TerminalStoryLayout.Build(fallbackHeavyStory, value => TerminalPngTextPreserver.Preserve(value, null));
        Assert(fallbackHeavyLayout.Lines.Count == 61 &&
               new PngTerminalStoryRenderer().Render(fallbackHeavyStory, null).Length > 8,
            "Fontless scalar preservation should not inflate valid logical stories beyond the expanded-line limit.");
        var flagLine = string.Concat(Enumerable.Repeat("🇵🇱", 14));
        var flagStory = TerminalStory.Create()
            .WithWidth(480)
            .WithTypography(24, 30)
            .WithFinalPrompt(false)
            .Output(string.Join("\n", Enumerable.Repeat(flagLine, 61)));
        var fontlessFlagLayout = TerminalStoryLayout.Build(flagStory, value => TerminalPngTextPreserver.Preserve(value, null));
        Assert(TerminalStoryLayout.TextElementCount("🇵🇱") == 1 &&
               TerminalStoryLayout.TextElementCount("👩‍💻") == 1 &&
               TerminalStoryLayout.TextElementCount("A\u200DB") == 3 &&
               TerminalStoryLayout.DisplayWidth("A\u200DB") == 2 &&
               fontlessFlagLayout.Lines.Count == 61 &&
               new PngTerminalStoryRenderer().Render(flagStory, null).Length > 8,
            "Terminal grapheme segmentation should keep flag and emoji ZWJ sequences stable without collapsing ordinary joined glyphs.");
        var mixedFallbackCluster = "©\u200D" + TerminalPngTextPreserver.EscapeStart + "[U+1F600]" + TerminalPngTextPreserver.EscapeEnd;
        var mixedFallbackLabel = TerminalPngTextPreserver.ClusterFallbackLabel(mixedFallbackCluster);
        Assert(TerminalStoryLayout.TextElementCount(mixedFallbackCluster) == 1 &&
               mixedFallbackLabel.Contains("U+A9", StringComparison.Ordinal) &&
               mixedFallbackLabel.Contains("U+1F600", StringComparison.Ordinal),
            "Mixed fallback clusters should retain supported and unsupported visible scalars in their fitted label.");
        var outlineFont = TrueTypeFont.TryLoadDefault();
        var shapedFallback = TerminalPngTextPreserver.Preserve("e\u0301", outlineFont);
        Assert(TerminalStoryLayout.TextElementCount(shapedFallback) == 1 &&
               shapedFallback.Contains(TerminalPngTextPreserver.EscapeStart) &&
               TerminalStoryLayout.DisplayWidth(shapedFallback) == 1 &&
               TerminalPngTextPreserver.Preserve(shapedFallback, outlineFont) == shapedFallback,
            "PNG terminal text should fit shaping-dependent graphemes as one fallback unit even when the outline font maps every scalar.");
        var decomposedLine = string.Concat(Enumerable.Repeat("e\u0301", 28));
        var decomposedStory = TerminalStory.Create()
            .WithWidth(480)
            .WithTypography(24, 30)
            .WithFinalPrompt(false)
            .Output(string.Join("\n", Enumerable.Repeat(decomposedLine, 61)));
        var fontlessDecomposedLayout = TerminalStoryLayout.Build(decomposedStory, value => TerminalPngTextPreserver.Preserve(value, null));
        Assert(TerminalStoryLayout.DisplayWidth(TerminalPngTextPreserver.Preserve("\u0301", null)) == 0 &&
               TerminalStoryLayout.DisplayWidth(TerminalPngTextPreserver.Preserve("e\u0301", null)) == 1 &&
               fontlessDecomposedLayout.Lines.Count == 61 &&
               new PngTerminalStoryRenderer().Render(decomposedStory, null).Length > 8,
            "Preserved combining marks should retain their original zero-column contribution inside a grapheme.");
        Assert(TerminalStoryLayout.DisplayWidth("©️") == 2 &&
               TerminalStoryLayout.DisplayWidth("❤️") == 2 &&
               TerminalStoryLayout.DisplayWidth("1️⃣") == 2,
            "Emoji-presentation and keycap clusters should reserve two display columns.");
        var emojiPresentationLine = string.Concat(Enumerable.Repeat("©️", 14));
        var emojiPresentationStory = TerminalStory.Create()
            .WithWidth(480)
            .WithTypography(24, 30)
            .WithFinalPrompt(false)
            .Output(string.Join("\n", Enumerable.Repeat(emojiPresentationLine, 61)));
        Assert(TerminalStoryLayout.Build(emojiPresentationStory, value => TerminalPngTextPreserver.Preserve(value, null)).Lines.Count == 61 &&
               new PngTerminalStoryRenderer().Render(emojiPresentationStory, null).Length > 8,
            "Fontless emoji-presentation clusters should retain their two-column layout identity.");
        var sentinelLiteral = "\uE000[U+1F600]\uE001";
        var repeatedSentinelLiteral = string.Concat(Enumerable.Repeat(sentinelLiteral, 3));
        var sentinelStory = TerminalStory.Create().WithWidth(480).WithTypography(24, 30).WithFinalPrompt(false).Output(repeatedSentinelLiteral);
        var sentinelLayout = TerminalStoryLayout.Build(sentinelStory);
        Assert(TerminalStoryLayout.DisplayWidth(sentinelLiteral) == TerminalStoryLayout.TextElementCount(sentinelLiteral) &&
               sentinelLayout.Lines.Count == 2 &&
               string.Concat(sentinelLayout.Lines.Select(line => line.Text)) == repeatedSentinelLiteral &&
               new PngTerminalStoryRenderer().Render(sentinelStory, null).Length > 8 &&
               TerminalTextSanitizer.Transcript(TerminalPngTextPreserver.EscapeStart + "[U+1F600]" + TerminalPngTextPreserver.EscapeEnd) == "[U+1F600]",
            "Caller text shaped like an internal scalar token should remain literal and renderer-independent.");

        var asciiTiming = TerminalStoryLayout.Build(TerminalStory.Create().WithFinalPrompt(false).Command(new string('x', 20))).DurationSeconds;
        var emojiTiming = TerminalStoryLayout.Build(TerminalStory.Create().WithFinalPrompt(false).Command(string.Concat(Enumerable.Repeat("😀", 20)))).DurationSeconds;
        Assert(Math.Abs(asciiTiming - emojiTiming) < 0.001, "Automatic command timing should count user-visible text elements instead of UTF-16 code units.");

        var widePromptStory = TerminalStory.Create()
            .WithDialect(TerminalDialect.Custom, "界 ")
            .Output("ready");
        var widePromptSvg = SvgDocument.Parse(widePromptStory.ToSvg());
        var widePromptCursor = widePromptSvg.Root.FindByTag("rect").First(element => element.GetAttribute("data-cfx-role") == "terminal-cursor");
        var cursorX = double.Parse(widePromptCursor.GetAttribute("x")!, CultureInfo.InvariantCulture);
        var expectedCursorX = 28 + TerminalStoryLayout.DisplayWidth("界 ") * widePromptStory.FontSize * 0.61 + 2;
        Assert(Math.Abs(cursorX - expectedCursorX) < 0.001, "SVG terminal cursors should follow display columns for full-width prompts.");

        var oversizedCapture = string.Join("\n", Enumerable.Repeat("captured", 121));
        AssertThrows<InvalidOperationException>(
            () => TerminalStoryLayout.Build(TerminalStory.Create().WithFinalPrompt(false).Output(oversizedCapture)),
            "Terminal layout should reject the 121st expanded line while materializing captured output.");

        var invariantTable = TerminalTable.Create().WithColumns("Value").AddRow(1234.5m);
        Assert(invariantTable.Rows[0][0] == "1234.5", "Object-valued table cells should use invariant formatting.");
    }

    private static void TerminalStoryContentOwnsMotionIdentity() {
        var first = TerminalStory.Create().Command("Get-First").Output("one");
        var second = TerminalStory.Create().Command("Get-Second").Output("two");
        var firstSvg = first.ToSvg();
        var secondSvg = second.ToSvg();
        var firstId = SvgDocument.Parse(firstSvg).Root.GetAttribute("id")!;
        var secondId = SvgDocument.Parse(secondSvg).Root.GetAttribute("id")!;

        Assert(!string.Equals(firstId, secondId, StringComparison.Ordinal), "Different terminal transcripts should produce different SVG identities.");
        Assert(firstSvg.Contains("@keyframes " + firstId + "-motion-type", StringComparison.Ordinal) && secondSvg.Contains("@keyframes " + secondId + "-motion-type", StringComparison.Ordinal), "Each transcript should bind keyframes to its final identity.");
        Assert(!firstSvg.Contains("cfx-terminal-seed-", StringComparison.Ordinal) && !secondSvg.Contains("cfx-terminal-seed-", StringComparison.Ordinal), "Terminal SVG output should not retain provisional identities.");
    }

    private static void TerminalDialectsProduceExpectedPrompts() {
        var bash = TerminalStory.Create().WithDialect(TerminalDialect.Bash).WithWorkingDirectory("~/src").Command("dotnet test").ToSvg();
        var python = TerminalStory.Create().WithDialect(TerminalDialect.Python).Command("print('ready')").ToSvg();
        var custom = TerminalStory.Create().WithDialect(TerminalDialect.Custom, "demo » ").Command("ship").ToSvg();

        Assert(bash.Contains("~/src $ ", StringComparison.Ordinal), "Bash stories should use shell-style prompts.");
        Assert(python.Contains("&gt;&gt;&gt; ", StringComparison.Ordinal), "Python stories should use interactive prompts.");
        Assert(custom.Contains("demo » ", StringComparison.Ordinal), "Custom terminal stories should preserve caller-defined prompts.");
    }

    private static void TerminalStoriesRejectUnsafeOrAmbiguousContracts() {
        var dialectStory = TerminalStory.Create();
        try {
            dialectStory.WithDialect(TerminalDialect.Custom);
        } catch (ArgumentException) {
        }
        Assert(dialectStory.Dialect == TerminalDialect.PowerShell && dialectStory.CustomPrompt.Length == 0, "Rejected custom prompts should not partially mutate the terminal story.");

        AssertThrows<InvalidOperationException>(() => TerminalStory.Create().ToSvg(), "Empty terminal stories should be rejected.");
        AssertThrows<ArgumentException>(() => TerminalStory.Create().WithDialect(TerminalDialect.Custom), "Custom dialects should require an explicit prompt.");
        AssertThrows<ArgumentException>(() => TerminalStory.Create().Command("one\ntwo"), "Commands should stay single-line typed events.");
        AssertThrows<ArgumentException>(() => TerminalTable.Create().WithColumns("One", "Two").AddRow("one"), "Terminal table rows should match their column count.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStory.Create().Progress("bad", 1.1), "Terminal progress values should stay within the unit interval.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStory.Create().WithTiming(0, 1, 0), "Typing speed should remain within usable presentation bounds.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStoryAnimationOptions.Create().WithFramesPerSecond(31), "Animated terminal frame rates should remain bounded.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStoryAnimationOptions.Create().WithOutputScale(5), "Animated terminal output scale should remain bounded.");
    }
}
