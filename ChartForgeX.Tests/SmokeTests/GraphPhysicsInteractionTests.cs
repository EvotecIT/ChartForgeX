using System;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GraphPhysicsContractsExposeSolverAndDragBehavior() {
        var scene = SampleGraphScene();
        scene.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.DragNodes);
        scene.Options.Physics.Solver = GraphPhysicsSolver.BarnesHut;
        scene.Options.Physics.Timestep = 0.35;
        scene.Options.Physics.Stabilization.Iterations = 640;
        scene.Options.Physics.Stabilization.UpdateInterval = 16;
        scene.Options.Physics.BarnesHut.Theta = 0.62;
        scene.Options.Physics.BarnesHut.GravitationalConstant = -3600;
        scene.Options.Physics.BarnesHut.SpringLength = 84;
        scene.Options.Physics.BarnesHut.SpringConstant = 0.06;
        scene.Options.Physics.BarnesHut.AvoidOverlap = 0.8;
        scene.Options.Physics.ForceAtlas2.GravitationalConstant = -64;
        scene.Options.Physics.Repulsion.NodeDistance = 140;
        scene.Options.Physics.HierarchicalRepulsion.NodeDistance = 150;
        scene.Options.Interaction.NodeDragBehavior = GraphNodeDragBehavior.ReleaseAndReheat;
        scene.Options.Interaction.DragMomentum = 0.24;
        scene.Validate();

        Assert(scene.Options.Physics.BarnesHut.Theta == 0.62 && scene.Options.Physics.BarnesHut.SpringConstant == 0.06 && scene.Options.Physics.ForceAtlas2.GravitationalConstant == -64, "Graph scenes should retain independent solver tuning instead of sharing one approximate force profile.");
        Assert(scene.Options.Interaction.NodeDragBehavior == GraphNodeDragBehavior.ReleaseAndReheat && scene.Options.Interaction.SimulateConnectedNodesWhileDragging && scene.Options.Interaction.ReheatAfterClusterChange && scene.Options.Interaction.ReheatAfterGraphChange, "Graph interactions should default to live response and topology-change reheating.");

        var invalidTheta = SampleGraphScene();
        invalidTheta.Options.Physics.BarnesHut.Theta = 0;
        AssertThrows<InvalidOperationException>(() => invalidTheta.Validate(), "Graph scenes should reject unusable Barnes-Hut approximation thresholds.");
        var invalidMomentum = SampleGraphScene();
        invalidMomentum.Options.Interaction.DragMomentum = 1.1;
        AssertThrows<InvalidOperationException>(() => invalidMomentum.Validate(), "Graph scenes should reject drag momentum outside the documented range.");
    }

    private static void GraphRuntimeSupportsReleaseReheatAndPhysicsConfiguration() {
        var scene = SampleGraphScene();
        scene.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.DragNodes | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.HierarchyNavigation);
        scene.Options.Physics.Solver = GraphPhysicsSolver.ForceAtlas2;
        scene.Options.Interaction.NodeDragBehavior = GraphNodeDragBehavior.ReleaseAndReheat;
        var html = scene.ToGraphExplorerHtmlPage(options => options.IncludePhysicsConfigurator = true);

        Assert(html.Contains("data-cfx-graph-drag-behavior=\"release-and-reheat\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-drag-live-physics=\"true\"", StringComparison.Ordinal), "HTML graph explorers should serialize release-and-reheat drag behavior explicitly.");
        Assert(html.Contains("data-cfx-role=\"graph-physics-configurator\"", StringComparison.Ordinal) && html.Contains("data-cfx-physics-field=\"springConstant\"", StringComparison.Ordinal) && html.Contains("Physics lab", StringComparison.Ordinal), "The optional physics configurator should expose solver and spring tuning without external dependencies.");
        Assert(html.Contains("runtime.dragging", StringComparison.Ordinal) && html.Contains("type: 'pin'", StringComparison.Ordinal) && html.Contains("type: 'release'", StringComparison.Ordinal), "Worker physics should accept live drag positions and release state while other nodes keep simulating.");
        Assert(html.Contains("reheatPhysics(root, 'cluster-change'", StringComparison.Ordinal) && html.Contains("reheatPhysics(root, 'hierarchy-change'", StringComparison.Ordinal) && html.Contains("reheatPhysics(root, 'graph-patch'", StringComparison.Ordinal), "Cluster, hierarchy, and incremental graph changes should reheat the visible physics state.");
        Assert(html.Contains("physicsMass", StringComparison.Ordinal) && html.Contains("settings.solver === 'ForceAtlas2'", StringComparison.Ordinal) && html.Contains("settings.theta", StringComparison.Ordinal), "Runtime physics should implement degree-weighted ForceAtlas2-style behavior and configurable Barnes-Hut approximation.");
        Assert(html.Contains("physics: (target, configuration)", StringComparison.Ordinal) && html.Contains("cfxgraphphysicschange", StringComparison.Ordinal), "The host API should support programmatic live physics tuning.");

        var staticSvg = scene.ToGraphSvg();
        Assert(!staticSvg.Contains("<script", StringComparison.OrdinalIgnoreCase) && string.Equals(staticSvg, scene.ToGraphSvg(), StringComparison.Ordinal), "Physics-enabled scenes should still produce deterministic script-free static SVG output.");
    }
}
