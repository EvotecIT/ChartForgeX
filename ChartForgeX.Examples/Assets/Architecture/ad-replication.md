# Active Directory replication

One attribute change, written on DC01, travels to every domain controller: change notification inside a site,
a scheduled pull across the site link, and what happens when that link is down.

```chartforgex architecture v1 {#ad-replication title="Active Directory replication" subtitle="How one attribute change reaches every domain controller"}
direction right

user admin "Admin" subtitle:"ad.evotec.xyz"
workstation adminpc "ADMIN-PC" subtitle:"RSAT tools"

group hq "Site: HQ"
  server dc01 "DC01" subtitle:"PDC emulator" status:healthy
  server dc02 "DC02" subtitle:"Bridgehead" status:healthy
end

group warsaw "Site: Warsaw"
  server dc03 "DC03" subtitle:"Bridgehead" status:warning
  server dc04 "DC04" subtitle:"Branch DC" status:healthy
end

admin -> adminpc "signs in"
adminpc -> dc01 "LDAP"
dc01 -> dc02 "RPC"
dc02 -> dc03 "Site link"
dc03 -> dc04 "RPC"

usecase replicate "Replicate a change" description:"A title change written on DC01 reaches DC04"
  admin -> adminpc "Set-ADUser -Title"
  adminpc -> dc01 "LDAP modify" json {
    "dn": "CN=Ana Nowak,OU=Users,DC=ad,DC=evotec,DC=xyz",
    "replace": { "title": "Site manager" }
  }
  dc01 --> adminpc "Success" json { "resultCode": 0 }
  visit dc01 "Commit locally" text "USN 48211, originating write"
  dc01 ->> dc02 "Change notification after 15 s"
  dc02 -> dc01 "DRSGetNCChanges" json { "usnFrom": 48190 }
  dc01 --> dc02 "1 object, 1 attribute" json { "usnTo": 48211, "objects": 1 }
  dc03 -> dc02 "Scheduled pull" json { "usnFrom": 31877, "compress": true }
  dc02 --> dc03 "Changes, compressed" json { "usnTo": 48211, "objects": 1 }
  dc03 ->> dc04 "Change notification"
  dc04 -> dc03 "DRSGetNCChanges"
  dc03 --> dc04 "1 object, 1 attribute"
end

usecase link-down "Site link down" description:"The Warsaw bridgehead cannot reach HQ"
  dc03 -x dc02 "Scheduled pull" text "RPC server is unavailable (1722)"
  visit dc03 "Retries at the next schedule" text "Replication latency grows until the link is back"
end
```
