using System;
using System.Globalization;
using System.Text;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Renders topology charts into simple HTML wrappers with inline SVG.
/// </summary>
public sealed class TopologyHtmlRenderer {
    private readonly TopologySvgRenderer _svg = new();

    /// <summary>
    /// Renders an embeddable HTML fragment.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>An HTML fragment.</returns>
    public string RenderFragment(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var id = string.IsNullOrWhiteSpace(chart.Id) ? "topology" : chart.Id!;
        if (options?.View != null && !string.IsNullOrWhiteSpace(options.View.Id)) id += "-" + options.View.Id;
        return "<div class=\"cfx-topology-wrapper\" data-chart-id=\"" + EscapeAttr(id) + "\" data-layout-mode=\"" + chart.LayoutMode + "\" data-cfx-interactive=\"" + ((options?.EnableHtmlInteractions ?? true) ? "true" : "false") + "\" style=\"width:100%;max-width:" + chart.Viewport.Width.ToString("0.###", CultureInfo.InvariantCulture) + "px;box-sizing:border-box\">" + _svg.Render(chart, options) + "</div>";
    }

    /// <summary>
    /// Renders a complete static HTML page containing one topology chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A complete HTML page.</returns>
    public string RenderPage(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        var theme = chart.Theme ?? TopologyTheme.Light();
        var title = string.IsNullOrWhiteSpace(chart.Title) ? "ChartForgeX topology" : chart.Title!;
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>" + Escape(title) + "</title>");
        sb.AppendLine("<style>body{margin:0;min-height:100vh;background:" + EscapeAttr(theme.Background) + ";font-family:" + CssFontFamily(theme.FontFamily) + ";padding:24px;box-sizing:border-box}.cfx-topology-wrapper{margin:0 auto}.cfx-topology-wrapper svg{max-width:100%;height:auto;display:block}.cfx-topology-wrapper [data-cfx-role='topology-node'],.cfx-topology-wrapper [data-cfx-role='topology-edge'],.cfx-topology-wrapper [data-cfx-role='topology-group']{cursor:pointer}.cfx-topology-wrapper .cfx-topology-html-selected{filter:drop-shadow(0 10px 18px rgba(37,99,235,.22))}.cfx-topology-wrapper .cfx-topology-html-muted{opacity:.32}.cfx-topology-wrapper[data-cfx-selection-kind='node'] [data-cfx-role='topology-edge'].cfx-topology-html-related{opacity:1}</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine(RenderFragment(chart, options));
        if (options.EnableHtmlInteractions) sb.AppendLine(InteractionScript());
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string InteractionScript() {
        return """
<script>
(() => {
  for (const wrapper of document.querySelectorAll('.cfx-topology-wrapper[data-cfx-interactive="true"]')) {
    const selectables = '[data-cfx-role="topology-node"],[data-cfx-role="topology-edge"],[data-cfx-role="topology-group"]';
    const clear = () => {
      wrapper.removeAttribute('data-cfx-selection-kind');
      wrapper.removeAttribute('data-cfx-selection-id');
      wrapper.querySelectorAll('.cfx-topology-html-selected,.cfx-topology-html-muted,.cfx-topology-html-related').forEach(item => {
        item.classList.remove('cfx-topology-html-selected', 'cfx-topology-html-muted', 'cfx-topology-html-related');
        item.removeAttribute('aria-selected');
      });
    };
    const identity = element => {
      const role = element.getAttribute('data-cfx-role') || '';
      if (role === 'topology-node') return { kind: 'node', id: element.getAttribute('data-node-id') || '' };
      if (role === 'topology-edge') return { kind: 'edge', id: element.getAttribute('data-edge-id') || '' };
      if (role === 'topology-group') return { kind: 'group', id: element.getAttribute('data-group-id') || '' };
      return { kind: 'unknown', id: '' };
    };
    const select = element => {
      const detail = identity(element);
      clear();
      wrapper.setAttribute('data-cfx-selection-kind', detail.kind);
      wrapper.setAttribute('data-cfx-selection-id', detail.id);
      element.classList.add('cfx-topology-html-selected');
      element.setAttribute('aria-selected', 'true');
      if (detail.kind === 'node') {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          const related = edge.getAttribute('data-source-node-id') === detail.id || edge.getAttribute('data-target-node-id') === detail.id;
          edge.classList.toggle('cfx-topology-html-related', related);
          edge.classList.toggle('cfx-topology-html-muted', !related);
        });
      }
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-select', { bubbles: true, detail }));
    };
    wrapper.addEventListener('click', event => {
      const element = event.target instanceof Element ? event.target.closest(selectables) : null;
      if (!element || !wrapper.contains(element)) return;
      event.preventDefault();
      select(element);
    });
    wrapper.addEventListener('keydown', event => {
      if (event.key !== 'Escape') return;
      clear();
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true }));
    });
  }
})();
</script>
""";
    }

}
