// ChartForgeX playground engine loader.
// Boots the .NET WebAssembly runtime published to /playground/engine/_framework/ (Website/tools/Playground)
// and exposes a small promise-based API. Everything runs in the browser; nothing is sent to a server.
//
//   import { loadPlaygroundEngine } from '/themes/chartforgex/assets/playground-engine.js';
//   const engine = loadPlaygroundEngine({ onProgress: (p) => console.log(p.phase, p.loaded, p.total) });
//   const info = await engine.ready;                       // { version, runtime, maxSourceLength, languages, themes }
//   const result = await engine.render('mermaid', source, 'dark');
//   // { ok, svg, error, line, column, diagramType, elapsedMs, wallMs, language, theme, themed, artifactCount, diagnostics }
//   const samples = await engine.samples();                // [{ id, title, language, source }]

const DEFAULT_FRAMEWORK_URL = '/playground/engine/_framework/';
const EXPORTS_NAMESPACE = ['ChartForgeX', 'Playground', 'PlaygroundExports'];
const PREVIEW_FONTS = [
  { file: 'Carlito-Regular.ttf', weight: '400' },
  { file: 'Carlito-Bold.ttf', weight: '700' }
];

let sharedEngine = null;

/**
 * Returns the page-wide engine, starting the runtime download on the first call.
 * @param {{ frameworkUrl?: string, onProgress?: (progress: EngineProgress) => void }} [options]
 * @returns {PlaygroundEngine}
 */
export function loadPlaygroundEngine(options = {}) {
  if (!sharedEngine) {
    sharedEngine = createEngine(options.frameworkUrl || DEFAULT_FRAMEWORK_URL);
  }
  if (typeof options.onProgress === 'function') {
    sharedEngine.onProgress(options.onProgress);
  }
  return sharedEngine;
}

/**
 * @typedef {{ phase: 'loading' | 'downloading' | 'starting' | 'ready' | 'failed', loaded: number, total: number, elapsedMs: number, error?: Error }} EngineProgress
 * @typedef {{ status: string, error: Error | null, info: object | null, bootMs: number | null, fontCss: string, ready: Promise<object>,
 *   render: (language: string, source: string, theme?: string) => Promise<object>, samples: () => Promise<object[]>,
 *   onProgress: (listener: (progress: EngineProgress) => void) => () => void }} PlaygroundEngine
 */
function createEngine(frameworkUrl) {
  const listeners = new Set();
  const started = performance.now();
  let api = null;
  let samplesCache = null;
  let lastProgress = { phase: 'loading', loaded: 0, total: 0, elapsedMs: 0 };

  const emit = (phase, loaded, total, error) => {
    // The runtime can keep reporting downloads after boot has already failed; the failure stays the final state.
    if (engine.status === 'failed' && phase !== 'failed') return;
    lastProgress = { phase, loaded, total, elapsedMs: Math.round(performance.now() - started) };
    if (error) lastProgress.error = error;
    for (const listener of listeners) {
      try { listener(lastProgress); } catch (listenerError) { console.error(listenerError); }
    }
  };

  const engine = {
    status: 'loading',
    error: null,
    info: null,
    bootMs: null,
    fontCss: '',
    ready: null,

    onProgress(listener) {
      listeners.add(listener);
      try { listener(lastProgress); } catch (listenerError) { console.error(listenerError); }
      return () => listeners.delete(listener);
    },

    async render(language, source, theme = 'light') {
      await engine.ready;
      const begin = performance.now();
      let result;
      try {
        result = JSON.parse(api.Render(String(language ?? ''), String(source ?? ''), String(theme ?? 'light')));
      } catch (error) {
        // The engine returns failures as JSON; reaching here means the runtime itself faulted.
        result = { ok: false, svg: null, error: 'Engine fault: ' + (error && error.message ? error.message : String(error)),
          line: null, column: null, diagramType: null, elapsedMs: null, diagnostics: [] };
      }
      result.wallMs = Math.round((performance.now() - begin) * 100) / 100;
      return result;
    },

    async samples() {
      await engine.ready;
      if (!samplesCache) samplesCache = JSON.parse(api.Samples());
      return samplesCache.map((sample) => ({ ...sample }));
    }
  };

  engine.ready = (async () => {
    try {
      emit('loading', 0, 0);
      const base = new URL(frameworkUrl, document.baseURI);
      const { dotnet } = await import(new URL('dotnet.js', base).href);
      const runtime = await dotnet
        .withModuleConfig({
          onDownloadResourceProgress: (loaded, total) => emit(loaded < total ? 'downloading' : 'starting', loaded, total)
        })
        .create();
      const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
      api = EXPORTS_NAMESPACE.reduce((scope, name) => (scope ? scope[name] : undefined), exports);
      if (!api || typeof api.Render !== 'function') {
        throw new Error('The playground engine loaded, but its exports were not found.');
      }
      // The renderer measures with embedded Carlito; preview and isolated SVG images need the same faces.
      engine.fontCss = await loadPreviewFonts();
      engine.info = JSON.parse(api.Info());
      engine.bootMs = Math.round(performance.now() - started);
      engine.status = 'ready';
      emit('ready', lastProgress.total, lastProgress.total);
      return engine.info;
    } catch (cause) {
      const error = cause instanceof Error ? cause : new Error(String(cause));
      engine.status = 'failed';
      engine.error = error;
      emit('failed', lastProgress.loaded, lastProgress.total, error);
      throw error;
    }
  })();

  // Callers that only use render()/samples() still see the failure; avoid an extra unhandled-rejection report.
  engine.ready.catch(() => {});
  return engine;
}

async function loadPreviewFonts() {
  const rules = await Promise.all(PREVIEW_FONTS.map(async ({ file, weight }) => {
    const response = await fetch('/examples/generated/fonts/' + file);
    if (!response.ok) throw new Error('The preview font could not be loaded: ' + file);
    const bytes = new Uint8Array(await response.arrayBuffer());
    const face = new FontFace('Carlito', bytes, { weight, style: 'normal' });
    document.fonts.add(await face.load());
    let binary = '';
    for (const byte of bytes) binary += String.fromCharCode(byte);
    // SVG images have their own font set, so downloads carry the exact measured font bytes.
    return '@font-face{font-family:Carlito;font-style:normal;font-weight:' + weight +
      ';src:url(data:font/ttf;base64,' + btoa(binary) + ') format("truetype")}';
  }));
  await document.fonts.ready;
  return rules.join('\n');
}
