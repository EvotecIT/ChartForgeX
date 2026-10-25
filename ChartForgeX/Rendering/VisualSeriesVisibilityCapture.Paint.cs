using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSeriesVisibilityCapture {
    private PreparedVisual Paint(PreparedVisual basis, double[] opacities, ChartVisibilityView view) {
        var scene = _basis.Scene; var nodes = scene.Nodes; var output = new List<VisualSceneNode>(nodes.Count);
        var values = new Dictionary<SeriesRow, double>(); for (var index = 0; index < _series.Length; index++) values.Add(_series[index], opacities[index]);
        var grouped = view.LabelLayering == ChartVisibilityLabelLayering.GroupWithSeries;
        for (var index = 0; index < nodes.Count; index++) {
            if (_plots.TryGetValue(index, out var plot)) {
                var opacity = values[plot];
                if (grouped) {
                    output.Add(new VisualSceneGroup("series-visibility", null, null,
                        new Dictionary<string, string> { ["data-cfx-visibility-series-key"] = plot.Key }, null, null, null, null, null, null, null, opacity));
                    AppendOwned(plot.Scope, plot, opacity, false);
                    foreach (var label in plot.Labels) AppendLabel(label, plot, opacity);
                    output.Add(new VisualSceneEndGroup());
                } else AppendOwned(plot.Scope, plot, opacity, true);
                index = _tree.End[index]; continue;
            }
            if (_labels.TryGetValue(index, out var labelOwner)) {
                if (!grouped) AppendOwned(index, labelOwner, values[labelOwner], true);
                index = _tree.End[index]; continue;
            }
            if (nodes[index] is VisualSceneGroup legend && legend.Role is "legend-entry" or "legend-entry-omitted"
                && legend.Metadata.TryGetValue("data-cfx-series-key", out var key)) {
                var row = _series.FirstOrDefault(item => item.Key == key);
                if (row != null && values[row] == 0) {
                    var metadata = new Dictionary<string, string>(); foreach (var item in legend.Metadata) metadata.Add(item.Key, item.Value);
                    metadata["data-cfx-series-hidden"] = "true";
                    output.Add(Copy(legend, metadata, legend.Opacity)); continue;
                }
            }
            output.Add(nodes[index]);
        }
        var regions = scene.Regions.Where(region => !_regionOwners.TryGetValue(region.Id, out var owner) || values[owner] > 0).ToArray();
        var painted = new VisualScene(scene.Size, output, scene.Diagnostics, regions, scene.DataProjection, scene.ValueCapture, this);
        return ApplyAppearance(basis.WithNativeFrame(painted));

        void AppendOwned(int start, SeriesRow owner, double opacity, bool isolate) {
            var hidden = opacity == 0;
            for (var item = start; item <= _tree.End[start]; item++) {
                if (nodes[item] is VisualSceneGroup group) {
                    IReadOnlyDictionary<string, string> metadata = group.Metadata;
                    if (hidden) {
                        var facts = new Dictionary<string, string>(); foreach (var pair in metadata) facts.Add(pair.Key, pair.Value);
                        facts["data-cfx-visible"] = "false"; metadata = facts;
                    }
                    output.Add(Copy(group, metadata, isolate && item == start ? opacity * group.Opacity : group.Opacity));
                } else output.Add(nodes[item]);
            }
        }
        void AppendLabel(int start, SeriesRow owner, double opacity) {
            var plotAncestors = _tree.Ancestors(owner.Scope); var labelAncestors = _tree.Ancestors(start); var shared = 0;
            while (shared < plotAncestors.Count && shared < labelAncestors.Count && plotAncestors[shared] == labelAncestors[shared]) shared++;
            if (shared != plotAncestors.Count)
                throw new NotSupportedException("Grouped native series labels cannot inherit a different series ancestor context.");
            // Real Cartesian captions have only ordinary clip/transform context outside the series. Never duplicate semantic ink owners.
            for (var item = shared; item < labelAncestors.Count; item++) {
                var context = (VisualSceneGroup)nodes[labelAncestors[item]];
                if (context.Opacity != 1 || context.Id != null || context.Role != null || context.Metadata.Count != 0
                    || context.Href != null || context.Tooltip != null || context.ImageResource != null || context.ColorScope != null
                    || context.SurfaceShadow != null || context.RetainedEffect != null)
                    throw new NotSupportedException("Grouped native series labels require an ordinary captured clip/transform context.");
                output.Add(context);
            }
            AppendOwned(start, owner, opacity, false);
            for (var item = shared; item < labelAncestors.Count; item++) output.Add(new VisualSceneEndGroup());
        }
    }
    private static VisualSceneGroup Copy(VisualSceneGroup group, IReadOnlyDictionary<string, string> metadata, double opacity) =>
        new(group.Role, group.Id, group.Clip, metadata, group.PathClip, group.Rotation, group.Href, group.Tooltip,
            group.Translation, group.ImageResource, group.ColorScope, opacity, group.SurfaceShadow, group.RetainedEffect);
}
