using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Allocates one slot per directed endpoint; a self flow consumes two slots on its own node.</summary>
internal static class ChartChordLayout {
    internal static ChartChordModel Compute(ChartSeries series, ChartChordOptions options, ChartRect plot) {
        var facts = series.Relationships;
        var nodes = new List<ChartChordNode>();
        var links = new List<ChartChordLink>();
        var model = new ChartChordModel(nodes, links, plot.Left + plot.Width / 2, plot.Top + plot.Height / 2,
            Math.Max(0, Math.Min(plot.Width, plot.Height) / 2 - 1), options.NodeThicknessRatio);
        if (facts == null) return model;
        for (var index = 0; index < facts.Nodes.Count; index++) nodes.Add(new ChartChordNode(index, facts.Nodes[index],
            facts.FlowIncomingValues[index], facts.FlowOutgoingValues[index], facts.FlowEndpointValues[index]));
        for (var index = 0; index < facts.FlowLinks.Count; index++) links.Add(new ChartChordLink(index, facts.FlowLinks[index], facts.Source(index), facts.Target(index)));
        model.WeightReference = nodes.Count == 0 ? 0 : nodes.Max(node => node.Value);
        if (model.WeightReference == 0) return model;

        // Global raw sums can overflow for independent finite flows. Ratios stay bounded,
        // including when all authored values are subnormal; never form pixels/rawWeight.
        var total = nodes.Sum(node => node.Value / model.WeightReference);
        var positive = nodes.Count(node => node.Value > 0);
        var availableDegrees = options.SweepAngleDegrees - positive * options.NodeGapDegrees;
        if (availableDegrees <= 0) throw new InvalidOperationException("Chord node gaps must leave positive arc space inside the configured circular span.");
        model.NormalizedAngularScale = availableDegrees * (Math.PI / 180) / total;
        var gap = options.NodeGapDegrees * (Math.PI / 180);
        var angle = options.StartAngleDegrees % 360 * (Math.PI / 180);
        foreach (var node in nodes) {
            node.Start = angle;
            node.Sweep = model.Angle(node.Value);
            node.IsVisible = node.Value > 0 && model.OuterRadius > model.InnerRadius && model.HasDistinctArc(node.Start, node.Sweep, model.OuterRadius);
            if (node.Value > 0) angle += node.Sweep + gap;
        }
        var outgoing = nodes.Select(node => node.Start).ToArray();
        var incoming = nodes.Select(node => node.Start + model.Angle(node.Outgoing)).ToArray();
        foreach (var link in links) {
            link.Sweep = model.Angle(link.Fact.Value);
            link.SourceStart = outgoing[link.Source];
            link.TargetStart = incoming[link.Target];
            outgoing[link.Source] += link.Sweep;
            incoming[link.Target] += link.Sweep;
            link.IsVisible = link.Fact.Value > 0 && model.HasDistinctArc(link.SourceStart, link.Sweep, model.InnerRadius)
                && model.HasDistinctArc(link.TargetStart, link.Sweep, model.InnerRadius);
        }
        return model;
    }

    internal static ChartPath Ribbon(ChartChordModel model, ChartChordLink link) {
        var sourceStart = model.On(link.SourceStart, model.InnerRadius);
        var sourceEnd = model.On(link.SourceStart + link.Sweep, model.InnerRadius);
        var targetStart = model.On(link.TargetStart, model.InnerRadius);
        var targetEnd = model.On(link.TargetStart + link.Sweep, model.InnerRadius);
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(sourceStart.X, sourceStart.Y) };
        ChartPathBuilder.AddCircularArc(commands, model.CenterX, model.CenterY, model.InnerRadius, link.SourceStart, link.Sweep);
        Connect(commands, model, sourceEnd, targetStart);
        ChartPathBuilder.AddCircularArc(commands, model.CenterX, model.CenterY, model.InnerRadius, link.TargetStart, link.Sweep);
        Connect(commands, model, targetEnd, sourceStart);
        return new ChartPath(commands);
    }

    private static void Connect(List<ChartPathCommand> commands, ChartChordModel model, ChartPoint from, ChartPoint to) =>
        commands.Add(ChartPathCommand.CubicTo(model.CenterX + (from.X - model.CenterX) * .2, model.CenterY + (from.Y - model.CenterY) * .2,
            model.CenterX + (to.X - model.CenterX) * .2, model.CenterY + (to.Y - model.CenterY) * .2, to.X, to.Y));

    internal static ChartPath DirectionCue(ChartChordModel model, ChartChordLink link) {
        var angle = link.TargetStart + link.Sweep / 2;
        var depth = Math.Min(8, Math.Min(model.InnerRadius * .15, link.Sweep * model.InnerRadius * .4));
        var tip = model.On(angle, model.InnerRadius);
        var back = model.On(angle, model.InnerRadius - depth);
        var width = depth * .55;
        var left = new ChartPoint(back.X - Math.Sin(angle) * width, back.Y + Math.Cos(angle) * width);
        var right = new ChartPoint(back.X + Math.Sin(angle) * width, back.Y - Math.Cos(angle) * width);
        var notch = model.On(angle, model.InnerRadius - depth * .55);
        return new ChartPath(new[] { ChartPathCommand.MoveTo(tip.X, tip.Y), ChartPathCommand.LineTo(left.X, left.Y),
            ChartPathCommand.LineTo(notch.X, notch.Y), ChartPathCommand.LineTo(right.X, right.Y) });
    }

    internal static ChartRect NodeBounds(ChartChordModel model, ChartChordNode node) {
        if (!node.IsVisible) { var point = model.On(node.Start, model.OuterRadius); return new ChartRect(point.X, point.Y, 0, 0); }
        var outer = ChartCurveFlattening.Arc(model.CenterX, model.CenterY, model.OuterRadius, node.Start, node.Sweep, 1);
        outer.AddRange(ChartCurveFlattening.Arc(model.CenterX, model.CenterY, model.InnerRadius, node.Start, node.Sweep, 1));
        return Bounds(outer);
    }

    internal static ChartRect Bounds(IReadOnlyList<ChartPoint> points) {
        if (points.Count == 0) return default;
        var left = points.Min(point => point.X); var right = points.Max(point => point.X);
        var top = points.Min(point => point.Y); var bottom = points.Max(point => point.Y);
        return new ChartRect(left, top, right - left, bottom - top);
    }
}

internal sealed class ChartChordModel {
    internal ChartChordModel(List<ChartChordNode> nodes, List<ChartChordLink> links, double centerX, double centerY, double radius, double thickness) {
        Nodes = nodes; Links = links; CenterX = centerX; CenterY = centerY; OuterRadius = radius; InnerRadius = radius * (1 - thickness);
    }
    internal IReadOnlyList<ChartChordNode> Nodes { get; }
    internal IReadOnlyList<ChartChordLink> Links { get; }
    internal double CenterX { get; }
    internal double CenterY { get; }
    internal double OuterRadius { get; }
    internal double InnerRadius { get; }
    internal double WeightReference { get; set; }
    internal double NormalizedAngularScale { get; set; }
    internal double Angle(double weight) => WeightReference == 0 ? 0 : weight / WeightReference * NormalizedAngularScale;
    internal bool HasDistinctArc(double start, double sweep, double radius) {
        if (radius <= 0 || sweep <= 0 || start + sweep <= start) return false;
        if (sweep >= Math.PI) return true;
        var first = On(start, radius); var last = On(start + sweep, radius);
        return first.X != last.X || first.Y != last.Y;
    }
    internal ChartPoint On(double angle, double radius) => new(CenterX + Math.Cos(angle) * radius, CenterY + Math.Sin(angle) * radius);
}

internal sealed class ChartChordNode {
    internal ChartChordNode(int index, ChartNode fact, double incoming, double outgoing, double value) { Index = index; Fact = fact; Incoming = incoming; Outgoing = outgoing; Value = value; }
    internal int Index { get; }
    internal ChartNode Fact { get; }
    internal double Incoming { get; }
    internal double Outgoing { get; }
    internal double Value { get; }
    internal double Start { get; set; }
    internal double Sweep { get; set; }
    internal bool IsVisible { get; set; }
}

internal sealed class ChartChordLink {
    internal ChartChordLink(int index, ChartFlowLink fact, int source, int target) { Index = index; Fact = fact; Source = source; Target = target; }
    internal int Index { get; }
    internal ChartFlowLink Fact { get; }
    internal int Source { get; }
    internal int Target { get; }
    internal double SourceStart { get; set; }
    internal double TargetStart { get; set; }
    internal double Sweep { get; set; }
    internal bool IsVisible { get; set; }
}
