using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

// Explicit animation host composition. Static geometry and route planning stay in the native scene;
// Phase 4 moves this adapter and the existing raster animation policy into Stories.
internal sealed class TopologyMotionSvgAdapter {
    private readonly string _source;
    private readonly string _background;
    private readonly string _markerColor;
    private readonly double _radius;
    private readonly double _scale;
    private readonly TopologyMotionOptions _motion;
    private readonly Route[] _routes;
    private readonly (string Id, ChartPoint Center, string Color)[] _nodes;

    internal TopologyMotionSvgAdapter(TopologyMotionPlan plan, TopologyMotionOptions motion, string background,
        Func<TopologyEdge, string> edgeColor, (string Id, ChartPoint Center, string Color)[] nodes, double scale) {
        if (plan.Entries.Count > VisualArtifactInterchangeValidation.MaximumEdges || nodes.Length > VisualArtifactInterchangeValidation.MaximumNodes)
            throw new ArgumentException("The topology animation exceeds the supported diagram entity budget.");
        _source = plan.SourceId; _motion = motion.Clone(); _background = background; _scale = scale;
        _radius = motion.MarkerRadius * scale; _nodes = nodes.ToArray();
        _routes = plan.Entries.Select(entry => new Route(entry.Edge.Id, Path(entry.Points), edgeColor(entry.Edge))).ToArray();
        _markerColor = _routes[0].Color;
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
            new XAttribute("d", string.Join(" ", _routes.Select(route => route.Path))), new XAttribute("fill", "none"), new XAttribute("stroke", "none")));
        for (var i = 0; i < _routes.Length; i++) {
            var route = _routes[i];
            underlay.Add(new XElement(ns + "path", new XAttribute("id", scope + "-motion-route-" + i),
                new XAttribute("data-cfx-role", "topology-motion-route"), new XAttribute("data-edge-id", route.Id),
                new XAttribute("d", route.Path), new XAttribute("fill", "none"), new XAttribute("stroke", route.Color),
                new XAttribute("stroke-width", F(Math.Max(2.5 * _scale, _radius * .7))), new XAttribute("stroke-linecap", "round"),
                new XAttribute("stroke-linejoin", "round"), new XAttribute("stroke-dasharray", F(10 * _scale) + " " + F(16 * _scale)),
                new XAttribute("opacity", ".76"), Animation("animate", new XAttribute("attributeName", "stroke-dashoffset"),
                    new XAttribute("from", F(26 * _scale)), new XAttribute("to", "0"))));
        }
        var firstNode = content.Elements().FirstOrDefault(element => element.DescendantsAndSelf().Any(child => (string?)child.Attribute("data-cfx-role") == "topology-node"));
        if (firstNode == null) content.Add(underlay); else firstNode.AddBeforeSelf(underlay);
        var overlay = Group("topology-motion-overlay");
        XNamespace xlink = "http://www.w3.org/1999/xlink";
        var move = Animation("animateMotion", new XAttribute("rotate", "auto"));
        move.Add(new XElement(ns + "mpath", new XAttribute(XNamespace.Xmlns + "xlink", xlink.NamespaceName),
            new XAttribute("href", "#" + tourId), new XAttribute(xlink + "href", "#" + tourId)));
        overlay.Add(new XElement(ns + "circle", new XAttribute("data-cfx-role", "topology-motion-marker"),
            new XAttribute("r", F(_radius)), new XAttribute("fill", _markerColor), new XAttribute("stroke", _background),
            new XAttribute("stroke-width", F(2 * _scale)), new XAttribute("opacity", ".95"), move));
        foreach (var node in _nodes) overlay.Add(new XElement(ns + "circle", new XAttribute("data-cfx-role", "topology-motion-node"),
            new XAttribute("data-node-id", node.Id), new XAttribute("cx", F(node.Center.X)), new XAttribute("cy", F(node.Center.Y)),
            new XAttribute("r", F(_radius + 2 * _scale)), new XAttribute("fill", "none"), new XAttribute("stroke", node.Color),
            new XAttribute("stroke-width", F(2 * _scale)), new XAttribute("opacity", ".22"),
            Animation("animate", new XAttribute("attributeName", "r"), new XAttribute("values", F(_radius + _scale) + ";" + F(_radius + 8 * _scale) + ";" + F(_radius + _scale))),
            Animation("animate", new XAttribute("attributeName", "opacity"), new XAttribute("values", ".18;.62;.18"))));
        content.Add(overlay);
        return document.ToString(SaveOptions.DisableFormatting);

        XElement Group(string role) => new(ns + "g", new XAttribute("data-cfx-role", role), new XAttribute("data-cfx-motion-source", _source), new XAttribute("data-cfx-motion-kind", "route-pulse"));
        XElement Animation(string name, params object[] attributes) => new(ns + name, attributes,
            new XAttribute("dur", F(_motion.DurationSeconds) + "s"), new XAttribute("repeatCount", _motion.Loop ? "indefinite" : "1"),
            new XAttribute("fill", _motion.Loop ? "remove" : "freeze"));
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Path(IReadOnlyList<ChartPoint> points) => string.Join(" ", points.Select((point, i) => (i == 0 ? "M " : "L ") + F(point.X) + " " + F(point.Y)));
    private sealed class Route {
        internal Route(string id, string path, string color) { Id = id; Path = path; Color = color; }
        internal string Id { get; }
        internal string Path { get; }
        internal string Color { get; }
    }
}
