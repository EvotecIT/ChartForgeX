using System.Globalization;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void WritePhysicsAttributes(StringBuilder writer, GraphScene scene) {
        var physics = scene.Options.Physics;
        Attribute(writer, "data-cfx-graph-physics", PhysicsSolverName(physics.Solver));
        Attribute(writer, "data-cfx-physics-min-velocity", Number(physics.MinVelocity));
        Attribute(writer, "data-cfx-physics-max-velocity", Number(physics.MaxVelocity));
        Attribute(writer, "data-cfx-physics-timestep", Number(physics.Timestep));
        Attribute(writer, "data-cfx-physics-adaptive-timestep", physics.AdaptiveTimestep ? "true" : "false");
        Attribute(writer, "data-cfx-physics-stabilization-enabled", physics.Stabilization.Enabled ? "true" : "false");
        Attribute(writer, "data-cfx-physics-stabilization-iterations", physics.Stabilization.Iterations.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-physics-stabilization-update-interval", physics.Stabilization.UpdateInterval.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-physics-stabilization-fit", physics.Stabilization.Fit ? "true" : "false");
        WriteSolverAttributes(writer, "barnes-hut", physics.BarnesHut, physics.BarnesHut.Theta, physics.BarnesHut.GravitationalConstant, null, null);
        WriteSolverAttributes(writer, "force-atlas2", physics.ForceAtlas2, physics.ForceAtlas2.Theta, physics.ForceAtlas2.GravitationalConstant, null, null);
        WriteSolverAttributes(writer, "repulsion", physics.Repulsion, null, null, physics.Repulsion.NodeDistance, physics.Repulsion.Strength);
        WriteSolverAttributes(writer, "hierarchical-repulsion", physics.HierarchicalRepulsion, null, null, physics.HierarchicalRepulsion.NodeDistance, physics.HierarchicalRepulsion.Strength);

        var interaction = scene.Options.Interaction;
        Attribute(writer, "data-cfx-graph-drag-behavior", interaction.NodeDragBehavior == GraphNodeDragBehavior.PinOnDrop ? "pin-on-drop" : "release-and-reheat");
        Attribute(writer, "data-cfx-graph-drag-live-physics", interaction.SimulateConnectedNodesWhileDragging ? "true" : "false");
        Attribute(writer, "data-cfx-graph-drag-momentum", Number(interaction.DragMomentum));
        Attribute(writer, "data-cfx-graph-reheat-drag", interaction.ReheatAfterDrag ? "true" : "false");
        Attribute(writer, "data-cfx-graph-reheat-cluster", interaction.ReheatAfterClusterChange ? "true" : "false");
        Attribute(writer, "data-cfx-graph-reheat-hierarchy", interaction.ReheatAfterHierarchyChange ? "true" : "false");
        Attribute(writer, "data-cfx-graph-reheat-patch", interaction.ReheatAfterGraphChange ? "true" : "false");
    }

    private static void WriteSolverAttributes(StringBuilder writer, string prefix, GraphPhysicsSolverOptions options, double? theta, double? gravitationalConstant, double? nodeDistance, double? strength) {
        Attribute(writer, "data-cfx-physics-" + prefix + "-central-gravity", Number(options.CentralGravity));
        Attribute(writer, "data-cfx-physics-" + prefix + "-spring-length", Number(options.SpringLength));
        Attribute(writer, "data-cfx-physics-" + prefix + "-spring-constant", Number(options.SpringConstant));
        Attribute(writer, "data-cfx-physics-" + prefix + "-damping", Number(options.Damping));
        Attribute(writer, "data-cfx-physics-" + prefix + "-avoid-overlap", Number(options.AvoidOverlap));
        if (theta.HasValue) Attribute(writer, "data-cfx-physics-" + prefix + "-theta", Number(theta.Value));
        if (gravitationalConstant.HasValue) Attribute(writer, "data-cfx-physics-" + prefix + "-gravitational-constant", Number(gravitationalConstant.Value));
        if (nodeDistance.HasValue) Attribute(writer, "data-cfx-physics-" + prefix + "-node-distance", Number(nodeDistance.Value));
        if (strength.HasValue) Attribute(writer, "data-cfx-physics-" + prefix + "-strength", Number(strength.Value));
    }

    private static void WritePhysicsConfigurator(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options) {
        if (!options.IncludePhysicsConfigurator || !CanRunRuntimePhysics(scene)) return;
        writer.Append("<details class=\"cfx-graph-physics-configurator\" data-cfx-role=\"graph-physics-configurator\"><summary>Physics lab</summary><div class=\"cfx-graph-physics-grid\">");
        writer.Append("<label>Solver<select data-cfx-physics-field=\"solver\">");
        WriteSolverOption(writer, GraphPhysicsSolver.BarnesHut, scene.Options.Physics.Solver);
        WriteSolverOption(writer, GraphPhysicsSolver.ForceAtlas2, scene.Options.Physics.Solver);
        WriteSolverOption(writer, GraphPhysicsSolver.Repulsion, scene.Options.Physics.Solver);
        WriteSolverOption(writer, GraphPhysicsSolver.HierarchicalRepulsion, scene.Options.Physics.Solver);
        writer.Append("</select></label>");
        WritePhysicsInput(writer, "Theta", "theta", 0.05, 2, 0.05);
        WritePhysicsInput(writer, "Gravity constant", "gravitationalConstant", -100000, -0.01, 1);
        WritePhysicsInput(writer, "Node distance", "nodeDistance", 1, 2000, 1);
        WritePhysicsInput(writer, "Repulsion strength", "strength", 0.01, 20, 0.05);
        WritePhysicsInput(writer, "Central gravity", "centralGravity", 0, 5, 0.005);
        WritePhysicsInput(writer, "Spring length", "springLength", 1, 2000, 1);
        WritePhysicsInput(writer, "Spring strength", "springConstant", 0.0001, 2, 0.005);
        WritePhysicsInput(writer, "Damping", "damping", 0, 1, 0.01);
        WritePhysicsInput(writer, "Avoid overlap", "avoidOverlap", 0, 1, 0.05);
        WritePhysicsInput(writer, "Minimum velocity", "minVelocity", 0.001, 100, 0.01);
        WritePhysicsInput(writer, "Maximum velocity", "maxVelocity", 0.01, 1000, 1);
        WritePhysicsInput(writer, "Timestep", "timestep", 0.01, 5, 0.05);
        WritePhysicsInput(writer, "Iterations", "iterations", 1, 100000, 1);
        writer.Append("<label class=\"cfx-graph-physics-check\"><input type=\"checkbox\" data-cfx-physics-field=\"adaptiveTimestep\"");
        if (scene.Options.Physics.AdaptiveTimestep) writer.Append(" checked");
        writer.Append(">Adaptive timestep</label></div><div class=\"cfx-graph-physics-actions\"><button type=\"button\" data-cfx-physics-action=\"reheat\">Reheat</button><button type=\"button\" data-cfx-physics-action=\"reset\">Reset</button><output data-cfx-role=\"graph-physics-status\" aria-live=\"polite\"></output></div></details>");
    }

    private static void WriteSolverOption(StringBuilder writer, GraphPhysicsSolver solver, GraphPhysicsSolver selected) {
        var name = PhysicsSolverName(solver);
        writer.Append("<option value=\"").Append(name).Append('"');
        if (solver == selected) writer.Append(" selected");
        writer.Append('>').Append(name).Append("</option>");
    }

    private static void WritePhysicsInput(StringBuilder writer, string label, string field, double minimum, double maximum, double step) {
        writer.Append("<label>").Append(Text(label)).Append("<input type=\"number\" data-cfx-physics-field=\"").Append(field).Append("\" min=\"").Append(Number(minimum)).Append("\" max=\"").Append(Number(maximum)).Append("\" step=\"").Append(Number(step)).Append("\"></label>");
    }

    private static string PhysicsSolverName(GraphPhysicsSolver solver) {
        return solver switch {
            GraphPhysicsSolver.Repulsion => "Repulsion",
            GraphPhysicsSolver.BarnesHut => "BarnesHut",
            GraphPhysicsSolver.ForceAtlas2 => "ForceAtlas2",
            GraphPhysicsSolver.HierarchicalRepulsion => "HierarchicalRepulsion",
            GraphPhysicsSolver.None => "None",
            _ => "StaticPrepared"
        };
    }
}
