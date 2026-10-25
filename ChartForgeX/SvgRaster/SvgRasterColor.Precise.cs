using System;
using System.Globalization;
using ChartForgeX.Rendering;
namespace ChartForgeX.SvgRaster;
internal static partial class SvgRasterColor {
    /// <summary>Reads the same supported literal color grammar without quantizing intermediate derivation channels.</summary>
    internal static bool TryParsePrecise(string? value,out ChartColorMath.PreciseColor color) {
        color=default;if(string.IsNullOrWhiteSpace(value))return false;var text=value!.Trim();
        if(TryReadHsl(text,out var h,out var s,out var l,out var a)){color=ChartColorMath.PreciseColor.FromHsl(h,s,l,a);return true;}
        var open=text.IndexOf('(');if(open>0&&text[text.Length-1]==')') {
            var function=text.Substring(0,open).Trim();if(function.Equals("rgb",StringComparison.OrdinalIgnoreCase)||function.Equals("rgba",StringComparison.OrdinalIgnoreCase)) {
                var parts=SplitFunctionArguments(text.Substring(open+1,text.Length-open-2));if(parts.Count<3||parts.Count>4||!Scalar(parts[0],false,out var r)||!Scalar(parts[1],false,out var g)||!Scalar(parts[2],false,out var b))return false;
                var alpha=1d;if(parts.Count==4&&!Scalar(parts[3],true,out alpha))return false;color=ChartColorMath.PreciseColor.FromRgb(r,g,b,alpha);return true;
            }
        }
        if(!TryParse(text,out var native))return false;color=ChartColorMath.PreciseColor.FromColor(native);return true;
        static bool Scalar(string input,bool alpha,out double result) {
            result=0;if(TryParsePercent(input,out var percent)){if(double.IsNaN(percent)||double.IsInfinity(percent))return false;result=percent*(alpha?1:255);return true;}
            if(!double.TryParse(input,NumberStyles.Float,CultureInfo.InvariantCulture,out var value)||double.IsNaN(value)||double.IsInfinity(value))return false;
            result=alpha?Math.Max(0,Math.Min(1,value<=1?value:value/255)):Math.Max(0,Math.Min(255,value));return true;
        }
    }
}
