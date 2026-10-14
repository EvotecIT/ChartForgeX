using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ColorScalePartialDomainNativeTests {
    private static readonly ChartColor Low = ChartColor.FromHex("#204060"), High = ChartColor.FromHex("#E0A060"), Missing = ChartColor.FromHex("#A0B0C0");

    private static Chart Create(string family, double?[] values, int width = 800, bool dark = false) {
        const string font = "CFX Partial Color Carlito";
        FontRegistry.Register(font, Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        var chart = Chart.Create().WithSize(width, 480).WithDataLabels(false).WithPngSupersampling(1).WithFontFamily(font)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.Options.TransparentBackground = false; chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.ShowGrid = false;
        if (family is "scatter" or "bubble") {
            var initial = values.Length > 0 ? values : new double?[] { 0 };
            if (family == "bubble") chart.AddBubble("Observed", initial.Select((value, index) => new ChartBubble(index + 1, 10 + index * 5, 36).WithColorValue(value)));
            else chart.AddScatter("Observed", initial.Select((value, index) => new ChartPoint(index + 1, 10 + index * 5).WithColorValue(value)));
            if (values.Length == 0) {
                if (family == "bubble") chart.Series[0].ReplaceBubbles(Array.Empty<ChartBubble>());
                else chart.Series[0].Points.Clear();
            }
            chart.WithXAxisBounds(0, 5).WithYAxisBounds(0, 35);
        } else if (family is "heatmap" or "hexbin") {
            // A refresh may clear observations while the row retains its declared columns.
            var initial = values.Any(value => value.HasValue) ? values : values.Select((value, index) => index == 0 ? (double?)0 : null).ToArray();
            if (family == "heatmap") chart.AddHeatmapRow("Observed", initial); else chart.AddHexbinHeatmapRow("Observed", initial);
            if (!values.Any(value => value.HasValue)) chart.Series[0].Points.Clear();
        } else if (family == "calendar") {
            var initial = values.Length > 0 ? values : new double?[] { 0 };
            chart.AddCalendarHeatmap("Observed", initial.Select((value, index) => (value, index))
                .Where(item => item.value.HasValue).Select(item => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5).AddDays(item.index), item.value!.Value)));
            if (values.Length == 0) chart.Series[0].ReplaceCalendarItems(Array.Empty<ChartCalendarHeatmapItem>());
        }
        else if (family is "treemap" or "sunburst") {
            var leaves = values.Select((value, index) => new ChartHierarchyItem("leaf-" + index, "Observation " + index,
                family == "sunburst" ? "root" : null, index + 1, value));
            if (family == "sunburst") chart.AddSunburst("Observed", new[] { new ChartHierarchyItem("root", "All") }.Concat(leaves));
            else chart.AddTreemap("Observed", leaves).ConfigureTreemap(options => options.ShowGroupLabels = false);
        } else {
            var codes = new[] { "CA", "NY", "TX", "WA" };
            var items = values.Select((value, index) => (value, index)).Where(item => item.value.HasValue)
                .Select(item => new ChartRegionMapItem(codes[item.index], item.value!.Value)).ToArray();
            var initial = items.Length > 0 ? items : new[] { new ChartRegionMapItem(codes[0], 0) };
            if (family == "region-map") chart.AddRegionMap("Observed", ChartMapCatalog.Get("us-states"), initial);
            else chart.AddTileMap("Observed", ChartTileMapCatalog.Get("us-states"), initial);
            if (items.Length == 0) chart.Series[0].ReplaceRegionValues(Array.Empty<ChartRegionMapItem>());
            chart.WithMapLabels(false);
        }
        return chart;
    }

    private static void SetScale(Chart chart, string family, ChartColorScale scale) {
        if (family is "scatter" or "bubble") chart.Series[0].WithColorScale(scale);
        else if (family is "heatmap" or "hexbin" or "calendar") chart.WithHeatmapColorScale(scale);
        else if (family == "sunburst") chart.ConfigureSunburst(options => options.ColorScale = scale);
        else if (family == "treemap") chart.ConfigureTreemap(options => options.ColorScale = scale);
        else chart.WithMapColorScale(scale);
    }

    private static ChartColorDimensionOptions Dimension(Chart chart, string family) => family switch {
        "scatter" or "bubble" => chart.Series[0].ColorDimension,
        "heatmap" or "hexbin" or "calendar" => chart.Options.HeatmapColorDimension,
        "sunburst" => chart.Options.Sunburst.ColorDimension, "treemap" => chart.Options.Treemap.ColorDimension,
        _ => chart.Options.MapColorDimension
    };
    private static string Prefix(string family) => family switch {
        "scatter" or "bubble" => "color-scale-0", "heatmap" or "hexbin" => "heatmap-scale", "calendar" => "calendar-scale",
        "treemap" or "sunburst" => family + "-color-scale", _ => "map-scale"
    };
    private static string SourceRole(string family) => family switch {
        "scatter" or "bubble" => "point", "heatmap" => "heatmap-cell", "hexbin" => "hexbin-cell", "calendar" => "calendar-cell",
        "sunburst" => "sunburst-segment", "treemap" => "treemap-tile", _ => family + "-region-source"
    };
    private static string MarkRole(string family) => family switch {
        "scatter" => "marker", "bubble" => "bubble", "heatmap" => "heatmap-cell-shape", "hexbin" => "hexbin-cell-shape", "calendar" => "calendar-cell-shape",
        "sunburst" => "sunburst-segment-mark", "treemap" => "treemap-tile-mark", _ => family + "-region"
    };
    private static string ValueAttribute(string family) => family is "scatter" or "bubble" or "sunburst" or "treemap" ? "data-cfx-color-value" : "data-cfx-value";
    private static XElement[] Role(XDocument document, string role) => document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static RgbaImage Image(PreparedVisual prepared) => RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));

    private static void AssertKeyBounds(PreparedVisual prepared, string prefix, double width, double height) {
        var boxes = prepared.Scene.Nodes.OfType<VisualSceneRectangle>()
            .Where(node => node.Role == prefix + "-step" || node.Role == prefix + "-swatch")
            .Select(node => node.Bounds).Concat(prepared.Scene.Nodes.OfType<VisualSceneGradient>()
                .Where(node => node.Role == prefix + "-gradient")
                .Select(node => Assert.IsType<VisualSceneRectangle>(node.Shape).Bounds)).ToArray();
        Assert.NotEmpty(boxes);
        Assert.All(boxes, box => Assert.True(box.Width > 0 && box.Height > 0 && box.Left >= 0 && box.Top >= 0
            && box.Right <= width + .000001 && box.Bottom <= height + .000001));
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == prefix + "-label"
            || node.Role == prefix + "-title" || node.Role == prefix + "-midpoint-label").ToArray();
        Assert.NotEmpty(captions);
        Assert.All(captions, caption => {
            Assert.InRange(caption.Baseline - caption.Text.Ascent, -.000001, height);
            Assert.InRange(caption.Baseline - caption.Text.Ascent + caption.Text.Metrics.Height, 0, height + .000001);
            Assert.All(caption.Text.Lines, line => {
                Assert.InRange(caption.LineLeft(line), -.000001, width);
                Assert.InRange(caption.LineLeft(line) + line.Width, 0, width + .000001);
            });
        });
    }

    private static void AssertPixel(RgbaImage image, double x, double y, ChartColor color, int tolerance = 1) {
        var backdrop = ChartColor.FromRgba(image.Pixels[0], image.Pixels[1], image.Pixels[2], image.Pixels[3]);
        var expected = color.A == 255 ? color : ChartColorMath.Blend(backdrop, ChartColor.FromRgb(color.R, color.G, color.B), color.A / 255d);
        var offset = ((int)y * image.Width + (int)x) * 4;
        Assert.InRange(Math.Abs(image.Pixels[offset] - expected.R), 0, tolerance);
        Assert.InRange(Math.Abs(image.Pixels[offset + 1] - expected.G), 0, tolerance);
        Assert.InRange(Math.Abs(image.Pixels[offset + 2] - expected.B), 0, tolerance);
        Assert.Equal(255, image.Pixels[offset + 3]);
    }
}
