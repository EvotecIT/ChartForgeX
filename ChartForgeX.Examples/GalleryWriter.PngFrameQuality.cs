using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;

public static partial class GalleryWriter {
    // A shared frame intentionally reaches the canvas edge. Only its declared stroke geometry and colours are
    // allowed there; arbitrary marks, text, or extra strokes still count as edge ink.
    private static PngFrameAllowance? ReadPngFrameAllowance(string pngPath, AssetDimensions png) {
        var svgPath=Path.ChangeExtension(pngPath,".svg");
        if (!File.Exists(svgPath)) return null;
        try {
            var root=XDocument.Load(svgPath).Root;
            if (root == null) return null;
            if (root.Attribute("transform")!=null) return null;
            // Embedded panels have their own frames; only the root's untransformed surfaces can
            // explain pixels at this artifact's perimeter.
            var frames=root.Elements().Where(e=>(string?)e.Attribute("data-cfx-role")=="frame-card" && e.Attribute("transform")==null).ToArray();
            var svg=ReadSvgDimensions(svgPath);
            if (!TryPngSurfaceScale(svg,png,out var scale)) return null;
            PngFrameAllowance? allowance=null;
            if (frames.Length==1) {
                var frame=frames[0];
                var x=SurfaceNumber(frame,"x"); var y=SurfaceNumber(frame,"y");
                var width=SurfaceNumber(frame,"width"); var height=SurfaceNumber(frame,"height");
                var stroke=SurfaceNumber(frame,"stroke-width",1);
                if (stroke>0 && stroke<=2 && Math.Abs(x-stroke/2)<=.01 && Math.Abs(y-stroke/2)<=.01 &&
                    Math.Abs(width+stroke-svg.LogicalWidth)<=.01 && Math.Abs(height+stroke-svg.LogicalHeight)<=.01 &&
                    SvgRasterColor.TryParse((string?)frame.Attribute("fill")??"",out var fill) && fill.A==255 &&
                    SvgRasterColor.TryParse((string?)frame.Attribute("stroke")??"",out var line))
                    allowance=new PngFrameAllowance(x*scale,y*scale,width*scale,height*scale,SurfaceNumber(frame,"rx")*scale,stroke*scale,fill,line);
            }
            var backgrounds=new List<PngSurface>(); var shadows=new List<PngSurface>();
            foreach (var element in root.Elements().Where(e=>e.Name.LocalName=="rect" && e.Attribute("transform")==null)) {
                var role=(string?)element.Attribute("data-cfx-role");
                if (role is not ("background" or "frame-card-shadow") ||
                    !SvgRasterColor.TryParse((string?)element.Attribute("fill")??"",out var color)) continue;
                var x=SurfaceNumber(element,"x"); var y=SurfaceNumber(element,"y");
                var width=SurfaceNumber(element,"width"); var height=SurfaceNumber(element,"height");
                if (x<0 || y<0 || width<=0 || height<=0 || x+width>svg.LogicalWidth+.001 || y+height>svg.LogicalHeight+.001) continue;
                if (role=="background") {
                    if (color.A!=255 || x!=0 || y!=0 || Math.Abs(width-svg.LogicalWidth)>.001 ||
                        Math.Abs(height-svg.LogicalHeight)>.001 || SurfaceNumber(element,"rx")!=0) continue;
                    backgrounds.Add(new PngSurface(x*scale,y*scale,width*scale,height*scale,0,color));
                } else if (color.A>0 && color.A<255) {
                    shadows.Add(new PngSurface(x*scale,y*scale,width*scale,height*scale,SurfaceNumber(element,"rx")*scale,color));
                }
            }
            if (allowance==null && backgrounds.Count==0 && shadows.Count==0) return null;
            allowance??=new PngFrameAllowance(0,0,0,0,0,0,default,default);
            allowance.Backgrounds=backgrounds; allowance.Shadows=shadows;
            return allowance;
        } catch (System.Xml.XmlException) {
            return null;
        }
    }

    private static double SurfaceNumber(XElement element,string name,double fallback=0) =>
        double.TryParse((string?)element.Attribute(name),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)?value:fallback;

    private static bool TryPngSurfaceScale(AssetDimensions svg,AssetDimensions png,out double scale) {
        scale=0;
        if (svg.LogicalWidth<=0 || svg.LogicalHeight<=0) return false;
        // Native exports ceil each physical extent; a fractional logical height must not be
        // rounded before determining the integer output scale or the final coverage row.
        scale=Math.Round(Math.Min(png.Width/svg.LogicalWidth,png.Height/svg.LogicalHeight));
        return scale>=1 && Math.Ceiling(svg.LogicalWidth*scale)==png.Width &&
            Math.Ceiling(svg.LogicalHeight*scale)==png.Height;
    }

    private static bool HasDeclaredTransparentPngPerimeter(string pngPath, AssetDimensions png) {
        var svgPath=Path.ChangeExtension(pngPath,".svg");
        if (!File.Exists(svgPath)) return false;
        try {
            var root=XDocument.Load(svgPath).Root;
            if (root == null) return false;
            foreach (var background in root.Descendants().Where(e=>(string?)e.Attribute("data-cfx-role")=="background")) {
                if (!SvgRasterColor.TryParse((string?)background.Attribute("fill")??"",out var color) || color.A!=0) return false;
            }
            var frames=root.Elements().Where(e=>(string?)e.Attribute("data-cfx-role")=="frame-card").ToArray();
            if (frames.Length!=1 || frames[0].Parent!=root || frames[0].Attribute("transform")!=null || root.Attribute("transform")!=null) return false;
            var svg=ReadSvgDimensions(svgPath);
            if (!TryPngSurfaceScale(svg,png,out var scale)) return false;
            var frame=frames[0];
            double Read(string name) => double.TryParse((string?)frame.Attribute(name),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)?value:double.NaN;
            var x=Read("x"); var y=Read("y"); var width=Read("width"); var height=Read("height"); var stroke=Read("stroke-width");
            var band=PngEdgeBandSize(png)/scale;
            // A detached native card leaves the declared canvas perimeter transparent. An opaque
            // edge mark must not become the inferred background merely because it is the only ink.
            return stroke>=0 && stroke<=2 && width>0 && height>0 && x-stroke/2>=band && y-stroke/2>=band &&
                x+width+stroke/2<=svg.LogicalWidth-band && y+height+stroke/2<=svg.LogicalHeight-band;
        } catch (System.Xml.XmlException) {
            return false;
        }
    }

    private sealed class PngFrameAllowance {
        internal List<PngSurface> Backgrounds { get; set; }=new();
        internal List<PngSurface> Shadows { get; set; }=new();
        private readonly double _x,_y,_width,_height,_radius,_stroke;
        private readonly ChartColor _fill,_line;
        internal PngFrameAllowance(double x,double y,double width,double height,double radius,double stroke,ChartColor fill,ChartColor line) {
            _x=x; _y=y; _width=width; _height=height; _radius=Math.Max(0,Math.Min(radius,Math.Min(width,height)/2)); _stroke=stroke;
            _fill=fill; _line=line;
        }
        internal int Fill => PngColorKey(_fill.R,_fill.G,_fill.B,_fill.A);
        internal int NormalizeEdgeSample(int x,int y,int rgba) {
            if ((rgba&255)==0) return rgba;
            if (_stroke>0 && Includes(x,y,rgba)) return Fill;
            foreach (var background in Backgrounds) {
                if (background.Contains(x,y) && background.MatchesRgb(rgba))
                    return PngColorKey(background.Color.R,background.Color.G,background.Color.B,255);
            }
            var remainingAlpha=1d;
            foreach (var shadow in Shadows) {
                if (shadow.Contains(x,y) && shadow.MatchesRgb(rgba)) remainingAlpha*=1-shadow.Color.A/255d;
            }
            // Only coverage produced by declared translucent layers is allowed. Opaque extra
            // marks, even in exactly the shadow's RGB, remain observable edge ink.
            if ((rgba&255)<=Math.Ceiling((1-remainingAlpha)*255)+2 && remainingAlpha<1) return 0;
            return rgba;
        }
        internal bool Includes(int x,int y,int rgba) {
            var dx=Math.Abs(x+.5-(_x+_width/2))-(_width/2-_radius);
            var dy=Math.Abs(y+.5-(_y+_height/2))-(_height/2-_radius);
            var distance=Math.Sqrt(Math.Pow(Math.Max(dx,0),2)+Math.Pow(Math.Max(dy,0),2))+Math.Min(Math.Max(dx,dy),0)-_radius;
            if (Math.Abs(distance)>_stroke/2+1.25) return false;
            // In straight-alpha output the stroke's contribution cannot exceed its authored alpha
            // divided by the resulting pixel alpha. This covers partially filled antialiased edges
            // while rejecting opaque marks that use the same RGB as a translucent border.
            var alpha=rgba&255;
            if (alpha==0) return true;
            var dr=_line.R-_fill.R; var dg=_line.G-_fill.G; var db=_line.B-_fill.B;
            var r=(rgba>>24)&255; var g=(rgba>>16)&255; var b=(rgba>>8)&255;
            var denominator=dr*dr+dg*dg+db*db;
            if (denominator==0) return Math.Abs(r-_fill.R)<=2 && Math.Abs(g-_fill.G)<=2 && Math.Abs(b-_fill.B)<=2;
            var maximum=Math.Min(1,_line.A/(double)alpha);
            // Find the closest colour on the permitted compositing segment before checking channel
            // rounding. Antialiased endpoint RGB can project just beyond the segment after byte
            // rounding; that is still the authored border, rather than additional edge ink.
            var fraction=Math.Max(0,Math.Min(maximum,((r-_fill.R)*dr+(g-_fill.G)*dg+(b-_fill.B)*db)/(double)denominator));
            return Math.Abs(r-_fill.R-fraction*dr)<=2 &&
                Math.Abs(g-_fill.G-fraction*dg)<=2 && Math.Abs(b-_fill.B-fraction*db)<=2;
        }
    }

    private sealed class PngSurface {
        private readonly double _x,_y,_width,_height,_radius;
        internal PngSurface(double x,double y,double width,double height,double radius,ChartColor color) {
            _x=x; _y=y; _width=width; _height=height;
            _radius=Math.Max(0,Math.Min(radius,Math.Min(width,height)/2)); Color=color;
        }
        internal ChartColor Color { get; }
        internal bool MatchesRgb(int rgba) => Math.Abs(((rgba>>24)&255)-Color.R)<=2 &&
            Math.Abs(((rgba>>16)&255)-Color.G)<=2 && Math.Abs(((rgba>>8)&255)-Color.B)<=2;
        internal bool Contains(int x,int y) {
            var dx=Math.Abs(x+.5-(_x+_width/2))-(_width/2-_radius);
            var dy=Math.Abs(y+.5-(_y+_height/2))-(_height/2-_radius);
            var distance=Math.Sqrt(Math.Pow(Math.Max(dx,0),2)+Math.Pow(Math.Max(dy,0),2))+Math.Min(Math.Max(dx,dy),0)-_radius;
            return distance<=1.25;
        }
    }
}
