using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FactBlockBadgeTypographyTests {
    [Theory]
    [InlineData("table", false, false)]
    [InlineData("table", false, true)]
    [InlineData("table", true, false)]
    [InlineData("table", true, true)]
    [InlineData("activity", false, false)]
    [InlineData("activity", true, false)]
    [InlineData("schedule", false, false)]
    [InlineData("schedule", true, false)]
    public void BadgesReserveTheDrawnBoldCaptionWidthBeforeFitting(string family, bool dark, bool dense) {
        var caption = family == "schedule" ? "WWWW" : "WWWWWW";
        var theme = VisualTheme.Report();
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        IVisualBlock block;
        string role;
        if (family == "table") {
            var table = ChartTable.Create().WithTheme(theme, mode).WithSize(480, 180).WithColumns("Name", "Status", "History")
                .WithDenseMode(dense).AddRow("Configured", "Status", "History");
            table.ConfigureRow(0, row => row.Cells[1].WithBadge(caption, VisualStatus.Positive));
            block = table;
            role = "visual-badge";
        } else if (family == "activity") {
            block = ActivityTimelineBlock.Create().WithTheme(theme, mode).WithSize(480, 180).AddEvent("Configured", badge: caption);
            role = "activity-badge";
        } else {
            block = ScheduleTimelineBlock.Create().WithTheme(theme, mode).WithSize(480, 180)
                .WithTimeRange(8, 17).AddEvent("Configured", 9, 16, badge: caption);
            role = "schedule-event-badge";
        }
        var badge = Assert.Single(XDocument.Parse(block.ToSvg()).Descendants(), element =>
            (string?)element.Attribute("data-cfx-role") == role);
        var text = badge.ElementsAfterSelf().First(element => element.Name.LocalName == "text");

        Assert.Equal(caption, text.Value);
        var fontSize = Number(text, "font-size");
        var measured = TextLayoutEngine.Measure(text.Value, new TextStyle {
            Font = new FontSpec { Family = theme.Typography.Family, Weight = 700 },
            FontSize = fontSize,
            LineHeight = 1
        }).Width;
        Assert.InRange(fontSize, 10, 14);
        Assert.True(measured <= Number(badge, "width") - (family == "schedule" ? 10 : 14) + .01,
            "A badge with available cell space must reserve its actual bold caption width and readable padding.");
    }

    [Fact]
    public void ConstrainedTableBadgesFitInsideTheCellAtTheirReadableFontSize() {
        var table = ChartTable.Create().WithSize(180, 120).AddColumn("Status", width: 76).AddRow("Status");
        table.ConfigureRow(0, row => row.Cells[0].WithBadge("WWWWWW", VisualStatus.Positive));
        var badge = Assert.Single(XDocument.Parse(table.ToSvg()).Descendants(), element =>
            (string?)element.Attribute("data-cfx-role") == "visual-badge");
        var text = badge.ElementsAfterSelf().First(element => element.Name.LocalName == "text");
        Assert.EndsWith("...", text.Value);
        Assert.InRange(Number(badge, "width"), 1, 60);
        Assert.Equal(11.5, Number(text, "font-size"));
        var measured = TextLayoutEngine.Measure(text.Value, new TextStyle {
            Font = new FontSpec { Family = table.Options.Theme.Typography.Family, Weight = 700 },
            FontSize = 11.5,
            LineHeight = 1
        }).Width;
        Assert.True(measured <= Number(badge, "width") - 14 + .01);
    }

    private static double Number(XElement element, string attribute) =>
        double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
