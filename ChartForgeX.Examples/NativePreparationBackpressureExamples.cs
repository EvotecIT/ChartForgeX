using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Exercises opt-in native callback admission with a finite catalog of genuinely prepared sections.</summary>
internal static class NativePreparationBackpressureExamples {
    internal static void Write(string output) {
        Directory.CreateDirectory(output); ExampleTypography.Register();
        foreach (var typed in new[] { false, true }) foreach (var width in new[] { 390, 900 }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var chart = Model(typed, width, mode); var renderer = new HtmlInteractiveChartRenderer();
            var sections = Enumerable.Range(0, 4).ToDictionary(inset => inset.ToString(System.Globalization.CultureInfo.InvariantCulture),
                inset => renderer.PrepareSection(chart, inset == 0 ? null : new ChartCartesianViewport(x: new ChartAxisWindow(inset, 10 - inset)), options => {
                    options.Interaction.ChartId = "native-admission-example";
                    options.Interaction.Enable(ChartInteractionFeatures.Export);
                    options.Interaction.Disable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
                }));
            var catalog = JsonSerializer.Serialize(sections.ToDictionary(pair => pair.Key, pair => new { html = pair.Value.Html, basis = "view-" + pair.Key }));
            var name = "native-preparation-" + (typed ? "typed-" : "numeric-") + width + "-" + mode.ToString().ToLowerInvariant();
            File.WriteAllText(Path.Combine(output, name + ".svg"), sections["0"].PreparedVisual.ToSvg());
            File.WriteAllBytes(Path.Combine(output, name + ".png"), sections["0"].PreparedVisual.ToPng());
            File.WriteAllText(Path.Combine(output, name + ".sections.json"), catalog);
            File.WriteAllText(Path.Combine(output, name + ".interactive.html"), "<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
                + ExampleTypography.EmbeddedStyle + "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
                + "body{margin:16px;font:14px '" + ExampleTypography.FontFamily + "',sans-serif}.controls{display:flex;gap:8px;flex-wrap:wrap;margin:12px 0}button{font:inherit;padding:8px 12px}output{display:block;margin:12px 0}</style></head><body>"
                + "<p>This transport demo uses four native sections prepared by this program. Release finishes one host callback; an aborted callback deliberately waits for release.</p>"
                + "<div class=\"controls\"><button id=\"request\">Request view</button><button id=\"burst\">Queue 1000 windows</button><button id=\"release\" disabled>Release callback</button></div><output id=\"status\"></output>"
                + sections["0"].Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>const nativeSections=" + catalog + ";" + Script + "</script></body></html>");
        }
    }

    private static Chart Model(bool typed, int width, VisualThemeMode mode) {
        var series = typed ? ChartSeries.CreateRangeBar(new ChartSeriesId("readings"), "Readings", Enumerable.Range(0, 11).Select(value => new ChartInterval(value, 20 + value * 5, 28 + value * 5)))
            : ChartSeries.CreateLine(new ChartSeriesId("readings"), "Readings", Enumerable.Range(0, 11).Select(value => new ChartPoint(value, 20 + value * 5)));
        series.WithObservationIds(Enumerable.Range(0, 11).Select(value => new ChartObservationId("reading-" + value)));
        return Chart.Create().WithSize(width, 480).WithTheme(VisualTheme.Graphite(), mode).WithFontFamily(ExampleTypography.FontFamily)
            .WithTitle(typed ? "Native typed interval admission" : "Native numeric admission").WithLegend(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 100).AddSeries(series);
    }

    private const string Script = """
        (()=>{
          const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')),status=document.querySelector('#status'),release=document.querySelector('#release');
          let finish=null,calls=0,changes=0;const paint=()=>{status.textContent=`Admitted callbacks: ${calls}; accepted scenes: ${changes}`;release.disabled=!finish};
          chart.connectNative(request=>{
            const inset=request.kind==='reset'?0:request.kind==='window'?request.sourceViewport?.x?.minimum:null;
            if(!Number.isInteger(inset)||!Object.hasOwn(nativeSections,String(inset)))throw new TypeError('This finite transport catalog accepts only its authored windows or Reset.');
            calls++;return new Promise(resolve=>{finish=()=>{finish=null;resolve(nativeSections[String(inset)]);paint()};paint()});
          },{basis:'view-0',axes:['x'],preparationBackpressure:'latest-window'});
          chart.root.addEventListener('cfxviewportchange',()=>{changes++;paint()});
          document.querySelector('#request').onclick=()=>chart.setViewport({x:{minimum:1,maximum:9}});
          document.querySelector('#burst').onclick=()=>{for(let index=0;index<1000;index++){const inset=1+index%3;chart.setViewport({x:{minimum:inset,maximum:10-inset}})}};
          release.onclick=()=>finish?.();paint();
        })();
        """;
}
