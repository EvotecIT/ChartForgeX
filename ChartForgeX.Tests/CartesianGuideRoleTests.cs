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
        chart.ConfigureTheme(builder => {
            builder.Light.Axis = ChartColor.FromHex("#A02030");
            builder.Light.Grid = ChartColor.FromHex("#2040A0");
            builder.Light.Border = ChartColor.FromHex("#20A040");
        });
        chart.Options.GridLineStyle = new ChartGridLineStyle { ShowVerticalLines = true, HorizontalOpacity = 1, VerticalOpacity = 1 };
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
        Assert.NotEqual(chart.ToPng(), chart.ConfigureTheme(builder => { builder.Light.Axis = ChartColors.Transparent; builder.Light.Grid = ChartColors.Transparent; }).ToPng());
    }
}
