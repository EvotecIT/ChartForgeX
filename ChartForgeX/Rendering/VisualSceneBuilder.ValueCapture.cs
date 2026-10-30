using System;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    private VisualValueCapture? _valueCapture;
    private Func<VisualScene, IVisualValuePlan?>? _pendingValuePlan;
    private VisualValueBoundary _valueFirst, _valueLast;
    internal readonly struct VisualValueBoundary {
        internal VisualValueBoundary(int node, int region, int diagnostic) { Node = node; Region = region; Diagnostic = diagnostic; }
        internal int Node { get; }
        internal int Region { get; }
        internal int Diagnostic { get; }
    }
    internal VisualValueBoundary ValueBoundary() => new(_nodes.Count, _regions.Count, _diagnostics.Count);
    internal void CaptureValue(IVisualValuePlan plan) {
        var first = _nodes.Count; var region = _regions.Count; var diagnostic = _diagnostics.Count;
        plan.Paint(this);
        _valueCapture = new VisualValueCapture(plan, first, _nodes.Count, region, _regions.Count,
            diagnostic, _diagnostics.Count);
    }
    /// <summary>Defers only immutable template binding until native readouts and authored identities are complete.</summary>
    internal void CaptureExistingValue(Func<VisualScene, IVisualValuePlan?> bind, VisualValueBoundary first, VisualValueBoundary last) {
        if (bind == null) throw new ArgumentNullException(nameof(bind));
        if (first.Node < 0 || last.Node <= first.Node || last.Node > _nodes.Count || first.Region < 0 || last.Region < first.Region || last.Region > _regions.Count
            || first.Diagnostic < 0 || last.Diagnostic < first.Diagnostic || last.Diagnostic > _diagnostics.Count) throw new ArgumentOutOfRangeException(nameof(first));
        _pendingValuePlan = bind; _valueFirst = first; _valueLast = last;
    }
    internal void CompleteValueIdentity() {
        var bind = _pendingValuePlan; if (bind == null) return;
        _pendingValuePlan = null; var plan = bind(Build());
        if (plan is IVisualCompleteValuePlan) _valueCapture = new VisualValueCapture(plan, 0, _nodes.Count, 0, _regions.Count, 0, _diagnostics.Count);
        else if (plan != null) _valueCapture = new VisualValueCapture(plan, _valueFirst.Node, _valueLast.Node,
            _valueFirst.Region, _valueLast.Region, _valueFirst.Diagnostic, _valueLast.Diagnostic);
    }
}
