using System.Text.Json;
using ChartForgeX;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class JourneyTimelineInterchangeBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 390)] [InlineData(false, 1100)] [InlineData(true, 1100)]
    public async Task ImportedAndEditedCategoricalFactsUseTheSameScriptFreeNativePaint(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        foreach (var kind in new[] { "journey", "timeline-lr", "timeline-td" }) {
            var envelope = VisualArtifactInterchangeEnvelope.FromJson(JourneyTimelineInterchangeTests.Parsed(kind, width, dark).ToInterchangeJson());
            if (kind == "journey") envelope.Journey!.Tasks[0] = new JourneyTask("task-0", "Imported task", 1, new[] { "Reviewer" }, "section-0");
            else envelope.Timeline!.Periods[0] = new TimelinePeriod("period-0", "Before launch", new[] { "Imported event", "Review findings" }, "section-0");
            var block = JourneyTimelineInterchangeTests.Import(envelope); var svg = block.RenderSvg("portable-browser"); var png = block.ToPng();
            await using var session = await BrowserProofFonts.OpenAsync(svg, width, 720);
            var pageErrors = new List<string>(); session.Page.PageError += (_, error) => pageErrors.Add(error);
            var observed = await session.Page.EvaluateAsync<JsonElement>("""
                () => {
                    const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect();
                    const read=role=>[...svg.querySelectorAll(`[data-cfx-role=${role}]`)].map(e=>({
                        id:e.dataset.taskId??e.dataset.periodId??null,section:e.dataset.sectionId??null,score:e.dataset.score??null,
                        visible:e.getBoundingClientRect().width>0&&e.getBoundingClientRect().height>0}));
                    return {tasks:read('journey-task'),periods:read('timeline-period'),events:read('timeline-event'),
                        score:[...svg.querySelectorAll('[data-cfx-role=journey-score]')].map(e=>+e.getAttribute('cy')),
                        text:[...svg.querySelectorAll('text')].map(e=>e.textContent),active:svg.querySelectorAll('script,image,foreignObject').length,
                        width:frame.width,height:frame.height};
                }
                """);
            var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(output)) {
                Directory.CreateDirectory(output); var stem = Path.Combine(output, "portable-" + kind + "-" + width + (dark ? "-dark" : "-light"));
                File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".native.png", png); File.WriteAllText(stem + ".json", envelope.ToJson());
                File.WriteAllText(stem + ".observed.json", JsonSerializer.Serialize(new { observed, pageErrors, session.ConsoleLog }));
                await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            }
            Assert.Equal(0, observed.GetProperty("active").GetInt32()); Assert.Equal(width, observed.GetProperty("width").GetDouble());
            if (kind == "journey") {
                var tasks = observed.GetProperty("tasks").EnumerateArray().ToArray(); Assert.Equal(3, tasks.Length);
                Assert.Equal("task-0", tasks[0].GetProperty("id").GetString()); Assert.Equal("1", tasks[0].GetProperty("score").GetString());
                Assert.NotEqual(tasks[0].GetProperty("section").GetString(), tasks[2].GetProperty("section").GetString());
                Assert.All(tasks, task => Assert.True(task.GetProperty("visible").GetBoolean()));
                Assert.Contains("Imported task", string.Join(" ", observed.GetProperty("text").EnumerateArray().Select(text => text.GetString())));
            } else {
                var periods = observed.GetProperty("periods").EnumerateArray().ToArray(); Assert.Equal(3, periods.Length);
                Assert.Equal(5, observed.GetProperty("events").GetArrayLength());
                Assert.All(periods, period => Assert.True(period.GetProperty("visible").GetBoolean()));
                Assert.NotEqual(periods[0].GetProperty("section").GetString(), periods[2].GetProperty("section").GetString());
                Assert.Contains("Imported event", string.Join(" ", observed.GetProperty("text").EnumerateArray().Select(text => text.GetString())));
            }
            Assert.Empty(pageErrors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }
}
