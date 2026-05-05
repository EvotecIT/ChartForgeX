using System;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void InteractivityContractsAreHostNeutral() {
        var options = ChartInteractionOptions.ReportReview();
        Assert(options.HasFeature(ChartInteractionFeatures.Tooltips), "Report-review interactivity should include tooltips.");
        Assert(options.HasFeature(ChartInteractionFeatures.Selection), "Report-review interactivity should include selection.");
        Assert(options.HasFeature(ChartInteractionFeatures.LegendToggles), "Report-review interactivity should include legend toggles.");
        Assert(options.HasFeature(ChartInteractionFeatures.KeyboardNavigation), "Report-review interactivity should include keyboard navigation.");
        Assert(!options.HasFeature(ChartInteractionFeatures.Zoom), "Report-review interactivity should leave zoom as an opt-in host feature.");

        options.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.SynchronizedCharts);
        options.ChartId = "security-posture";
        options.GroupName = "executive-dashboard";
        Assert(options.HasFeature(ChartInteractionFeatures.Zoom) && options.HasFeature(ChartInteractionFeatures.Pan), "Interactivity contracts should model future zoom and pan adapters.");
        Assert(options.HasFeature(ChartInteractionFeatures.SynchronizedCharts), "Interactivity contracts should model synchronized chart groups.");
        Assert(options.ChartId == "security-posture" && options.GroupName == "executive-dashboard", "Interactivity identifiers should preserve trimmed values.");
        AssertThrows<ArgumentException>(() => options.ChartId = " ", "Interactivity chart IDs should reject empty values.");
        AssertThrows<ArgumentException>(() => options.GroupName = "", "Interactivity group names should reject empty values.");
    }

    private static void InteractiveHtmlWrapsSvgWithoutExternalDependencies() {
        var html = SampleChart().ToInteractiveHtmlPage(options => {
            options.PageTitle = "Interactive security posture";
            options.IdScope = "security-posture";
            options.ScriptNonce = "nonce-1";
            options.Interaction.ChartId = "security-posture";
            options.Interaction.GroupName = "dashboard-a";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts);
        });

        Assert(html.Contains("<title>Interactive security posture</title>", StringComparison.Ordinal), "Interactive HTML should use the configured page title.");
        Assert(html.Contains("data-cfx-chart-id=\"security-posture\"", StringComparison.Ordinal), "Interactive HTML should expose a stable chart ID.");
        Assert(html.Contains("data-cfx-interaction-group=\"dashboard-a\"", StringComparison.Ordinal), "Interactive HTML should expose a synchronized chart group.");
        Assert(html.Contains("data-cfx-interaction-features=\"", StringComparison.Ordinal) && html.Contains("Zoom", StringComparison.Ordinal) && html.Contains("Pan", StringComparison.Ordinal) && html.Contains("Brush", StringComparison.Ordinal) && html.Contains("SynchronizedCharts", StringComparison.Ordinal), "Interactive HTML should describe enabled host-neutral features.");
        Assert(html.Contains("<script nonce=\"nonce-1\">", StringComparison.Ordinal), "Interactive HTML should support CSP nonces.");
        Assert(html.Contains("class=\"cfx-tooltip\"", StringComparison.Ordinal), "Interactive HTML should include an HTML tooltip surface.");
        Assert(html.Contains("cfx-selected", StringComparison.Ordinal), "Interactive HTML should include selectable-region styling and behavior.");
        Assert(html.Contains("cfx-series-muted", StringComparison.Ordinal), "Interactive HTML should include legend-toggle behavior.");
        Assert(html.Contains("const setSeriesMuted = (root, series, muted)", StringComparison.Ordinal), "Interactive HTML should target SVG series metadata for legend toggles.");
        Assert(html.Contains("data-cfx-zoom=\"in\"", StringComparison.Ordinal) && html.Contains("data-cfx-zoom=\"out\"", StringComparison.Ordinal), "Interactive HTML should include zoom controls when zoom is enabled.");
        Assert(html.Contains("data-cfx-mode-button=\"pan\"", StringComparison.Ordinal), "Interactive HTML should include a pan mode control when pan is enabled.");
        Assert(html.Contains("data-cfx-mode-button=\"brush\"", StringComparison.Ordinal), "Interactive HTML should include a brush mode control when brush is enabled.");
        Assert(html.Contains("data-cfx-export=\"svg\"", StringComparison.Ordinal), "Interactive HTML should include an SVG export control when export is enabled.");
        Assert(html.Contains("data-cfx-export=\"png\"", StringComparison.Ordinal), "Interactive HTML should include a PNG export control when export is enabled.");
        Assert(html.Contains("class=\"cfx-brush-box\"", StringComparison.Ordinal), "Interactive HTML should include a brush selection overlay.");
        Assert(html.Contains("stage.addEventListener('wheel'", StringComparison.Ordinal), "Interactive HTML should support wheel zoom.");
        Assert(html.Contains("root.dataset.cfxBrush", StringComparison.Ordinal) && html.Contains("'cfxbrush'", StringComparison.Ordinal), "Interactive HTML should publish brush selection state for host code.");
        Assert(html.Contains("new XMLSerializer().serializeToString(svg)", StringComparison.Ordinal) && html.Contains("'cfxexport'", StringComparison.Ordinal), "Interactive HTML should export inline SVG without external libraries.");
        Assert(html.Contains("canvas.toBlob((blob)", StringComparison.Ordinal) && html.Contains("image/png", StringComparison.Ordinal), "Interactive HTML should export inline SVG as PNG without external libraries.");
        Assert(html.Contains("new CustomEvent(name", StringComparison.Ordinal) && html.Contains("'cfxviewport'", StringComparison.Ordinal) && html.Contains("'cfxselect'", StringComparison.Ordinal) && html.Contains("'cfxseries'", StringComparison.Ordinal) && html.Contains("'cfxreset'", StringComparison.Ordinal), "Interactive HTML should publish host events for viewport, selection, series toggles, and reset.");
        Assert(html.Contains("new CustomEvent('cfxsync'", StringComparison.Ordinal) && html.Contains("applySync(peer, detail)", StringComparison.Ordinal), "Interactive HTML should synchronize grouped chart state without external libraries.");
        Assert(html.Contains("action: 'viewport'", StringComparison.Ordinal) && html.Contains("action: 'series'", StringComparison.Ordinal) && html.Contains("action: 'selection'", StringComparison.Ordinal) && html.Contains("action: 'brush'", StringComparison.Ordinal), "Interactive HTML should synchronize viewport, series, selection, and brush changes.");
        Assert(html.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-reset=\"true\">Reset</button>", StringComparison.Ordinal), "Interactive HTML should include a reset control by default.");
        Assert(html.Contains("<svg", StringComparison.Ordinal), "Interactive HTML should embed the static SVG output.");
        Assert(html.Contains("aria-live=\"polite\"", StringComparison.Ordinal), "Interactive tooltips should expose polite assistive announcements.");
        Assert(!html.Contains("<link", StringComparison.OrdinalIgnoreCase), "Interactive HTML should not reference external stylesheets.");
        Assert(!html.Contains("@import", StringComparison.OrdinalIgnoreCase), "Interactive HTML should not import external stylesheets.");
        var withoutSvgNamespace = html.Replace("http://www.w3.org/2000/svg", string.Empty);
        Assert(!withoutSvgNamespace.Contains("http://", StringComparison.OrdinalIgnoreCase) && !withoutSvgNamespace.Contains("https://", StringComparison.OrdinalIgnoreCase), "Interactive HTML should remain self-contained.");

        var noReset = SampleChart().ToInteractiveHtmlPage(options => {
            options.IncludeResetButton = false;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
        });
        Assert(!noReset.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-reset=\"true\">Reset</button>", StringComparison.Ordinal), "Interactive HTML should allow reset controls to be suppressed.");
        Assert(!noReset.Contains("data-cfx-zoom=\"in\"", StringComparison.Ordinal), "Interactive HTML should hide zoom controls when zoom is disabled.");
        Assert(!noReset.Contains("data-cfx-mode-button=\"pan\"", StringComparison.Ordinal), "Interactive HTML should hide pan controls when pan is disabled.");
        Assert(!noReset.Contains("data-cfx-mode-button=\"brush\"", StringComparison.Ordinal), "Interactive HTML should hide brush controls when brush is disabled.");
        Assert(!noReset.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-export=\"svg\" title=\"Download SVG\">SVG</button>", StringComparison.Ordinal), "Interactive HTML should hide SVG export controls when export is disabled.");
        Assert(!noReset.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-export=\"png\" title=\"Download PNG\">PNG</button>", StringComparison.Ordinal), "Interactive HTML should hide PNG export controls when export is disabled.");
        Assert(noReset.Contains("data-cfx-interaction-features=\"Tooltips\"", StringComparison.Ordinal), "Interactive HTML should honor narrowed feature profiles.");
    }

    private static void InteractiveHtmlEscapesHostProvidedAttributes() {
        var html = SampleChart().ToInteractiveHtmlPage(options => {
            options.PageTitle = "A < B & \"C\"";
            options.ScriptNonce = "nonce\" data-bad=\"1";
            options.Interaction.ChartId = "chart\" onmouseover=\"bad";
            options.Interaction.GroupName = "group<one>&\"two\"";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Export);
        });

        Assert(html.Contains("<title>A &lt; B &amp; &quot;C&quot;</title>", StringComparison.Ordinal), "Interactive HTML should escape page titles.");
        Assert(html.Contains("<script nonce=\"nonce&quot; data-bad=&quot;1\">", StringComparison.Ordinal), "Interactive HTML should escape CSP nonces without creating extra attributes.");
        Assert(html.Contains("data-cfx-chart-id=\"chart&quot; onmouseover=&quot;bad\"", StringComparison.Ordinal), "Interactive HTML should escape host-provided chart IDs.");
        Assert(html.Contains("data-cfx-interaction-group=\"group&lt;one&gt;&amp;&quot;two&quot;\"", StringComparison.Ordinal), "Interactive HTML should escape synchronized group names.");
        Assert(!html.Contains("data-bad=\"1\"", StringComparison.Ordinal), "Escaped nonces should not become additional HTML attributes.");
        Assert(!html.Contains("onmouseover=\"bad\"", StringComparison.Ordinal), "Escaped chart IDs should not become event handler attributes.");
        Assert(html.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-zoom=\"in\" title=\"Zoom in\">Zoom +</button>", StringComparison.Ordinal), "Typed toolbar rendering should preserve zoom button markup.");
        Assert(html.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-export=\"png\" title=\"Download PNG\">PNG</button>", StringComparison.Ordinal), "Typed toolbar rendering should preserve PNG export button markup.");
    }

    private static void InteractiveHtmlDashboardSynchronizesMultipleCharts() {
        var html = new[] { SampleChart(), SampleChart() }.ToInteractiveHtmlDashboardPage(options => {
            options.PageTitle = "Executive interactive dashboard";
            options.IdScope = "exec-dashboard";
            options.Columns = 2;
            options.ScriptNonce = "nonce-dashboard";
            options.Interaction.GroupName = "exec-review";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts);
        });

        Assert(html.Contains("<title>Executive interactive dashboard</title>", StringComparison.Ordinal), "Interactive dashboards should use the configured page title.");
        Assert(html.Contains("<script nonce=\"nonce-dashboard\">", StringComparison.Ordinal), "Interactive dashboards should support CSP nonces.");
        Assert(html.Contains("class=\"cfx-dashboard\"", StringComparison.Ordinal), "Interactive dashboards should render a grouped dashboard surface.");
        Assert(html.Contains("--cfx-dashboard-columns:2", StringComparison.Ordinal), "Interactive dashboards should honor configured column counts.");
        Assert(CountOccurrences(html, "class=\"cfx-interactive-chart\"") == 2, "Interactive dashboards should render one interactive chart section per chart.");
        Assert(html.Contains("data-cfx-chart-id=\"exec-dashboard-1\"", StringComparison.Ordinal) && html.Contains("data-cfx-chart-id=\"exec-dashboard-2\"", StringComparison.Ordinal), "Interactive dashboards should assign deterministic child chart IDs.");
        Assert(CountOccurrences(html, "data-cfx-interaction-group=\"exec-review\"") == 2, "Interactive dashboards should place every child chart in the shared interaction group.");
        Assert(html.Contains("new CustomEvent('cfxsync'", StringComparison.Ordinal) && html.Contains("applySync(peer, detail)", StringComparison.Ordinal), "Interactive dashboards should include grouped synchronization runtime.");
        Assert(html.Contains("data-cfx-export=\"svg\"", StringComparison.Ordinal) && html.Contains("data-cfx-export=\"png\"", StringComparison.Ordinal), "Interactive dashboards should include enabled per-chart export controls.");
        Assert(!html.Contains("<link", StringComparison.OrdinalIgnoreCase), "Interactive dashboards should not reference external stylesheets.");
        Assert(!html.Contains("@import", StringComparison.OrdinalIgnoreCase), "Interactive dashboards should not import external stylesheets.");
        var withoutSvgNamespace = html.Replace("http://www.w3.org/2000/svg", string.Empty);
        Assert(!withoutSvgNamespace.Contains("http://", StringComparison.OrdinalIgnoreCase) && !withoutSvgNamespace.Contains("https://", StringComparison.OrdinalIgnoreCase), "Interactive dashboards should remain self-contained.");
        AssertThrows<ArgumentException>(() => new HtmlInteractiveDashboardRenderer().RenderPage(Array.Empty<ChartForgeX.Core.Chart>()), "Interactive dashboards should reject empty chart sets.");
    }
}
