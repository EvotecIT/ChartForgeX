using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>One immutable marks template bound to actual native authored identities, projections, surfaces and built-in readouts.</summary>
    private sealed class CartesianValuePlan : IVisualValuePlan {
        private readonly CartesianValuePolicy _policy;
        private readonly VisualSceneNode[] _template;
        private readonly VisualSemanticRegion[] _regions;
        private readonly VisualDiagnostic[] _diagnostics;
        private readonly VisualSeriesVisibilityCapture.SpanTree _tree;
        private readonly Dictionary<int, (int Series, int Point, int Reading)> _points = new();
        private readonly Dictionary<int, int> _lines = new();
        private readonly IReadOnlyList<VisualValueReading> _readings;
        internal CartesianValuePlan(CartesianValuePolicy policy, VisualScene scene, VisualSceneBuilder.VisualValueBoundary first, VisualSceneBuilder.VisualValueBoundary last) {
            _policy = policy; _template = scene.Nodes.Skip(first.Node).Take(last.Node - first.Node).ToArray();
            _regions = scene.Regions.Skip(first.Region).Take(last.Region - first.Region).ToArray();
            _diagnostics = scene.Diagnostics.Skip(first.Diagnostic).Take(last.Diagnostic - first.Diagnostic).ToArray();
            _tree = new VisualSeriesVisibilityCapture.SpanTree(_template);
            var readings = new List<VisualValueReading>(); var offsets = new int[policy.Series.Length];
            for (var series = 0; series < policy.Series.Length; series++) {
                offsets[series] = readings.Count; var row = policy.Series[series];
                var domain = row.Side == ChartAxisSide.Secondary ? policy.Axes.SecondaryY! : policy.Axes.Y;
                foreach (var point in row.Points) readings.Add(new VisualValueReading(point.Id, point.Source.Y, domain.Minimum, domain.Maximum, row.Key));
            }
            _readings = Array.AsReadOnly(readings.ToArray());
            for (var index = 0; index < _template.Length; index++) {
                if (_template[index] is VisualSceneGroup point && point.Role == "point"
                    && ReadOrdinal(point, "data-cfx-series", out var series) && ReadOrdinal(point, "data-cfx-point", out var ordinal)) {
                    if (series < 0 || series >= policy.Series.Length || ordinal < 0 || ordinal >= policy.Series[series].Points.Length
                        || !point.Metadata.TryGetValue("data-cfx-series-key", out var key) || key != policy.Series[series].Key
                        || !point.Metadata.TryGetValue("data-cfx-observation-id", out var id) || id != policy.Series[series].Points[ordinal].Id)
                        throw new InvalidOperationException("Native Cartesian value capture lost its authored point binding.");
                    _points.Add(index, (series, ordinal, offsets[series] + ordinal));
                } else if (_template[index] is VisualScenePath path && path.Role != null && path.Role.StartsWith("line", StringComparison.Ordinal)) {
                    for (var ancestor = _tree.Parent[index]; ancestor >= 0; ancestor = _tree.Parent[ancestor]) {
                        if (_template[ancestor] is VisualSceneGroup parent && parent.Role == "series" && ReadOrdinal(parent, "data-cfx-series", out var owner)) {
                            _lines.Add(index, owner); break;
                        }
                    }
                }
            }
            if (_points.Count != readings.Count) throw new InvalidOperationException("Native Cartesian value capture requires every authored point exactly once.");
        }
        public string Kind => "cartesian-value";
        public string SeriesKey => _policy.Series[0].Key;
        public VisualSize Size => _policy.Context.Layout.Size;
        public FontSpec Font => _policy.Context.Font;
        public IReadOnlyList<VisualValueReading> Readings => _readings;
        public bool AllowsOutOfDomainReadings => false;
        public bool SamePolicy(IVisualValuePlan other) => other is CartesianValuePlan plan && _policy.Same(plan._policy);
        public void Paint(VisualSceneBuilder builder, double[]? readings = null, bool captured = false) {
            var values = readings ?? _readings.Select(row => row.Value).ToArray();
            if (values.Length != _readings.Count) throw new ArgumentException("Native Cartesian reading count changed.", nameof(readings));
            for (var index = 0; index < values.Length; index++) if (!ChartMath.IsFinite(values[index]) || values[index] < _readings[index].Minimum || values[index] > _readings[index].Maximum)
                throw new NotSupportedException("Native Cartesian value readings retain their captured raw axis domains.");
            var native = new ChartPoint[_policy.Series.Length][]; var reading = 0;
            for (var series = 0; series < native.Length; series++) {
                var row = _policy.Series[series]; native[series] = new ChartPoint[row.Points.Length];
                for (var point = 0; point < row.Points.Length; point++) native[series][point] = new ChartPoint(row.Points[point].X, row.Map.Y(values[reading++]), row.Points[point].Source.BreakBefore);
            }
            var output = new List<VisualSceneNode>(_template.Length); var regions = new Dictionary<string, VisualSemanticRegion>(StringComparer.Ordinal);
            for (var index = 0; index < _template.Length; index++) {
                if (_points.TryGetValue(index, out var binding)) {
                    var series = _policy.Series[binding.Series]; var point = series.Points[binding.Point]; var y = values[binding.Reading]; var position = native[binding.Series][binding.Point];
                    var group = (VisualSceneGroup)_template[index]; var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var item in group.Metadata) metadata.Add(item.Key, item.Value);
                    var caption = point.FixedDisplay == null ? _policy.Format.Format(y) : string.Empty; var rawCaption = point.FixedDisplay ?? caption;
                    var display = TextCaseTransformer.Apply(rawCaption, point.TextCase, CultureInfo.InvariantCulture);
                    var description = series.Name + ": " + display + " (" + ChartNumericFormatter.FormatCoordinate(point.Source.X) + ", " + ChartNumericFormatter.FormatCoordinate(y) + ")";
                    metadata["data-cfx-y"] = Number(y); metadata["data-cfx-label"] = display; metadata["aria-label"] = description;
                    var numericCaption = _policy.NumericCaption && point.FixedDisplay == null ? caption : null;
                    if (numericCaption != null) metadata["data-cfx-formatted-y"] = numericCaption;
                    metadata["data-cfx-formatted-value"] = series.Readout(y, numericCaption);
                    // Frames announce the same formatted name as the prepared chart they interpolate between.
                    if (VisualSceneBuilder.FormattedPointDescription(series.Name, metadata) is { } formatted) metadata["aria-label"] = description = formatted;
                    var bounds = point.MarkerPaint.Bounds(position.X, position.Y, point.Radius);
                    var child = new VisualSceneBuilder(Size, Font);
                    if (series.Kind == ChartSeriesKind.Bar) {
                        var direction = series.Direction(y, position.Y);
                        bounds = VerticalBarBounds(point.Bar.Style, point.Left, point.Width, series.Baseline, position.Y, direction);
                        DrawBarSurface(child, bounds, point.Color, point.Bar, direction: direction);
                    } else if (point.Marker && point.Radius > 0 && (!_policy.Clip || position.X >= _policy.Plot.Left && position.X <= _policy.Plot.Right
                        && position.Y >= _policy.Plot.Top && position.Y <= _policy.Plot.Bottom)) point.MarkerPaint.Paint(child, position.X, position.Y, point.Radius);
                    output.Add(new VisualSceneGroup(group.Role, group.Id, group.Clip, metadata, group.PathClip, group.Rotation, group.Href,
                        group.Tooltip, group.Translation, group.ImageResource, group.ColorScope, group.Opacity));
                    output.AddRange(child.Build().Nodes); output.Add(new VisualSceneEndGroup());
                    regions.Add(group.Id!, new VisualSemanticRegion(group.Id!, "point", bounds, description));
                    index = _tree.End[index]; continue;
                }
                if (_lines.TryGetValue(index, out var lineSeries)) {
                    var source = (VisualScenePath)_template[index]; var series = _policy.Series[lineSeries];
                    output.Add(new VisualScenePath(ChartPathBuilder.FromPoints(native[lineSeries], series.Interpolation, series.Step), source.Close,
                        source.Fill, source.Stroke, source.StrokeWidth, source.Role, source.Id, source.Dash?.ToArray(), source.Cap, source.Join, source.Paint, source.DashOffset, source.Opacity));
                } else output.Add(_template[index]);
            }
            builder.Append(new VisualScene(Size, output, _diagnostics, _regions.Select(region => regions.TryGetValue(region.Id, out var row) ? row : region).ToArray()));
        }
        private static bool ReadOrdinal(VisualSceneGroup group, string key, out int number) {
            number = -1; return group.Metadata.TryGetValue(key, out var text) && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number);
        }
    }
}
