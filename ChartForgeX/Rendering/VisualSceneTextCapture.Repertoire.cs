using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneTextCapture {
    /// <summary>Attempts a detached extension of actual fallback decisions before freeze. Failure preserves the incumbent stable-catalog plan.</summary>
    internal VisualSceneTextCapture TryCaptureValueRepertoire(ChartValueFormat format, IEnumerable<(string Owner, string Text)> nativeObservations) {
        if (_mode != CaptureMode.Recording || !SupportsCapturedReplay
            || !format.TryGetNativeTextRepertoire(out var owner, out var values)
            || _requests.Values.Any(request => request.Cases.Any(value => !Enum.IsDefined(typeof(TextCaseTransform), value)))) return this;
        var candidate = new VisualSceneTextCapture(); candidate._strict = _strict; candidate._captured = _captured;
        foreach (var pair in _requests) candidate._requests.Add(pair.Key, pair.Value.ContinueRecording());
        candidate._runs.AddRange(_runs);
        try {
            var observations = BoundedNativeObservations(values.Select(value => (Owner: owner, Text: value)).Concat(nativeObservations));
            foreach (var request in candidate._requests.Values) foreach (var textCase in request.Cases.OrderBy(value => value)) {
                var style = request.Style; style.TextCase = textCase;
                // Latent observations extend captured replay. The incumbent strict-primary flag still describes real measured runs.
                var face = new VisualSceneTextFace(style, request.Face, false, candidate.ValidateCapturedRun, request.Clusters);
                foreach (var observation in observations) {
                    var prepared = face.Prepare(observation.Text, style.EffectiveFontSize);
                    if (!candidate.SupportsCapturedReplay) return this;
                    request.ObserveProbe(observation.Owner, observation.Text, textCase,
                        string.Join("\n", prepared.Lines.Select(line => line.Text)),
                        textCase is TextCaseTransform.TitleCase or TextCaseTransform.SentenceCase or TextCaseTransform.ToggleCase || !observation.Owner.StartsWith("value-format-invariant-", StringComparison.Ordinal) && !observation.Owner.StartsWith("radial-", StringComparison.Ordinal) ? string.Empty : null);
                }
            }
        } catch (NotSupportedException) { return this; }
        catch (ArgumentException) { return this; }
        catch (InvalidOperationException) { return this; }
        return candidate.SupportsCapturedReplay ? candidate : this;
    }

    /// <summary>Commits only the successful same-face recording extension; caller charts and registry lookups never escape into replay.</summary>
    internal void AdoptValueRepertoire(VisualSceneTextCapture candidate) {
        if (_mode != CaptureMode.Recording || candidate._mode != CaptureMode.Recording || !candidate.SupportsCapturedReplay
            || _requests.Count != candidate._requests.Count || _requests.Any(pair => !candidate._requests.TryGetValue(pair.Key, out var next) || (!ReferenceEquals(pair.Value.Face.Font, next.Face.Font) || pair.Value.Face.SynthesizeBold != next.Face.SynthesizeBold || pair.Value.Face.SynthesizeItalic != next.Face.SynthesizeItalic)))
            throw new InvalidOperationException("A native value repertoire must extend the same actual captured font requests before freeze.");
        _requests.Clear(); foreach (var pair in candidate._requests) _requests.Add(pair.Key, pair.Value);
        _strict = candidate._strict; _captured = candidate._captured;
    }

    /// <summary>Records latent native family requests transactionally through the same builder and captured face owner.</summary>
    internal VisualSceneTextCapture CaptureNativeRequests(Action<VisualSceneBuilder> observe, VisualSize size, FontSpec font) {
        if (_mode != CaptureMode.Recording) throw new InvalidOperationException("Native requests must be observed before capture freezes.");
        var candidate = new VisualSceneTextCapture(); candidate._strict = _strict; candidate._captured = _captured;
        foreach (var pair in _requests) candidate._requests.Add(pair.Key, pair.Value.ContinueRecording());
        candidate._runs.AddRange(_runs); observe(new VisualSceneBuilder(size, font, candidate));
        if (!candidate.SupportsCapturedReplay) throw new NotSupportedException("Native latent text requires captured covered faces and bounded fallback decisions.");
        return candidate;
    }
    internal void AdoptNativeRequests(VisualSceneTextCapture candidate) {
        if (_mode != CaptureMode.Recording || candidate._mode != CaptureMode.Recording || !candidate.SupportsCapturedReplay
            || _requests.Any(pair => !candidate._requests.TryGetValue(pair.Key, out var next) || !ReferenceEquals(pair.Value.Face.Font, next.Face.Font)
                || pair.Value.Face.SynthesizeBold != next.Face.SynthesizeBold || pair.Value.Face.SynthesizeItalic != next.Face.SynthesizeItalic))
            throw new InvalidOperationException("Native request capture must preserve every previously captured face.");
        _requests.Clear(); foreach (var pair in candidate._requests) _requests.Add(pair.Key, pair.Value);
        _runs.Clear(); _runs.AddRange(candidate._runs);
        _strict = candidate._strict; _captured = candidate._captured;
    }

    internal bool HasContextCaseRequests => _requests.Values.Any(request => request.Cases.Any(value => value is TextCaseTransform.TitleCase or TextCaseTransform.SentenceCase or TextCaseTransform.ToggleCase));
    private static IReadOnlyList<(string Owner, string Text)> BoundedNativeObservations(IEnumerable<(string Owner, string Text)> source) {
        var rows = new List<(string Owner, string Text)>(); var distinct = new HashSet<(string Owner, string Text)>(); var scalars = 0;
        foreach (var row in source) {
            if (!distinct.Add(row)) continue;
            var count = NativeScalarCount(row.Text);
            if (rows.Count >= TextShaper.CapturedClusters.MaximumClusters || count > TextShaper.CapturedClusters.MaximumScalars - scalars)
                throw new NotSupportedException("Native formatter/context observations exceed the captured request budget.");
            rows.Add(row); scalars += count;
        }
        return rows.OrderBy(row => row.Owner, StringComparer.Ordinal).ThenBy(row => row.Text, StringComparer.Ordinal).ToArray();
    }
    private static int NativeScalarCount(string value) {
        var count = 0; for (var index = 0; index < value.Length;) { _ = TrueTypeFont.ReadCodePoint(value, ref index); if (++count > TextShaper.CapturedClusters.MaximumScalars) break; } return count;
    }

    internal sealed partial class Request {
        private readonly HashSet<TextCaseTransform> _cases = new();
        private readonly List<ValueTextProbe> _probes = new();
        private int _probeScalars;
        private bool _factsImmutable;
        internal IReadOnlyCollection<TextCaseTransform> Cases => _cases.ToArray();
        internal IReadOnlyList<ValueTextProbe> Probes => _probes.AsReadOnly();
        internal void ObserveCase(TextCaseTransform value) {
            if (_factsImmutable) throw new InvalidOperationException("Captured native value text facts are immutable.");
            _cases.Add(value);
        }
        internal void ObserveProbe(string owner, string raw, TextCaseTransform textCase, string displayed, string? caseCulture = null) {
            if (_factsImmutable) throw new InvalidOperationException("Captured native value text facts are immutable.");
            var rawScalars = NativeScalarCount(raw); var displayedScalars = NativeScalarCount(displayed);
            if (_probes.Count >= TextShaper.CapturedClusters.MaximumClusters || rawScalars > TextShaper.CapturedClusters.MaximumScalars
                || displayedScalars > TextShaper.CapturedClusters.MaximumScalars - rawScalars
                || _probeScalars > TextShaper.CapturedClusters.MaximumScalars - rawScalars - displayedScalars)
                throw new NotSupportedException("Native value text observations exceed the captured per-request cluster/scalar budget.");
            _probes.Add(new ValueTextProbe(owner, raw, textCase, displayed, caseCulture)); _probeScalars += rawScalars + displayedScalars;
        }
        internal Request ContinueRecording() => CopyFacts(new Request(_style, Face, Clusters.ContinueRecording()));
        private Request CopyFacts(Request target, bool immutable = false) {
            target._cases.Clear(); foreach (var value in _cases) target._cases.Add(value);
            target._probes.AddRange(_probes); target._probeScalars = _probeScalars; target._factsImmutable = immutable; return target;
        }
    }
    internal sealed class ValueTextProbe {
        internal ValueTextProbe(string owner, string raw, TextCaseTransform textCase, string displayed, string? caseCulture = null) {
            Owner = owner; Raw = raw; Case = textCase; Displayed = displayed; CaseCulture = caseCulture;
        }
        internal string Owner { get; }
        internal string Raw { get; }
        internal TextCaseTransform Case { get; }
        internal string Displayed { get; }
        internal string? CaseCulture { get; }
    }
}
