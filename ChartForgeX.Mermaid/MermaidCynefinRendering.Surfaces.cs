using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidCynefinRendering {
    private static readonly string[] LightBackgrounds = { "#E8F5E9", "#E3F2FD", "#FBE9E7", "#FFF8E1", "#F3E5F5" };
    private static readonly string[] DarkBackgrounds = { "#1B5E20", "#0D47A1", "#BF360C", "#F57F17", "#4A148C" };

    private static ChartColor DomainBackground(TopologyChart chart, MermaidCynefinSettings? settings, string name, int index, double opacity) {
        var defaults = chart.ThemeMode == VisualThemeMode.Dark ? DarkBackgrounds : LightBackgrounds;
        var color = settings?.Color(name + "Bg", ChartColor.FromHex(defaults[index])) ?? ChartColor.FromHex(defaults[index]);
        return color.WithOpacity(color.A / 255d * opacity);
    }

    private static void AddDomainBackgrounds(TopologyChart chart, MermaidCynefinSettings? settings, double x, double y, double width, double height) {
        var names = new[] { "complex", "complicated", "chaotic", "clear" };
        for (var i = 0; i < names.Length; i++) {
            var path = ChartPathBuilder.RoundedRectangle(new ChartRect(x + i % 2 * width / 2, y + i / 2 * height / 2, width / 2, height / 2), 0);
            chart.PathDecorations.Add(new TopologyPathDecoration("cynefin-background-" + names[i], NativePath(path),
                fill: DomainBackground(chart, settings, names[i], i, .4), close: true));
        }
    }

    private static void AddConfusionBackground(TopologyChart chart, MermaidCynefinSettings? settings, double x, double y, double width, double height) {
        var colors = chart.Theme.Resolve(chart.ThemeMode);
        var ellipse = ChartPathBuilder.Ellipse(x + width / 2, y + height / 2, width * .15, height * .15);
        chart.PathDecorations.Add(new TopologyPathDecoration("cynefin-confusion", NativePath(ellipse),
            fill: DomainBackground(chart, settings, "confusion", 4, .5), stroke: settings?.Color("boundaryColor", colors.Border) ?? colors.Border,
            strokeWidth: 1.5, close: true, dashPattern: new[] { 4d, 2d }));
    }

    private static SvgPathData NativePath(ChartPath path) {
        var result = new SvgPathData();
        foreach (var command in path.Commands) result.Add(command.Kind == ChartPathCommandKind.MoveTo
            ? new SvgPathCommand('M', new[] { command.X, command.Y }) : command.Kind == ChartPathCommandKind.LineTo
            ? new SvgPathCommand('L', new[] { command.X, command.Y })
            : new SvgPathCommand('C', new[] { command.Control1X, command.Control1Y, command.Control2X, command.Control2Y, command.X, command.Y }));
        return result;
    }
}
