using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;

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
            if (!ChartColor.TryParse((string?)frame.Attribute("fill")??"",out var fill) ||
                !ChartColor.TryParse((string?)frame.Attribute("stroke")??"",out var line) || fill.A!=255 || line.A!=255) return null;
            return new PngFrameAllowance(x*scale,y*scale,width*scale,height*scale,Read("rx")*scale,stroke*scale,fill,line);
        } catch (System.Xml.XmlException) {
            return null;
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
            // Antialiasing mixes the stroke with its surface; another colour within the border's geometry is still ink.
            bool Between(int value,int a,int b) => value>=Math.Min(a,b)-2 && value<=Math.Max(a,b)+2;
            return Between((rgba>>24)&255,_fill.R,_line.R) && Between((rgba>>16)&255,_fill.G,_line.G) && Between((rgba>>8)&255,_fill.B,_line.B);
        }
    }
}
