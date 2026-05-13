using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Html;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Renders topology charts into simple HTML wrappers with inline SVG.
/// </summary>
public sealed class TopologyHtmlRenderer {
    private const string DefaultCssClassPrefix = "cfx-topology";
    private readonly TopologySvgRenderer _svg = new();

    /// <summary>
    /// Renders an embeddable HTML fragment.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>An HTML fragment.</returns>
    public string RenderFragment(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        var id = string.IsNullOrWhiteSpace(chart.Id) ? "topology" : chart.Id!;
        if (options.View != null && !string.IsNullOrWhiteSpace(options.View.Id)) id += "-" + options.View.Id;
        var cssPrefix = CssClassPrefix(options);
        var wrapperClass = cssPrefix + "-wrapper";
        var viewportClass = cssPrefix + "-viewport";
        var controlsClass = cssPrefix + "-controls";
        var enableViewportControls = options.EnableHtmlInteractions && options.EnableHtmlViewportControls;
        var enableExportControls = options.EnableHtmlInteractions && options.EnableHtmlExportControls;
        var enableSync = options.EnableHtmlInteractions && options.EnableHtmlSynchronizedState && !string.IsNullOrWhiteSpace(options.HtmlSyncGroupName);
        var syncGroup = enableSync ? options.HtmlSyncGroupName!.Trim() : string.Empty;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("div")
            .Attribute("class", wrapperClass)
            .Attribute("data-chart-id", id)
            .Attribute("data-layout-mode", chart.LayoutMode.ToString())
            .Attribute("data-cfx-interactive", options.EnableHtmlInteractions)
            .Attribute("data-cfx-viewport-controls", enableViewportControls)
            .Attribute("data-cfx-export-controls", enableExportControls)
            .Attribute("data-cfx-sync-enabled", enableSync)
            .Attribute("data-cfx-sync-group", syncGroup)
            .Attribute("style", "width:100%;max-width:" + chart.Viewport.Width.ToString("0.###", CultureInfo.InvariantCulture) + "px;box-sizing:border-box;overflow:visible")
            .EndStartElement();
        writer.StartElement("div").Attribute("class", viewportClass).EndStartElement();
        if (enableViewportControls || enableExportControls) {
            writer.StartElement("div").Attribute("class", controlsClass).Attribute("aria-label", "Topology controls").EndStartElement();
            if (enableViewportControls) {
                WriteButton(writer, "data-cfx-topology-zoom", "in", "Zoom in", "Zoom in", null, "+");
                WriteButton(writer, "data-cfx-topology-zoom", "out", "Zoom out", "Zoom out", null, "-");
                WriteButton(writer, "data-cfx-topology-mode", "pan", "Pan topology", "Pan topology", false, "Pan");
                WriteButton(writer, "data-cfx-topology-reset", "true", "Reset viewport", "Reset viewport", null, "0");
            }

            if (enableExportControls) {
                WriteButton(writer, "data-cfx-topology-export", "svg", "Export SVG", "Export SVG", null, "SVG");
                WriteButton(writer, "data-cfx-topology-export", "png", "Export PNG", "Export PNG", null, "PNG");
            }

            writer.EndElement();
        }

        writer.RawTrusted(_svg.Render(chart, options));
        writer.EndElement().EndElement();
        return writer.Build();
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
        var cssPrefix = CssClassPrefix(options);
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, StyleSheet(cssPrefix, CssFontFamily(theme.FontFamily), theme.Background));
        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line()
            .RawTrusted(RenderFragment(chart, options)).Line();
        if (options.EnableHtmlInteractions) writer.RawTrusted(InteractionScript(cssPrefix)).Line();
        writer.EndElement().Line()
            .EndElement().Line();
        return writer.Build();
    }

    private static void WriteButton(HtmlMarkupWriter writer, string dataAttribute, string dataValue, string title, string ariaLabel, bool? pressed, string text) {
        writer.StartElement("button")
            .Attribute("type", "button")
            .Attribute(dataAttribute, dataValue)
            .Attribute("title", title)
            .Attribute("aria-label", ariaLabel)
            .Attribute("aria-pressed", pressed)
            .EndStartElement()
            .Text(text)
            .EndElement();
    }

    private static string StyleSheet(string cssPrefix, string fontFamily, string background) {
        var stylesheet = HtmlSurfacePolish.ReportBodyCss(background, fontFamily, "24px") + ".cfx-topology-wrapper{margin:0 auto;overflow:visible}.cfx-topology-viewport{position:relative}.cfx-topology-wrapper svg{max-width:100%;height:auto;display:block;overflow:visible}.cfx-topology-wrapper[data-cfx-viewport-controls='true'] .cfx-topology-viewport{overflow:hidden;touch-action:none}.cfx-topology-wrapper[data-cfx-viewport-controls='true'] svg{transform:translate(var(--cfx-topology-pan-x,0),var(--cfx-topology-pan-y,0)) scale(var(--cfx-topology-zoom,1));transform-origin:center center;transition:transform .12s ease;will-change:transform}.cfx-topology-controls{position:absolute;z-index:5;top:12px;left:12px;display:grid;gap:6px}.cfx-topology-controls button{min-width:34px;height:34px;border:1px solid rgba(37,99,235,.28);border-radius:6px;background:rgba(255,255,255,.92);color:#1e3a8a;cursor:pointer;font:700 12px/1 " + fontFamily + ";box-shadow:0 8px 18px rgba(15,23,42,.1)}.cfx-topology-controls button:hover,.cfx-topology-controls button:focus-visible,.cfx-topology-controls button[aria-pressed='true']{border-color:#2563eb;color:#0f172a;background:#eff6ff}.cfx-topology-wrapper[data-cfx-topology-mode='pan'] .cfx-topology-viewport{cursor:grab}.cfx-topology-wrapper[data-cfx-topology-mode='pan'] .cfx-topology-viewport:active{cursor:grabbing}.cfx-topology-wrapper [data-cfx-role='topology-node'],.cfx-topology-wrapper [data-cfx-role='topology-edge'],.cfx-topology-wrapper [data-cfx-role='topology-group']{cursor:pointer}.cfx-topology-wrapper .cfx-topology-html-selected{filter:drop-shadow(0 10px 18px rgba(37,99,235,.22))}.cfx-topology-wrapper .cfx-topology-html-muted{opacity:.32}.cfx-topology-wrapper .cfx-topology-html-related{opacity:1}.cfx-topology-wrapper .cfx-topology-html-hovered{filter:drop-shadow(0 8px 16px rgba(15,23,42,.16))}.cfx-topology-wrapper .cfx-topology-html-hover-muted{opacity:.42}.cfx-topology-wrapper .cfx-topology-html-hover-related{opacity:1}" + HtmlSurfacePolish.ResponsiveCenteredBodyCss + HtmlSurfacePolish.PrintBodyCss("0", ".cfx-topology-wrapper{width:100%;max-width:none}.cfx-topology-wrapper svg{width:100%;height:auto}");
        return stylesheet
            .Replace(".cfx-topology-wrapper", "." + cssPrefix + "-wrapper")
            .Replace(".cfx-topology-viewport", "." + cssPrefix + "-viewport")
            .Replace(".cfx-topology-controls", "." + cssPrefix + "-controls")
            .Replace(".cfx-topology-html-", "." + cssPrefix + "-html-");
    }

    private static string InteractionScript(string cssPrefix) {
        var script = """
<script>
(() => {
  for (const wrapper of document.querySelectorAll('.cfx-topology-wrapper[data-cfx-interactive="true"]')) {
    const selectables = '[data-cfx-role="topology-node"],[data-cfx-role="topology-edge"],[data-cfx-role="topology-group"]';
    const viewportControls = wrapper.getAttribute('data-cfx-viewport-controls') === 'true';
    const exportControls = wrapper.getAttribute('data-cfx-export-controls') === 'true';
    const syncEnabled = wrapper.getAttribute('data-cfx-sync-enabled') === 'true';
    const syncGroup = wrapper.getAttribute('data-cfx-sync-group') || '';
    const viewport = wrapper.querySelector('.cfx-topology-viewport') || wrapper;
    let applyingSync = false;
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
    const clamp = (value, min, max) => Math.max(min, Math.min(max, value));
    const numberAttr = (name, fallback) => {
      const value = Number(wrapper.getAttribute(name) || '');
      return Number.isFinite(value) ? value : fallback;
    };
    const viewportState = () => ({
      zoom: numberAttr('data-cfx-topology-zoom', 1),
      panX: numberAttr('data-cfx-topology-pan-x', 0),
      panY: numberAttr('data-cfx-topology-pan-y', 0)
    });
    const emitSync = (action, payload) => {
      if (!syncEnabled || !syncGroup || applyingSync) return;
      const detail = Object.assign({ chartId: attr(wrapper, 'data-chart-id'), group: syncGroup, action }, payload || {});
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-sync', { bubbles: true, detail }));
      document.querySelectorAll('.cfx-topology-wrapper[data-cfx-interactive="true"][data-cfx-sync-enabled="true"]').forEach(peer => {
        if (peer === wrapper || peer.getAttribute('data-cfx-sync-group') !== syncGroup) return;
        peer.dispatchEvent(new CustomEvent('cfx-topology-apply-sync', { detail }));
      });
    };
    const applyViewport = state => {
      if (!viewportControls) return;
      const next = {
        zoom: clamp(Number(state.zoom) || 1, 0.5, 4),
        panX: clamp(Number(state.panX) || 0, -4000, 4000),
        panY: clamp(Number(state.panY) || 0, -4000, 4000)
      };
      wrapper.setAttribute('data-cfx-topology-zoom', next.zoom.toFixed(3));
      wrapper.setAttribute('data-cfx-topology-pan-x', next.panX.toFixed(1));
      wrapper.setAttribute('data-cfx-topology-pan-y', next.panY.toFixed(1));
      wrapper.style.setProperty('--cfx-topology-zoom', next.zoom.toFixed(3));
      wrapper.style.setProperty('--cfx-topology-pan-x', next.panX.toFixed(1) + 'px');
      wrapper.style.setProperty('--cfx-topology-pan-y', next.panY.toFixed(1) + 'px');
    };
    const emitViewport = () => {
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-viewport', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), state: viewportState() } }));
      emitSync('viewport', { state: viewportState() });
    };
    const zoomBy = factor => {
      const state = viewportState();
      applyViewport({ zoom: state.zoom * factor, panX: state.panX, panY: state.panY });
      emitViewport();
    };
    const resetViewport = () => {
      applyViewport({ zoom: 1, panX: 0, panY: 0 });
      wrapper.removeAttribute('data-cfx-topology-mode');
      wrapper.querySelectorAll('[data-cfx-topology-mode]').forEach(button => button.setAttribute('aria-pressed', 'false'));
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-reset', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id') } }));
      emitViewport();
    };
    const setViewportMode = mode => {
      const next = wrapper.getAttribute('data-cfx-topology-mode') === mode ? '' : mode;
      if (next) wrapper.setAttribute('data-cfx-topology-mode', next);
      else wrapper.removeAttribute('data-cfx-topology-mode');
      wrapper.querySelectorAll('[data-cfx-topology-mode]').forEach(button => button.setAttribute('aria-pressed', attr(button, 'data-cfx-topology-mode') === next ? 'true' : 'false'));
    };
    const svgElement = () => wrapper.querySelector('.cfx-topology-viewport svg') || wrapper.querySelector('svg');
    const exportName = () => (attr(wrapper, 'data-chart-id') || 'topology').replace(/[^a-z0-9_.-]+/gi, '-').replace(/^-+|-+$/g, '') || 'topology';
    const serializeSvg = () => {
      const svg = svgElement();
      return svg ? { svg, data: new XMLSerializer().serializeToString(svg) } : null;
    };
    const emitExport = format => {
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-export', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), format } }));
    };
    const downloadBlob = (blob, extension, format) => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = exportName() + '.' + extension;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
      emitExport(format);
    };
    const svgSize = svg => {
      const viewBox = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
      const rect = svg.getBoundingClientRect ? svg.getBoundingClientRect() : { width: 0, height: 0 };
      const width = Math.max(1, Math.round((viewBox && viewBox.width) || (svg.width && svg.width.baseVal ? svg.width.baseVal.value : 0) || rect.width || 800));
      const height = Math.max(1, Math.round((viewBox && viewBox.height) || (svg.height && svg.height.baseVal ? svg.height.baseVal.value : 0) || rect.height || 450));
      return { width, height };
    };
    const exportSvg = () => {
      const serialized = serializeSvg();
      if (!serialized) return;
      downloadBlob(new Blob([serialized.data], { type: 'image/svg+xml;charset=utf-8' }), 'svg', 'svg');
    };
    const exportPng = () => {
      const serialized = serializeSvg();
      if (!serialized) return;
      const source = new Blob([serialized.data], { type: 'image/svg+xml;charset=utf-8' });
      const sourceUrl = URL.createObjectURL(source);
      const image = new Image();
      image.onload = () => {
        const size = svgSize(serialized.svg);
        const scale = Math.max(1, Math.ceil(window.devicePixelRatio || 1));
        const canvas = document.createElement('canvas');
        canvas.width = size.width * scale;
        canvas.height = size.height * scale;
        const context = canvas.getContext('2d');
        if (!context) {
          URL.revokeObjectURL(sourceUrl);
          return;
        }
        context.setTransform(scale, 0, 0, scale, 0, 0);
        context.clearRect(0, 0, size.width, size.height);
        context.drawImage(image, 0, 0, size.width, size.height);
        canvas.toBlob(blob => {
          URL.revokeObjectURL(sourceUrl);
          if (blob) downloadBlob(blob, 'png', 'png');
        }, 'image/png');
      };
      image.onerror = () => URL.revokeObjectURL(sourceUrl);
      image.src = sourceUrl;
    };
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
          iconId: attr(element, 'data-node-icon-id'),
          iconPack: attr(element, 'data-node-icon-pack'),
          iconLabel: attr(element, 'data-node-icon-label'),
          iconShape: attr(element, 'data-node-icon-shape'),
          iconArtwork: attr(element, 'data-node-icon-artwork'),
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
          iconId: attr(element, 'data-group-icon-id'),
          iconPack: attr(element, 'data-group-icon-pack'),
          iconLabel: attr(element, 'data-group-icon-label'),
          iconShape: attr(element, 'data-group-icon-shape'),
          iconArtwork: attr(element, 'data-group-icon-artwork'),
          longitude: attr(element, 'data-group-longitude'),
          latitude: attr(element, 'data-group-latitude'),
          geoVisible: attr(element, 'data-group-geo-visible'),
          layoutPolicy: attr(element, 'data-group-layout-policy'),
          appliedLayoutPolicy: attr(element, 'data-group-applied-layout-policy'),
          callout: {
            visualRole: attr(element, 'data-cfx-visual-role'),
            placement: attr(element, 'data-callout-placement'),
            anchorX: attr(element, 'data-callout-anchor-x'),
            anchorY: attr(element, 'data-callout-anchor-y'),
            nodeCount: attr(element, 'data-callout-node-count'),
            healthyCount: attr(element, 'data-callout-healthy-count'),
            warningCount: attr(element, 'data-callout-warning-count'),
            criticalCount: attr(element, 'data-callout-critical-count'),
            unknownCount: attr(element, 'data-callout-unknown-count'),
            disabledCount: attr(element, 'data-callout-disabled-count')
          }
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
    const elementKey = element => {
      const detail = identity(element);
      return detail.kind + ':' + detail.id;
    };
    const publicDetail = element => {
      const detail = identity(element);
      detail.related = related(detail);
      delete detail.element;
      return detail;
    };
    const relatedElements = detail => {
      const items = [];
      const seen = new Set();
      const add = (kind, id) => {
        if (!id) return;
        const key = kind + ':' + id;
        if (seen.has(key) || (detail.kind === kind && detail.id === id)) return;
        const element = findSelection({ kind, id });
        if (!element) return;
        seen.add(key);
        items.push(element);
      };
      (detail.related.nodeIds || []).forEach(id => add('node', id));
      (detail.related.edgeIds || []).forEach(id => add('edge', id));
      (detail.related.groupIds || []).forEach(id => add('group', id));
      return items;
    };
    const focusRelated = (element, offset) => {
      const detail = identity(element);
      detail.related = related(detail);
      const candidates = relatedElements(detail);
      if (!candidates.length) return false;
      const sourceKey = detail.kind + ':' + detail.id;
      const previousTarget = wrapper.getAttribute('data-cfx-navigation-source') === sourceKey ? wrapper.getAttribute('data-cfx-navigation-target') || '' : '';
      const previousIndex = candidates.findIndex(candidate => elementKey(candidate) === previousTarget);
      const nextIndex = previousIndex >= 0 ? (previousIndex + offset + candidates.length) % candidates.length : (offset > 0 ? 0 : candidates.length - 1);
      const target = candidates[nextIndex];
      const targetKey = elementKey(target);
      wrapper.setAttribute('data-cfx-navigation-source', sourceKey);
      wrapper.setAttribute('data-cfx-navigation-target', targetKey);
      if (target.focus) target.focus({ preventScroll: true });
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-navigate', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), from: publicDetail(element), to: publicDetail(target) } }));
      return true;
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
      emitSync('selection', { selection: { kind: detail.kind, id: detail.id, status: detail.status } });
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
    if (viewportControls) {
      applyViewport(viewportState());
      wrapper.querySelectorAll('[data-cfx-topology-zoom]').forEach(button => {
        button.addEventListener('click', () => zoomBy(attr(button, 'data-cfx-topology-zoom') === 'in' ? 1.2 : 0.8333333333));
      });
      wrapper.querySelectorAll('[data-cfx-topology-mode]').forEach(button => {
        button.addEventListener('click', () => setViewportMode(attr(button, 'data-cfx-topology-mode')));
      });
      wrapper.querySelectorAll('[data-cfx-topology-reset]').forEach(button => {
        button.addEventListener('click', () => resetViewport());
      });
      let drag = null;
      let suppressClick = false;
      viewport.addEventListener('wheel', event => {
        event.preventDefault();
        zoomBy(event.deltaY < 0 ? 1.08 : 0.92);
      }, { passive: false });
      viewport.addEventListener('pointerdown', event => {
        if (event.button !== 0 || wrapper.getAttribute('data-cfx-topology-mode') !== 'pan') return;
        const state = viewportState();
        drag = { id: event.pointerId, x: event.clientX, y: event.clientY, panX: state.panX, panY: state.panY, moved: false };
        viewport.setPointerCapture(event.pointerId);
      });
      viewport.addEventListener('pointermove', event => {
        if (!drag || drag.id !== event.pointerId) return;
        const dx = event.clientX - drag.x;
        const dy = event.clientY - drag.y;
        if (Math.abs(dx) > 2 || Math.abs(dy) > 2) {
          drag.moved = true;
          suppressClick = true;
        }
        applyViewport({ zoom: viewportState().zoom, panX: drag.panX + dx, panY: drag.panY + dy });
      });
      viewport.addEventListener('pointerup', event => {
        if (!drag || drag.id !== event.pointerId) return;
        viewport.releasePointerCapture(event.pointerId);
        if (drag.moved) emitViewport();
        drag = null;
      });
      viewport.addEventListener('pointercancel', event => {
        if (drag && drag.id === event.pointerId) drag = null;
      });
      wrapper.addEventListener('cfx-topology-set-viewport', event => {
        applyViewport(event.detail || {});
        emitViewport();
      });
      wrapper.addEventListener('cfx-topology-reset-viewport', () => resetViewport());
      wrapper.addEventListener('click', event => {
        if (!suppressClick) return;
        suppressClick = false;
        event.preventDefault();
        event.stopPropagation();
      }, true);
    }
    if (exportControls) {
      wrapper.querySelectorAll('[data-cfx-topology-export]').forEach(button => {
        button.addEventListener('click', () => {
          if (attr(button, 'data-cfx-topology-export') === 'png') exportPng();
          else exportSvg();
        });
      });
    }
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
        emitSync('clear-selection', {});
        return;
      }
      if (event.key === 'ArrowRight' || event.key === 'ArrowDown' || event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
        const element = event.target instanceof Element ? event.target.closest(selectables) : null;
        if (!element || !wrapper.contains(element)) return;
        if (focusRelated(element, event.key === 'ArrowRight' || event.key === 'ArrowDown' ? 1 : -1)) event.preventDefault();
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
      emitSync('clear-selection', {});
    });
    wrapper.addEventListener('cfx-topology-apply-sync', event => {
      const detail = event.detail || {};
      if (!syncEnabled || !syncGroup || detail.group !== syncGroup) return;
      applyingSync = true;
      try {
        if (detail.action === 'selection' && detail.selection) {
          const element = findSelection(detail.selection);
          if (element) select(element);
        } else if (detail.action === 'clear-selection') {
          clear();
          wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true }));
        } else if (detail.action === 'viewport' && detail.state) {
          applyViewport(detail.state);
          wrapper.dispatchEvent(new CustomEvent('cfx-topology-viewport', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), state: viewportState(), sourceChartId: detail.chartId } }));
        }
      } finally {
        applyingSync = false;
      }
    });
  }
})();
</script>
""";
        return script
            .Replace(".cfx-topology-wrapper", "." + cssPrefix + "-wrapper")
            .Replace(".cfx-topology-viewport", "." + cssPrefix + "-viewport")
            .Replace("cfx-topology-html-", cssPrefix + "-html-");
    }

    private static string CssClassPrefix(TopologyRenderOptions options) {
        return NormalizeCssClassPrefix(options.CssClassPrefix, DefaultCssClassPrefix);
    }

}
