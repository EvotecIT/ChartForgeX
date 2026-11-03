using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WordCloudLayoutTests {
    [Fact]
    public void LandscapeCloudUsesFullCanvasAndPlacesAllThirtySixTermsDeterministically() {
        var terms = new[] { "Availability", "Identity", "Network", "Storage", "Recovery", "Capacity", "Services", "Validation",
            "Security", "Automation", "Reports", "Monitoring", "Performance", "Compliance", "Cloud", "Backup", "Endpoint", "Incident",
            "Response", "Operations", "Platform", "Telemetry", "Analytics", "Discovery", "Inventory", "Deployment", "Configuration",
            "Patching", "Replication", "Directory", "Database", "Compute", "Virtualization", "Containers", "Resilience", "Governance" };
        var chart = Chart.Create().WithWordCloudAngles(0).AddWordCloud("Topics", terms.Select((term, index) => new ChartWordCloudItem(term, 100 - index * 2)));
        var plot = new ChartRect(0, 0, 752, 350);
        var placed = ChartWordCloudLayout.Compute(chart, plot);
        Assert.Equal(36, placed.Count);
        Assert.Equal(placed.Select(term => (term.X, term.Y, term.FontSize, term.Angle)),
            ChartWordCloudLayout.Compute(chart, plot).Select(term => (term.X, term.Y, term.FontSize, term.Angle)));
        Assert.True(placed.Max(term => term.X + term.Width / 2) - placed.Min(term => term.X - term.Width / 2) > plot.Width * .75);
        for (var index = 0; index < placed.Count; index++) {
            var first = placed[index];
            Assert.InRange(first.X - first.Width / 2, plot.Left, plot.Right);
            Assert.InRange(first.X + first.Width / 2, plot.Left, plot.Right);
            Assert.InRange(first.Y - first.Height / 2, plot.Top, plot.Bottom);
            Assert.InRange(first.Y + first.Height / 2, plot.Top, plot.Bottom);
            for (var other = index + 1; other < placed.Count; other++) {
                var second = placed[other];
                Assert.False(Math.Abs(first.X - second.X) < (first.Width + second.Width) / 2
                    && Math.Abs(first.Y - second.Y) < (first.Height + second.Height) / 2);
            }
        }
    }
}
