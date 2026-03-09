---
workbench:
  type: runbook
  workItems: []
  codeRefs: []
  pathHistory: []
  path: /docs/50-runbooks/cloudflare-kv-probe.md
---

# Cloudflare KV Manual Probe

Use this runbook to validate real Cloudflare KV connectivity with credentials from a local file.

## Purpose

This probe performs:

1. `PUT` a probe key/value
2. `GET` and verify value equality
3. `LIST` by prefix
4. `DELETE` (unless `--keep-key` is set)
5. `GET` after delete to confirm removal

It is designed for debugging API failures in app environments where Cloudflare is external and not under direct control.

## Credentials File

Start from:

- `docs/50-runbooks/cloudflare-kv-probe.sample.json`

Do not commit real credentials.

## Run

From repo root:

```powershell
pwsh -File ./scripts/run-kv-probe.ps1 -ConfigPath "C:\path\to\kv-probe.json"
```

Keep the key for manual inspection:

```powershell
pwsh -File ./scripts/run-kv-probe.ps1 -ConfigPath "C:\path\to\kv-probe.json" -KeepKey
```

Or run directly:

```powershell
dotnet run --project src/Incursa.Integrations.Cloudflare.KvProbe/Incursa.Integrations.Cloudflare.KvProbe.csproj -p:NuGetAudit=false -- --config "C:\path\to\kv-probe.json"
```

## Common Failure Categories

- Invalid/expired API token
- Token missing KV permissions
- Wrong `accountId` or `namespaceId`
- Network/proxy egress restrictions
- API base URL mismatch
- Key encoding/path mismatch for specific keys

If the probe fails, capture the full output (including `status`, `cf-ray`, and Cloudflare error codes/messages) for targeted debugging.
