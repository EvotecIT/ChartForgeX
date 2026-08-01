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
            .WithWindowStyle(TerminalWindowStyle.MacOS)
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

        var fiveLineDemo = TerminalStory.Create()
            .WithTitle("dotnet run - ChartForgeX")
            .WithDialect(TerminalDialect.CSharp)
            .WithWorkingDirectory(@"C:\Charts")
            .WithTheme(TerminalTheme.Dark())
            .WithWindowStyle(TerminalWindowStyle.WindowsTerminal)
            .WithWidth(1000)
            .Command("using ChartForgeX; using ChartForgeX.Core; using System.Linq;", 0.65)
            .Command("var chart = Chart.Create().WithTitle(\"Weekly builds\");", 0.65)
            .Command("chart.WithXLabels(\"Mon\", \"Tue\", \"Wed\", \"Thu\", \"Fri\");", 0.65)
            .Command("chart.AddLine(\"Builds\", new[] { 12d, 18d, 15d, 24d, 31d }.Select((y, x) => new ChartPoint(x + 1, y)));", 1.1)
            .Command("chart.SavePng(\"weekly-builds.png\");", 0.65)
            .Output("Saved weekly-builds.png (1000 x 560)", TerminalTextTone.Success);
        var animation = TerminalStoryAnimationOptions.Create()
            .WithFramesPerSecond(10)
            .WithEndHold(1.4);
        fiveLineDemo.SaveGif(Path.Combine(output, "chart-in-five-lines-console-story.gif"), animation);
        fiveLineDemo.SaveApng(Path.Combine(output, "chart-in-five-lines-console-story.apng"), animation);

        var multiShell = TerminalStory.Create()
            .WithTitle("PowerShell")
            .WithDialect(TerminalDialect.PowerShell)
            .WithWorkingDirectory(@"C:\OpenSource")
            .WithTheme(TerminalTheme.Campbell())
            .WithWindowStyle(TerminalWindowStyle.WindowsTerminal)
            .WithWidth(1100)
            .Command("Get-Module ImagePlayground")
            .Output("ImagePlayground  3.2.0", TerminalTextTone.Success)
            .OpenTab("windows-powershell", "Windows PowerShell", TerminalDialect.PowerShell, @"C:\Legacy", TerminalTheme.WindowsPowerShell(), TerminalTabIcon.WindowsPowerShell)
            .Command("$PSVersionTable.PSVersion")
            .OpenTab("ubuntu", "Ubuntu", TerminalDialect.Bash, "~/src", TerminalTheme.Ubuntu(), TerminalTabIcon.Ubuntu)
            .Command("dotnet test")
            .Output("Passed! Failed: 0", TerminalTextTone.Success)
            .SelectTab("main")
            .Output("All environments are ready.", TerminalTextTone.Success);
        multiShell.SaveSvg(Path.Combine(output, "multi-shell-console-story.svg"));
        multiShell.SaveGif(Path.Combine(output, "multi-shell-console-story.gif"), animation);
    }
}
