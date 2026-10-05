using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteGalleryTests {
    [Fact]
    public void GalleryAllowsDeclaredFlatFrameButStillDetectsEdgeMarks() {
        var output=Path.Combine(Path.GetTempPath(),"ChartForgeX-graphite-gallery-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var chart=Chart.Create().WithSize(320,220).WithTitle("Graphite frame").AddBar("Counts",new[]{new ChartPoint(1,20),new ChartPoint(2,50)});
            foreach(var name in new[]{"normal","edge-mark"}) {
                File.WriteAllText(Path.Combine(output,name+".svg"),chart.ToSvg());
                File.WriteAllText(Path.Combine(output,name+".html"),chart.ToHtmlPage());
                var canvas=new PngChartRenderer().RenderCanvas(chart);
                if(name=="edge-mark") canvas.FillRect(0,50,3,20,ChartColor.Black);
                File.WriteAllBytes(Path.Combine(output,name+".png"),PngWriter.WriteRgba(canvas.ToImage()));
            }
            GalleryWriter.Write(output);
            using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"svg-png-comparison.json")));
            var charts=manifest.RootElement.GetProperty("charts").EnumerateArray().ToDictionary(e=>e.GetProperty("name").GetString()!,e=>e.Clone());
            Assert.True(charts["normal"].GetProperty("png").GetProperty("healthy").GetBoolean());
            Assert.Equal(0,charts["normal"].GetProperty("png").GetProperty("edgeInkPixels").GetInt64());
            Assert.True(charts["normal"].GetProperty("html").GetProperty("hasFlatSurface").GetBoolean());
            Assert.False(charts["edge-mark"].GetProperty("png").GetProperty("healthy").GetBoolean());
            Assert.True(charts["edge-mark"].GetProperty("png").GetProperty("edgeInkPixels").GetInt64()>0);
        } finally {
            Directory.Delete(output,recursive:true);
        }
    }
}
