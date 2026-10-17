using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData("cadence", 0)]
    [InlineData("cadence", 1_000)]
    [InlineData("cadence", 9_000)]
    [InlineData("state", 0)]
    [InlineData("state", 1_000)]
    [InlineData("state", 9_000)]
    public async Task PreciseHalfOpenTimeIntervalsRoundTripWithoutAliasingAdjacentInstants(string family, int phaseTicks) {
        if (!Enabled) return;
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(phaseTicks);
        var boundary = start.AddDays(1);
        await using var session = await OpenAsync(ChartLinkedProjectionContractsTests.PreciseTimeIntervalsChart(family, start).ToInteractiveHtmlPage(Fit), 820, 650);
        await BindLinkedEventsAsync(session.Page);
        var result = await session.Page.EvaluateAsync<JsonElement>("""
            facts=>{
                const select=range=>{chartController.select({ranges:[range]});return chartController.getSelection().map(target=>target.observationId)};
                chartController.select({observationIds:['first']});const first=chartController.getSelection()[0];
                return{first,roundTrip:select(first.range),reversed:select([...first.range].reverse()),
                    boundary:select([facts.boundary,facts.boundary]),offsetBoundary:select([facts.offsetBoundary,facts.offsetBoundary]),
                    predecessor:select([facts.predecessor,facts.predecessor]),interior:select([facts.interior,facts.interior]),
                    mixed:select([facts.boundary,0]),invalid:select(['invalidZ','invalidZ']),events:linkedEvents.length};
            }
            """, new {
                boundary = boundary.ToString("O"),
                offsetBoundary = new DateTimeOffset(boundary).ToOffset(TimeSpan.FromHours(2)).ToString("O"),
                predecessor = boundary.AddTicks(-1).ToString("O"), interior = boundary.AddTicks(-500).ToString("O")
            });
        Assert.Equal(boundary.AddTicks(-1).ToString("O"), result.GetProperty("first").GetProperty("range")[1].GetString());
        foreach (var name in new[] { "roundTrip", "reversed", "predecessor", "interior" })
            Assert.Equal("first", Assert.Single(result.GetProperty(name).EnumerateArray()).GetString());
        foreach (var name in new[] { "boundary", "offsetBoundary" })
            Assert.Equal("second", Assert.Single(result.GetProperty(name).EnumerateArray()).GetString());
        Assert.Empty(result.GetProperty("mixed").EnumerateArray());
        Assert.Empty(result.GetProperty("invalid").EnumerateArray());
        Assert.Equal(0, result.GetProperty("events").GetInt32());
        await CaptureAsync(session.Page, "hosted-precise-halfopen-" + family + "-" + phaseTicks, result);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task PreciseTimePointMatchesKeepDistinctInstantsAndNormalizeOffsets() {
        if (!Enabled) return;
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var instants = new[] { start, start.AddTicks(1_000), start.AddTicks(9_000) };
        var chart = Chart.Create().WithSize(640, 360).WithXAxisTimeScale()
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("precise-points"), "Readings", instants.Select((instant, index) => new ChartPoint(instant, index + 1)))
                .WithObservationIds(new[] { new ChartObservationId("zero"), new ChartObservationId("one"), new ChartObservationId("nine") }));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Fit), 820, 650);
        await BindLinkedEventsAsync(session.Page);
        var result = await session.Page.EvaluateAsync<JsonElement>("""
            facts=>{const selections=facts.map(instant=>{chartController.select({ranges:[[instant,instant]]});return chartController.getSelection().map(target=>target.observationId)});
                return{selections,events:linkedEvents.length}}
            """, instants.Select(instant => instant.ToString("O")).Concat(new[] {
                new DateTimeOffset(instants[1]).ToOffset(TimeSpan.FromHours(-5)).ToString("O"),
                instants[1].AddTicks(1).ToString("O")
            }).ToArray());
        var selections = result.GetProperty("selections");
        foreach (var (index, id) in new[] { (0, "zero"), (1, "one"), (2, "nine"), (3, "one") })
            Assert.Equal(id, Assert.Single(selections[index].EnumerateArray()).GetString());
        Assert.Empty(selections[4].EnumerateArray());
        Assert.Equal(0, result.GetProperty("events").GetInt32());
        await CaptureAsync(session.Page, "hosted-precise-time-points", result);
        AssertNoConsoleErrors(session);
    }
}
