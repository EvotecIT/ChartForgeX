using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

/// <summary>Bounds native contour paint values once for authoring, preparation and portable captures.</summary>
internal static class TopologySurfacePaintRules {
    internal const int MaximumDashValues=64;
    internal static bool TryColor(string? value, ChartColor fallback, out ChartColor color) {
        color = fallback;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (string.Equals(value!.Trim(), "none", StringComparison.OrdinalIgnoreCase)) { color = ChartColor.Transparent; return true; }
        return SvgRasterColor.TryParse(value, out color) || SvgPaint.TryCssVariable(value, fallback, out color, out _);
    }
    internal static string? BorderColorError(string? value) => TryColor(value, ChartColor.Black, out _) ? null : "contour color must use an existing safe native color, none or CSS variable";
    internal static void Opacity(double? value,string parameter) {
        if(value.HasValue&&(double.IsNaN(value.Value)||double.IsInfinity(value.Value)||value.Value<0||value.Value>1))
            throw new ArgumentOutOfRangeException(parameter,value,"Opacity must be finite and between zero and one.");
    }
    internal static string? DashError(IReadOnlyList<double> dash) {
        if(dash.Count>MaximumDashValues)return "dash pattern cannot exceed 64 values";
        if(dash.Count%2!=0)return "dash pattern must contain alternating dash and gap pairs";
        foreach(var value in dash)if(double.IsNaN(value)||double.IsInfinity(value)||value<=0)return "dash pattern values must be positive finite numbers";
        return null;
    }
    internal static string DashCode(IReadOnlyList<double> dash)=>dash.Count>MaximumDashValues?"dash-pattern-limit":dash.Count%2!=0?"dash-pattern-pairs":"dash-pattern-value";
    internal static string? Error(double? width,double? opacity,double? surface,IReadOnlyList<double> dash) {
        if(width.HasValue&&(double.IsNaN(width.Value)||double.IsInfinity(width.Value)||width.Value<=0))return "stroke width must be positive and finite";
        if(!ValidOpacity(opacity)||!ValidOpacity(surface))return "opacity must be finite and between zero and one";
        return DashError(dash);
    }
    internal static bool HasOverrides(double? width,double? opacity,double? surface,IReadOnlyList<double> dash)=>
        width.HasValue||opacity.HasValue&&opacity.Value!=1||surface.HasValue&&surface.Value!=1||dash.Count>0;
    private static bool ValidOpacity(double? value)=>!value.HasValue||!double.IsNaN(value.Value)&&!double.IsInfinity(value.Value)&&value.Value>=0&&value.Value<=1;
}
