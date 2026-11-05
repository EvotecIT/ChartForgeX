using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Gantt charts narrower than 400 px keep readable, complete captions.</summary>
public sealed class CompactGanttCaptionTests {
    [Theory]
    [InlineData(280, VisualThemeMode.Light)]
    [InlineData(320, VisualThemeMode.Dark)]
    [InlineData(360, VisualThemeMode.Light)]
    public void CompactGanttDatesKeepOneEmApartAndTaskNamesNeverSplitAWord(int width, VisualThemeMode mode) {
        var chart = GanttRecipe.Create("compact", mode).WithSize(width, 300).WithTitle("Delivery plan");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "schedule-tick-label").OrderBy(node => node.X).ToArray();
        Assert.True(captions.Length >= 2);
        // CompactCalendarAxisTests pins each caption to its grid line; this pins the clear space between them.
        for (var index = 1; index < captions.Length; index++) {
            var clear = captions[index].X - captions[index].Text.Metrics.Width / 2 - (captions[index - 1].X + captions[index - 1].Text.Metrics.Width / 2);
            Assert.True(clear + .001 >= captions[index].Text.Size, $"Dates below 400 px keep one em apart; {clear:0.##} px is too tight.");
        }
        foreach (var name in prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "schedule-row-label")) {
            var lines = name.Text.Lines.Select(line => line.Text.Trim()).ToArray();
            var source = Assert.Single(chart.Series, series => series.Name.StartsWith(lines[0].TrimEnd('…'), StringComparison.Ordinal)).Name;
            // A name is complete, wrapped between whole words, or shortened on one line.
            Assert.True(string.Join(" ", lines) == source || lines.Length == 1 && lines[0].EndsWith('…'),
                "A task name must not break inside a word: " + string.Join(" / ", lines));
        }
    }
}
