using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Terminal;

internal static class TerminalStoryExamples {
    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var portfolio = TerminalTable.Create()
            .WithColumns("PROJECT", "STACK", "STATUS")
            .AddRow("ChartForgeX", ".NET", "ready")
            .AddRow("ImagePlayground", "PowerShell", "ready")
            .AddRow("OfficeIMO", ".NET", "active");

        var story = TerminalStory.Create()
            .WithTitle(@"pwsh - C:\OpenSource")
            .WithDialect(TerminalDialect.PowerShell)
            .WithWorkingDirectory(@"C:\OpenSource")
            .WithTheme(TerminalTheme.PowerShell())
            .WithPngOutputScale((int)pngOutputScale)
            .Command("Get-ActivePortfolio | Format-Table")
            .Table(portfolio)
            .Blank()
            .Command(@".\Invoke-ReleaseValidation.ps1")
            .Output("Restoring packages...", TerminalTextTone.Muted)
            .Progress("Tests", 1)
            .Output("PASS  755 tests", TerminalTextTone.Success)
            .Output("Ready to publish", TerminalTextTone.Success);

        story.SaveSvg(Path.Combine(output, "powershell-console-story.svg"));
        story.SaveHtml(Path.Combine(output, "powershell-console-story.html"));
        story.SavePng(Path.Combine(output, "powershell-console-story.png"));
    }
}
