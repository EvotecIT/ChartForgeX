using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class GraphEdgeStyleBrowserTests {
    private const string Markup = "Tooltips are text: <b>markup</b> & entities stay literal.";

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg)]
    [InlineData(HtmlGraphRenderBackend.Canvas)]
    [InlineData(HtmlGraphRenderBackend.WebGl)]
    public async Task HoveringANodeAndAnEdgeShowsTheirAuthoredTextBesideThePointerInsideTheViewport(HtmlGraphRenderBackend backend) {
        if (!Enabled) return;
        var graph = Scene();
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => options.RenderBackend = backend), 760, 520);
        var page = session.Page;
        var active = await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-renderer-active");
        Assert.Equal(backend.ToString().ToLowerInvariant(), active);

        // The far-right node pushes its text against the viewport edge; markup in the text stays literal.
        var node = await HoverAsync(page, "escape");
        Assert.Equal(Markup, node.GetProperty("text").GetString());
        Assert.Equal(0, node.GetProperty("children").GetInt32());
        AssertBeside(node, 9);

        var edge = await HoverAsync(page, "a", "b");
        Assert.Equal("Bar to diamond.\nScaled by 1.5.", edge.GetProperty("text").GetString());
        AssertBeside(edge, 0);

        await page.Mouse.MoveAsync(2, 2);
        Assert.True(await page.EvaluateAsync<bool>("()=>document.querySelector('.cfx-graph-hover-tip').hidden"));

        if (backend == HtmlGraphRenderBackend.Svg) {
            // Keyboard focus describes the focused item with the same tooltip.
            await page.Locator("[data-cfx-role=graph-node][data-node-id=a]").FocusAsync();
            var focused = await page.EvaluateAsync<JsonElement>("()=>{const tip=document.querySelector('.cfx-graph-hover-tip'),node=document.querySelector('[data-node-id=a]');return{text:tip.textContent,hidden:tip.hidden,described:node.getAttribute('aria-describedby')===tip.id,role:tip.getAttribute('role')}}");
            Assert.Equal("Source\nOwns the relationship.", focused.GetProperty("text").GetString());
            Assert.False(focused.GetProperty("hidden").GetBoolean());
            Assert.True(focused.GetProperty("described").GetBoolean());
            Assert.Equal("tooltip", focused.GetProperty("role").GetString());

            // Hover and selection widths replace the resting width of the relationship.
            var path = page.Locator("[data-cfx-role=graph-edge][data-edge-id=a-b]");
            await HoverAsync(page, "a", "b");
            Assert.Equal("6", await path.EvaluateAsync<string>("path=>path.style.strokeWidth"));
            await page.Mouse.DownAsync(); await page.Mouse.UpAsync();
            Assert.Equal("9", await path.EvaluateAsync<string>("path=>path.style.strokeWidth"));
        } else {
            // Accelerated surfaces read the selected item, including its tooltip, through the selection readout.
            await page.Locator($"[data-cfx-role=graph-{active}]").FocusAsync();
            await page.Keyboard.PressAsync("ArrowRight");
            var readout = await page.EvaluateAsync<string>("()=>document.querySelector('.cfx-graph-tooltip').textContent");
            Assert.EndsWith("\nSource\nOwns the relationship.", readout);
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task LiveSvgArrowsAndValueWidthsMatchTheStaticExport() {
        if (!Enabled) return;
        var graph = Scene();
        var exported = XDocument.Parse(graph.ToGraphSvg());
        XNamespace svg = "http://www.w3.org/2000/svg";
        var staticArrows = exported.Descendants(svg + "g").Where(group => (string?)group.Attribute("data-cfx-role") == "graph-edge-arrows")
            .ToDictionary(group => (string)group.Attribute("data-edge-arrows-for")!, group => group.Elements(svg + "polygon").Select(Polygon).ToArray());
        var staticWidths = exported.Descendants(svg + "path").Where(path => (string?)path.Attribute("data-cfx-role") == "graph-edge")
            .ToDictionary(path => (string)path.Attribute("data-edge-id")!, path => (string?)path.Attribute("data-edge-width"));
        Assert.Equal(new[] { "a-b", "loop", "middle", "route" }, staticArrows.Keys.OrderBy(id => id, StringComparer.Ordinal));
        Assert.Equal(new[] { "1", "2.5", "4" }, new[] { "value-1", "value-2", "value-3" }.Select(id => staticWidths[id]));

        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => options.RenderBackend = HtmlGraphRenderBackend.Svg), 760, 520);
        var live = await session.Page.EvaluateAsync<JsonElement>("""
            ()=>({runtimePainted:[...document.querySelectorAll('[data-cfx-role=graph-edge-arrows]')].every(group=>[...group.querySelectorAll('polygon')].every(polygon=>!/var\(/.test(polygon.getAttribute('style')))),
              arrows:Object.fromEntries([...document.querySelectorAll('[data-cfx-role=graph-edge-arrows]')].map(group=>[group.dataset.edgeArrowsFor,
                [...group.querySelectorAll('polygon')].map(polygon=>polygon.getAttribute('points'))])),
              widths:Object.fromEntries(['value-1','value-2','value-3'].map(id=>[id,document.querySelector(`[data-edge-id=${id}]`).style.strokeWidth]))})
            """);
        Assert.True(live.GetProperty("runtimePainted").GetBoolean(), "The live arrows must come from the browser runtime, not the server markup.");
        foreach (var (id, polygons) in staticArrows) {
            var livePolygons = live.GetProperty("arrows").GetProperty(id).EnumerateArray().Select(item => Polygon(item.GetString()!)).ToArray();
            Assert.Equal(polygons.Length, livePolygons.Length);
            for (var index = 0; index < polygons.Length; index++) {
                Assert.Equal(polygons[index].Length, livePolygons[index].Length);
                for (var point = 0; point < polygons[index].Length; point++) Assert.InRange(Math.Abs(polygons[index][point] - livePolygons[index][point]), 0, .01);
            }
        }
        foreach (var id in new[] { "value-1", "value-2", "value-3" }) Assert.Equal(staticWidths[id], live.GetProperty("widths").GetProperty(id).GetString());
        AssertNoConsoleErrors(session);
    }

    private static GraphScene Scene() {
        var graph = GraphScene.Create("edge-style-proof", "Edge style proof");
        graph.Options.Features = GraphSceneFeatures.Selection;
        graph.Options.EdgeWidthScale.MinimumWidth = 1;
        graph.Options.EdgeWidthScale.MaximumWidth = 4;
        void Node(string id, double x, double y, string? tooltip = null) => graph.AddNode(id, id, node => { node.X = x; node.Y = y; node.Fixed = true; node.Size = 9; node.Tooltip = tooltip; });
        Node("a", 120, 120, "Source\nOwns the relationship."); Node("b", 420, 120);
        Node("c", 120, 220); Node("d", 420, 260);
        Node("loop-node", 620, 140); Node("e", 120, 330); Node("f", 420, 380);
        Node("escape", 900, 460, Markup);
        graph.AddEdge("a-b", "a", "b", configure: edge => {
            edge.SourceArrow = true; edge.TargetArrow = true;
            edge.Style.SourceArrowShape = GraphEdgeArrowShape.Bar; edge.Style.TargetArrowShape = GraphEdgeArrowShape.Diamond; edge.Style.ArrowScale = 1.5;
            edge.Style.Width = 3; edge.Style.HoverWidth = 6; edge.Style.SelectionWidth = 9;
            edge.Tooltip = "Bar to diamond.\nScaled by 1.5.";
        });
        graph.AddEdge("middle", "c", "d", configure: edge => {
            edge.Shape = GraphEdgeShape.Curve; edge.Curvature = -40; edge.MiddleArrow = true; edge.TargetArrow = true;
            edge.Style.MiddleArrowShape = GraphEdgeArrowShape.Circle; edge.Style.TargetArrowShape = GraphEdgeArrowShape.Triangle;
        });
        graph.AddEdge("loop", "loop-node", "loop-node", configure: edge => { edge.Shape = GraphEdgeShape.SelfReference; edge.MiddleArrow = true; edge.Style.MiddleArrowShape = GraphEdgeArrowShape.Vee; });
        graph.AddEdge("route", "e", "f", configure: edge => {
            edge.Shape = GraphEdgeShape.Polyline; edge.MiddleArrow = true; edge.Directed = true;
            foreach (var (x, y) in new[] { (120d, 330d), (260d, 330d), (260d, 380d), (420d, 380d) }) edge.RoutePoints.Add(new GraphScenePoint(x, y));
        });
        for (var index = 1; index <= 3; index++) {
            var value = 4 * index - 3;
            graph.AddNode("value-" + index + "-a", "v" + index, node => { node.X = 560; node.Y = 260 + index * 50; node.Fixed = true; node.Size = 6; });
            graph.AddNode("value-" + index + "-b", "w" + index, node => { node.X = 760; node.Y = 260 + index * 50; node.Fixed = true; node.Size = 6; });
            graph.AddEdge("value-" + index, "value-" + index + "-a", "value-" + index + "-b", configure: edge => edge.Style.Value = value);
        }
        return graph;
    }

    // Moves the pointer onto a node, or onto the midpoint between two nodes, through the explorer's own viewport mapping.
    private static async Task<JsonElement> HoverAsync(IPage page, string first, string? second = null) {
        var point = await page.EvaluateAsync<JsonElement>("""
            ([first,second])=>{const root=document.querySelector('.cfx-graph-explorer'),graph=ChartForgeXGraphExplorer.get(root),view=graph.viewport,
              renderer=root.dataset.cfxGraphRendererActive,surface=root.querySelector(`[data-cfx-role=graph-${renderer==='svg'?'scene':renderer}]`),
              rect=surface.getBoundingClientRect(),box=root.querySelector('[data-cfx-role=graph-scene]').viewBox.baseVal,
              fit=Math.min(rect.width/box.width,rect.height/box.height),
              node=id=>graph.nodes.find(item=>item.id===id),a=node(first),b=second?node(second):a,
              x=(a.x+b.x)/2,y=(a.y+b.y)/2,scale=view.scale*fit;
              return{x:rect.left+(rect.width-box.width*fit)/2+(x*view.scale+view.x)*fit,y:rect.top+(rect.height-box.height*fit)/2+(y*view.scale+view.y)*fit,scale}}
            """, new[] { first, second ?? string.Empty });
        var x = point.GetProperty("x").GetDouble();
        var y = point.GetProperty("y").GetDouble();
        await page.Mouse.MoveAsync((float)x - 40, (float)y - 40);
        await page.Mouse.MoveAsync((float)x, (float)y, new MouseMoveOptions { Steps = 4 });
        await page.WaitForFunctionAsync("()=>!document.querySelector('.cfx-graph-hover-tip')?.hidden");
        return await page.EvaluateAsync<JsonElement>("""
            ([x,y,scale])=>{const tip=document.querySelector('.cfx-graph-hover-tip'),rect=tip.getBoundingClientRect();
              return{text:tip.textContent,children:tip.children.length,x,y,scale,left:rect.left,top:rect.top,right:rect.right,bottom:rect.bottom,width:innerWidth,height:innerHeight}}
            """, new[] { x, y, point.GetProperty("scale").GetDouble() });
    }

    // The tooltip stays inside the viewport and clear of the pointer and, for nodes, of the whole mark.
    private static void AssertBeside(JsonElement tip, double nodeSize) {
        double Value(string name) => tip.GetProperty(name).GetDouble();
        Assert.InRange(Value("left"), 0, Value("width")); Assert.InRange(Value("right"), 0, Value("width"));
        Assert.InRange(Value("top"), 0, Value("height")); Assert.InRange(Value("bottom"), 0, Value("height"));
        var reach = nodeSize * Value("scale") + 1;
        var clearX = Value("left") >= Value("x") + reach || Value("right") <= Value("x") - reach;
        var clearY = Value("top") >= Value("y") + reach || Value("bottom") <= Value("y") - reach;
        Assert.True(clearX || clearY, "The tooltip covers its target: " + tip);
    }

    private static double[] Polygon(XElement polygon) => Polygon((string)polygon.Attribute("points")!);

    private static double[] Polygon(string points) => points.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray();
}
