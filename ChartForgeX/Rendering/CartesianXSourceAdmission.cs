using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

/// <summary>Detached source eligibility, evaluated before projected paint or caption clearance.</summary>
internal readonly struct CartesianXSourceAdmission {
    private CartesianXSourceAdmission(double minimum, double maximum, bool point) { Minimum = minimum; Maximum = maximum; IsPoint = point; }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal bool IsPoint { get; }
    internal static CartesianXSourceAdmission Point(double value) => new(value, value, true);
    internal static CartesianXSourceAdmission Range(double? minimum, double? maximum) => new(
        minimum ?? double.NegativeInfinity, maximum ?? double.PositiveInfinity, false);
    internal bool Allows(double minimum, double maximum) => IsPoint ? Minimum >= minimum && Minimum <= maximum
        : Maximum > minimum && Minimum < maximum;
    internal string HiddenReason => IsPoint ? "anchor-outside-window" : "range-outside-window";

    /// <summary>Filters named native paint scopes. An excluded point retains its descriptive group and source region,
    /// while its links, hit areas and static-state paint are removed together.</summary>
    internal static void AppendPaint(List<VisualSceneNode> destination, IReadOnlyList<VisualSceneNode> source, double minimum, double maximum) {
        var suppressedDepth = 0; var closeSourceFact = false;
        foreach (var node in source) {
            if (suppressedDepth > 0) {
                if (node is VisualSceneGroup) suppressedDepth++;
                else if (node is VisualSceneEndGroup && --suppressedDepth == 0 && closeSourceFact) destination.Add(node);
            } else if (node is VisualSceneGroup group && group.SourceX.HasValue && !group.SourceX.Value.Allows(minimum, maximum)) {
                suppressedDepth = 1;
                closeSourceFact = group.Role == "point";
                if (!closeSourceFact) continue;
                var metadata = VisualSceneBuilder.NonLinkMarkMetadata(group);
                metadata["data-cfx-visible"] = "false";
                destination.Add(new VisualSceneGroup(group.Role, group.Id, group.Clip, metadata, group.PathClip, group.Rotation,
                    null, group.Tooltip, group.Translation, group.ImageResource, group.ColorScope, group.Opacity,
                    group.SurfaceShadow, group.RetainedEffect, group.SourceX));
            } else destination.Add(node);
        }
        if (suppressedDepth != 0) throw new InvalidOperationException("A source-admitted native paint scope must be balanced.");
    }
}
