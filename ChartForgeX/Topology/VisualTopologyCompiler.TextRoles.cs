using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    // Preferred role sizes are logical; candidate sizes passed to measurement are already scaled and fitted.
    private static double RoleTextSize(double size, TextStyleOverride? role) => role?.FontSize ?? size;
    private double RoleTextOutputSize(double size, TextStyleOverride? role) => role?.FontSize is double authored ? authored * _scale : size;
    private TextStyle RoleTextStyle(double size, int weight, TextStyleOverride role) {
        var style = TopologyTextPolicy.ResolveRole(_textPolicy, role, _context.Font, size, weight);
        style.LetterSpacing *= _scale; return style;
    }
    private TextMetrics MeasureRoleText(string text, double size, int weight, TextStyleOverride? role) =>
        role?.HasOverrides == true ? _builder.MeasureText(text, RoleTextStyle(size, weight, role)) : MeasurePolicyText(text, size, weight);
    private double RoleTextAscent(double size, int weight, TextStyleOverride? role) =>
        role?.HasOverrides == true ? _builder.TextAscent(RoleTextStyle(size, weight, role)) : PolicyTextAscent(size, weight);
    private void DrawRoleText(string text, double x, double y, double size, ChartColor color, int weight,
        string roleName, TextAlignment alignment, SvgPaint paint, TextStyleOverride? role) {
        if (role?.HasOverrides != true) DrawPolicyText(text, x, y, size, color, weight, roleName, alignment: alignment, paint: paint);
        else { var style = RoleTextStyle(size, weight, role); style.Color = color; style.Alignment = alignment;
            _builder.Text(text, x, y, style, roleName, paint: paint); }
    }
}
