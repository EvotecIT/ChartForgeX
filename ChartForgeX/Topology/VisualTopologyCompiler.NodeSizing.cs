using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void FitNodeLabels(TopologyChart chart) {
        if ((!_options.IncludeNodeLabels && !chart.Nodes.Any(node => node.TableContent != null)) || !chart.Nodes.Any(node => node.FitLabelToContent)) return;
        var ordinarySize = _context.Theme.Typography.DataLabelSize;
        var ordinaryLineHeight = _builder.MeasureText("Ag", ordinarySize, 600).LineHeight;
        foreach (var node in chart.Nodes) {
            if (!_options.IncludeNodeLabels && node.TableContent == null) continue;
            using var textPolicy = UseTextPolicy(node.TextStyle);
            var size = TextPolicySize(ordinarySize);
            var lineHeight = _textPolicy == null ? ordinaryLineHeight : MeasurePolicyText("Ag", size, 600).LineHeight;
            if (!node.ShowLabel && node.TableContent == null) continue;
            if (node.FitLabelToContent && (EffectiveNodeDisplayMode(node, _options) == TopologyNodeDisplayMode.Card
                    || node.TableContent != null && EffectiveNodeDisplayMode(node, _options) == TopologyNodeDisplayMode.CompactCard)
                && UseEnclosedShapeContent(node, TopologyNodeDisplayMode.Card, _options)) {
                GrowShapeContent(node);
                continue;
            }
            if (!CanFitNodeLabelToContent(node)) continue;
            var label = node.MaximumLabelCharacters.HasValue ? TrimTo(node.Label, node.MaximumLabelCharacters.Value) : node.Label;
            var source = label.Replace("\r\n", "\n").Replace('\r', '\n');
            if (!_options.AllowMultilineNodeLabels) source = source.Replace('\n', ' ');
            var lines = source.Split('\n').Take(Math.Max(1, _options.MaxNodeLabelLines)).ToArray();
            var width = lines.Max(line => MeasurePolicyText(line, size, 600).Width) + .001;
            var height = lines.Length * lineHeight + .001;
            if (node.LabelContent.IsStructured) {
                var content = DisplayRows(NodeCaptionContent(node));
                var layout = ChartForgeX.Typography.TextLayoutEngine.Layout(content, double.MaxValue / 4,
                    InlineStyle(size, _colors.Foreground), ChartForgeX.Typography.TextWrapMode.NoWrap, Math.Max(1, _options.MaxNodeLabelLines));
                width = layout.Metrics.Width + .001; height = layout.Metrics.Height + .001;
            }
            node.Width = Math.Max(node.Width, width + 20);
            node.Height = Math.Max(node.Height, height + 12);
            if (node.Shape == TopologyNodeShape.Diamond) node.Height = Math.Max(node.Height, (height + 8) * 2);
            // Use the same interior geometry as the caption painter. Curved and slanted
            // surfaces cannot allocate their complete bounding rectangle to text.
            for (var attempt = 0; attempt < 16; attempt++) {
                if (node.Shape is TopologyNodeShape.Circle or TopologyNodeShape.DoubleCircle)
                    node.Width = node.Height = Math.Max(node.Width, node.Height);
                var bounds = node.Shape.HasValue ? TopologyNodeShapeGeometry.CaptionBounds(node, height)
                    : new ChartRect(node.X + 10, node.Y + 6, Math.Max(0, node.Width - 22), Math.Max(0, node.Height - 12));
                if (bounds.Width >= width && bounds.Height >= height) break;
                if (bounds.Height < height) node.Height *= Math.Max(1.05, height / Math.Max(1, bounds.Height));
                if (bounds.Width < width) node.Width *= Math.Max(1.05, width / Math.Max(1, bounds.Width));
            }
        }
    }

    private bool CanFitNodeLabelToContent(TopologyNode node) => node.FitLabelToContent
        && EffectiveNodeDisplayMode(node, _options) == TopologyNodeDisplayMode.Card
        && !TopologyNodeShapeGeometry.IsActor(node) && node.Kind == TopologyNodeKind.Generic && !node.IconShape.HasValue && string.IsNullOrWhiteSpace(node.IconId)
        && string.IsNullOrWhiteSpace(node.Symbol) && ResolveRenderableNodeArtwork(node, _options) == null
        && string.IsNullOrWhiteSpace(node.Subtitle) && node.Details.Count == 0 && string.IsNullOrWhiteSpace(node.Badge);

    private double FitMeasuredNodeLabelSize(string label, double preferred, Func<double, ChartRect> contentBounds) =>
        ChartTextFitting.FitFontSize(Math.Min(11, preferred), preferred, candidate => {
            // Pixel-size corrections and synthetic emphasis need not scale proportionally.
            // Recheck the final font against its output cell, including prepared-layout resizing.
            var bounds = contentBounds(candidate);
            var size = candidate * _scale;
            var lineHeight = MeasurePolicyText("Ag", size, 600).LineHeight;
            var maximum = Math.Min(Math.Max(1, _options.MaxNodeLabelLines), (int)Math.Floor(bounds.Height / lineHeight));
            if (bounds.Width <= 0 || maximum < 1) return false;
            FitTextLines(label, bounds.Width, size, 600, maximum, false, false, out var overflow);
            return !overflow;
        });
}
