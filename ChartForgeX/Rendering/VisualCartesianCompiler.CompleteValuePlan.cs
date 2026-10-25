using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Owns full copied authored rows; every frame uses the native stack/path/label/readout compiler.</summary>
    private sealed class CompleteCartesianValuePlan : IVisualCompleteValuePlan {
        private readonly ChartVisibilitySource _source;
        private readonly VisualRenderContext _context;
        private readonly VisualSceneTextCapture _text;
        private readonly ChartCartesianProjection _axes;
        private readonly string _policy, _fonts;
        private readonly IReadOnlyList<VisualValueReading> _readings;
        internal CompleteCartesianValuePlan(ChartVisibilitySource source, VisualRenderContext context,
            VisualSceneTextCapture text, ChartCartesianProjection axes) {
            _source = source; _context = context; _text = text; _axes = axes;
            _policy = VisualArtifactInterchangeJson.SerializeVisibilitySource(source, context, includeValues: false);
            _fonts = text.CanonicalFacts(includeRuns: false);
            _readings = Array.AsReadOnly(source.SeriesRows.SelectMany(row => row.Points.Select((point, index) => {
                var axis = row.ValueAxis == ChartAxisSide.Secondary ? axes.SecondaryY! : axes.Y;
                return new VisualValueReading(row.ObservationIds[index].Value, point.Y, axis.Minimum, axis.Maximum, row.Id.Value);
            })).ToArray());
        }
        // Preserve keeps full-source stack geometry and shared totals; only series ink/owned point labels change opacity.
        internal bool SupportsJointSeriesValues => _source.SeriesRows.All(row => row.Kind is ChartSeriesKind.Line or ChartSeriesKind.Bar);
        public string Kind => "cartesian-value";
        public string SeriesKey => _source.SeriesRows[0].Id.Value;
        public VisualSize Size => _context.Layout.Size;
        public FontSpec Font => _context.Font;
        public IReadOnlyList<VisualValueReading> Readings => _readings;
        public bool AllowsOutOfDomainReadings => false;
        public bool SamePolicy(IVisualValuePlan other) => other is CompleteCartesianValuePlan plan
            && _policy == plan._policy && _fonts == plan._fonts && SameAxes(_axes, plan._axes);
        public void Paint(VisualSceneBuilder builder, double[]? readings = null, bool captured = false) =>
            throw new InvalidOperationException("Complete Cartesian values use the native chart compilation owner.");
        public PreparedVisual Frame(PreparedVisual basis, double[] values) {
            if (values.Length != _readings.Count || values.Where((value, index) => !ChartMath.IsFinite(value)
                || value < _readings[index].Minimum || value > _readings[index].Maximum).Any())
                throw new NotSupportedException("Complete Cartesian readings retain their captured finite raw native axis domains.");
            var result = _source.CreateNativeValueView((double[])values.Clone())
                .Prepare(_context.WithNativeTextCapture(_text.ReplayCapturedClusters()));
            if (!VisualSeriesVisibilityCapture.SameAxis(_axes.X, result.XProjection)
                || !VisualSeriesVisibilityCapture.SameAxis(_axes.Y, result.YProjection)
                || !VisualSeriesVisibilityCapture.SameAxis(_axes.SecondaryX, result.SecondaryXProjection)
                || !VisualSeriesVisibilityCapture.SameAxis(_axes.SecondaryY, result.SecondaryYProjection))
                throw new NotSupportedException("Complete Cartesian value frames must retain the exact accepted native axis and plot basis; changed layout needs an explicit source/destination frame plan.");
            // Fresh native captions, accessibility, regions and projections remain owned by this frame.
            return result.WithCapturedExportPolicy(basis);
        }
        private static bool SameAxes(ChartCartesianProjection first, ChartCartesianProjection second) => first.Horizontal == second.Horizontal
            && VisualSeriesVisibilityCapture.SameAxis(first.X, second.X) && VisualSeriesVisibilityCapture.SameAxis(first.Y, second.Y)
            && VisualSeriesVisibilityCapture.SameAxis(first.SecondaryX, second.SecondaryX) && VisualSeriesVisibilityCapture.SameAxis(first.SecondaryY, second.SecondaryY);
    }
}
