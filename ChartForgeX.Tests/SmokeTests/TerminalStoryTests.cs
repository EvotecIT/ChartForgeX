using System;
using System.Linq;
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
        Assert(TerminalStoryLayout.TextElementCount(unicodeTableRows[0].Text.Substring(0, firstSeparator)) ==
               TerminalStoryLayout.TextElementCount(unicodeTableRows[1].Text.Substring(0, secondSeparator)),
            "Table columns should align equivalent precomposed and combining-character cells by rendered text elements.");

        var controlStrings = "before\u001BPgraphics payload\u001B\\middle\u001B_status payload\u009Cafter";
        Assert(TerminalTextSanitizer.Transcript(controlStrings) == "beforemiddleafter", "Captured transcripts should consume complete ST-terminated ANSI control strings.");

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
    }
}
