using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Verify = Xunit.Assert;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SpecializedSvgSegmentsExposeDataMetadata() {
        // Source groups carry data independently of painter node names and XML attribute order.
        static VisualSceneGroup Source(PreparedVisual prepared, string role, string id) {
            var group = Verify.Single(FamilyGroups(prepared, role), candidate => candidate.Id == id);
            var svgSource = Verify.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), element =>
                (string?)element.Attribute("data-cfx-source-id") == id && (string?)element.Attribute("data-cfx-role") == role);
            Verify.All(group.Metadata, pair => Verify.Equal(pair.Value, (string?)svgSource.Attribute(pair.Key)));
            Verify.Contains(prepared.Regions, region => region.Id == id);
            Verify.NotEmpty(prepared.ToPng());
            return group;
        }

        var polar = PreparedFamily(Chart.Create().WithXLabels("Coverage", "Policy", "Alerts").AddPolarArea("Control share", Points(60, 30, 10)));
        var polarSource = Source(polar, "polar-area-point-source", "series-0-point-0");
        Verify.Equal("Coverage", polarSource.Metadata["data-cfx-label"]);
        Verify.Equal(60, FamilyNumber(polarSource, "data-cfx-value"));
        Verify.Equal(.6, FamilyNumber(polarSource, "data-cfx-percent"));
        var sectors = polar.Scene.Nodes.OfType<VisualSceneSlice>().Where(mark => mark.Role == "polar-area-segment").ToArray();
        Verify.Equal(3, sectors.Length);
        Verify.All(sectors, sector => Verify.Equal(Math.PI * 2 / 3, sector.Sweep, 10));
        Verify.Equal(.5, Math.Pow(sectors[1].Outer / sectors[0].Outer, 2), 10);

        var funnel = PreparedFamily(Chart.Create().WithXLabels("Discovered", "Validated", "Fixed").AddFunnel("Pipeline", Points(100, 70, 35)));
        var stage = Source(funnel, "funnel-stage", "series-0-point-1");
        Verify.Equal("Validated", stage.Metadata["data-cfx-label"]);
        Verify.Equal(70, FamilyNumber(stage, "data-cfx-value"));
        Verify.Equal(.7, FamilyNumber(stage, "data-cfx-retention"));
        Verify.Equal(.3, FamilyNumber(stage, "data-cfx-dropoff"), 10);
        Verify.Equal(3, funnel.Scene.Nodes.OfType<VisualScenePath>().Count(mark => mark.Role == "funnel-segment" && mark.Close));

        var radial = PreparedFamily(Chart.Create().WithXLabels("Identity", "Device", "Network").AddProgressRing("Coverage", Points(92, 74, 66)));
        var radialSource = Source(radial, "progress-ring-point", "series-0-point-0");
        Verify.Equal("Identity", radialSource.Metadata["data-cfx-label"]);
        Verify.Equal(92, FamilyNumber(radialSource, "data-cfx-value"));
        Verify.Equal(0, FamilyNumber(radialSource, "data-cfx-min"));
        Verify.Equal(100, FamilyNumber(radialSource, "data-cfx-max"));
        var ring = radial.Scene.Nodes.OfType<VisualSceneSlice>().First(mark => mark.Role == "progress-ring-ring");
        Verify.Equal(.92, ring.Sweep / (Math.PI * 2), 10);
        var positionedRadial = PreparedFamily(Chart.Create().WithLegendPosition(ChartLegendPosition.TopRight)
            .WithXLabels("Identity", "Device", "Network").AddProgressRing("Coverage", Points(92, 74, 66)));
        Verify.Equal(new[] { "Identity", "Device", "Network" }, FamilyLabels(positionedRadial, "legend-label").Select(FamilyContent));
        var firstRing = positionedRadial.Scene.Nodes.OfType<VisualSceneSlice>().First(mark => mark.Role == "progress-ring-ring");
        Verify.True(positionedRadial.Regions.Where(region => region.Role == "legend").Max(region => region.Bounds.Bottom) < firstRing.Cy);
        Verify.NotEmpty(positionedRadial.ToPng());

        var gauge = PreparedFamily(Chart.Create().AddGauge("Score", 84, 0, 100));
        var gaugeSource = Source(gauge, "gauge", "series-0");
        Verify.Equal(84, FamilyNumber(gaugeSource, "data-cfx-value"));
        Verify.Equal(0, FamilyNumber(gaugeSource, "data-cfx-min"));
        Verify.Equal(100, FamilyNumber(gaugeSource, "data-cfx-max"));
        Verify.Equal(.84, FamilyNumber(gaugeSource, "data-cfx-percent"));
        Verify.Equal(ChartSeriesState.None.ToString(), gaugeSource.Metadata["data-cfx-status"]);
        Verify.Equal("Score: 84", gaugeSource.Metadata["aria-label"]);
        var gaugeArc = Verify.Single(gauge.Scene.Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "gauge-value");
        Verify.Equal(.84, gaugeArc.Sweep / (Math.PI * 4 / 3), 10);

        var circle = PreparedFamily(Chart.Create().AddCircle("Readiness", 72, 0, 100));
        var circleSource = Source(circle, "circle-chart", "series-0");
        Verify.Equal(72, FamilyNumber(circleSource, "data-cfx-value"));
        Verify.Equal(.72, FamilyNumber(circleSource, "data-cfx-percent"));
        Verify.Contains(circle.Regions, region => region.Id == "series-0" && region.Label == "Readiness: 72");
        var circleArc = Verify.Single(circle.Scene.Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "circle-value");
        Verify.Equal(.72, circleArc.Sweep / (Math.PI * 2), 10);

        var bullet = PreparedFamily(Chart.Create().AddBullet("DMARC", 82, 90, 0, 100, new[] { 60d, 80d }));
        var bulletSource = Source(bullet, "bullet-row", "series-0");
        Verify.Equal("DMARC", bulletSource.Metadata["data-cfx-label"]);
        Verify.Equal("below-target", bulletSource.Metadata["data-cfx-status"]);
        Verify.Equal(82, FamilyNumber(bulletSource, "data-cfx-value"));
        Verify.Equal(90, FamilyNumber(bulletSource, "data-cfx-target"));
        Verify.Equal(0, FamilyNumber(bulletSource, "data-cfx-min"));
        Verify.Equal(100, FamilyNumber(bulletSource, "data-cfx-max"));
        Verify.Equal("60,80", bulletSource.Metadata["data-cfx-source-range-ends"]);
        var bulletValue = Verify.Single(bullet.Scene.Nodes.OfType<VisualSceneRectangle>(), mark => mark.Role == "bullet-value");
        var bulletTarget = Verify.Single(bullet.Scene.Nodes.OfType<VisualSceneLine>(), mark => mark.Role == "bullet-target");
        Verify.True(bulletValue.Bounds.Right < bulletTarget.Start.X);

        var waterfall = PreparedFamily(Chart.Create().WithXLabels("Opened", "Closed").AddWaterfall("Findings", Points(18, -7)));
        var waterfallSource = Source(waterfall, "point", "series-0-point-1");
        Verify.Equal(18, FamilyNumber(waterfallSource, "data-cfx-start"));
        Verify.Equal(11, FamilyNumber(waterfallSource, "data-cfx-end"));
        Verify.Equal(-7, FamilyNumber(waterfallSource, "data-cfx-delta"));
        Verify.Equal("1", waterfallSource.Metadata["data-cfx-source-point"]);
        Verify.Contains(waterfall.Regions, region => region.Id == "series-0-point-1" && region.Label!.Contains("start=18 end=11 Change=-7", StringComparison.Ordinal));
    }
}
