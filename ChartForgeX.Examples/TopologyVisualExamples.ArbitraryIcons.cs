using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private static TopologyIconCatalog BuildSecureAccessIconCatalog() {
        return TopologyIconCatalog.Default().AddPack(new TopologyIconPack("secure-access", "Secure Access Diagram Icons", vendor: "ChartForgeX sample")
            .WithTags("secure-access", "sample", "inline-svg")
            .AddIcon(new TopologyIconDefinition("secure-access", "tenant-cloud", "Tenant Cloud", TopologyNodeKind.Cloud, TopologyIconShape.Cloud) {
                Symbol = "TEN",
                Color = "#0E7490",
                Category = "Tenant",
                DisplayMode = TopologyNodeDisplayMode.Icon,
                Artwork = TopologyIconArtwork.InlineSvg("<path d=\"M14 34 H38 C44 34 48 30 48 24 C48 18 44 14 38 14 C36 7 30 4 24 6 C19 7 15 11 14 16 C8 16 4 20 4 25 C4 31 8 34 14 34 Z\" fill=\"#1D9ED8\"/><path d=\"M25 10 L35 20 L25 30 L15 20 Z M25 15 L20 20 L25 25 L30 20 Z\" fill=\"#FFFFFF\" opacity=\"0.88\"/>", "0 0 52 42")
            }.WithTags("tenant", "cloud", "identity"))
            .AddIcon(new TopologyIconDefinition("secure-access", "device-stack", "Device Stack", TopologyNodeKind.Endpoint, TopologyIconShape.Desktop) {
                Symbol = "DEV",
                Color = "#334155",
                Category = "Endpoint",
                DisplayMode = TopologyNodeDisplayMode.Tile,
                Artwork = TopologyIconArtwork.InlineSvg("<rect x=\"10\" y=\"13\" width=\"26\" height=\"19\" rx=\"3\" fill=\"#111827\"/><rect x=\"13\" y=\"16\" width=\"20\" height=\"12\" rx=\"1.8\" fill=\"#F8FAFC\"/><rect x=\"19\" y=\"34\" width=\"8\" height=\"3\" rx=\"1\" fill=\"#111827\"/><rect x=\"5\" y=\"20\" width=\"12\" height=\"20\" rx=\"3\" fill=\"#111827\"/><rect x=\"8\" y=\"25\" width=\"6\" height=\"10\" rx=\"1.2\" fill=\"#F8FAFC\"/>", "0 0 44 44")
            }.WithTags("device", "client", "endpoint"))
            .AddIcon(new TopologyIconDefinition("secure-access", "lock", "Policy Lock", TopologyNodeKind.Service, TopologyIconShape.Service) {
                Symbol = "POL",
                Color = "#0E7490",
                Category = "Policy",
                DisplayMode = TopologyNodeDisplayMode.Icon,
                Artwork = TopologyIconArtwork.InlineSvg("<circle cx=\"22\" cy=\"22\" r=\"18\" fill=\"#0E7490\"/><rect x=\"14\" y=\"20\" width=\"16\" height=\"13\" rx=\"3\" fill=\"#FFFFFF\"/><path d=\"M17 20 v-5 a5 5 0 0 1 10 0 v5\" fill=\"none\" stroke=\"#FFFFFF\" stroke-width=\"3\" stroke-linecap=\"round\"/><circle cx=\"22\" cy=\"26\" r=\"2\" fill=\"#0E7490\"/>", "0 0 44 44")
            }.WithTags("policy", "lock", "secure"))
            .AddIcon(new TopologyIconDefinition("secure-access", "gateway", "Tunnel Gateway", TopologyNodeKind.Gateway, TopologyIconShape.LoadBalancer) {
                Symbol = "GW",
                Color = "#0E7490",
                Category = "Gateway",
                Artwork = TopologyIconArtwork.InlineSvg("<rect x=\"6\" y=\"13\" width=\"36\" height=\"18\" rx=\"9\" fill=\"#0E7490\"/><path d=\"M15 22 H31 M26 17 L32 22 L26 27\" fill=\"none\" stroke=\"#FFFFFF\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>", "0 0 48 44")
            }.WithTags("gateway", "tunnel"))
            .AddIcon(new TopologyIconDefinition("secure-access", "saas", "SaaS", TopologyNodeKind.Cloud, TopologyIconShape.Application) {
                Symbol = "365",
                Color = "#6366F1",
                Category = "Destination",
                DisplayMode = TopologyNodeDisplayMode.Card,
                Artwork = TopologyIconArtwork.InlineSvg("<path d=\"M24 5 L40 14 V32 L24 41 L8 32 V14 Z\" fill=\"#6366F1\"/><path d=\"M24 11 L34 17 V29 L24 35 L14 29 V17 Z\" fill=\"#22D3EE\" opacity=\"0.78\"/><path d=\"M24 17 L29 20 V26 L24 29 L19 26 V20 Z\" fill=\"#FFFFFF\" opacity=\"0.92\"/>", "0 0 48 48")
            }.WithTags("saas", "m365", "cloud"))
            .AddIcon(new TopologyIconDefinition("secure-access", "internet", "Internet", TopologyNodeKind.Network, TopologyIconShape.Network) {
                Symbol = "NET",
                Color = "#111827",
                Category = "Destination",
                DisplayMode = TopologyNodeDisplayMode.Card,
                Artwork = TopologyIconArtwork.InlineSvg("<circle cx=\"24\" cy=\"24\" r=\"18\" fill=\"none\" stroke=\"#111827\" stroke-width=\"3\"/><path d=\"M6 24 H42 M24 6 C16 14 16 34 24 42 M24 6 C32 14 32 34 24 42\" fill=\"none\" stroke=\"#111827\" stroke-width=\"2.4\" stroke-linecap=\"round\"/>", "0 0 48 48")
            }.WithTags("internet", "web", "ai"))
            .AddIcon(new TopologyIconDefinition("secure-access", "datacenter", "Datacenter", TopologyNodeKind.Location, TopologyIconShape.Site) {
                Symbol = "DC",
                Color = "#111827",
                Category = "Destination",
                DisplayMode = TopologyNodeDisplayMode.Card,
                Artwork = TopologyIconArtwork.InlineSvg("<path d=\"M10 40 V12 H27 L38 23 V40\" fill=\"none\" stroke=\"#111827\" stroke-width=\"3\" stroke-linejoin=\"round\"/><path d=\"M27 12 V23 H38 M16 20 H23 M16 27 H23 M16 34 H23 M30 30 H34 M30 36 H34\" fill=\"none\" stroke=\"#111827\" stroke-width=\"2.4\" stroke-linecap=\"round\"/>", "0 0 48 48")
            }.WithTags("iaas", "paas", "datacenter")));
    }

    private static TopologyIconArtwork BuildServiceEdgeCloudArtwork() {
        return TopologyIconArtwork.InlineSvg("<path d=\"M150 248 H424 C492 248 538 208 538 154 C538 99 491 58 430 67 C405 25 356 7 310 21 C272 32 244 61 231 98 C189 84 139 101 119 139 C75 144 42 177 42 218 C42 238 88 248 150 248 Z\" fill=\"#EAF4EE\" opacity=\"0.82\"/><path d=\"M150 248 H424 C492 248 538 208 538 154 C538 99 491 58 430 67 C405 25 356 7 310 21 C272 32 244 61 231 98 C189 84 139 101 119 139 C75 144 42 177 42 218 C42 238 88 248 150 248 Z\" fill=\"none\" stroke=\"#D8E8DE\" stroke-width=\"6\"/><circle cx=\"190\" cy=\"160\" r=\"6\" fill=\"#0E7490\" opacity=\"0.22\"/><circle cx=\"270\" cy=\"118\" r=\"7\" fill=\"#FACC15\" opacity=\"0.34\"/><circle cx=\"352\" cy=\"148\" r=\"5\" fill=\"#0E7490\" opacity=\"0.24\"/><circle cx=\"310\" cy=\"195\" r=\"6\" fill=\"#15803D\" opacity=\"0.28\"/><path d=\"M118 205 C180 170 233 185 280 150 S384 120 452 158\" fill=\"none\" stroke=\"#D6EAD9\" stroke-width=\"3\" opacity=\"0.7\"/>", "0 0 580 280");
    }

    private static TopologyIconArtwork BuildIdentityCloudArtwork() {
        return TopologyIconArtwork.InlineSvg("<path d='M50 100 H175 C207 100 230 79 230 50 C230 22 205 2 177 10 C164 -6 135 -9 112 4 C94 14 81 31 75 52 C56 45 32 55 23 74 C10 76 2 86 2 99 C2 108 18 112 50 100 Z' fill='#1D9ED8'/><path d='M116 21 L146 51 L116 81 L86 51 Z M116 32 L97 51 L116 70 L135 51 Z M116 39 L128 51 L116 63 L104 51 Z' fill='#FFFFFF' opacity='0.88'/><text x='116' y='70' text-anchor='middle' fill='#FFFFFF' font-family='Inter,Segoe UI,Arial,sans-serif' font-size='13' font-weight='700'>Microsoft Entra ID /</text><text x='116' y='88' text-anchor='middle' fill='#FFFFFF' font-family='Inter,Segoe UI,Arial,sans-serif' font-size='13' font-weight='700'>Cross Tenant Access</text>", "0 0 232 116")
            .WithPreserveAspectRatio("none");
    }

    private static TopologyIconArtwork BuildTenantCloudArtwork(string fill, bool includeApps) {
        var apps = includeApps
            ? "<rect x='144' y='22' width='14' height='14' fill='#0078D4'/><rect x='160' y='22' width='14' height='14' fill='#61B7FF'/><rect x='144' y='38' width='14' height='14' fill='#1D9ED8'/><rect x='160' y='38' width='14' height='14' fill='#8BD7FF'/><rect x='186' y='12' width='28' height='26' rx='3' fill='#1E5A9E'/><text x='200' y='29' text-anchor='middle' fill='#FFFFFF' font-family='Consolas,monospace' font-size='14' font-weight='700'>&lt;/&gt;</text><path d='M196 55 L214 65 V85 L196 95 L178 85 V65 Z' fill='none' stroke='#8B5CF6' stroke-width='3'/><path d='M178 65 L196 75 L214 65 M196 75 V95' fill='none' stroke='#22D3EE' stroke-width='2.4'/>"
            : "<rect x='164' y='34' width='24' height='22' rx='4' fill='#A78BFA'/><path d='M176 28 L199 41 L199 68 L176 81 L153 68 L153 41 Z' fill='none' stroke='#38BDF8' stroke-width='3'/><rect x='194' y='42' width='28' height='26' rx='3' fill='#1E5A9E'/><text x='208' y='59' text-anchor='middle' fill='#FFFFFF' font-family='Consolas,monospace' font-size='14' font-weight='700'>&lt;/&gt;</text>";
        return TopologyIconArtwork.InlineSvg("<path d='M30 76 H108 C122 76 132 67 132 55 C132 43 123 35 111 35 C106 20 91 12 76 17 C64 20 56 29 52 42 C44 37 31 39 24 47 C12 48 4 57 4 68 C4 75 12 76 30 76 Z' fill='" + fill + "'/><path d='M71 25 L91 45 L71 65 L51 45 Z M71 33 L59 45 L71 57 L83 45 Z M71 39 L77 45 L71 51 L65 45 Z' fill='#FFFFFF' opacity='0.82'/>" + apps, "0 0 228 104")
            .WithPreserveAspectRatio("xMidYMid meet");
    }

    private static TopologyIconArtwork BuildClientDeviceArtwork(string color) {
        return TopologyIconArtwork.InlineSvg("<rect x='70' y='4' width='68' height='48' rx='6' fill='none' stroke='" + color + "' stroke-width='6'/><rect x='15' y='22' width='34' height='62' rx='7' fill='none' stroke='" + color + "' stroke-width='6'/><rect x='23' y='69' width='18' height='4' rx='2' fill='" + color + "'/><text x='80' y='112' text-anchor='middle' fill='#111827' font-family='Inter,Segoe UI,Arial,sans-serif' font-size='15' font-weight='800'>Devices with</text><text x='80' y='130' text-anchor='middle' fill='#111827' font-family='Inter,Segoe UI,Arial,sans-serif' font-size='15' font-weight='800'>Global Secure Access</text><text x='80' y='148' text-anchor='middle' fill='#111827' font-family='Inter,Segoe UI,Arial,sans-serif' font-size='15' font-weight='800'>clients</text><rect x='12' y='166' width='17' height='17' fill='#0078D4'/><rect x='31' y='166' width='17' height='17' fill='#1D9ED8'/><rect x='12' y='185' width='17' height='17' fill='#1D9ED8'/><rect x='31' y='185' width='17' height='17' fill='#0078D4'/><path d='M78 181 C76 170 84 163 91 165 C93 159 99 156 104 158 C101 164 96 166 91 166 C96 170 106 169 111 177 C116 186 110 201 103 201 C98 201 96 198 91 198 C86 198 83 201 78 201 C70 201 62 187 67 177 C70 171 74 181 78 181 Z' fill='#BDBDBD'/><path d='M139 171 C151 171 160 180 160 191 C160 202 151 211 139 211 C127 211 118 202 118 191 C118 180 127 171 139 171 Z' fill='#E6F5D6'/><path d='M126 184 V199 M152 184 V199 M129 179 L125 173 M149 179 L153 173 M133 191 H145' stroke='#D5EAC8' stroke-width='4' stroke-linecap='round'/>", "0 0 176 216")
            .WithPreserveAspectRatio("xMidYMid meet");
    }

    private static TopologyIconArtwork BuildDestinationCardArtwork(string label, string icon) {
        string iconSvg = icon switch {
            "m365" => "<path d='M74 23 L110 43 V83 L74 103 L38 83 V43 Z' fill='#6366F1'/><path d='M74 36 L96 49 V76 L74 90 L52 76 V49 Z' fill='#22D3EE' opacity='0.82'/><path d='M74 50 L86 57 V70 L74 77 L62 70 V57 Z' fill='#FFFFFF' opacity='0.9'/>",
            "internet" => "<circle cx='74' cy='58' r='29' fill='none' stroke='#111827' stroke-width='5'/><path d='M45 58 H103 M74 29 C61 43 61 73 74 87 M74 29 C87 43 87 73 74 87' fill='none' stroke='#111827' stroke-width='4' stroke-linecap='round'/>",
            _ => "<path d='M48 99 V35 H82 L110 63 V99' fill='none' stroke='#111827' stroke-width='5' stroke-linejoin='round'/><path d='M82 35 V63 H110 M59 52 H73 M59 66 H73 M59 81 H73 M91 78 H100 M91 92 H100' fill='none' stroke='#111827' stroke-width='4' stroke-linecap='round'/>"
        };
        var text = label.Contains('\n', StringComparison.Ordinal)
            ? "<text x='74' y='119' text-anchor='middle' fill='#111827' font-family='Inter,Segoe UI,Arial,sans-serif' font-size='17' font-weight='800'>" + label.Split('\n')[0] + "</text><text x='74' y='140' text-anchor='middle' fill='#111827' font-family='Inter,Segoe UI,Arial,sans-serif' font-size='17' font-weight='800'>" + label.Split('\n')[1] + "</text>"
            : "<text x='74' y='126' text-anchor='middle' fill='#111827' font-family='Inter,Segoe UI,Arial,sans-serif' font-size='17' font-weight='800'>" + label + "</text>";
        return TopologyIconArtwork.InlineSvg("<rect x='0' y='0' width='148' height='148' rx='4' fill='#F3F4F6'/>" + iconSvg + text, "0 0 148 148")
            .WithPreserveAspectRatio("none");
    }

    private static TopologyChart BuildSecureAccessArbitraryIconTopology() {
        return TopologyChart.Create()
            .WithId("visual-secure-access-arbitrary-icons")
            .WithTitle("Secure Access Arbitrary Icon Topology")
            .WithSubtitle("Manual relationship diagram using catalog and node-supplied inline SVG artwork, icon-only nodes, gateway bars, panels, dashed tunnels, and destination cards.")
            .WithViewport(1280, 655, 22)
            .WithTheme(TopologyTheme.Light())
            .WithLegend(TopologyLegend.Create("Flow Legend")
                .AddNodeKind("Tenant", TopologyNodeKind.Cloud, "#0E7490", "TEN", iconId: "secure-access:tenant-cloud")
                .AddNodeKind("Gateway", TopologyNodeKind.Gateway, "#0E7490", "GW", iconId: "secure-access:gateway")
                .AddNodeKind("Destination", TopologyNodeKind.Location, "#111827", "DST", iconId: "secure-access:datacenter")
                .AddEdgeKind("Policy Tunnel", TopologyEdgeKind.AuthenticationPath, "#0E7490", TopologyEdgeLineStyle.Dashed)
                .AddEdgeKind("Service Route", TopologyEdgeKind.DataFlow, "#0E7490", TopologyEdgeLineStyle.Solid))
            .AddGroup("resource-tenant", "Resource Tenant", 22, 43, 248, 282, TopologyHealthStatus.Healthy, symbol: "TEN", color: "#0E7490", iconId: "secure-access:tenant-cloud")
            .AddGroup("home-tenant", "User's Home Tenant", 22, 343, 248, 282, TopologyHealthStatus.Unknown, symbol: "TEN", color: "#64748B", iconId: "secure-access:tenant-cloud")
            .AddArtworkNode("resource-cloud", "Resource Tenant", BuildTenantCloudArtwork("#1D9ED8", true), 52, 68, TopologyNodeKind.Cloud, TopologyHealthStatus.Unknown, "resource-tenant", width: 178, height: 84, symbol: "TEN", color: "#0E7490")
            .AddArtworkNode("resource-devices", "Devices with Global Secure Access clients", BuildClientDeviceArtwork("#111827"), 86, 154, TopologyNodeKind.Endpoint, TopologyHealthStatus.Unknown, "resource-tenant", width: 152, height: 158, symbol: "DEV", color: "#334155")
            .AddArtworkNode("home-cloud", "User Home Tenant", BuildTenantCloudArtwork("#8A8A8A", false), 52, 368, TopologyNodeKind.Cloud, TopologyHealthStatus.Unknown, "home-tenant", width: 178, height: 84, symbol: "TEN", color: "#64748B")
            .AddArtworkNode("home-devices", "Devices with Global Secure Access clients", BuildClientDeviceArtwork("#808080"), 86, 454, TopologyNodeKind.Endpoint, TopologyHealthStatus.Unknown, "home-tenant", width: 152, height: 158, symbol: "DEV", color: "#64748B")
            .AddNode("resource-lock", "Policy", 292, 160, TopologyNodeKind.Service, TopologyHealthStatus.Unknown, width: 44, height: 44, symbol: "POL", iconId: "secure-access:lock", color: "#0E7490")
            .AddNode("home-lock", "Policy", 292, 460, TopologyNodeKind.Service, TopologyHealthStatus.Unknown, width: 44, height: 44, symbol: "POL", iconId: "secure-access:lock", color: "#0E7490")
            .AddArtworkNode("entra", "Microsoft Entra ID / Cross Tenant Access", BuildIdentityCloudArtwork(), 625, 38, TopologyNodeKind.Cloud, TopologyHealthStatus.Unknown, width: 224, height: 114, symbol: "ID", color: "#1D9ED8")
            .AddArtworkNode("service-edge-cloud", "Service Edge Cloud", BuildServiceEdgeCloudArtwork(), 468, 192, TopologyNodeKind.Cloud, TopologyHealthStatus.Unknown, width: 540, height: 292, symbol: "SSE", color: "#0E7490")
            .AddNode("gateway-policy", "Microsoft tunnel gateways", 598, 291, TopologyNodeKind.Gateway, TopologyHealthStatus.Unknown, width: 308, height: 34, symbol: "GW", iconId: "secure-access:gateway", color: "#FACC15", backgroundColor: "#FEF3C7")
            .AddNode("gateway-internet", "Internet Access tunnel gateways", 598, 340, TopologyNodeKind.Gateway, TopologyHealthStatus.Unknown, width: 308, height: 34, symbol: "GW", iconId: "secure-access:gateway", color: "#0E7490")
            .AddNode("gateway-private", "Private Access tunnel gateways", 598, 390, TopologyNodeKind.Gateway, TopologyHealthStatus.Unknown, width: 308, height: 34, symbol: "GW", iconId: "secure-access:gateway", color: "#15803D")
            .AddNode("sse", "Microsoft's Security Service Edge (SSE) solution", 510, 435, TopologyNodeKind.Service, TopologyHealthStatus.Unknown, width: 500, height: 40, symbol: "SSE", color: "#334155", backgroundColor: "#2F4F83")
            .AddArtworkNode("m365", "Microsoft 365", BuildDestinationCardArtwork("Microsoft 365", "m365"), 1085, 45, TopologyNodeKind.Cloud, TopologyHealthStatus.Unknown, width: 170, height: 148, symbol: "365", color: "#6366F1")
            .AddArtworkNode("internet", "AI & Internet", BuildDestinationCardArtwork("AI & Internet", "internet"), 1085, 262, TopologyNodeKind.Network, TopologyHealthStatus.Unknown, width: 170, height: 148, symbol: "NET", color: "#111827")
            .AddArtworkNode("datacenter", "IaaS, PaaS,\nDatacenter", BuildDestinationCardArtwork("IaaS, PaaS\nDatacenter", "datacenter"), 1085, 480, TopologyNodeKind.Location, TopologyHealthStatus.Unknown, width: 170, height: 148, symbol: "DC", color: "#111827")
            .AddEdge("resource-policy", "resource-devices", "resource-lock", "user aware policy distribution", TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Healthy, TopologyDirection.Backward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "over tunnels", color: "#111827")
            .AddEdge("home-policy", "home-devices", "home-lock", "policy tunnel", TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Healthy, TopologyDirection.Backward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, color: "#0E7490")
            .AddEdge("resource-gateway", "resource-lock", "gateway-policy", null, TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, color: "#111827")
            .AddEdge("home-gateway", "home-lock", "gateway-private", null, TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, color: "#0E7490")
            .AddEdge("entra-policy", "entra", "gateway-policy", "policy", TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, color: "#0E7490")
            .AddEdge("gateway-m365", "gateway-policy", "m365", null, TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, color: "#0E7490")
            .AddEdge("gateway-internet-route", "gateway-internet", "internet", null, TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, color: "#0E7490")
            .AddEdge("gateway-private-route", "gateway-private", "datacenter", null, TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, color: "#0E7490")
            .WithNodeDisplay("resource-lock", TopologyNodeDisplayMode.Icon)
            .WithNodeDisplay("home-lock", TopologyNodeDisplayMode.Icon)
            .WithEdgeLineStyle("resource-policy", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("home-policy", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("resource-gateway", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("home-gateway", TopologyEdgeLineStyle.Dashed)
            .WithEdgePorts("resource-policy", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("home-policy", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("resource-gateway", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("home-gateway", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("gateway-m365", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("gateway-internet-route", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("gateway-private-route", TopologyEdgePort.Right, TopologyEdgePort.Left);
    }
}
