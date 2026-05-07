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
        sb.AppendLine("<style>body{margin:0;min-height:100vh;background:" + EscapeAttr(theme.Background) + ";font-family:" + CssFontFamily(theme.FontFamily) + ";padding:24px;box-sizing:border-box}.cfx-topology-wrapper{margin:0 auto}.cfx-topology-wrapper svg{max-width:100%;height:auto;display:block}.cfx-topology-wrapper [data-cfx-role='topology-node'],.cfx-topology-wrapper [data-cfx-role='topology-edge'],.cfx-topology-wrapper [data-cfx-role='topology-group']{cursor:pointer}.cfx-topology-wrapper .cfx-topology-html-selected{filter:drop-shadow(0 10px 18px rgba(37,99,235,.22))}.cfx-topology-wrapper .cfx-topology-html-muted{opacity:.32}.cfx-topology-wrapper .cfx-topology-html-related{opacity:1}.cfx-topology-wrapper .cfx-topology-html-hovered{filter:drop-shadow(0 8px 16px rgba(15,23,42,.16))}.cfx-topology-wrapper .cfx-topology-html-hover-muted{opacity:.42}.cfx-topology-wrapper .cfx-topology-html-hover-related{opacity:1}</style>");
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
    const attr = (element, name) => element.getAttribute(name) || '';
    const toCamel = value => value.replace(/-([a-z0-9])/g, (_, ch) => ch.toUpperCase());
    const collectPrefixed = (element, prefix) => {
      const values = {};
      for (const attribute of element.attributes) {
        if (attribute.name.startsWith(prefix)) values[toCamel(attribute.name.slice(prefix.length))] = attribute.value;
      }
      return values;
    };
    const unique = values => Array.from(new Set(values.filter(value => value)));
    const edgeIdentity = edge => ({
      id: attr(edge, 'data-edge-id'),
      kind: attr(edge, 'data-edge-kind'),
      status: attr(edge, 'data-cfx-status'),
      sourceNodeId: attr(edge, 'data-source-node-id'),
      targetNodeId: attr(edge, 'data-target-node-id'),
      sourceGroupId: attr(edge, 'data-source-group-id'),
      targetGroupId: attr(edge, 'data-target-group-id')
    });
    const related = detail => {
      if (detail.kind === 'node') {
        const edges = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-edge"]'))
          .filter(edge => attr(edge, 'data-source-node-id') === detail.id || attr(edge, 'data-target-node-id') === detail.id);
        return {
          nodeIds: unique(edges.flatMap(edge => [attr(edge, 'data-source-node-id'), attr(edge, 'data-target-node-id')]).filter(id => id !== detail.id)),
          edgeIds: edges.map(edge => attr(edge, 'data-edge-id')),
          groupIds: unique([detail.groupId].concat(edges.flatMap(edge => [attr(edge, 'data-source-group-id'), attr(edge, 'data-target-group-id')]))),
          edges: edges.map(edgeIdentity)
        };
      }
      if (detail.kind === 'edge') {
        return {
          nodeIds: unique([detail.sourceNodeId, detail.targetNodeId]),
          edgeIds: [detail.id],
          groupIds: unique([detail.sourceGroupId, detail.targetGroupId]),
          edges: [edgeIdentity(detail.element)]
        };
      }
      if (detail.kind === 'group') {
        const nodes = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-node"]'))
          .filter(node => attr(node, 'data-group-id') === detail.id);
        const edges = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-edge"]'))
          .filter(edge => attr(edge, 'data-source-group-id') === detail.id || attr(edge, 'data-target-group-id') === detail.id);
        return {
          nodeIds: nodes.map(node => attr(node, 'data-node-id')),
          edgeIds: edges.map(edge => attr(edge, 'data-edge-id')),
          groupIds: unique(edges.flatMap(edge => [attr(edge, 'data-source-group-id'), attr(edge, 'data-target-group-id')]).filter(id => id !== detail.id)),
          edges: edges.map(edgeIdentity)
        };
      }
      return { nodeIds: [], edgeIds: [], groupIds: [], edges: [] };
    };
    const clear = () => {
      wrapper.removeAttribute('data-cfx-selection-kind');
      wrapper.removeAttribute('data-cfx-selection-id');
      wrapper.removeAttribute('data-cfx-selection-status');
      wrapper.querySelectorAll('.cfx-topology-html-selected,.cfx-topology-html-muted,.cfx-topology-html-related').forEach(item => {
        item.classList.remove('cfx-topology-html-selected', 'cfx-topology-html-muted', 'cfx-topology-html-related');
        item.removeAttribute('aria-selected');
      });
    };
    const clearHover = () => {
      wrapper.removeAttribute('data-cfx-hover-kind');
      wrapper.removeAttribute('data-cfx-hover-id');
      wrapper.querySelectorAll('.cfx-topology-html-hovered,.cfx-topology-html-hover-muted,.cfx-topology-html-hover-related').forEach(item => {
        item.classList.remove('cfx-topology-html-hovered', 'cfx-topology-html-hover-muted', 'cfx-topology-html-hover-related');
      });
    };
    const identity = element => {
      const role = element.getAttribute('data-cfx-role') || '';
      const base = {
        role,
        elementId: attr(element, 'id'),
        status: attr(element, 'data-cfx-status'),
        selected: attr(element, 'data-cfx-selected') === 'true',
        metadata: collectPrefixed(element, 'data-cfx-meta-'),
        metrics: collectPrefixed(element, 'data-cfx-metric-'),
        element
      };
      if (role === 'topology-node') {
        return {
          ...base,
          kind: 'node',
          id: attr(element, 'data-node-id'),
          nodeKind: attr(element, 'data-node-kind'),
          groupId: attr(element, 'data-group-id'),
          displayMode: attr(element, 'data-node-display-mode'),
          badge: attr(element, 'data-node-badge'),
          color: attr(element, 'data-node-color'),
          longitude: attr(element, 'data-node-longitude'),
          latitude: attr(element, 'data-node-latitude'),
          geoVisible: attr(element, 'data-node-geo-visible')
        };
      }
      if (role === 'topology-edge') {
        return {
          ...base,
          kind: 'edge',
          id: attr(element, 'data-edge-id'),
          edgeKind: attr(element, 'data-edge-kind'),
          sourceNodeId: attr(element, 'data-source-node-id'),
          targetNodeId: attr(element, 'data-target-node-id'),
          sourceGroupId: attr(element, 'data-source-group-id'),
          targetGroupId: attr(element, 'data-target-group-id'),
          sourcePort: attr(element, 'data-source-port'),
          targetPort: attr(element, 'data-target-port'),
          routeLane: attr(element, 'data-route-lane'),
          layoutInference: attr(element, 'data-edge-layout-inference'),
          muted: attr(element, 'data-edge-muted') === 'true',
          lineStyle: attr(element, 'data-edge-line-style'),
          route: {
            strategy: attr(element, 'data-route-strategy'),
            curve: attr(element, 'data-route-curve'),
            controlX: attr(element, 'data-route-control-x'),
            controlY: attr(element, 'data-route-control-y'),
            corridor: attr(element, 'data-route-corridor'),
            candidateCount: attr(element, 'data-route-candidate-count'),
            fallbackReason: attr(element, 'data-route-fallback-reason'),
            segmentCount: attr(element, 'data-route-segment-count'),
            obstacleHits: attr(element, 'data-route-obstacle-hits'),
            labelObstacleHits: attr(element, 'data-route-label-obstacle-hits'),
            overlapScore: attr(element, 'data-route-overlap-score'),
            offset: attr(element, 'data-route-offset'),
            waypointCount: attr(element, 'data-waypoint-count')
          }
        };
      }
      if (role === 'topology-group') {
        return {
          ...base,
          kind: 'group',
          id: attr(element, 'data-group-id'),
          symbol: attr(element, 'data-group-symbol'),
          color: attr(element, 'data-group-color'),
          longitude: attr(element, 'data-group-longitude'),
          latitude: attr(element, 'data-group-latitude'),
          geoVisible: attr(element, 'data-group-geo-visible'),
          layoutPolicy: attr(element, 'data-group-layout-policy'),
          appliedLayoutPolicy: attr(element, 'data-group-applied-layout-policy')
        };
      }
      return { ...base, kind: 'unknown', id: '' };
    };
    const findSelection = request => {
      if (!request || !request.kind || !request.id) return null;
      return Array.from(wrapper.querySelectorAll(selectables)).find(element => {
        const detail = identity(element);
        return detail.kind === request.kind && detail.id === request.id;
      }) || null;
    };
    const applyRelatedClasses = (detail, classPrefix, selfClass) => {
      const relatedEdges = new Set(detail.related.edgeIds || []);
      const relatedNodes = new Set(detail.related.nodeIds || []);
      const relatedGroups = new Set(detail.related.groupIds || []);
      const relatedClass = classPrefix + 'related';
      const mutedClass = classPrefix + 'muted';
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
        const isSelf = detail.kind === 'edge' && attr(edge, 'data-edge-id') === detail.id;
        const isRelated = isSelf || relatedEdges.has(attr(edge, 'data-edge-id'));
        edge.classList.toggle(relatedClass, isRelated);
        edge.classList.toggle(mutedClass, !isRelated);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const isSelf = detail.kind === 'node' && attr(node, 'data-node-id') === detail.id;
        const isRelated = isSelf || relatedNodes.has(attr(node, 'data-node-id'));
        node.classList.toggle(relatedClass, isRelated);
        node.classList.toggle(mutedClass, !isRelated && detail.kind !== 'group');
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
        const isSelf = detail.kind === 'group' && attr(group, 'data-group-id') === detail.id;
        const isRelated = isSelf || relatedGroups.has(attr(group, 'data-group-id'));
        group.classList.toggle(relatedClass, isRelated);
      });
      if (detail.element && selfClass) detail.element.classList.add(selfClass);
    };
    const hover = element => {
      const detail = identity(element);
      detail.related = related(detail);
      clearHover();
      wrapper.setAttribute('data-cfx-hover-kind', detail.kind);
      wrapper.setAttribute('data-cfx-hover-id', detail.id);
      applyRelatedClasses(detail, 'cfx-topology-html-hover-', 'cfx-topology-html-hovered');
      delete detail.element;
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover', { bubbles: true, detail }));
    };
    const select = element => {
      const detail = identity(element);
      detail.related = related(detail);
      delete detail.element;
      clear();
      wrapper.setAttribute('data-cfx-selection-kind', detail.kind);
      wrapper.setAttribute('data-cfx-selection-id', detail.id);
      wrapper.setAttribute('data-cfx-selection-status', detail.status);
      element.classList.add('cfx-topology-html-selected');
      element.setAttribute('aria-selected', 'true');
      applyRelatedClasses({ ...detail, element }, 'cfx-topology-html-', '');
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-select', { bubbles: true, detail }));
    };
    wrapper.querySelectorAll(selectables).forEach(element => {
      if (!element.hasAttribute('tabindex')) element.setAttribute('tabindex', '0');
      if (!element.hasAttribute('role')) element.setAttribute('role', 'button');
      element.addEventListener('pointerenter', () => hover(element));
      element.addEventListener('pointerleave', () => {
        clearHover();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover-clear', { bubbles: true }));
      });
      element.addEventListener('focus', () => hover(element));
      element.addEventListener('blur', () => {
        clearHover();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover-clear', { bubbles: true }));
      });
    });
    wrapper.addEventListener('click', event => {
      const element = event.target instanceof Element ? event.target.closest(selectables) : null;
      if (!element || !wrapper.contains(element)) return;
      event.preventDefault();
      select(element);
    });
    wrapper.addEventListener('keydown', event => {
      if (event.key === 'Escape') {
        clear();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true }));
        return;
      }
      if (event.key !== 'Enter' && event.key !== ' ') return;
      const element = event.target instanceof Element ? event.target.closest(selectables) : null;
      if (!element || !wrapper.contains(element)) return;
      event.preventDefault();
      select(element);
    });
    wrapper.addEventListener('cfx-topology-set-selection', event => {
      const element = findSelection(event.detail);
      if (element) select(element);
    });
    wrapper.addEventListener('cfx-topology-clear-selection', () => {
      clear();
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true }));
    });
  }
})();
</script>
""";
    }

}
