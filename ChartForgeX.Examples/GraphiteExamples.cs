using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.VisualBlocks;
using System.Text;

/// <summary>Reproducible examples of the approved look in both output formats and themes.</summary>
internal static class GraphiteExamples {
    internal static void Write(string output, ChartPngOutputScale scale) {
        var gallery = new StringBuilder("<!doctype html><html><meta charset='utf-8'><title>Graphite gallery</title><style>body{margin:24px;background:#f2f3f4;font:14px Calibri,system-ui}section{margin-bottom:24px}h2{font-size:17px} .pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}img{width:100%;height:auto} .dark{background:#0c0d10;color:#f2f3f5;padding:16px}</style><h1>ChartForgeX Graphite</h1>");
        foreach (var dark in new[] { false, true }) {
            var suffix = dark ? "dark" : "light";
            gallery.Append("<section class='").Append(suffix).Append("'><h2>").Append(suffix).Append("</h2>");
            foreach (var item in Create(dark)) {
                var name = "graphite-" + suffix + "-" + item.Key;
                item.Value.WithPngOutputScale(scale);
                item.Value.SaveSvg(Path.Combine(output, name + ".svg"));
                item.Value.SavePng(Path.Combine(output, name + ".png"));
                item.Value.SaveHtml(Path.Combine(output, name + ".html"));
                if (item.Key == "line") item.Value.SaveInteractiveHtml(Path.Combine(output,name+"-interactive.html"));
                var nativeWidth = item.Value.Options.Size.Width;
                gallery.Append("<h2>").Append(item.Key).Append("</h2><div class='pair'><img alt='SVG' style='max-width:").Append(nativeWidth).Append("px' src='").Append(name).Append(".svg'><img alt='PNG' style='max-width:").Append(nativeWidth).Append("px' src='").Append(name).Append(".png'></div>");
            }
            var theme=dark?ChartTheme.GraphiteDark():ChartTheme.GraphiteLight();
            var metric=MetricCard.Create().WithSize(300,200).WithTheme(theme).WithMetric("Coverage","98.4%").WithCaption("Assets reporting")
                .WithMiniSparkline(new[]{92d,94,95,97,98.4},color:theme.Palette[0]);
            var table=ChartTable.Create().WithSize(300,200).WithTheme(theme).WithTitle("Controls").AddColumn("Control").AddColumn("Actual")
                .AddRow("TLS","92%").AddRow("DNSSEC","74%");
            var dashboard=VisualGrid.Create().WithTheme(theme).WithTitle("Control overview").WithColumns(2).WithPanelSize(300,220)
                .WithPngOutputScale((int)scale).Add(metric).Add(table).Add(Create(dark)["bar"].WithSize(300,220)).Add(Create(dark)["line"].WithSize(300,220));
            var dashboardName="graphite-"+suffix+"-dashboard";
            dashboard.SaveSvg(Path.Combine(output,dashboardName+".svg")); dashboard.SavePng(Path.Combine(output,dashboardName+".png")); dashboard.SaveHtml(Path.Combine(output,dashboardName+".html"));
            gallery.Append("<h2>Dashboard and KPI</h2><div class='pair'><img alt='SVG dashboard' src='").Append(dashboardName).Append(".svg'><img alt='PNG dashboard' src='").Append(dashboardName).Append(".png'></div>");
            gallery.Append("</section>");
        }
        gallery.Append("</html>");
        File.WriteAllText(Path.Combine(output,"graphite-gallery.html"),gallery.ToString());
        Console.WriteLine("Graphite gallery: " + output);
    }

    internal static Dictionary<string, Chart> Create(bool dark) {
        Chart Frame(string title,string subtitle,int width=596,int height=338) => Chart.Create().WithSize(width,height).WithTheme(dark?ChartTheme.GraphiteDark():ChartTheme.GraphiteLight()).WithTitle(title).WithSubtitle(subtitle);
        var bar=Frame("Findings by severity","Current and previous run").WithXLabels("Critical","High","Medium","Low","Info").WithDataLabels()
            .AddBar("Current",Points(6,30,82,124,208)).AddBar("Previous",Points(9,39,95,116,186));
        bar.Options.YAxis.Maximum=250; bar.Options.YAxis.TickCount=6;
        var line=Frame("Check results over time","Healthy checks recede; failures remain visible").WithXLabels("Mon","Tue","Wed","Thu","Fri","Sat","Sun")
            .AddLine("Passed",Points(820,940,980,1041,1120,1180,1230)).AddLine("Warnings",Points(120,138,131,112,98,86,72)).AddLine("Failed",Points(22,30,27,31,18,14,11))
            .WithSeriesState("Passed",ChartSeriesState.Quiet).WithSeriesState("Warnings",ChartSeriesState.Warning).WithSeriesState("Failed",ChartSeriesState.Danger);
        line.Options.YAxis.Maximum=1400; line.Options.YAxis.TickCount=8;
        var donut=Frame("Findings by control","Largest slices first",596,314).WithXLabels("Mail authentication","TLS","DNSSEC","Policy","Monitoring","Other")
            .AddDonut("Findings",Points(356,268,214,188,142,116));
        var gauge=Frame("Control readiness","Target 90",396,294).AddGauge("Readiness",87).WithGauge(o=> {
            o.Target=90; o.Caption="3 below target";
            o.Bands.Add(new(0,60,ChartSeriesState.Danger)); o.Bands.Add(new(60,80,ChartSeriesState.Warning)); o.Bands.Add(new(80,100,ChartSeriesState.Quiet));
        });
        var needle=Frame("Readiness needle","Explicit target and bands",396,294).AddGauge("Readiness",74).WithGauge(o=> { o.Form=ChartGaugeForm.Needle; o.Target=90; o.Bands.Add(new(0,60,ChartSeriesState.Danger)); o.Bands.Add(new(60,80,ChartSeriesState.Warning)); o.Bands.Add(new(80,100,ChartSeriesState.Quiet)); });
        var linear=Frame("Linear readiness","Explicit target and bands",596,230).AddLinearGauge("Readiness",87).WithGauge(o=> { o.Target=90; o.Bands.Add(new(0,60,ChartSeriesState.Danger)); o.Bands.Add(new(60,80,ChartSeriesState.Warning)); o.Bands.Add(new(80,100,ChartSeriesState.Quiet)); });
        var bullet=Frame("Control coverage","Actual values and targets",556,294).WithValueFormatter(value=>value.ToString("0",System.Globalization.CultureInfo.InvariantCulture)+" %").AddBullet("DMARC",88,95).AddBullet("DNSSEC",74,90).AddBullet("MTA-STS",63,85).AddBullet("TLS",92,80);
        var funnel=Frame("Remediation stages","Percentage of the first stage",556,324).WithXLabels("Detected","Triaged","Assigned","Fixed","Verified").AddFunnel("Findings",Points(1284,1012,744,521,466));
        var sankey=Frame("Remediation flow","Source-coloured links",556,324).AddSankey("Flow", new[] { new ChartNode("Assessment", "Assessment"), new ChartNode("Remediated", "Remediated"), new ChartNode("In progress", "In progress"), new ChartNode("Overdue", "Overdue"), new ChartNode("Monitoring", "Monitoring"), new ChartNode("GPO", "GPO") }, new[] {
            new ChartFlowLink("flow-1", "Assessment","Remediated",360),new("flow-2", "Assessment","In progress",110),new("flow-3", "Assessment","Overdue",50),new("flow-4", "Monitoring","Remediated",170),new("flow-5", "Monitoring","In progress",90),new("flow-6", "Monitoring","Overdue",50),new("flow-7", "GPO","Remediated",80),new("flow-8", "GPO","In progress",50),new("flow-9", "GPO","Overdue",40)
        });
        sankey.Series[0].WithNodeState("Remediated",ChartSeriesState.Neutral).WithNodeState("In progress",ChartSeriesState.Warning).WithNodeState("Overdue",ChartSeriesState.Danger);
        var heat=Frame("Activity by hour","Sequential ramp; zero is neutral",616,304).WithXLabels(Enumerable.Range(0,24).Select(i=>new[]{0,6,12,18,23}.Contains(i)?i.ToString("00"):"").ToArray());
        var days=new[]{"Mon","Tue","Wed","Thu","Fri","Sat","Sun"};
        for(var d=0;d<7;d++)heat.AddHeatmapRow(days[d],Enumerable.Range(0,24).Select(h=>new ChartPoint(h+1,HeatLevel(d,h))));
        heat.Options.HeatmapRelativeScale=true;
        var results=new Dictionary<string,Chart> { ["bar"]=bar,["line"]=line,["donut"]=donut,["gauge"]=gauge,["needle"]=needle,["linear"]=linear,["bullet"]=bullet,["sankey"]=sankey,["funnel"]=funnel,["heat"]=heat };
        results["stacked"]=Frame("Stacked composition","Surface separators and square baselines").WithStackedBars().WithXLabels("A","B","C").AddBar("First",Points(20,30,-15)).AddBar("Second",Points(12,14,-8));
        results["scatter"]=Frame("Scatter","6 px markers and surface outlines").AddScatter("Observations",Points(20,40,28,60,37));
        results["area"]=Frame("Single area","Flat 12 percent fill").AddArea("Volume",Points(20,40,28,60,37));
        results["narrow-donut"]=Frame("Findings","List beneath the donut",360,510).WithXLabels("Mail","TLS","DNSSEC","Policy","Monitoring","Other").AddDonut("Findings",Points(356,268,214,188,142,116));
        results["horizontal"]=Frame("Horizontal counts","Square baselines and value-end corners").WithXLabels("One","Two","Three").WithDataLabels().AddHorizontalBar("Counts",Points(50,30,20));
        results["treemap"]=Frame("Storage distribution","Flat tiles with readable ink").WithDataLabels().AddTreemap("Files",new[]{new ChartTreemapItem("One",50),new("Two",30),new("Three",20)});
        results["tree"]=Frame("Hierarchy","Shared flat surfaces and labels").WithDataLabels().AddTree("Structure", new[] { new ChartNode("Root", "Root"), new ChartNode("One", "One"), new ChartNode("Two", "Two"), new ChartNode("Three", "Three") }, new[]{new ChartTreeLink("Root","One"),new("Root","Two"),new("One","Three")});
        results["timeline"]=Frame("Delivery timeline","Flat intervals").AddTimelineRange("Plan",1,3).AddTimelineRange("Build",3,7).AddTimelineRange("Review",6,9);
        results["gantt"]=Frame("Delivery schedule","Flat tasks and progress").AddGanttTask("Plan",1,3,1).AddGanttTask("Build",3,7,.65,0).AddGanttTask("Review",6,9,.2,1);
        results["bubble"]=Frame("Bubble observations","70 percent fill with surface outline").AddBubble("Samples",new[]{new ChartBubble(1,20,12),new(2,50,30),new(3,38,20)});
        results["multi-area"]=Frame("Multiple area series","Lines without overlapping fills").AddArea("One",Points(20,30,40)).AddArea("Two",Points(15,25,35));
        results["waterfall"]=Frame("Changes","Flat increments and total").WithXLabels("Start","Added","Removed","Final").WithDataLabels().AddWaterfall("Changes",Points(60,20,-10,70));
        results["host-frame"]=Frame("Host-framed counts","No chart surface or outer padding").WithHostFrame().WithXLabels("One","Two","Three").WithDataLabels().AddBar("Counts",Points(50,30,20));
        results["inside-bar"]=Frame("Inside bar labels","Contrasting ink for categorical and state fills").WithXLabels("One","Two","Three")
            .WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside).AddBar("Observed",Points(50,30,20)).AddBar("Alert",Points(40,24,16)).WithSeriesState("Alert",ChartSeriesState.Danger);
        return results;
    }

    private static double HeatLevel(int day,int hour) {
        var level=(hour>=8&&hour<=17&&day<5?3:1)+((hour*7+day*13)%5==0?2:0)+(day==2&&hour==3?4:0)-((hour*3+day)%4==0?1:0);
        return day>=5&&hour%3==0?0:Math.Max(0,Math.Min(5,level));
    }

    private static ChartPoint[] Points(params double[] values) => values.Select((v,i)=>new ChartPoint(i+1,v)).ToArray();
}
