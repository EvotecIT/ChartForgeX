# Who calls whom, from a CMDB export or a config file.
$services = @'
Id,Name,Kind,Tier,Status,Callers,Latency
gateway,API gateway,Gateway,0,Healthy,,
identity,Identity,Service,1,Healthy,gateway,
orders,Orders,Service,1,Warning,gateway,
search,Search,Service,1,Healthy,gateway,
sql,Orders DB,Database,2,Warning,orders,41 ms
events,Events,Queue,2,Healthy,orders search,
'@ | ConvertFrom-Csv

$map = {
    # Tier sets the column; position in the tier sets the row.
    foreach ($tier in $services | Group-Object Tier) {
        $row = 0
        foreach ($s in $tier.Group) {
            $node = @{
                Id = $s.Id; Label = $s.Name; Kind = $s.Kind
                Status = $s.Status; Width = 180; Height = 64
                X = 300 * [int] $tier.Name
                Y = 100 * ($row++ - ($tier.Count - 1) / 2)
            }
            New-ImageTopologyNode @node
        }
    }
    foreach ($s in $services) {
        foreach ($caller in (-split $s.Callers) -ne '') {
            New-ImageTopologyEdge $caller $s.Id $s.Latency `
                -Direction Forward -Status $s.Status
        }
    }
}

$view = @{
    TopologyDefinition   = $map
    Title                = 'Orders platform'
    Subtitle             = 'Database latency above target'
    Layout               = 'Manual'
    Width                = 900
    Height               = 420
    FitContentToViewport = $true
}
New-ImageTopology @view -FilePath service-map.png
New-ImageTopology @view -FilePath service-map.html -InteractiveHtml

$services | Format-Table Name, Kind, Status, Callers -AutoSize
