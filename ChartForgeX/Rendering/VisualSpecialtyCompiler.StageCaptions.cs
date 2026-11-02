using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    /// <summary>Owns independent caption requests; geometry remains in the existing Funnel/Pyramid layouts.</summary>
    private sealed class StageCaptions {
        private const double RailGap = LabelPlacementService.DefaultGap + 1;
        internal readonly List<StageCaption> Items = new();
        private readonly Dictionary<int, List<StageCaption>> _byStage = new();
        private readonly double _gap;
        private readonly Chart _chart;
        private readonly VisualRenderContext _context;
        private readonly StageCaptionSurface _surface;
        private readonly bool _cone;
        internal ChartRect Plot { get; }
        internal ChartRect Frame { get; }
        internal bool Horizontal { get; }
        internal StageCaptions(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, bool funnel) {
            _chart = chart; _context = context; _surface = new StageCaptionSurface(context);
            _cone = funnel && chart.Options.Funnel.Form == ChartFunnelForm.Cone;
            var series = chart.Series[0]; var colors = context.Theme.Resolve(context.ThemeMode);
            Horizontal = (funnel ? chart.Options.Funnel.Orientation : chart.Options.Pyramid.Orientation) == ChartOrientation.Horizontal;
            Frame = plot;
            _gap = context.Theme.Spacing / 4;
            var percentages = funnel ? FunnelPercentageFormat(chart) : null;
            for (var index = 0; index < series.RenderPoints.Count; index++) {
                var raw = series.RenderPoints[index].Y;
                Add(index, "category", Category(chart, index), funnel ? chart.Options.Funnel.CategoryLabel : chart.Options.Pyramid.CategoryLabel, false);
                Add(index, "value", funnel ? FunnelValue(chart, index) : VisualStateSceneTools.Value(chart, series, index, raw), funnel ? chart.Options.Funnel.ValueLabel : chart.Options.Pyramid.ValueLabel, true);
                if (!funnel) continue;
                var retention = series.RenderPoints[0].Y > 0 ? raw / series.RenderPoints[0].Y : (double?)null;
                var previous = index > 0 ? series.RenderPoints[index - 1].Y : 0;
                var drop = index > 0 && previous > 0 ? (previous - raw) / previous : (double?)null;
                Add(index, "retention", retention.HasValue ? percentages!.FormatFraction(retention.Value) + " retained" : "No initial baseline", chart.Options.Funnel.RetentionLabel, false);
                if (index > 0) Add(index, "dropoff", drop.HasValue ? percentages!.FormatFraction(drop.Value) + " drop-off" : "No previous baseline", chart.Options.Funnel.DropOffLabel, false);
            }
            var leading = 0d; var trailing = 0d;
            foreach (var item in Items) {
                var side = item.Placement;
                var length = Horizontal ? builder.MeasureText(item.Text, item.Style).Height : builder.MeasureText(item.Text, item.Style).Width;
                if (side == (Horizontal ? ChartDataLabelPlacement.Above : ChartDataLabelPlacement.Left)) leading = Math.Max(leading, length + context.Theme.Spacing);
                if (side == (Horizontal ? ChartDataLabelPlacement.Below : ChartDataLabelPlacement.Right)) trailing = Math.Max(trailing, length + context.Theme.Spacing);
            }
            // Retain the existing Pyramid rail policy: complete requested type can reserve up to forty percent of the cross axis.
            var available = (Horizontal ? plot.Height : plot.Width) * .4;
            var total = leading + trailing;
            if (total > available) { leading *= available / total; trailing *= available / total; }
            Plot = Horizontal ? new ChartRect(plot.Left, plot.Top + leading, plot.Width, Math.Max(0, plot.Height - leading - trailing))
                : new ChartRect(plot.Left + leading, plot.Top, Math.Max(0, plot.Width - leading - trailing), plot.Height);

            void Add(int index, string component, string text, ChartValueLabelOptions policy, bool inside) {
                if (!policy.Visible) return;
                var inherited = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
                var placement = policy.Placement ?? inherited;
                if (placement == ChartDataLabelPlacement.Auto) placement = inside && !_cone ? ChartDataLabelPlacement.Inside
                    : Horizontal ? ChartDataLabelPlacement.Below : ChartDataLabelPlacement.Right;
                if (placement == ChartDataLabelPlacement.Outside) placement = Horizontal ? ChartDataLabelPlacement.Below : ChartDataLabelPlacement.Right;
                var contained = placement is ChartDataLabelPlacement.Inside or ChartDataLabelPlacement.Center;
                var color = Color(series, index, colors);
                var style = policy.Style.Resolve(Style(chart, context, index, contained ? ChartColorMath.AccessibleTextOnBackground(color) : colors.Foreground));
                var automaticContained = contained && !policy.Style.Color.HasValue && !VisualChartPaint.ExplicitDataLabelColor(chart, index);
                var paint = !automaticContained ? VisualChartPaint.Text(style) : SvgPaint.Contrast(color, VisualChartPaint.SeriesRole(series, index));
                var caption = new StageCaption(index, component, text, style, paint, placement, automaticContained);
                Items.Add(caption);
                if (!_byStage.TryGetValue(index, out var list)) _byStage[index] = list = new List<StageCaption>();
                list.Add(caption);
            }
        }

        internal void CaptureConeConnections(Chart chart, ChartFunnelLayout layout, VisualThemeColors colors) {
            var series = chart.Series[0];
            foreach (var connection in layout.Connections) {
                var style = FunnelConnectionStyle(chart, connection.FromIndex);
                if (style?.Visible == false) continue;
                var fill = FunnelConnectionFill(chart, connection.FromIndex, Color(series, connection.FromIndex, colors), ChartFunnelForm.Cone, style);
                var pattern = style?.FillPattern ?? FillPattern(series, connection.FromIndex);
                _surface.Add(connection.Path, fill.Color, fill.Paint, pattern != ChartFillPattern.None);
                var outline = style?.OutlineStroke;
                var strokeColor = outline == null ? ChartColor.Transparent
                    : ChartColorMath.WithOpacity(outline.Color ?? style!.Fill ?? Color(series, connection.FromIndex, colors), outline.Opacity);
                if (strokeColor.A > 0) _surface.AddStroke(connection.Path, outline!.Width ?? 1, closed: true);
            }
            foreach (var stage in layout.Stages) {
                var color = Color(series, stage.SourceIndex, colors);
                var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : _context.Theme.SeriesStrokeWidth;
                var outline = StageOutline(series, stage.SourceIndex, color, chart.Options.Funnel.StageOutline, stroke);
                if ((outline.Color ?? color).A > 0) _surface.AddStroke(new ChartPath(new[] {
                    ChartPathCommand.MoveTo(stage.LineStart.X, stage.LineStart.Y), ChartPathCommand.LineTo(stage.LineEnd.X, stage.LineEnd.Y)
                }), outline.Color.HasValue ? outline.Width : stroke);
                if (series.RenderPoints[stage.SourceIndex].Y == 0 && colors.Border.A > 0) _surface.AddStroke(new ChartPath(new[] {
                    ChartPathCommand.MoveTo(stage.ZeroStart.X, stage.ZeroStart.Y), ChartPathCommand.LineTo(stage.ZeroEnd.X, stage.ZeroEnd.Y)
                }), 1);
            }
        }

        internal void Place(VisualSceneBuilder builder, IReadOnlyList<StageCaptionFrame> stages) {
            var requests = new List<LabelPlacementRequest>();
            var obstacles = new List<LabelObstacle>();
            var bySide = Items.GroupBy(item => (item.Index, item.Placement)).ToDictionary(group => group.Key, group => group.ToArray());
            var railOffsets = ConeRailOffsets(builder, stages, bySide, out var pairedRows);
            foreach (var stage in stages) if (stage.HasPaint) obstacles.Add(new LabelObstacle(Id(stage.Index), new LabelMarkShape(
                VisualSceneGeometry.Flatten(new VisualScenePath(stage.Path, stage.Filled, null, null, 0, null, null), 1),
                stage.Filled, stage.StrokeWidth)));
            foreach (var item in Items) {
                var stage = stages[item.Index]; var side = item.Placement;
                var peers = bySide[(item.Index, side)];
                var size = builder.MeasureText(item.Text, item.Style); var gap = _gap;
                var inside = side is ChartDataLabelPlacement.Inside or ChartDataLabelPlacement.Center;
                // Separate rows share a rail; mark clearance must not consume each row's label gap.
                var peerGap = _cone && !inside ? Math.Min(gap, RailGap) : gap;
                var alongX = side is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;
                var anchor = stage.Anchor(side);
                var extents = peers.Select(peer => builder.MeasureText(peer.Text, peer.Style))
                    .Select(metrics => alongX ? metrics.Width : metrics.Height).ToArray();
                var rank = Array.IndexOf(peers, item);
                var paired = pairedRows.Contains((item.Index, side));
                if (paired) {
                    extents = new[] { Math.Max(extents[0], extents[1]) }.Concat(extents.Skip(2)).ToArray();
                    rank = Math.Max(0, rank - 1);
                }
                var offset = -(extents.Sum() + peerGap * (extents.Length - 1)) / 2 + extents.Take(rank).Sum() + peerGap * rank + extents[rank] / 2;
                var packed = railOffsets.TryGetValue((item.Index, side), out var railOffset);
                if (packed) offset += railOffset;
                LabelCandidate candidate; ChartRect bounds = Frame;
                if (inside) {
                    anchor = new ChartPoint(stage.Inside.Left + stage.Inside.Width / 2, stage.Inside.Top + stage.Inside.Height / 2);
                    candidate = new LabelCandidate(0, offset, .5, .5); bounds = stage.Inside;
                } else if (side == ChartDataLabelPlacement.Left) {
                    candidate = new LabelCandidate(-gap, offset, 1, .5);
                    if (!Horizontal) bounds = new ChartRect(Frame.Left, Frame.Top, Math.Max(0, Plot.Left - Frame.Left), Frame.Height);
                } else if (side == ChartDataLabelPlacement.Right) {
                    candidate = new LabelCandidate(gap, offset, 0, .5);
                    if (!Horizontal) bounds = new ChartRect(Plot.Right, Frame.Top, Math.Max(0, Frame.Right - Plot.Right), Frame.Height);
                } else if (side == ChartDataLabelPlacement.Above) {
                    candidate = new LabelCandidate(offset, -gap, .5, 1);
                    if (Horizontal) bounds = new ChartRect(Frame.Left, Frame.Top, Frame.Width, Math.Max(0, Plot.Top - Frame.Top));
                } else {
                    candidate = new LabelCandidate(offset, gap, .5, 0);
                    if (Horizontal) bounds = new ChartRect(Frame.Left, Plot.Bottom, Frame.Width, Math.Max(0, Frame.Bottom - Plot.Bottom));
                }
                // Complete cone groups use the measured rail; other candidates start at the actual mark edge.
                var candidates = new List<LabelCandidate> { candidate };
                if (!inside && (Horizontal ? side is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below : side is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right)) {
                    var x = Horizontal ? stage.Bounds.Left + stage.Bounds.Width / 2 + offset : bounds.Left + bounds.Width / 2;
                    var y = Horizontal ? bounds.Top + bounds.Height / 2 : stage.Bounds.Top + stage.Bounds.Height / 2 + offset;
                    if (packed) {
                        var rail = Horizontal
                            ? new LabelCandidate(offset, (side == ChartDataLabelPlacement.Above ? bounds.Bottom - gap : bounds.Top + gap) - anchor.Y,
                                .5, side == ChartDataLabelPlacement.Above ? 1 : 0)
                            : paired
                                ? new LabelCandidate((item.Component == "value" ? bounds.Right - gap : bounds.Left + gap) - anchor.X,
                                    offset, item.Component == "value" ? 1 : 0, .5)
                                : new LabelCandidate((side == ChartDataLabelPlacement.Left ? bounds.Right - gap : bounds.Left + gap) - anchor.X,
                                    offset, side == ChartDataLabelPlacement.Left ? 1 : 0, .5);
                        candidates.Insert(0, rail);
                        if (Array.IndexOf(peers, item) == 0 && stage.HasPaint) item.RailLeaderEnd = Horizontal
                            ? new ChartPoint(anchor.X + railOffset, side == ChartDataLabelPlacement.Above ? bounds.Bottom - gap / 2 : bounds.Top + gap / 2)
                            : new ChartPoint(side == ChartDataLabelPlacement.Left ? bounds.Right - gap / 2 : bounds.Left + gap / 2, anchor.Y + railOffset);
                    } else candidates.Add(new LabelCandidate(x - anchor.X, y - anchor.Y, .5, .5));
                }
                requests.Add(new LabelPlacementRequest(item.Text, anchor, item.Style, candidates) {
                    AssociatedMarkId = Id(item.Index), Bounds = bounds, Fallback = LabelFallbackRule.Drop,
                    MeasuredSize = size, HasLeaderLine = !inside && stage.HasPaint && !packed, LeaderOnFirstCandidate = true
                });
            }
            var results = new LabelPlacementService().Place(requests, Frame, obstacles, LabelPlacementService.DefaultGap,
                builder.MeasureText, avoidLeaderLabels: true);
            for (var index = 0; index < Items.Count; index++) {
                var item = Items[index]; var result = results[index]; item.Placed = result;
                if (result.IsDropped || !item.AutomaticContained) continue;
                var stage = stages[item.Index]; var series = _chart.Series[0];
                var color = Color(series, item.Index, _context.Theme.Resolve(_context.ThemeMode));
                // Positive opaque partitions keep their incumbent ink and SVG bytes.
                if (stage.Filled && stage.HasPaint && color.A == 255) continue;
                var ink = stage.Filled && stage.HasPaint
                    ? _surface.Filled(result.Bounds, stage.Path, color, VisualChartPaint.Series(series, color, item.Index), FillPattern(series, item.Index) != ChartFillPattern.None)
                    : _surface.Resolve(result.Bounds, !_cone && stage.HasPaint && _context.Theme.Resolve(_context.ThemeMode).Border.A > 0 ? stage : null);
                item.Style.Color = ink.Color; item.Paint = ink.Paint; item.Halo = ink.Halo; item.HaloPaint = ink.HaloPaint;
            }
        }

        private Dictionary<(int, ChartDataLabelPlacement), double> ConeRailOffsets(VisualSceneBuilder builder,
            IReadOnlyList<StageCaptionFrame> stages, Dictionary<(int Index, ChartDataLabelPlacement Placement), StageCaption[]> groups,
            out HashSet<(int, ChartDataLabelPlacement)> pairedRows) {
            var result = new Dictionary<(int, ChartDataLabelPlacement), double>();
            pairedRows = new HashSet<(int, ChartDataLabelPlacement)>();
            if (!_cone) return result;
            var gap = RailGap;
            var peerGap = Math.Min(_gap, gap);
            var start = Horizontal ? Frame.Left : Frame.Top;
            var end = Horizontal ? Frame.Right : Frame.Bottom;
            double Axis(ChartPoint point) => Horizontal ? point.X : point.Y;
            foreach (var side in groups.Where(pair => Horizontal
                ? pair.Key.Placement is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below
                : pair.Key.Placement is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right).GroupBy(pair => pair.Key.Placement)) {
                var rows = side.OrderBy(pair => Axis(stages[pair.Key.Index].Anchor(pair.Key.Placement))).ToArray();
                var metrics = rows.Select(pair => pair.Value.Select(item => builder.MeasureText(item.Text, item.Style)).ToArray()).ToArray();
                var lengths = metrics.Select(row => row.Sum(size => Horizontal ? size.Width : size.Height) + peerGap * (row.Length - 1)).ToArray();
                var paired = new bool[rows.Length];
                if (!Horizontal && lengths.Sum() + gap * (rows.Length - 1) > end - start) {
                    var width = side.Key == ChartDataLabelPlacement.Left ? Plot.Left - Frame.Left : Frame.Right - Plot.Right;
                    for (var index = 0; index < rows.Length; index++) {
                        var peers = rows[index].Value; var sizes = metrics[index];
                        if (peers.Length < 2 || peers[0].Component != "category" || peers[1].Component != "value"
                            || sizes[0].Width + sizes[1].Width + gap > width - _gap * 2) continue;
                        paired[index] = true;
                        lengths[index] = Math.Max(sizes[0].Height, sizes[1].Height) + sizes.Skip(2).Sum(size => size.Height) + peerGap * (sizes.Length - 2);
                    }
                }
                if (lengths.Sum() + gap * (rows.Length - 1) > end - start) continue;
                var tops = new double[rows.Length];
                for (var index = 0; index < rows.Length; index++)
                    tops[index] = Math.Max(index == 0 ? start : tops[index - 1] + lengths[index - 1] + gap,
                        Axis(stages[rows[index].Key.Index].Anchor(side.Key)) - lengths[index] / 2);
                for (var index = rows.Length - 1; index >= 0; index--)
                    tops[index] = Math.Min(tops[index], (index == rows.Length - 1 ? end : tops[index + 1] - gap) - lengths[index]);
                for (var index = 0; index < rows.Length; index++) {
                    result[rows[index].Key] = tops[index] + lengths[index] / 2 - Axis(stages[rows[index].Key.Index].Anchor(side.Key));
                    if (paired[index]) pairedRows.Add(rows[index].Key);
                }
            }
            return result;
        }

        internal void Draw(Chart chart, VisualSceneBuilder builder, int index, string family, VisualThemeColors colors) {
            if (!_byStage.TryGetValue(index, out var captions)) return;
            foreach (var item in captions.Where(item => item.RailLeaderEnd.HasValue))
                if (captions.Any(peer => peer.Placement == item.Placement && !peer.Placed!.IsDropped))
                    VisualStateSceneTools.Connector(builder, chart.Options, item.Placed!.Request.Anchor, item.RailLeaderEnd!.Value,
                        colors.MutedForeground, ChartDataLabelConnectorStyle.Straight);
            foreach (var item in captions) {
                var placed = item.Placed!; var id = Id(index) + "-" + item.Component + "-label";
                var bounds = placed.IsDropped ? new ChartRect(placed.Request.Anchor.X, placed.Request.Anchor.Y, 0, 0) : placed.Bounds;
                if (placed.HasLeaderLine) VisualStateSceneTools.Connector(builder, chart.Options, placed.Request.Anchor, placed.LeaderEnd, colors.MutedForeground,
                    _cone ? ChartDataLabelConnectorStyle.Straight : null);
                StageText(builder, item.Text, bounds, item.Style, family + "-" + item.Component + "-label", id,
                    TextAlignment.Left, item.Paint, family, halo: item.Halo, haloPaint: item.HaloPaint);
            }
        }
    }

    private sealed class StageCaption {
        internal StageCaption(int index, string component, string text, TextStyle style, SvgPaint paint, ChartDataLabelPlacement placement, bool automaticContained) {
            Index = index; Component = component; Text = text; Style = style; Paint = paint; Placement = placement; AutomaticContained = automaticContained;
        }
        internal int Index { get; }
        internal string Component { get; }
        internal string Text { get; }
        internal TextStyle Style { get; }
        internal SvgPaint Paint { get; set; }
        internal bool AutomaticContained { get; }
        internal ChartColor? Halo { get; set; }
        internal SvgPaint? HaloPaint { get; set; }
        internal ChartDataLabelPlacement Placement { get; }
        internal PlacedLabel? Placed { get; set; }
        internal ChartPoint? RailLeaderEnd { get; set; }
    }

    private sealed class StageCaptionFrame {
        internal StageCaptionFrame(int index, ChartRect bounds, ChartRect inside, ChartPath path, bool filled, bool hasPaint, double strokeWidth, ChartPoint left, ChartPoint right, ChartPoint top, ChartPoint bottom) {
            Index = index; Bounds = bounds; Inside = inside; Path = path; Filled = filled; HasPaint = hasPaint; StrokeWidth = strokeWidth;
            _left = left; _right = right; _top = top; _bottom = bottom;
        }
        private readonly ChartPoint _left, _right, _top, _bottom;
        internal int Index { get; }
        internal ChartRect Bounds { get; }
        internal ChartRect Inside { get; }
        internal ChartPath Path { get; }
        internal bool Filled { get; }
        internal bool HasPaint { get; }
        internal double StrokeWidth { get; }
        internal ChartPoint Anchor(ChartDataLabelPlacement side) => side switch {
            ChartDataLabelPlacement.Left => _left, ChartDataLabelPlacement.Right => _right,
            ChartDataLabelPlacement.Above => _top, _ => _bottom
        };
    }
}
