using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Svg;
using ChartForgeX.Terminal;
using ChartForgeX.Themes;

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
        Assert(svg.Contains("data-cfx-window-style=\"MacOS\"", StringComparison.Ordinal) &&
               svg.Contains("data-cfx-role=\"terminal-macos-controls\"", StringComparison.Ordinal) &&
               !svg.Contains("data-cfx-role=\"terminal-tab\"", StringComparison.Ordinal),
            "Default terminal stories should use explicit macOS chrome instead of coupling platform controls to the color palette.");
        Assert(svg.Contains("data-cfx-role=\"terminal-command\"", StringComparison.Ordinal) && svg.Contains("PS C:\\OpenSource&gt; ", StringComparison.Ordinal), "PowerShell terminal stories should render authentic prompts and typed commands.");
        Assert(svg.Contains("OfficeIMO", StringComparison.Ordinal) && svg.Contains("0 critical findings", StringComparison.Ordinal), "Terminal stories should retain formatted table and semantic output content.");
        Assert(svg.Contains("Terminal transcript:", StringComparison.Ordinal) && svg.Contains("Get-EvotecPortfolio -Active", StringComparison.Ordinal), "Accessible terminal descriptions should expose the completed transcript.");
        Assert(svg.Contains("@keyframes " + id + "-motion-glyph", StringComparison.Ordinal) && svg.Contains("animation:" + id + "-motion-glyph ", StringComparison.Ordinal), "Terminal typing keyframes should use the final rendered identity.");
        var renderedLayout = TerminalStoryLayout.Build(story);
        var typedElements = renderedLayout.Lines
            .Where((line, index) => line.IsCommand && (!story.ShowFinalPrompt || index < renderedLayout.Lines.Count - 1))
            .Sum(line => TerminalStoryLayout.VisibleTextElementCount(line.Text));
        Assert(CountOccurrences(svg, "class=\"cfx-terminal-glyph\"") == typedElements &&
               svg.Contains("--cfx-glyph-start:", StringComparison.Ordinal) &&
               !svg.Contains("steps(24,end)", StringComparison.Ordinal),
            "SVG terminal typing should reveal complete graphemes at the same boundaries used by animated raster output.");
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
        Assert(TerminalStoryAnimatedRasterRenderer.QuantizedDelayCentiseconds(30) == 4 &&
               TerminalStoryAnimatedRasterRenderer.QuantizedDelayCentiseconds(26) == 4 &&
               100d / TerminalStoryAnimatedRasterRenderer.QuantizedDelayCentiseconds(30) <= 30,
            "Animated raster timing should never quantize above the requested frame rate.");

        var cursorLayout = TerminalStoryLayout.Build(
            TerminalStory.Create().WithTiming(0, 200, 0).Output("ready"));
        var cursorLine = cursorLayout.Lines[cursorLayout.Lines.Count - 1];
        Assert(PngTerminalStoryRenderer.CursorVisible(cursorLayout, cursorLine, cursorLine.StartSeconds + 2) &&
               !PngTerminalStoryRenderer.CursorVisible(cursorLayout, cursorLine, cursorLine.StartSeconds + 2.5),
            "Animated raster cursors should keep blinking throughout the configured end hold.");

        var regularPromptCanvas = new RgbaCanvas(200, 50, 1, null, 1, useDefaultOutlineFont: false);
        var emphasizedPromptCanvas = new RgbaCanvas(200, 50, 1, null, 1, useDefaultOutlineFont: false);
        var promptColor = TerminalTheme.Dark().Accent;
        TerminalPngTextPreserver.Draw(regularPromptCanvas, 0, 0, "PS> ", promptColor, 18);
        TerminalPngTextPreserver.DrawEmphasized(emphasizedPromptCanvas, 0, 0, "PS> ", promptColor, 18);
        Assert(TerminalPngTextPreserver.MeasureEmphasized("PS> ", emphasizedPromptCanvas, 18) >
               TerminalPngTextPreserver.Measure("PS> ", regularPromptCanvas, 18) &&
               !emphasizedPromptCanvas.ToOutputPixels().SequenceEqual(regularPromptCanvas.ToOutputPixels()),
            "Raster prompts should preserve the same visual emphasis as SVG prompts.");

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

        var proportionalTheme = TerminalTheme.Dark();
        proportionalTheme.FontFamily = "Arial, sans-serif";
        var proportionalText = new string('W', 20);
        var proportionalStory = TerminalStory.Create()
            .WithWidth(480)
            .WithTypography(24, 30)
            .WithTheme(proportionalTheme)
            .WithFinalPrompt(false)
            .Output(proportionalText);
        var proportionalLayout = TerminalStoryLayout.Build(
            proportionalStory);
        Assert(Math.Abs(proportionalLayout.ColumnWidth - proportionalStory.FontSize) < 0.001 &&
               proportionalLayout.Lines.Count > 1 &&
               string.Concat(proportionalLayout.Lines.Select(line => line.Text)) == proportionalText,
            "SVG terminal layout should use a deterministic conservative width for proportional fonts instead of host font discovery.");

        var mixedWidthStory = TerminalStory.Create()
            .WithDialect(TerminalDialect.Custom, "> ")
            .WithFinalPrompt(false)
            .Command("A界", 3);
        var mixedWidthSvg = mixedWidthStory.ToSvg();
        Assert(mixedWidthSvg.Contains("class=\"cfx-terminal-glyph\" fill=\"#E6EDF7\" style=\"--cfx-glyph-start:", StringComparison.Ordinal) &&
               mixedWidthSvg.Contains(">A</tspan>", StringComparison.Ordinal) &&
               mixedWidthSvg.Contains(">界</tspan>", StringComparison.Ordinal) &&
               !mixedWidthSvg.Contains("clip-path:inset", StringComparison.Ordinal),
            "Mixed-width SVG commands should reveal each complete grapheme instead of clipping through wide glyphs.");

        var longCommand = "Invoke-LongRunningAudit -" + new string('x', 96);
        var longCommandLayout = TerminalStoryLayout.Build(TerminalStory.Create().WithWidth(480).WithTypography(24, 30).WithFinalPrompt(false).Command(longCommand));
        Assert(longCommandLayout.Lines.Count > 1 &&
               string.Concat(longCommandLayout.Lines.Select(line => line.Text)) == @"PS C:\> " + longCommand &&
               longCommandLayout.Lines.All(line => !line.Text.Contains("…", StringComparison.Ordinal)),
            "Long terminal commands should wrap without losing transcript content.");

        var longTitle = "PowerShell terminal title that is deliberately much wider than the minimum terminal chrome";
        var longTitleStory = TerminalStory.Create().WithWidth(480).WithTitle(longTitle).Output("ready");
        var fittedTitle = TerminalStoryLayout.FitTitle(longTitle, 480, TerminalWindowStyle.MacOS);
        var longTitleSvg = longTitleStory.ToSvg();
        Assert(fittedTitle.EndsWith("…", StringComparison.Ordinal) &&
               longTitleSvg.Contains(">" + fittedTitle + "</text>", StringComparison.Ordinal) &&
               longTitleSvg.Contains(">" + longTitle + "</title>", StringComparison.Ordinal),
            "Visible terminal titles should fit the chrome while retaining the full accessible title.");
        Assert(longTitleStory.ToPng().Length > 8, "PNG terminal titles should use the same fitted display text.");

        var windowsStory = TerminalStory.Create()
            .WithWindowStyle(TerminalWindowStyle.WindowsTerminal)
            .WithTitle("Administrator: PowerShell")
            .Command("Get-Process")
            .Output("ready", TerminalTextTone.Success);
        var windowsSvg = windowsStory.ToSvg();
        Assert(windowsSvg.Contains("data-cfx-window-style=\"WindowsTerminal\"", StringComparison.Ordinal) &&
               windowsSvg.Contains("data-cfx-role=\"terminal-tab\"", StringComparison.Ordinal) &&
               windowsSvg.Contains("data-cfx-role=\"terminal-shell-icon\"", StringComparison.Ordinal) &&
               windowsSvg.Contains("data-cfx-role=\"terminal-window-minimize\"", StringComparison.Ordinal) &&
               windowsSvg.Contains("data-cfx-role=\"terminal-window-maximize\"", StringComparison.Ordinal) &&
               windowsSvg.Contains("data-cfx-role=\"terminal-window-close\"", StringComparison.Ordinal) &&
               !windowsSvg.Contains("data-cfx-role=\"terminal-macos-controls\"", StringComparison.Ordinal),
            "Windows Terminal chrome should render a tab strip, shell icon, and native window controls without macOS traffic lights.");
        SvgDocument.Parse(windowsSvg);
        Assert(windowsStory.ToPng().Length > 8, "Windows Terminal chrome should render through the dependency-free raster path.");

        var windowsPowerShellTabTheme = TerminalTheme.WindowsPowerShell();
        windowsPowerShellTabTheme.FontFamily = "'Segoe UI', sans-serif";
        var ubuntuTabTheme = TerminalTheme.Ubuntu();
        ubuntuTabTheme.FontFamily = "'Courier New', monospace";
        var tabbedStory = TerminalStory.Create()
            .WithWindowStyle(TerminalWindowStyle.WindowsTerminal)
            .WithTitle("PowerShell")
            .WithTheme(TerminalTheme.Campbell())
            .WithWorkingDirectory(@"C:\Work")
            .Command("Get-ChildItem", 0.1)
            .OpenTab("windows-powershell", "Windows PowerShell", TerminalDialect.PowerShell, @"C:\Legacy", windowsPowerShellTabTheme, TerminalTabIcon.WindowsPowerShell, transitionSeconds: 0.1)
            .Command("$PSVersionTable.PSVersion", 0.1)
            .OpenTab("ubuntu", "Ubuntu", TerminalDialect.Bash, "~/src", ubuntuTabTheme, TerminalTabIcon.Ubuntu, transitionSeconds: 0.1)
            .Command("dotnet test", 0.1)
            .SelectTab("main", 0.1)
            .Output("Back in PowerShell", TerminalTextTone.Success);
        var tabbedLayout = TerminalStoryLayout.Build(tabbedStory);
        var tabbedSvg = tabbedStory.ToSvg("tabs");
        Assert(tabbedStory.Tabs.Count == 3 && tabbedStory.ActiveTabId == "main" &&
               tabbedStory.Steps.Count(step => step.Kind == TerminalStoryStepKind.OpenTab) == 2 &&
               tabbedStory.Steps.Count(step => step.Kind == TerminalStoryStepKind.SelectTab) == 1,
            "Terminal stories should model declared tabs and selections as first-class timeline steps.");
        Assert(tabbedLayout.Tabs[0].Lines.Count == 3 &&
               tabbedLayout.Tabs[1].Lines.Count == 1 &&
               tabbedLayout.Tabs[2].Lines.Count == 1 &&
               tabbedLayout.FinalTabId == "main",
            "Each terminal tab should preserve its own buffer while the final prompt belongs to the completed active tab.");
        var wrappedFinalPromptLayout = TerminalStoryLayout.Build(
            TerminalStory.Create()
                .WithWidth(480)
                .WithWorkingDirectory(@"C:\" + new string('x', 120))
                .Output("ready"));
        var wrappedFinalPromptLines = wrappedFinalPromptLayout.Tabs[0].Lines;
        Assert(wrappedFinalPromptLines.Count > 1 &&
               wrappedFinalPromptLines.Count(line => line.IsFinalPrompt) == 1 &&
               wrappedFinalPromptLines[wrappedFinalPromptLines.Count - 1].IsFinalPrompt,
            "A wrapped final prompt should place the blinking cursor only on its final visual line.");
        Assert(tabbedSvg.Contains("data-cfx-tab=\"windows-powershell\"", StringComparison.Ordinal) &&
               tabbedSvg.Contains("data-cfx-tab=\"ubuntu\"", StringComparison.Ordinal) &&
               tabbedSvg.Contains("data-cfx-tab=\"windows-powershell\" data-cfx-terminal=\"PowerShell\"", StringComparison.Ordinal) &&
               tabbedSvg.Contains("data-cfx-tab=\"ubuntu\" data-cfx-terminal=\"Bash\"", StringComparison.Ordinal) &&
               tabbedSvg.Contains("font-family=\"'Segoe UI', sans-serif\"", StringComparison.Ordinal) &&
               tabbedSvg.Contains("font-family=\"'Courier New', monospace\"", StringComparison.Ordinal) &&
               tabbedSvg.Contains("#300A24", StringComparison.OrdinalIgnoreCase) &&
               tabbedSvg.Contains("cfx-terminal-tab-final", StringComparison.Ordinal) &&
               tabbedSvg.Contains("[Ubuntu] ~/src $ dotnet test", StringComparison.Ordinal) &&
               !tabbedSvg.Contains("cfx-terminal-seed-", StringComparison.Ordinal),
            "SVG terminal stories should render tab identities, independent palettes, final reduced-motion state, and a complete multi-tab transcript.");
        var transformedTabIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        TerminalStoryLayout.Build(
            tabbedStory,
            (tab, value) => {
                transformedTabIds.Add(tab.Id);
                return value;
            },
            _ => null,
            (tab, value) => {
                transformedTabIds.Add(tab.Id);
                return value;
            });
        Assert(transformedTabIds.SetEquals(new[] { "main", "windows-powershell", "ubuntu" }),
            "Raster layout transformations should receive each owning tab so mixed-font stories preserve and measure text with the correct session font.");
        var perTabWrappingStory = TerminalStory.Create()
            .WithWidth(480)
            .WithTypography(24, 30)
            .WithFinalPrompt(false)
            .WithInitialTab("proportional", "Proportional", TerminalDialect.Custom, "/src", proportionalTheme, TerminalTabIcon.None, "> ")
            .Command(new string('x', 25), 0.1)
            .OpenTab("mono", "Mono", TerminalDialect.Custom, "/src", ubuntuTabTheme, TerminalTabIcon.None, "> ", transitionSeconds: 0.1)
            .Command(new string('x', 25), 0.1);
        var perTabWrappingLayout = TerminalStoryLayout.Build(perTabWrappingStory);
        Assert(perTabWrappingLayout.Tabs[0].Lines.Count == 2 && perTabWrappingLayout.Tabs[1].Lines.Count == 1,
            "Each terminal tab should wrap text against its own font metrics instead of inheriting the initial tab's column capacity.");
        Assert(tabbedLayout.TabOpacity("main", null) == 1 &&
               tabbedLayout.TabOpacity("ubuntu", null) == 0 &&
               tabbedLayout.TabOpacity("windows-powershell", tabbedLayout.Transitions[0].StartSeconds + 0.05) > 0 &&
               !tabbedLayout.TabVisible("ubuntu", tabbedLayout.Transitions[0].StartSeconds) &&
               tabbedLayout.TabVisible("ubuntu", null),
            "Static and animated tab selection should resolve deterministic active-session opacity.");
        SvgDocument.Parse(tabbedSvg);
        Assert(tabbedStory.ToPng().Length > 8 &&
               tabbedStory.ToGif(TerminalStoryAnimationOptions.Create().WithFramesPerSecond(4).WithMaximumFrames(80)).Length > 800,
            "Persistent tab stories should render through completed and animated raster paths.");

        var pacedTabStory = TerminalStory.Create()
            .WithInitialTab("PowerShell", "PowerShell", TerminalDialect.PowerShell, @"C:\Work", TerminalTheme.Campbell(), TerminalTabIcon.PowerShell)
            .WithTiming(0, 200, 0)
            .WithTabHold(1.5)
            .Command("dotnet build", 0.1)
            .OpenTab("Ubuntu", "Ubuntu", TerminalDialect.Bash, "~/src", TerminalTheme.Ubuntu(), TerminalTabIcon.Ubuntu, transitionSeconds: 0.2)
            .Output("ready", TerminalTextTone.Success);
        var pacedTabLayout = TerminalStoryLayout.Build(pacedTabStory);
        Assert(pacedTabStory.Tabs[0].Id == "PowerShell" && pacedTabStory.ActiveTabId == "Ubuntu",
            "Callers should be able to name and style the initial persistent terminal tab.");
        Assert(Math.Abs(pacedTabLayout.Transitions[0].StartSeconds - 1.6) < 0.0001,
            "A tab switch should preserve the configured reading dwell after the active tab's final content completes.");
        Assert(!pacedTabLayout.TabVisible("Ubuntu", pacedTabLayout.Transitions[0].StartSeconds - 0.001) &&
               pacedTabLayout.TabVisible("Ubuntu", pacedTabLayout.Transitions[0].StartSeconds),
            "Opening a tab should reveal it atomically with its activation instead of exposing it during the previous tab's reading dwell.");

        var slowStory = TerminalStory.Create().WithPlaybackSpeed(TerminalStoryPlaybackSpeed.Slow);
        var normalStory = TerminalStory.Create().WithPlaybackSpeed(TerminalStoryPlaybackSpeed.Normal);
        var fastStory = TerminalStory.Create().WithPlaybackSpeed(TerminalStoryPlaybackSpeed.Fast);
        Assert(slowStory.CharactersPerSecond < normalStory.CharactersPerSecond &&
               normalStory.CharactersPerSecond < fastStory.CharactersPerSecond &&
               slowStory.TabHoldSeconds > normalStory.TabHoldSeconds &&
               normalStory.TabHoldSeconds > fastStory.TabHoldSeconds,
            "Playback speed presets should adjust typing and tab reading time in the expected direction.");

        var declaredTabStory = TerminalStory.Create()
            .WithInitialTab("PowerShell", "PowerShell", TerminalDialect.PowerShell, @"C:\Work", TerminalTheme.Campbell(), TerminalTabIcon.PowerShell)
            .WithTiming(0, 200, 0)
            .WithTabHold(1)
            .Command("ready", 0.1)
            .DeclareTab("Ubuntu", "Ubuntu", TerminalDialect.Bash, "~/src", TerminalTheme.Ubuntu(), TerminalTabIcon.Ubuntu)
            .SelectTab("Ubuntu", 0.2)
            .SelectTab("PowerShell", 0.2);
        var declaredTabLayout = TerminalStoryLayout.Build(declaredTabStory);
        Assert(declaredTabStory.Steps.Count(step => step.Kind == TerminalStoryStepKind.DeclareTab) == 1 &&
               declaredTabLayout.Transitions.Count == 2 &&
               Math.Abs(declaredTabLayout.Transitions[1].StartSeconds - (declaredTabLayout.Transitions[0].StartSeconds + 1.2)) < 0.0001,
            "Declared tabs should remain inactive until selected and every selected tab should retain its configured reading dwell.");
        Assert(!declaredTabLayout.TabVisible("Ubuntu", 0.099) &&
               declaredTabLayout.TabVisible("Ubuntu", 0.1) &&
               declaredTabLayout.Transitions[0].StartSeconds > 0.1,
            "Background declarations should expose the tab when authored while leaving activation to an explicit later selection.");

        var longWindowsTitle = new string('W', 80);
        var defaultWidthWindowsTitle = TerminalStoryLayout.FitTitle(longWindowsTitle, 886, TerminalWindowStyle.WindowsTerminal);
        var maximumWidthWindowsTitle = TerminalStoryLayout.FitTitle(longWindowsTitle, 1800, TerminalWindowStyle.WindowsTerminal);
        Assert(defaultWidthWindowsTitle.EndsWith("…", StringComparison.Ordinal) &&
               defaultWidthWindowsTitle == maximumWidthWindowsTitle &&
               TerminalTextWidth.Measure(defaultWidthWindowsTitle) * TerminalWindowChrome.TitleFontSize <= TerminalWindowChrome.WindowsTitleAvailableWidth(886),
            "Windows Terminal titles should fit the capped tab with the same conservative width policy used for proportional terminal text.");
        var longWindowsTitleStory = TerminalStory.Create()
            .WithWidth(1800)
            .WithTheme(proportionalTheme)
            .WithWindowStyle(TerminalWindowStyle.WindowsTerminal)
            .WithTitle(longWindowsTitle)
            .Output("ready");
        var longWindowsTitleSvg = longWindowsTitleStory.ToSvg();
        Assert(longWindowsTitleSvg.Contains(">" + maximumWidthWindowsTitle + "</text>", StringComparison.Ordinal) &&
               longWindowsTitleSvg.Contains(">" + longWindowsTitle + "</title>", StringComparison.Ordinal),
            "Windows Terminal rendering should keep the fitted tab title and the complete accessible title at maximum width.");
        Assert(longWindowsTitleStory.ToPng().Length > 8, "Windows Terminal raster rendering should preserve the capped-tab title fit at maximum width.");

        var minimalStory = TerminalStory.Create().WithWindowStyle(TerminalWindowStyle.Minimal).WithTitle("Portable shell").Output("ready");
        var minimalSvg = minimalStory.ToSvg();
        Assert(minimalSvg.Contains("data-cfx-window-style=\"Minimal\"", StringComparison.Ordinal) &&
               minimalSvg.Contains("data-cfx-role=\"terminal-title\"", StringComparison.Ordinal) &&
               !minimalSvg.Contains("data-cfx-role=\"terminal-macos-controls\"", StringComparison.Ordinal) &&
               !minimalSvg.Contains("data-cfx-role=\"terminal-tab\"", StringComparison.Ordinal),
            "Minimal chrome should retain a title without platform-specific controls.");

        var chromeFreeStory = TerminalStory.Create().WithWindowStyle(TerminalWindowStyle.None).WithTitle("Accessible title").Output("ready");
        var chromeFreeSvg = chromeFreeStory.ToSvg();
        Assert(chromeFreeSvg.Contains("data-cfx-window-style=\"None\"", StringComparison.Ordinal) &&
               !chromeFreeSvg.Contains("data-cfx-role=\"terminal-titlebar\"", StringComparison.Ordinal) &&
               !chromeFreeSvg.Contains("data-cfx-role=\"terminal-title\"", StringComparison.Ordinal) &&
               chromeFreeSvg.Contains(">Accessible title</title>", StringComparison.Ordinal),
            "Chrome-free stories should remove visible title-bar controls while preserving the accessible title.");
        Assert(TerminalStoryLayout.Build(windowsStory).HeaderHeightValue == 50 &&
               TerminalStoryLayout.Build(story).HeaderHeightValue == 42 &&
               TerminalStoryLayout.Build(minimalStory).HeaderHeightValue == 38 &&
               TerminalStoryLayout.Build(chromeFreeStory).HeaderHeightValue == 0,
            "Each window style should own its header geometry so transcript content starts below the selected chrome.");

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
        Assert(TerminalTextSanitizer.Transcript("one\u2028two\u2029three") == "one\ntwo\nthree",
            "Captured transcripts should normalize Unicode line and paragraph separators to layout line breaks.");

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
               TerminalStoryLayout.TextElementCount("👩\u0903\u200D💻") == 2 &&
               TerminalStoryLayout.TextElementCount("A\u200DB") == 2 &&
               TerminalStoryLayout.DisplayWidth("A\u200DB") == 2 &&
               fontlessFlagLayout.Lines.Count == 61 &&
               new PngTerminalStoryRenderer().Render(flagStory, null).Length > 8,
            "Terminal grapheme segmentation should keep flags and emoji joins stable while attaching ordinary ZWJ controls to the preceding grapheme.");
        var devanagariConjunct = "\u0915\u094D\u0937";
        var devanagariZwjConjunct = "\u0915\u094D\u200D\u0937";
        var bengaliConjunct = "\u0995\u09CD\u09B7";
        var preservedDevanagariConjunct = TerminalPngTextPreserver.Preserve(devanagariConjunct, null);
        Assert(TerminalStoryLayout.TextElementCount(devanagariConjunct) == 1 &&
               TerminalStoryLayout.TextElementCount(devanagariZwjConjunct) == 1 &&
               TerminalStoryLayout.TextElementCount(bengaliConjunct) == 1 &&
               TerminalStoryLayout.TextElementCount(preservedDevanagariConjunct) == 1 &&
               TerminalStoryLayout.DisplayWidth(devanagariConjunct) == 1 &&
               TerminalStoryLayout.DisplayWidth(preservedDevanagariConjunct) == 1 &&
               TerminalStoryLayout.TextElementCount("\u0915\u094D\u093E\u0937") == 2,
            "Indic conjunct segmentation should apply GB9c without extending the no-break rule through unrelated spacing marks.");
        var mixedFallbackCluster = "©\u200D" + TerminalPngTextPreserver.EscapeStart + "[U+1F600]" + TerminalPngTextPreserver.EscapeEnd;
        var mixedFallbackLabel = TerminalPngTextPreserver.ClusterFallbackLabel(mixedFallbackCluster);
        Assert(TerminalStoryLayout.TextElementCount(mixedFallbackCluster) == 1 &&
               mixedFallbackLabel.Contains("U+A9", StringComparison.Ordinal) &&
               mixedFallbackLabel.Contains("U+1F600", StringComparison.Ordinal),
            "Mixed fallback clusters should retain supported and unsupported visible scalars in their fitted label.");
        var outlineFont = TrueTypeFont.TryLoadDefault();
        Assert(outlineFont == null || !outlineFont.HasGlyph('\t'),
            "Outline-font capability checks should not report unmapped whitespace as a drawable glyph.");
        var shapedFallback = TerminalPngTextPreserver.Preserve("e\u0301", outlineFont);
        Assert(TerminalStoryLayout.TextElementCount(shapedFallback) == 1 &&
               shapedFallback.Contains(TerminalPngTextPreserver.EscapeStart) &&
               TerminalStoryLayout.DisplayWidth(shapedFallback) == 1 &&
               TerminalPngTextPreserver.Preserve(shapedFallback, outlineFont) == shapedFallback,
            "PNG terminal text should fit shaping-dependent graphemes as one fallback unit even when the outline font maps every scalar.");
        var arabicWord = "\u0633\u0644\u0627\u0645";
        var preservedArabicWord = TerminalPngTextPreserver.Preserve(arabicWord, outlineFont);
        Assert(preservedArabicWord.Contains(TerminalPngTextPreserver.EscapeStart) &&
               TerminalPngTextPreserver.RasterUnits(preservedArabicWord).Count() == 1 &&
               TerminalStoryLayout.DisplayWidth(preservedArabicWord) == TerminalStoryLayout.DisplayWidth(arabicWord),
            "PNG terminal text should fit contextual shaping runs as one fallback unit instead of drawing disconnected nominal glyphs.");
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
        var formatControls = "\u00AD\u061C\u180E\u200C\u200E\u200F\u202A\u202E\u2060\u2066\u2069\uFEFF" +
                             char.ConvertFromUtf32(0x1BCA0) +
                             char.ConvertFromUtf32(0x1D173) +
                             char.ConvertFromUtf32(0xE0001);
        var preservedFormatControls = TerminalPngTextPreserver.Preserve(formatControls, null);
        var outlinePreservedFormatControls = TerminalPngTextPreserver.Preserve(formatControls, TrueTypeFont.TryLoadDefault());
        Assert(TerminalStoryLayout.DisplayWidth(formatControls) == 0 &&
               TerminalStoryLayout.DisplayWidth(preservedFormatControls) == 0 &&
               TerminalStoryLayout.DisplayWidth(outlinePreservedFormatControls) == 0 &&
               TerminalPngTextPreserver.ClusterFallbackLabel(preservedFormatControls).Length == 0 &&
               TerminalPngTextPreserver.ClusterFallbackLabel(outlinePreservedFormatControls).Length == 0 &&
               TerminalStoryLayout.DisplayWidth("\u0600") == 1 &&
               TerminalStoryLayout.TextElementCount("\u0600\u0627") == 1 &&
               TerminalStoryLayout.TextElementCount("\u0600\n") == 2 &&
               TerminalStoryLayout.TextElementCount("\u0600\r") == 2 &&
               TerminalStoryLayout.DisplayWidth("\u0600\u0627") == 1,
            "Default-ignorable format controls should consume no columns or raster fallback labels without hiding visible prepended marks.");
        var oversizedSourceRun = new string('x', 1_000_000);
        Assert(TerminalTextWidth.FitContent(oversizedSourceRun, 3, static value => value.Length) == "xxx",
            "Source fitting should stop at the visible width without preallocating the complete source run.");
        var oversizedFallback = string.Concat(Enumerable.Repeat(
            TerminalPngTextPreserver.EscapeStart + "[U+1F600]" + TerminalPngTextPreserver.EscapeEnd,
            64));
        var boundedFallbackLabel = TerminalPngTextPreserver.ClusterFallbackLabel(oversizedFallback);
        Assert(CountOccurrences(boundedFallbackLabel, "U+") == 4 &&
               boundedFallbackLabel.EndsWith(" …", StringComparison.Ordinal),
            "Raster fallback labels should bound their representation before allocating a fitted text buffer.");
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
        var visibleCommandTiming = TerminalStoryLayout.Build(TerminalStory.Create().WithFinalPrompt(false).Command("x")).DurationSeconds;
        var controlPaddedCommand = new string('\u200E', 64) + "x" + new string('\u2066', 64);
        var controlPaddedTiming = TerminalStoryLayout.Build(TerminalStory.Create().WithFinalPrompt(false).Command(controlPaddedCommand)).DurationSeconds;
        Assert(Math.Abs(visibleCommandTiming - controlPaddedTiming) < 0.001 &&
               TerminalTextWidth.VisibleElements(controlPaddedCommand).Count() == 1 &&
               string.Concat(TerminalTextWidth.VisibleElements(controlPaddedCommand)) == controlPaddedCommand,
            "Automatic typing time and reveal units should ignore retained zero-width controls without dropping them.");

        var widePromptStory = TerminalStory.Create()
            .WithDialect(TerminalDialect.Custom, "界 ")
            .Output("ready");
        var widePromptSvg = SvgDocument.Parse(widePromptStory.ToSvg());
        var widePromptCursor = widePromptSvg.Root.FindByTag("tspan").First(element => element.GetAttribute("data-cfx-role") == "terminal-cursor");
        Assert(widePromptCursor.GetAttribute("dx") == "2" &&
               widePromptCursor.GetAttribute("x") == null,
            "SVG terminal cursors should flow directly after full-width prompts instead of using a fixed coordinate.");

        var proportionalPromptTheme = TerminalTheme.Dark();
        proportionalPromptTheme.FontFamily = "Arial, sans-serif";
        var proportionalPromptStory = TerminalStory.Create()
            .WithDialect(TerminalDialect.Custom, "WWWW ")
            .WithTheme(proportionalPromptTheme)
            .Output("ready");
        var proportionalPromptLayout = TerminalStoryLayout.Build(proportionalPromptStory);
        var proportionalPromptSvg = SvgDocument.Parse(proportionalPromptStory.ToSvg());
        var proportionalPromptCursor = proportionalPromptSvg.Root.FindByTag("tspan").First(element => element.GetAttribute("data-cfx-role") == "terminal-cursor");
        Assert(Math.Abs(proportionalPromptLayout.ColumnWidth - proportionalPromptStory.FontSize) < 0.001 &&
               proportionalPromptCursor.GetAttribute("dx") == "2" &&
               proportionalPromptCursor.GetAttribute("x") == null,
            "SVG terminal cursors should use browser text flow without coupling deterministic layout to an installed font.");

        var proportionalTableStory = TerminalStory.Create()
            .WithTheme(proportionalPromptTheme)
            .WithFinalPrompt(false)
            .Table(TerminalTable.Create().WithColumns("Glyph", "Value").AddRow("WW", 1).AddRow("ii", 2));
        var proportionalTableLayout = TerminalStoryLayout.Build(proportionalTableStory);
        var proportionalTableSvg = SvgDocument.Parse(proportionalTableStory.ToSvg());
        var tableText = proportionalTableSvg.Root.FindByTag("text")
            .Where(element => element.GetAttribute("data-cfx-role") == "terminal-output")
            .ToArray();
        Assert(proportionalTableLayout.Lines.All(line => line.IsTable) &&
               tableText.Length == proportionalTableLayout.Lines.Count &&
               tableText.All(element => element.GetAttribute("font-family") == ChartFontStacks.Mono) &&
               proportionalTableStory.ToPng().Length > 8,
            "Terminal tables should use stable monospace geometry even when surrounding terminal text uses a proportional font.");

        var explicitMonoTableTheme = TerminalTheme.Dark();
        explicitMonoTableTheme.FontFamily = "'Courier New', monospace";
        var explicitMonoTableSvg = SvgDocument.Parse(TerminalStory.Create()
            .WithTheme(explicitMonoTableTheme)
            .WithFinalPrompt(false)
            .Table(TerminalTable.Create().WithColumns("State").AddRow("ready"))
            .ToSvg());
        Assert(explicitMonoTableSvg.Root.FindByTag("text")
                .Where(element => element.GetAttribute("data-cfx-role") == "terminal-output")
                .All(element => element.GetAttribute("font-family") == explicitMonoTableTheme.FontFamily),
            "SVG tables should preserve an explicitly configured monospace family used by raster output.");

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
        Assert(firstSvg.Contains("@keyframes " + firstId + "-motion-glyph", StringComparison.Ordinal) && secondSvg.Contains("@keyframes " + secondId + "-motion-glyph", StringComparison.Ordinal), "Each transcript should bind keyframes to its final identity.");
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
        AssertThrows<ArgumentException>(() => TerminalStory.Create().WithTitle("one\u2028two"), "Titles should reject Unicode line separators.");
        AssertThrows<ArgumentException>(() => TerminalStory.Create().Command("one\u2029two"), "Commands should reject Unicode paragraph separators.");
        AssertThrows<ArgumentException>(() => TerminalTable.Create().WithColumns("One", "Two").AddRow("one"), "Terminal table rows should match their column count.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStory.Create().Progress("bad", 1.1), "Terminal progress values should stay within the unit interval.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStory.Create().WithWindowStyle((TerminalWindowStyle)99), "Unknown terminal window styles should be rejected before mutation.");
        AssertThrows<ArgumentException>(() => TerminalStory.Create().Output("ready").OpenTab("bad id", "Bad", TerminalDialect.Bash, "~", TerminalTheme.Ubuntu()), "Terminal tab identifiers should remain safe deterministic keys.");
        AssertThrows<ArgumentException>(() => TerminalStory.Create().OpenTab("tools", "Tools", TerminalDialect.Bash, "~", TerminalTheme.Ubuntu()).OpenTab("TOOLS", "Duplicate", TerminalDialect.Bash, "~", TerminalTheme.Ubuntu()), "Terminal tab identifiers should be unique without case ambiguity.");
        AssertThrows<ArgumentException>(() => TerminalStory.Create().Output("ready").SelectTab("missing"), "Tab switching should reject undeclared sessions.");
        var atomicTabStory = TerminalStory.Create().Output("ready");
        AssertThrows<ArgumentOutOfRangeException>(() => atomicTabStory.OpenTab("ubuntu", "Ubuntu", TerminalDialect.Bash, "~", TerminalTheme.Ubuntu(), transitionSeconds: 3), "Invalid tab transitions should be rejected before mutating story tabs.");
        Assert(atomicTabStory.Tabs.Count == 1 && atomicTabStory.ActiveTabId == "main", "Rejected tab steps should leave tab declarations and active state unchanged.");
        AssertThrows<InvalidOperationException>(
            () => TerminalStory.Create()
                .WithWidth(480)
                .WithWindowStyle(TerminalWindowStyle.WindowsTerminal)
                .OpenTab("one", "One", TerminalDialect.Bash, "~", TerminalTheme.Ubuntu())
                .OpenTab("two", "Two", TerminalDialect.Bash, "~", TerminalTheme.Ubuntu())
                .OpenTab("three", "Three", TerminalDialect.Bash, "~", TerminalTheme.Ubuntu())
                .Output("ready")
                .ToSvg(),
            "Windows Terminal stories should reject tab strips that cannot preserve a readable minimum tab width.");
        AssertThrows<InvalidOperationException>(() => TerminalStory.Create().Output("ready").WithTitle("Too late"), "Initial tab configuration should be frozen after timeline authoring starts.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStory.Create().Transcript(Array.Empty<string>(), (TerminalTextTone)99), "Empty transcripts should still validate their semantic tone.");
        var throwingTranscriptStory = TerminalStory.Create().Output("existing");
        AssertThrows<InvalidOperationException>(() => throwingTranscriptStory.Transcript(ThrowingTranscript()), "Transcript enumeration failures should reject the complete batch.");
        Assert(throwingTranscriptStory.Steps.Count == 1 && throwingTranscriptStory.Steps[0].Text == "existing", "A failed transcript enumeration should leave the story unchanged.");
        var capacityTranscriptStory = TerminalStory.Create();
        for (var index = 0; index < 119; index++) {
            capacityTranscriptStory.Output("line");
        }
        AssertThrows<InvalidOperationException>(() => capacityTranscriptStory.Transcript(new[] { "line 120", "line 121" }), "Transcript batches should validate their complete step capacity before mutating the story.");
        Assert(capacityTranscriptStory.Steps.Count == 119, "A capacity-rejected transcript batch should leave the story unchanged.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStory.Create().WithTiming(0, 1, 0), "Typing speed should remain within usable presentation bounds.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStory.Create().WithPlaybackSpeed((TerminalStoryPlaybackSpeed)99), "Unknown playback speed presets should be rejected before mutation.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStory.Create().WithTabHold(11), "Tab reading dwell should remain within the bounded story timeline.");
        AssertThrows<InvalidOperationException>(() => TerminalStory.Create().Command("ready").WithInitialTab("late", "Late", TerminalDialect.Bash, "~", TerminalTheme.Ubuntu()), "Initial tab identity should be configured before timeline authoring starts.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStoryAnimationOptions.Create().WithFramesPerSecond(31), "Animated terminal frame rates should remain bounded.");
        AssertThrows<ArgumentOutOfRangeException>(() => TerminalStoryAnimationOptions.Create().WithOutputScale(5), "Animated terminal output scale should remain bounded.");
    }

    private static IEnumerable<string> ThrowingTranscript() {
        yield return "prospective";
        throw new InvalidOperationException("Synthetic transcript enumeration failure.");
    }
}
