using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects complete automatic endpoint marks while keeping authored clipping windows and dates intact.</summary>
public sealed class GanttEndpointMarkTests {
    private static readonly DateTime Start = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Finish = Start.AddDays(9);
    private static readonly ChartColor MilestoneColor = ChartColor.FromHex("#C34AA7");

    [Theory]
    [InlineData(360, false)]
    [InlineData(360, true)]
    [InlineData(800, false)]
    [InlineData(800, true)]
    public void AutomaticEndpointMilestonesKeepCompleteDiamondsDatesAndSharedTickProjection(int width, bool dark) {
        var chart = Milestones(width, dark);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var document = XDocument.Parse(prepared.ToSvg());
        var plot = ClipBounds(document, "gantt-milestone-shape");
        var milestones = prepared.Regions.Where(region => region.Role == "gantt-milestone").ToArray();
        Assert.Equal(new[] { "series-1-point-0", "series-2-point-0" }, milestones.Select(region => region.Id));
        Assert.All(milestones, milestone => {
            Assert.True(milestone.Bounds.Width > 0);
            Assert.True(milestone.Bounds.Left >= plot.Left - .001);
            Assert.True(milestone.Bounds.Right <= plot.Right + .001);
        });
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").OrderBy(line => line.Start.X).ToArray();
        Assert.Equal(2, grid.Length);
        Assert.Equal(milestones[0].Bounds.Left + milestones[0].Bounds.Width / 2, grid[0].Start.X, 6);
        Assert.Equal(milestones[1].Bounds.Left + milestones[1].Bounds.Width / 2, grid[1].Start.X, 6);
        var source = Role(document, "gantt-milestone");
        Assert.Equal(Start.ToOADate(), (double)source[0].Attribute("data-cfx-start")!);
        Assert.Equal(Finish.ToOADate(), (double)source[1].Attribute("data-cfx-start")!);
        Assert.Equal(Start.ToOADate(), chart.Series[1].Points[0].X);
        Assert.Equal(Finish.ToOADate(), chart.Series[2].Points[0].X);
        var window = Assert.Single(Role(document, "gantt-chart"));
        Assert.Equal(Start.ToOADate(), (double)window.Attribute("data-cfx-min")!);
        Assert.Equal(Finish.ToOADate(), (double)window.Attribute("data-cfx-max")!);
        var image = prepared.ToRgba();
        Assert.All(milestones, milestone => {
            var bounds = milestone.Bounds; var y = (int)(bounds.Top + bounds.Height / 2);
            foreach (var x in new[] { bounds.Left + bounds.Width / 4, bounds.Right - bounds.Width / 4 }) {
                var offset = (y * image.Width + (int)x) * 4;
                Assert.InRange(Math.Abs(image.Pixels[offset] - MilestoneColor.R), 0, 8);
                Assert.InRange(Math.Abs(image.Pixels[offset + 1] - MilestoneColor.G), 0, 8);
                Assert.InRange(Math.Abs(image.Pixels[offset + 2] - MilestoneColor.B), 0, 8);
            }
        });
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void AuthoredAxisSidesKeepTheirMilestoneClippingWindow(bool minimum, bool maximum) {
        var chart = Milestones(360, false).ConfigureXAxis(axis => {
            if (minimum) axis.Minimum = Start.ToOADate();
            if (maximum) axis.Maximum = Finish.ToOADate();
        });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = ClipBounds(XDocument.Parse(prepared.ToSvg()), "gantt-milestone-shape");
        var milestones = prepared.Regions.Where(region => region.Role == "gantt-milestone").ToArray();
        Assert.Equal(minimum, milestones[0].Bounds.Left < plot.Left - .001);
        Assert.Equal(maximum, milestones[1].Bounds.Right > plot.Right + .001);
        Assert.Equal(Start.ToOADate(), chart.Options.XAxis.Minimum ?? chart.Series[1].Points[0].X);
        Assert.Equal(Finish.ToOADate(), chart.Options.XAxis.Maximum ?? chart.Series[2].Points[0].X);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaneEndpointChevronsAndOutlinedStrokesReserveOnlyAutomaticSides(bool explicitBounds) {
        var chart = Chart.Create().WithSize(360, 320).WithLegend(false).WithGanttLaneNow(Finish)
            .WithStateCategories(new ChartStateCategory("up", "Available", MilestoneColor, ChartStatePattern.Outlined))
            .AddGanttLane("Service", new[] { new ChartGanttLaneItem(Start, Start.AddDays(3), "up"), new ChartGanttLaneItem(Start.AddDays(3), null, "up") });
        if (explicitBounds) chart.ConfigureXAxis(axis => axis.WithBounds(Start.ToOADate(), Finish.ToOADate()));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = prepared.Regions.Single(region => region.Role == "schedule-plot").Bounds;
        var outlines = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "gantt-lane-item-shape-outline").ToArray();
        Assert.Equal(2, outlines.Length);
        var chevron = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-lane-open-end");
        var chevronRight = chevron.Commands.Max(command => command.X) + chevron.StrokeWidth / 2;
        if (explicitBounds) Assert.True(chevronRight > plot.Right);
        else {
            Assert.True(chevronRight <= plot.Right + .001);
            Assert.All(outlines, outline => {
                Assert.True(outline.Commands.Min(command => command.X) - outline.StrokeWidth / 2 >= plot.Left - .001);
                Assert.True(outline.Commands.Max(command => command.X) + outline.StrokeWidth / 2 <= plot.Right + .001);
            });
        }
        var open = prepared.Regions.Single(region => region.Id == "series-0-point-1");
        var now = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "gantt-now-line");
        Assert.Equal(open.Bounds.Right, now.Start.X, 6);
        Assert.Equal(Start.AddDays(3).ToOADate(), chart.Series[0].GanttLaneItems[1].Start);
    }

    [Fact]
    public void AutomaticMaximumKeepsTheSupportedInstantTaskPixelFloor() {
        var chart = Chart.Create().WithSize(360, 320).WithLegend(false)
            .AddGanttTask("Work", Start, Finish.AddDays(-1)).AddGanttTask("Instant", Finish, Finish);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = ClipBounds(XDocument.Parse(prepared.ToSvg()), "gantt-task-shape");
        var instant = prepared.Regions.Single(region => region.Id == "series-1-point-0");
        Assert.True(instant.Bounds.Width >= 1);
        Assert.True(instant.Bounds.Right <= plot.Right + .001);
        Assert.Equal(Finish.ToOADate(), chart.Series[1].Points[0].X);
        Assert.Equal(Finish.ToOADate(), chart.Series[1].Points[0].Y);
    }

    private static Chart Milestones(int width, bool dark) => Chart.Create().WithSize(width, 360)
        .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithLegend(false)
        .WithGridStyle(style => style.ShowVerticalLines = true)
        .AddGanttTask("Work", Start, Finish, .5)
        .AddGanttMilestone("Start", Start, color: MilestoneColor).AddGanttMilestone("Delivery", Finish, dependsOn: 0, color: MilestoneColor)
        .ConfigureXAxis(axis => {
            axis.Labels.Add(new ChartAxisLabel(Start, "Start")); axis.Labels.Add(new ChartAxisLabel(Finish, "Delivery"));
            axis.LabelDensity = ChartLabelDensity.All;
        });

    private static XElement[] Role(XDocument document, string role) => document.Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static ChartRect ClipBounds(XDocument document, string role) {
        var reference = Role(document, role)[0].AncestorsAndSelf().Select(element => (string?)element.Attribute("clip-path")).First(value => value != null)!;
        var id = reference.Substring(5, reference.Length - 6);
        var rectangle = document.Descendants().Single(element => element.Name.LocalName == "clipPath" && (string?)element.Attribute("id") == id).Elements().Single();
        return new ChartRect((double)rectangle.Attribute("x")!, (double)rectangle.Attribute("y")!, (double)rectangle.Attribute("width")!, (double)rectangle.Attribute("height")!);
    }
}
