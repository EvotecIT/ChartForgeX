using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianGuideRoleTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitAxisAndGridColorsKeepTheirIndependentRoles(bool horizontal) {
        var chart = Chart.Create().WithSize(480, 320).WithHeader(false).WithLegend(false);
        chart.Options.Theme.Axis = ChartColor.FromHex("#A02030");
        chart.Options.Theme.Grid = ChartColor.FromHex("#2040A0");
        chart.Options.Theme.CardBorder = ChartColor.FromHex("#20A040");
        chart.Options.GridLineStyle = new ChartGridLineStyle { HorizontalOpacity = 1, VerticalOpacity = 1 };
        var points = new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) };
        if (horizontal) chart.AddHorizontalBar("Values", points);
        else chart.AddLine("Values", points);
        var document = XDocument.Parse(chart.ToSvg());
        var axes = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") is "axis-x" or "axis-y").ToArray();
        var grid = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") is "grid-x" or "grid-y").ToArray();
        Assert.Equal(2, axes.Length);
        Assert.NotEmpty(grid);
        Assert.All(axes, element => Assert.Equal("#A02030", (string?)element.Attribute("stroke")));
        Assert.All(grid, element => Assert.Equal("#2040A0", (string?)element.Attribute("stroke")));
        Assert.NotEqual(chart.ToPng(), chart.ConfigureTheme(theme => { theme.Axis = ChartColors.Transparent; theme.Grid = ChartColors.Transparent; }).ToPng());
    }
}
