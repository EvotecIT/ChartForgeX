using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;

public static partial class GalleryWriter {
    // Native block corners include the declared canvas behind an antialiased rounded
    // card. Use the existing mark painter's exact coverage and compositing, rather than
    // widening the edge-ink tolerance or treating every corner color as background.
    private static Dictionary<(int X,int Y),int> ReadNativeFrameCornerPixels(XElement root, XElement frame, AssetDimensions svg, int scale) {
        var result=new Dictionary<(int X,int Y),int>();
        if (svg.LogicalWidth!=Math.Floor(svg.LogicalWidth) || svg.LogicalHeight!=Math.Floor(svg.LogicalHeight)) return result;
        var width=(int)svg.LogicalWidth; var height=(int)svg.LogicalHeight;
        var radius=Math.Max(0,Math.Min(SurfaceNumber(frame,"rx"),Math.Min(width,height)/2d));
        var side=(int)Math.Ceiling(radius+SurfaceNumber(frame,"stroke-width",1)/2+2);
        var patchWidth=Math.Min(width,side); var patchHeight=Math.Min(height,side);
        if (patchWidth<=0 || patchHeight<=0) return result;
        var surfaces=root.Elements().Where(element=>element==frame || (string?)element.Attribute("data-cfx-role")=="visual-background")
            .Where(element=>element.Attribute("transform")==null).ToArray();
        if (surfaces.Length!=2) return result;
        foreach (var left in new[] {0,width-patchWidth}.Distinct()) foreach (var top in new[] {0,height-patchHeight}.Distinct()) {
            var translated=new XElement(root.Name.Namespace+"g",new XAttribute("transform","translate("+(-left).ToString(CultureInfo.InvariantCulture)+" "+(-top).ToString(CultureInfo.InvariantCulture)+")"),
                surfaces.Select(element=>new XElement(element)));
            var surfaceRoot=new XElement(root.Name,root.Attributes(),root.Elements().Where(element=>element.Name.LocalName is "defs" or "style")
                .Select(element=>new XElement(element)),translated);
            var canvas=new RgbaCanvas(patchWidth,patchHeight,2,null,scale,useDefaultOutlineFont:false);
            canvas.Clear(ChartColor.Transparent);
            SvgRasterRenderer.PaintChartMarks(canvas,SvgRasterParser.FromDocumentRoot(surfaceRoot));
            var pixels=canvas.ToOutputPixels();
            for (var y=0;y<canvas.OutputHeight;y++) for (var x=0;x<canvas.OutputWidth;x++) {
                var offset=(y*canvas.OutputWidth+x)*4;
                result[(left*scale+x,top*scale+y)]=PngColorKey(pixels[offset],pixels[offset+1],pixels[offset+2],pixels[offset+3]);
            }
        }
        return result;
    }
}
