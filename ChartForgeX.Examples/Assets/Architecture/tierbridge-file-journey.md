# TierBridge file journey

A file crosses from a Tier 2 share to a Tier 0 share only after inspection, and only after an owner approves it when
inspection asks for that.

```chartforgex architecture v1 {#tierbridge-file-journey title="TierBridge file journey" subtitle="From the source share to the target share, with inspection and approval"}
direction right

user requester "Requester" subtitle:"Tier 2 admin"

group source "Source zone (Tier 2)"
  share inbox "Drop share" subtitle:"\\\\fs02\\drop$"
end

boundary bridge "TierBridge"
  service watcher "Watcher" subtitle:"new file events"
  process inspection "Inspection" subtitle:"AV, type, hash"
  storage quarantine "Quarantine" status:warning
  service approval "Approval" subtitle:"owner sign-off"
end

user approver "Approver" subtitle:"Tier 0 owner"

group target "Target zone (Tier 0)"
  share destination "Target share" subtitle:"\\\\fs-t0\\in$"
end

requester -> inbox "copy"
inbox -> watcher "event"
watcher -> inspection "inspect"
watcher -> quarantine "hold"
quarantine -> approval "request"
approval -> approver "ask"
approval -> destination "release"

usecase approved "Approved file" description:"An installer needs an owner's approval before release"
  requester -> inbox "Copy setup.msi" text "18.2 MB"
  inbox ->> watcher "File created" json { "path": "\\\\fs02\\drop$\\setup.msi" }
  watcher -> inspection "Inspect" json { "file": "setup.msi", "sha256": "9f2c41d7" }
  inspection --> watcher "Needs approval" json { "verdict": "approval", "rules": ["executable"] }
  watcher -> quarantine "Hold" text "Moved to quarantine, ticket TB-1042"
  quarantine ->> approval "Request approval" json { "ticket": "TB-1042" }
  approval ->> approver "Ask the owner" text "Approve setup.msi for Tier 0?"
  approver -> approval "Approve"
  approval --> approver "Recorded"
  approval -> destination "Release" json { "ticket": "TB-1042", "released": true }
  visit destination "Released to Tier 0"
end

usecase blocked "Blocked file" description:"Inspection rejects a file and nothing reaches Tier 0"
  requester -> inbox "Copy report.docm"
  inbox ->> watcher "File created" json { "path": "\\\\fs02\\drop$\\report.docm" }
  watcher -> inspection "Inspect" json { "file": "report.docm" }
  inspection --> watcher "Blocked" json { "verdict": "blocked", "rules": ["macro"] }
  watcher -> quarantine "Hold" text "Kept for the security team"
  quarantine ->> approval "Report blocked file" json { "ticket": "TB-1043", "verdict": "blocked" }
  approval -x destination "Release refused" text "Blocked files are never released to Tier 0"
end
```
