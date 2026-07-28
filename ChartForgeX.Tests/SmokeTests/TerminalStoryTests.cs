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
        Assert(svg.Contains("@keyframes " + id + "-motion-type", StringComparison.Ordinal) && svg.Contains("animation:" + id + "-motion-type ", StringComparison.Ordinal), "Terminal typing keyframes should use the final rendered identity.");
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

        var narrowTable = TerminalTable.Create()
            .WithColumns("ColumnOne", "ColumnTwo", "ColumnThree", "ColumnFour", "ColumnFive", "ColumnSix", "ColumnSeven", "ColumnEight")
            .AddRow("abcdefghij", "abcdefghij", "abcdefghij", "abcdefghij", "abcdefghij", "abcdefghij", "abcdefghij", "abcdefghij");
        var narrowLayout = TerminalStoryLayout.Build(TerminalStory.Create().WithWidth(480).WithTypography(24, 30).WithFinalPrompt(false).Table(narrowTable));
        Assert(narrowLayout.Lines.All(line => line.Text.Length <= 28), "Tables should compact separators and columns enough to remain inside the narrowest supported terminal at maximum font size.");

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
        AssertThrows<InvalidOperationException>(() => TerminalStory.Create().ToSvg(), "Empty terminal stories should be rejected.");
        AssertThrows<ArgumentException>(() => TerminalStory.Create().WithDialect(TerminalDialect.Custom), "Custom dialects should require an explicit prompt.");
        AssertThrows<ArgumentException>(() => TerminalStory.Create().Command("one\ntwo"), "Commands should stay single-line typed events.");
        AssertThrows<ArgumentException>(() => TerminalTable.Create().WithColumns("One", "Two").AddRow("one"), "Terminal table rows should match their column count.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStory.Create().Progress("bad", 1.1), "Terminal progress values should stay within the unit interval.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStory.Create().WithTiming(0, 1, 0), "Typing speed should remain within usable presentation bounds.");
    }
}
