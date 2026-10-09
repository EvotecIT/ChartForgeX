using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

/// <summary>Composes script-free animation using captured native routes, paint and motion timing.</summary>
internal sealed class TopologyMotionSvgAdapter {
    private readonly ResolvedTopologyMotionVisual _visual;

    internal TopologyMotionSvgAdapter(ResolvedTopologyMotionVisual visual) {
        if (visual.Routes.Length > VisualArtifactInterchangeValidation.MaximumEdges || visual.Nodes.Length > VisualArtifactInterchangeValidation.MaximumNodes)
            throw new ArgumentException("The topology animation exceeds the supported diagram entity budget.");
        _visual = visual;
        PolicyIdentity = Digest();
    }

    internal string PolicyIdentity { get; }

    private string Digest() {
        using var hash = SHA256.Create();
        using var stream = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true)) {
            writer.Write("cfx-topology-motion-2"); writer.Write(_visual.SourceId); writer.Write(_visual.Background.Svg);
            writer.Write(_visual.Radius); writer.Write(_visual.Scale); writer.Write(_visual.Motion.DurationSeconds); writer.Write(_visual.Motion.Loop);
            writer.Write(_visual.Routes.Length);
            foreach (var route in _visual.Routes) { writer.Write(route.Id); writer.Write(Path(route.Points)); writer.Write(route.Paint.Svg); writer.Write(route.StartProgress); }
            writer.Write(_visual.Nodes.Length);
            foreach (var node in _visual.Nodes) { writer.Write(node.Id); writer.Write(node.Center.X); writer.Write(node.Center.Y); writer.Write(node.Paint.Svg); }
        }
        stream.FlushFinalBlock();
        return string.Concat(hash.Hash!.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    internal string Compose(string nativeSvg) {
        var document = XDocument.Parse(nativeSvg, LoadOptions.PreserveWhitespace);
        var root = document.Root!; var ns = root.Name.Namespace;
        var topology = root.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology");
        var content = topology.Elements().FirstOrDefault(element => element.Attribute("clip-path") != null) ?? topology;
        var scope = (string?)root.Descendants().First(element => element.Attribute("id") != null).Attribute("id");
        var tourId = scope + "-motion-tour";
        var underlay = Group("topology-motion");
        underlay.Add(new XElement(ns + "path", new XAttribute("id", tourId), new XAttribute("data-cfx-role", "topology-motion-tour-path"),
            new XAttribute("d", string.Join(" ", _visual.Routes.Select(route => Path(route.Points)))), new XAttribute("fill", "none"), new XAttribute("stroke", "none")));
        for (var i = 0; i < _visual.Routes.Length; i++) {
            var route = _visual.Routes[i];
            underlay.Add(new XElement(ns + "path", new XAttribute("id", scope + "-motion-route-" + i),
                new XAttribute("data-cfx-role", "topology-motion-route"), new XAttribute("data-edge-id", route.Id),
                new XAttribute("d", Path(route.Points)), new XAttribute("fill", "none"), new XAttribute("stroke", route.Paint.Svg),
                new XAttribute("stroke-width", F(_visual.RouteWidth)), new XAttribute("stroke-linecap", "round"),
                new XAttribute("stroke-linejoin", "round"), new XAttribute("stroke-dasharray", F(_visual.DashLength) + " " + F(_visual.DashPeriod - _visual.DashLength)),
                new XAttribute("stroke-opacity", F(ResolvedTopologyMotionVisual.RouteOpacity)),
                Animation("animate", new XAttribute("attributeName", "stroke-dashoffset"), new XAttribute("calcMode", "linear"),
                    new XAttribute("from", F(_visual.DashOffset(0))), new XAttribute("to", "0"))));
        }
        var firstNode = content.Elements().FirstOrDefault(element => element.DescendantsAndSelf().Any(child => (string?)child.Attribute("data-cfx-role") == "topology-node"));
        if (firstNode == null) content.Add(underlay); else firstNode.AddBeforeSelf(underlay);
        var overlay = Group("topology-motion-overlay");
        MovingCircle(_visual.HaloRadius, "topology-motion-halo", true, ResolvedTopologyMotionVisual.HaloOpacity);
        MovingCircle(_visual.SurfaceRadius, "topology-motion-surface", false, ResolvedTopologyMotionVisual.SurfaceOpacity);
        MovingCircle(_visual.Radius, "topology-motion-marker", true, 1, marker: true);
        MovingCircle(_visual.SurfaceRadius, "topology-motion-outline", true, ResolvedTopologyMotionVisual.OutlineOpacity, outline: true);
        foreach (var node in _visual.Nodes) overlay.Add(new XElement(ns + "circle", new XAttribute("data-cfx-role", "topology-motion-node"),
            new XAttribute("data-node-id", node.Id), new XAttribute("cx", F(node.Center.X)), new XAttribute("cy", F(node.Center.Y)),
            new XAttribute("r", F(_visual.PulseLowRadius)), new XAttribute("fill", "none"), new XAttribute("stroke", node.Paint.Svg),
            new XAttribute("stroke-width", F(_visual.MarkerStrokeWidth)), new XAttribute("stroke-opacity", F(_visual.PulseOpacity(0))),
            Pulse("r", _visual.PulseLowRadius, _visual.PulseHighRadius),
            Pulse("stroke-opacity", ResolvedTopologyMotionVisual.PulseLowOpacity, ResolvedTopologyMotionVisual.PulseHighOpacity)));
        content.Add(overlay);
        return document.ToString(SaveOptions.DisableFormatting);

        void MovingCircle(double radius, string role, bool color, double opacity, bool marker = false, bool outline = false) {
            var circle = new XElement(ns + "circle", new XAttribute("data-cfx-role", role), new XAttribute("r", F(radius)),
                new XAttribute("fill", outline ? "none" : color ? _visual.Routes[0].Paint.Svg : _visual.Background.Svg),
                new XAttribute(outline ? "stroke-opacity" : "fill-opacity", F(opacity)));
            if (marker || outline) {
                circle.Add(new XAttribute("stroke", outline ? _visual.Routes[0].Paint.Svg : _visual.Background.Svg),
                    new XAttribute("stroke-width", F(outline ? _visual.OutlineWidth : _visual.MarkerStrokeWidth)));
            }
            if (color && _visual.Routes.Select(route => route.Paint.Svg).Distinct(StringComparer.Ordinal).Count() > 1) {
                circle.Add(Animation("animate", new XAttribute("attributeName", outline ? "stroke" : "fill"),
                    new XAttribute("calcMode", "discrete"),
                    new XAttribute("values", string.Join(";", _visual.Routes.Select(route => route.Paint.Svg).Concat(new[] { _visual.Routes[_visual.Routes.Length - 1].Paint.Svg }))),
                    new XAttribute("keyTimes", string.Join(";", _visual.Routes.Select(route => T(route.StartProgress)).Concat(new[] { "1" })))));
            }
            XNamespace xlink = "http://www.w3.org/1999/xlink";
            var move = Animation("animateMotion", new XAttribute("calcMode", "linear"), new XAttribute("keyPoints", "0;1"), new XAttribute("keyTimes", "0;1"));
            move.Add(new XElement(ns + "mpath", new XAttribute(XNamespace.Xmlns + "xlink", xlink.NamespaceName),
                new XAttribute("href", "#" + tourId), new XAttribute(xlink + "href", "#" + tourId)));
            circle.Add(move); overlay.Add(circle);
        }

        XElement Pulse(string attribute, double low, double high) => Animation("animate", new XAttribute("attributeName", attribute),
            new XAttribute("calcMode", "linear"), new XAttribute("keyTimes", "0;0.5;1"), new XAttribute("values", F(low) + ";" + F(high) + ";" + F(low)));
        XElement Group(string role) => new(ns + "g", new XAttribute("data-cfx-role", role), new XAttribute("data-cfx-motion-source", _visual.SourceId),
            new XAttribute("data-cfx-motion-kind", "route-pulse"), new XAttribute("pointer-events", "none"));
        XElement Animation(string name, params object[] attributes) => new(ns + name, attributes,
            new XAttribute("dur", T(_visual.Motion.DurationSeconds) + "s"), new XAttribute("repeatCount", _visual.Motion.Loop ? "indefinite" : "1"),
            new XAttribute("fill", _visual.Motion.Loop ? "remove" : "freeze"));
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string T(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string Path(IReadOnlyList<ChartPoint> points) => string.Join(" ", points.Select((point, i) => (i == 0 ? "M " : "L ") + F(point.X) + " " + F(point.Y)));
}
