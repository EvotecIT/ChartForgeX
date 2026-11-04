using ChartForgeX.Interactivity.Html;

internal static class BrowserDataExamples {
    internal static void Write(string output) {
        var script = HtmlDataChartAssets.Script.Replace("</", "<\\/");
        var page = """
            <!doctype html><html lang="en"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <title>Orders by region</title><style>
            body{font:16px system-ui;margin:32px auto;padding:0 20px;max-width:960px;color:#334155;background:#f8fafc}
            main{background:white;border:1px solid #e2e8f0;border-radius:12px;padding:20px}
            nav{display:flex;gap:12px;align-items:center;flex-wrap:wrap}select,button{font:inherit;padding:6px 10px}
            #chart{width:100%;margin-top:16px}[role=button]:focus-visible{outline:2px solid currentColor;outline-offset:2px}
            </style></head><body><h1>Orders by region</h1><main><nav><label>Chart type <select id="kind">
            <option>line</option><option>area</option><option>bar</option><option selected>column</option>
            <option>stacked</option><option>win-loss</option><option>bullet</option></select></label>
            <button id="update">Show next period</button></nav><div id="chart"></div>
            <p id="selected" role="status">Select a mark to inspect its raw value.</p></main><script>
            __RENDERER__
            const data={type:'column',label:'Orders by region',categories:[{id:'north',label:'North'},
              {id:'south',label:'South'},{id:'west',label:'West'}],series:[
              {id:'open',name:'Open',values:[12,8,-2]},{id:'closed',name:'Closed',values:[6,10,4]}]};
            const chart=ChartForgeXRenderer.create(document.getElementById('chart'),data,{onSelect:mark=>{
              document.getElementById('selected').textContent=mark.categoryId+' · '+mark.seriesId+' · '+mark.value;
            }});
            document.getElementById('kind').onchange=event=>{data.type=event.target.value;chart.update(data);};
            document.getElementById('update').onclick=()=>{data.series[0].values=data.series[0].values.map(value=>value+1);chart.update(data);};
            </script></body></html>
            """;
        File.WriteAllText(Path.Combine(output, "browser-data-charts.html"), page.Replace("__RENDERER__", script));
    }
}
