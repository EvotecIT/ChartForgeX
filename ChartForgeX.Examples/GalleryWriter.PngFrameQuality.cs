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
            var frames=root.Descendants().Where(e=>(string?)e.Attribute("data-cfx-role")=="frame-card").ToArray();
            if (frames.Length!=1) return null;
            var frame=frames[0];
            var svg=ReadSvgDimensions(svgPath);
            if (svg.Width<=0 || svg.Height<=0) return null;
            var scale=(double)png.Width/svg.Width;
            if (Math.Abs(scale-(double)png.Height/svg.Height)>.001) return null;
            double Read(string name,double fallback=0) => double.TryParse((string?)frame.Attribute(name),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)?value:fallback;
            var x=Read("x"); var y=Read("y"); var width=Read("width"); var height=Read("height");
            var stroke=Read("stroke-width",1);
            if (stroke<=0 || stroke>2 || Math.Abs(x-stroke/2)>.01 || Math.Abs(y-stroke/2)>.01 ||
                Math.Abs(width+stroke-svg.Width)>.01 || Math.Abs(height+stroke-svg.Height)>.01) return null;
            if (!SvgRasterColor.TryParse((string?)frame.Attribute("fill")??"",out var fill) ||
                !SvgRasterColor.TryParse((string?)frame.Attribute("stroke")??"",out var line) || fill.A!=255) return null;
            return new PngFrameAllowance(x*scale,y*scale,width*scale,height*scale,Read("rx")*scale,stroke*scale,fill,line);
        } catch (System.Xml.XmlException) {
            return null;
        }
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
            var frames=root.Descendants().Where(e=>(string?)e.Attribute("data-cfx-role")=="frame-card").ToArray();
            if (frames.Length!=1 || frames[0].Parent!=root || frames[0].Attribute("transform")!=null || root.Attribute("transform")!=null) return false;
            var svg=ReadSvgDimensions(svgPath);
            if (svg.Width<=0 || svg.Height<=0) return false;
            var scale=(double)png.Width/svg.Width;
            if (Math.Abs(scale-(double)png.Height/svg.Height)>.001) return false;
            var frame=frames[0];
            double Read(string name) => double.TryParse((string?)frame.Attribute(name),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)?value:double.NaN;
            var x=Read("x"); var y=Read("y"); var width=Read("width"); var height=Read("height"); var stroke=Read("stroke-width");
            var band=PngEdgeBandSize(png)/scale;
            // A detached native card leaves the declared canvas perimeter transparent. An opaque
            // edge mark must not become the inferred background merely because it is the only ink.
            return stroke>=0 && stroke<=2 && width>0 && height>0 && x-stroke/2>=band && y-stroke/2>=band &&
                x+width+stroke/2<=svg.Width-band && y+height+stroke/2<=svg.Height-band;
        } catch (System.Xml.XmlException) {
            return false;
        }
    }

    private sealed class PngFrameAllowance {
        private readonly double _x,_y,_width,_height,_radius,_stroke;
        private readonly ChartColor _fill,_line;
        internal PngFrameAllowance(double x,double y,double width,double height,double radius,double stroke,ChartColor fill,ChartColor line) {
            _x=x; _y=y; _width=width; _height=height; _radius=Math.Max(0,Math.Min(radius,Math.Min(width,height)/2)); _stroke=stroke;
            _fill=fill; _line=line;
        }
        internal int Fill => PngColorKey(_fill.R,_fill.G,_fill.B,_fill.A);
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
}
