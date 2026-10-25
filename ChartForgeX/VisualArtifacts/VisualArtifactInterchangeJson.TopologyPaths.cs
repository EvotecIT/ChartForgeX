using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void TopologyPaths(VisualArtifactInterchangeJsonWriter writer, IReadOnlyList<TopologyPathDecoration> paths) {
        if (paths.Count == 0) return;
        writer.Property("pathDecorations"); writer.StartArray();
        foreach (var path in paths) {
            writer.StartObject(); String(writer, "id", path.Id);
            PathCommands(writer, path.Commands);
            OptionalString(writer, "fill", PathColor(path.Fill)); OptionalString(writer, "stroke", PathColor(path.Stroke));
            Number(writer, "strokeWidth", path.StrokeWidth); Boolean(writer, "close", path.Close);
            if (path.DashPattern != null) {
                writer.Property("dashPattern"); writer.StartArray();
                foreach (var value in path.DashPattern) writer.Number(value);
                writer.EndArray();
            }
            writer.EndObject();
        }
        writer.EndArray();
    }

    private static void ReadTopologyPaths(Dictionary<string, GeoJsonValue> root, ICollection<TopologyPathDecoration> target) {
        var paths = OptionalArray(root, "pathDecorations");
        if (paths.Count > TopologyPathDecoration.MaximumPaths) throw new ArgumentException("Topology path decorations exceed 1024 paths.");
        var total = 0;
        foreach (var entry in paths) {
            var item = entry.AsObject("topology path decoration");
            var geometry = ReadPathCommands(item);
            total += geometry.Commands.Count;
            if (total > TopologyPathDecoration.MaximumTotalCommands) throw new ArgumentException("Topology paths exceed 65536 commands.");
            var dashValues = OptionalArray(item, "dashPattern");
            double[]? dash = null;
            if (item.TryGetValue("dashPattern", out var dashValue) && !dashValue.IsNull) {
                if (dashValues.Count == 0 || dashValues.Count > 32) throw new ArgumentException("Path dash patterns require 1 through 32 lengths.");
                dash = new double[dashValues.Count];
                for (var index = 0; index < dash.Length; index++) dash[index] = dashValues[index].AsNumber("path dash length");
            }
            target.Add(new TopologyPathDecoration(RequiredString(item, "id"), new SvgPathData(geometry.Commands), ReadPathColor(item, "fill"), ReadPathColor(item, "stroke"),
                RequiredNumber(item, "strokeWidth"), OptionalBool(item, "close") ?? false, dash));
        }
    }

    private static void PathGeometry(VisualArtifactInterchangeJsonWriter writer, TopologyPathGeometry? geometry) {
        if (geometry == null) return;
        writer.Property("pathGeometry"); writer.StartObject(); PathCommands(writer, geometry.Commands); writer.EndObject();
    }
    private static TopologyPathGeometry? ReadPathGeometry(Dictionary<string, GeoJsonValue> root) {
        var item = OptionalObject(root, "pathGeometry"); return item == null ? null : ReadPathCommands(item);
    }
    private static void PathCommands(VisualArtifactInterchangeJsonWriter writer, IReadOnlyList<SvgPathCommand> commands) {
        writer.Property("commands"); writer.StartArray();
        foreach (var command in commands) {
            writer.StartArray(); writer.String(command.Command.ToString());
            foreach (var value in command.Values) writer.Number(value);
            writer.EndArray();
        }
        writer.EndArray();
    }
    private static TopologyPathGeometry ReadPathCommands(Dictionary<string, GeoJsonValue> item) {
        var commands = OptionalArray(item, "commands");
        if (commands.Count < 2 || commands.Count > TopologyPathDecoration.MaximumCommands) throw new ArgumentException("Topology paths require 2 through 4096 commands.");
        var path = new SvgPathData();
        foreach (var token in commands) {
            var command = token.AsArray("path command");
            if (command.Count == 0) throw new ArgumentException("Path commands require an absolute M, L or C token and its coordinates.");
            var name = command[0].AsString("path command token");
            var arity = name == "C" ? 6 : name == "M" || name == "L" ? 2 : -1;
            if (arity < 0 || command.Count != arity + 1) throw new ArgumentException("Path commands require exact M/L arity 2 or C arity 6.");
            var values = new double[arity];
            for (var i = 0; i < arity; i++) values[i] = command[i + 1].AsNumber("path coordinate");
            path.Add(new SvgPathCommand(name[0], values));
        }
        return new TopologyPathGeometry(path);
    }

    private static string? PathColor(ChartColor? color) => color is { } value ? value.A == 255 ? value.ToHex() : value.ToHexRgba() : null;
    private static ChartColor? ReadPathColor(Dictionary<string, GeoJsonValue> item, string name) =>
        OptionalString(item, name) is string value ? ChartColor.Parse(value) : (ChartColor?)null;
}
