using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ChartForgeX.Rendering;

/// <summary>Owns a native value subscene and its detached typed policy; no SVG or DOM facts are read.</summary>
internal sealed partial class VisualValueCapture {
    private readonly int _first, _last, _firstRegion, _lastRegion, _firstDiagnostic, _lastDiagnostic;
    internal VisualValueCapture(IVisualValuePlan plan, int first, int last,
        int firstRegion, int lastRegion, int firstDiagnostic, int lastDiagnostic) {
        Plan = plan; _first = first; _last = last; _firstRegion = firstRegion; _lastRegion = lastRegion;
        _firstDiagnostic = firstDiagnostic; _lastDiagnostic = lastDiagnostic;
    }
    internal IVisualValuePlan Plan { get; }
    internal PreparedVisual Frame(PreparedVisual basis, double[] values) {
        if (basis.Scene.VisibilityCapture != null) return basis.Scene.VisibilityCapture.ValueFrame(basis, values);
        if (basis.TryRemoveRootEffects(out var bare, out var effects))
            return Frame(bare, values).WithRootEffects(effects);
        if (Plan is IVisualCompleteValuePlan complete) return complete.Frame(basis, values);
        var builder = new VisualSceneBuilder(Plan.Size, Plan.Font);
        Plan.Paint(builder, values, captured: true);
        var rows = builder.Build(); var scene = basis.Scene;
        var nodes = scene.Nodes.Take(_first).Concat(rows.Nodes).Concat(scene.Nodes.Skip(_last)).ToArray();
        var regions = scene.Regions.Take(_firstRegion).Concat(rows.Regions).Concat(scene.Regions.Skip(_lastRegion)).ToArray();
        var diagnostics = scene.Diagnostics.Take(_firstDiagnostic).Concat(rows.Diagnostics).Concat(scene.Diagnostics.Skip(_lastDiagnostic)).ToArray();
        return basis.WithNativeFrame(new VisualScene(scene.Size, nodes, diagnostics, regions, scene.DataProjection));
    }

    /// <summary>Captures at most sixteen complete native observations for one identified supported value edit.</summary>
    internal static IReadOnlyList<PreparedVisual> Frames(PreparedVisual from, PreparedVisual to, int frameCount, CancellationToken cancellationToken = default) {
        return FramesCore(from, to, frameCount, true, null, cancellationToken);
    }

    /// <summary>Captures the same finite native plans for one or more identified edits, optionally flashing accepted paint.</summary>
    internal static IReadOnlyList<PreparedVisual> NativeFrames(PreparedVisual from, PreparedVisual to, int frameCount,
        ChartForgeX.Primitives.ChartColor? flashColor, CancellationToken cancellationToken = default) =>
        FramesCore(from, to, frameCount, false, flashColor, cancellationToken);

    private static IReadOnlyList<PreparedVisual> FramesCore(PreparedVisual from, PreparedVisual to, int frameCount,
        bool oneReading, ChartForgeX.Primitives.ChartColor? flashColor, CancellationToken cancellationToken) {
        if (from == null) throw new ArgumentNullException(nameof(from));
        if (to == null) throw new ArgumentNullException(nameof(to));
        if (frameCount < 2 || frameCount > 16) throw new ArgumentOutOfRangeException(nameof(frameCount), "Native value transitions require 2..16 captured frames.");
        cancellationToken.ThrowIfCancellationRequested();
        var first = from.Scene.ValueCapture; var last = to.Scene.ValueCapture;
        if ((first == null && from.NativeTextCapture?.SupportsCapturedReplay == false) || (last == null && to.NativeTextCapture?.SupportsCapturedReplay == false))
            throw new NotSupportedException("Native complete value text capture requires covered glyphs and finite, stable captured font/fallback decisions; ordinary rendering remains available.");
        if (first == null || last == null || (first._last <= first._first && first.Plan is not IVisualSourceValuePlan { CapturesRawSource: true }) || (last._last <= last._first && last.Plan is not IVisualSourceValuePlan { CapturesRawSource: true }) || !first.Plan.SamePolicy(last.Plan))
            throw new NotSupportedException("Native value transitions require matching captured typed rows, authored identities, domains, built-in format and native faces.");
        if (from.CapturedRootEffectIdentity != to.CapturedRootEffectIdentity)
            throw new NotSupportedException("Native value endpoints require the same raw ordered captured appearance policy.");
        var before = first.Plan.Readings.Select(row => row.Value).ToArray(); var after = last.Plan.Readings.Select(row => row.Value).ToArray(); var changed = 0;
        for (var index = 0; index < before.Length; index++) {
            var row = first.Plan.Readings[index];
            if (!first.Plan.AllowsOutOfDomainReadings && (before[index] < row.Minimum || before[index] > row.Maximum || after[index] < row.Minimum || after[index] > row.Maximum))
                throw new NotSupportedException("Native value transition readings must be within their captured row domains.");
            if (before[index] != after[index]) changed++;
        }
        if (oneReading && first.Plan is IVisualComponentValuePlan)
            throw new NotSupportedException("Typed component values require PrepareNativeTransitionJson and their captured series/observation/component source facts.");
        if (oneReading && first.Plan is IVisualRawValuePlan { CapturesRawSource: true })
            throw new NotSupportedException("Opt-in raw source values require PrepareNativeTransitionJson and its complete native source facts.");
        if (oneReading && changed != 1) throw new NotSupportedException("A native value transition changes exactly one identified row value.");
        if (!oneReading && changed == 0) throw new NotSupportedException("A native transition changes at least one identified reading.");
        if (flashColor.HasValue && frameCount < 3) throw new ArgumentOutOfRangeException(nameof(frameCount), "A native flash needs at least three captured observations.");
        // Verify endpoint paint using the same native policy, without parsing either SVG or
        // retaining a mutable Chart. This also rejects changed frame, label, paint and layout policy.
        const string scope = "cfx-value-transition-check";
        if (first.Frame(from, before).ToSvg(scope) != from.ToSvg(scope)
            || first.Frame(from, after).ToSvg(scope) != to.ToSvg(scope))
            throw new NotSupportedException("Native value transition endpoints must share their complete captured frame and presentation policy.");
        var frames = new PreparedVisual[frameCount]; frames[0] = flashColor.HasValue ? to : from; frames[frameCount - 1] = to;
        var changedInk = flashColor.HasValue ? new FlashSelection(first.Plan, last.Plan, from, to) : null;
        for (var frame = 1; frame < frameCount - 1; frame++) {
            cancellationToken.ThrowIfCancellationRequested(); var ratio = frame / (double)(frameCount - 1);
            var readings = new double[before.Length];
            for (var row = 0; row < readings.Length; row++) readings[row] = flashColor.HasValue ? after[row] : ChartMath.InterpolateRange(before[row], after[row], ratio);
            var observation = flashColor.HasValue ? to : first.Frame(from, readings);
            var peakIndex = (frameCount - 1) / 2;
            var flashAmount = frame <= peakIndex ? frame / (double)peakIndex : (frameCount - 1 - frame) / (double)(frameCount - 1 - peakIndex);
            frames[frame] = flashColor.HasValue ? Flash(observation, changedInk!, flashColor.Value, flashAmount) : observation;
        }
        cancellationToken.ThrowIfCancellationRequested(); return Array.AsReadOnly(frames);
    }
}
