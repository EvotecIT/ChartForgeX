using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeLabelAuditTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeEdgeCaptionCoversItsOwnRouteButDetectsForeignRoutes(bool foreignRoute) {
        var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='100'>"
            + "<g data-edge-id='own'><path data-cfx-role='topology-edge-line' d='M 20 50 L 180 50' fill='none' stroke='black' stroke-width='2'/></g>"
            + (foreignRoute ? "<g data-edge-id='foreign'><path data-cfx-role='topology-edge-line' d='M 100 20 L 100 80' fill='none' stroke='black' stroke-width='2'/></g>" : "")
            + "<g data-edge-id='own' data-cfx-role='topology-edge-label'><rect x='60' y='40' width='80' height='22' fill='white'/>"
            + "<g data-cfx-role='topology-edge-label-text'><text x='70' y='54' font-family='Arial' font-size='11'>Caption</text></g></g></svg>";
        var report = ChartLabelScene.Inspect(svg, FontSpec.SystemSans());
        Assert.Equal(1, report.Visible);
        Assert.Equal(0, report.LabelLabel);
        Assert.Equal(foreignRoute ? 1 : 0, report.LabelMark);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeNodeCaptionsCountContainmentAndKeepRealTextOverlaps(bool overlap) {
        var font = FontSpec.SystemSans();
        var builder = new VisualSceneBuilder(new VisualSize(200, 100), font);
        using (builder.PushGroup("node", "topology-node", new Dictionary<string, string> { ["data-node-id"] = "node" })) {
            builder.Rect(new ChartRect(10, 10, 180, 80), ChartColor.White, role: "topology-node-surface");
            builder.Text("Title", 20, 30, 11, ChartColor.Black, 600, "topology-node-label");
            var secondBaseline = 30 - builder.TextAscent(11, 600) + builder.MeasureText("Title", 11, 600).Height
                + builder.TextAscent(9.35) - (overlap ? 1 : 0);
            builder.Text("Subtitle", 20, secondBaseline, 9.35, ChartColor.Black, role: "topology-node-subtitle");
        }
        var report = ChartLabelScene.Inspect(VisualSceneSvgRenderer.Render(builder.Build()), font);
        Assert.Equal(2, report.Visible);
        Assert.Equal(overlap ? 1 : 0, report.LabelLabel);
        Assert.Equal(0, report.LabelMark);
        Assert.Equal(2, report.Contained);
    }
}
