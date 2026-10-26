using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

/// <summary>Opt-in recording/replay at the existing face owner. Every measurement, ascent and painted request uses the same captured resolution.</summary>
internal sealed partial class VisualSceneTextCapture {
    private readonly Dictionary<string, Request> _requests;
    private readonly List<VisualScenePreparedText> _runs;
    private enum CaptureMode { Recording, Snapshot, StrictReplay, CapturedReplay, TopologyContinuation }
    private readonly CaptureMode _mode;
    private bool _captured = true;
    private bool _strict = true;
    internal VisualSceneTextCapture() { _requests = new(StringComparer.Ordinal); _runs = new(); }
    private VisualSceneTextCapture(VisualSceneTextCapture source, CaptureMode mode) {
        _requests = source._requests.ToDictionary(pair => pair.Key, pair => mode == CaptureMode.TopologyContinuation ? pair.Value.ContinueTopology() : pair.Value.Snapshot(mode == CaptureMode.CapturedReplay), StringComparer.Ordinal);
        _runs = mode is CaptureMode.CapturedReplay or CaptureMode.TopologyContinuation ? new() : new(source._runs); _strict = source._strict; _captured = source._captured; _mode = mode;
    }
    internal bool IsSnapshot => _mode == CaptureMode.Snapshot;
    internal bool IsReplay => _mode is CaptureMode.StrictReplay or CaptureMode.CapturedReplay;
    internal bool SupportsCapturedReplay => _captured && _requests.Count > 0 && _requests.Values.All(request => request.Clusters.Supported);
    internal bool SupportsStrictPrimary => _strict && _requests.Count > 0;
    internal IReadOnlyList<Request> Requests => _requests.Values.OrderBy(row => row.PublicKey, StringComparer.Ordinal).ToArray();
    internal IReadOnlyList<VisualScenePreparedText> Runs => _runs.AsReadOnly();
    internal VisualSceneTextCapture Freeze() => new(this, CaptureMode.Snapshot);
    internal VisualSceneTextCapture ContinueTopology() => new(this, CaptureMode.TopologyContinuation);
    internal VisualSceneTextCapture Replay() {
        if (!SupportsStrictPrimary) throw new NotSupportedException("Native complete value frames require captured primary-only text; fallback-dependent replay remains unsupported in this cohort.");
        return new VisualSceneTextCapture(this, CaptureMode.StrictReplay);
    }
    internal VisualSceneTextCapture ReplayCapturedClusters() {
        if (!SupportsCapturedReplay) throw new NotSupportedException("Native captured text replay requires covered, stable fallback decisions within the native cluster/scalar limits.");
        return new VisualSceneTextCapture(this, CaptureMode.CapturedReplay);
    }
    internal VisualSceneTextFace Bind(TextStyle style) {
        if (_mode == CaptureMode.Snapshot) throw new InvalidOperationException("A captured native font snapshot is immutable; select an explicit replay mode.");
        var key = VisualArtifactInterchangeJson.NativeTextRequestKey(style, includeFilePath: true);
        if (!_requests.TryGetValue(key, out var request)) {
            if (IsReplay) throw new NotSupportedException("A native value frame requested an uncaptured font/language policy.");
            var face = new VisualSceneTextFace(style, useAttachedCapture: false).CapturedFace;
            request = new Request(style, face); _requests.Add(key, request);
            if (face.Font == null || face.Font.PaletteContext != null) { _strict = false; _captured = false; }
        }
        if (_mode is CaptureMode.Recording or CaptureMode.TopologyContinuation) request.ObserveCase(style.TextCase);
        Action<VisualScenePreparedText>? record = _mode is CaptureMode.Recording or CaptureMode.TopologyContinuation ? Record : _mode == CaptureMode.CapturedReplay ? RecordReplay : null;
        return new VisualSceneTextFace(style, request.Face, _mode == CaptureMode.StrictReplay, record,
            _mode is CaptureMode.Recording or CaptureMode.CapturedReplay or CaptureMode.TopologyContinuation ? request.Clusters : null);
    }
    private void Record(VisualScenePreparedText prepared) {
        _runs.Add(prepared); ObserveRun(prepared);
    }
    private void ObserveRun(VisualScenePreparedText prepared) {
        ValidateCapturedRun(prepared);
        if (prepared.Face.Font == null) { _strict = false; return; }
        foreach (var line in prepared.Lines) {
            try {
                var strict = TextShaper.ShapeWithLetterSpacing(prepared.Face.Font, line.Text, prepared.Size, prepared.Style.LetterSpacing, strictPrimary: true);
                if (strict.Count != line.Glyphs.Count) { _strict = false; continue; }
                for (var index = 0; index < strict.Count; index++) {
                    var a = strict[index]; var b = line.Glyphs[index];
                    if (!ReferenceEquals(a.Face, b.Face) || a.Glyph != b.Glyph || a.SourceIndex != b.SourceIndex
                        || a.Advance != b.Advance || a.OffsetX != b.OffsetX || a.OffsetY != b.OffsetY || a.TrailingAdvance != b.TrailingAdvance) _strict = false;
                }
            } catch (NotSupportedException) { _strict = false; }
        }
    }
    private void ValidateCapturedRun(VisualScenePreparedText prepared) {
        if (prepared.Face.Font == null || prepared.Lines.Any(line => line.Glyphs.Any(glyph => glyph.Glyph == 0))) _captured = false;
    }
    private void RecordReplay(VisualScenePreparedText prepared) {
        _runs.Add(prepared); ValidateCapturedRun(prepared);
        if (!_captured) throw new NotSupportedException("A captured native frame produced an uncovered glyph; missing text is not a replay fallback.");
    }
    internal string CanonicalFacts(bool includeRuns) => VisualArtifactInterchangeJson.SerializeNativeTextCapture(this, includeRuns);
    internal sealed partial class Request {
        private readonly TextStyle _style;
        internal Request(TextStyle style, ResolvedTypeface face, TextShaper.CapturedClusters? clusters = null) {
            _style = style.Clone(); _style.Font = style.Font.WithoutCapturedText(); Face = face; Clusters = clusters ?? new TextShaper.CapturedClusters(); ObserveCase(style.TextCase); PublicKey = VisualArtifactInterchangeJson.NativeTextRequestKey(style, includeFilePath: false);
        }
        internal TextShaper.CapturedClusters Clusters { get; }
        internal Request ContinueTopology() => CopyFacts(new Request(_style, Face, Clusters.ContinueCaptured()));
        internal Request Snapshot(bool replay) => CopyFacts(new Request(_style, Face, replay ? Clusters.Replay() : Clusters.Freeze()), immutable: true);
        internal string PublicKey { get; }
        internal TextStyle Style => _style.Clone();
        internal ResolvedTypeface Face { get; }
    }
}
