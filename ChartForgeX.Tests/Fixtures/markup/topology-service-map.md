# Service Map

```chartforgex topology
id service-map
title "Service Dependency Map"
subtitle "Production dependencies and latency"
viewport 1280x760 32
layout layered lr

groups
  platform "Platform" status:healthy color:#2563eb icon:service
  data "Data Layer" status:warning color:#f59e0b icon:database
end

nodes
  api "Public API" kind:service group:platform status:healthy icon:service badge:v2
  worker "Billing Worker" kind:process group:platform status:warning icon:worker
  sql "SQL Primary" kind:database group:data status:warning icon:database subtitle:"failover lag 2m"
end

edges
  api -> worker "queue" kind:dataflow status:warning direction:forward routing:orthogonal
  worker -> sql "84 ms" kind:dependency status:warning direction:forward
end
```
