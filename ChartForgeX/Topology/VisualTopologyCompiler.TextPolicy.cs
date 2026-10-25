using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private TextStyleOverride? _textPolicy;
    private Dictionary<int, VisualSceneTextFace>? _textPolicyFaces;
    private IDisposable UseTextPolicy(TextStyleOverride? policy) {
        if (policy?.HasOverrides != true && _textPolicy == null) return EmptyTextPolicyScope.Instance;
        var prior = _textPolicy; var priorFaces = _textPolicyFaces;
        _textPolicy = policy?.HasOverrides == true ? policy : null; _textPolicyFaces = null;
        return new TextPolicyScope(() => { _textPolicy = prior; _textPolicyFaces = priorFaces; });
    }
    private double TextPolicySize(double size) => TopologyTextPolicy.Size(_textPolicy, size, _context.Theme.Typography.DataLabelSize);
    private TextStyle TextPolicyStyle(double size, int weight) {
        var style = TopologyTextPolicy.Resolve(_textPolicy, _context.Font, size, weight);
        // Callers have already selected and fitted the logical size. Never reapply the authored preferred size here.
        style.FontSize = size; return style;
    }
    private TextMetrics MeasurePolicyText(string text, double size, int weight) =>
        _textPolicy == null || !_textPolicy.HasOverrides ? _builder.MeasureText(text, size, weight)
            : TextPolicyFace(weight).Prepare(text, size).Metrics;
    private double PolicyTextAscent(double size, int weight) =>
        _textPolicy == null || !_textPolicy.HasOverrides ? _builder.TextAscent(size, weight)
            : TextPolicyFace(weight).Ascent(size);
    private void DrawPolicyText(string text, double x, double y, double size, ChartColor color, int weight,
        string? role = null, string? id = null, TextAlignment alignment = TextAlignment.Left, ChartForgeX.Themes.SvgPaint? paint = null) {
        if (_textPolicy == null || !_textPolicy.HasOverrides) _builder.Text(text, x, y, size, color, weight, role, id, alignment, paint);
        else { var style = TextPolicyStyle(size, weight); style.Color = color; style.Alignment = alignment; _builder.Text(text, x, y, style, role, id, paint: paint); }
    }
    private VisualSceneTextFace TextPolicyFace(int weight) {
        _textPolicyFaces ??= new Dictionary<int, VisualSceneTextFace>();
        if (!_textPolicyFaces.TryGetValue(weight, out var face)) {
            face = new VisualSceneTextFace(TextPolicyStyle(1, weight)); _textPolicyFaces.Add(weight, face);
        }
        return face;
    }
    private sealed class EmptyTextPolicyScope : IDisposable {
        internal static readonly EmptyTextPolicyScope Instance = new();
        public void Dispose() { }
    }
    private sealed class TextPolicyScope : IDisposable {
        private Action? _restore;
        internal TextPolicyScope(Action restore) => _restore = restore;
        public void Dispose() { var restore = _restore; _restore = null; restore?.Invoke(); }
    }
}
