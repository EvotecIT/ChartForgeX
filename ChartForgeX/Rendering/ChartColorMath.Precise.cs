using System;
using ChartForgeX.Primitives;
namespace ChartForgeX.Rendering;
internal static partial class ChartColorMath {
    /// <summary>Unquantized channels for a finite sequence of native color adjustments. Byte conversion occurs only at the paint boundary.</summary>
    internal readonly struct PreciseColor {
        internal readonly double R,G,B,A;
        private PreciseColor(double r,double g,double b,double a){R=Clamp01(r);G=Clamp01(g);B=Clamp01(b);A=Clamp01(a);}
        internal static PreciseColor FromColor(ChartColor color)=>new(color.R/255d,color.G/255d,color.B/255d,color.A/255d);
        internal static PreciseColor FromRgb(double r,double g,double b,double a=1)=>new(r/255d,g/255d,b/255d,a);
        internal static PreciseColor FromHsl(double hue,double saturation,double lightness,double alpha=1) {
            return FromNormalizedHsl((hue%360+360)%360,Clamp01(saturation),Clamp01(lightness),alpha);
        }
        internal static PreciseColor FromNormalizedHsl(double hue,double saturation,double lightness,double alpha=1) {
            var chroma=(1-Math.Abs(2*lightness-1))*saturation;var secondary=chroma*(1-Math.Abs(hue/60%2-1));var offset=lightness-chroma/2;
            double r=0,g=0,b=0;if(hue<60){r=chroma;g=secondary;}else if(hue<120){r=secondary;g=chroma;}else if(hue<180){g=chroma;b=secondary;}else if(hue<240){g=secondary;b=chroma;}else if(hue<300){r=secondary;b=chroma;}else{r=chroma;b=secondary;}
            return new PreciseColor(r+offset,g+offset,b+offset,alpha);
        }
        /// <summary>Adjusts HSL in degrees/percentage points, rounding retained expression channels to ten decimal places without byte quantization.</summary>
        internal PreciseColor AdjustHsl(double hue=0,double saturation=0,double lightness=0) {
            Hsl(out var h,out var s,out var l);h=(h+hue)%360;s=Math.Max(0,Math.Min(100,s*100+saturation));l=Math.Max(0,Math.Min(100,l*100+lightness));
            return FromHsl(Round10(h),Round10(s)/100,Round10(l)/100,A);
        }
        /// <summary>Inverts unassociated RGB channels while retaining alpha and the existing expression precision.</summary>
        internal PreciseColor Invert()=>FromRgb(Round10((1-R)*255),Round10((1-G)*255),Round10((1-B)*255),A);
        private void Hsl(out double h,out double s,out double l) {
            var max=Math.Max(R,Math.Max(G,B));var min=Math.Min(R,Math.Min(G,B));var difference=max-min;l=(max+min)/2;
            if(difference==0){h=0;s=0;return;}s=difference/(1-Math.Abs(2*l-1));
            h=max==R?60*((G-B)/difference%6):max==G?60*((B-R)/difference+2):60*((R-G)/difference+4);h=(h+360)%360;
        }
        internal ChartColor ToColor()=>ChartColor.FromRgba(QuantizeSrgb(R*255),QuantizeSrgb(G*255),QuantizeSrgb(B*255),QuantizeSrgb(A*255));
        private static double Round10(double value)=>Math.Floor(value*10000000000d+.5)/10000000000d;
    }
    // Match the established native SVG color boundary; this is not HSL-expression rounding.
    internal static byte QuantizeSrgb(double value){if(double.IsNaN(value)||value<=0)return 0;if(value>=255)return 255;return (byte)Math.Round(value);}
}
