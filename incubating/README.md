# Incubating

`incubating/` preserves code that may still be useful but is not part of the public, releasable `Incursa.Platform` package surface today.

Use this area for:

- provider code that has not been split into clean capability packages yet
- integrations that mix reusable infrastructure with product or workflow assumptions
- staging imports from sibling repos during consolidation

Current promoted slices:

- `incubating/workos/` contributed the public `Incursa.Platform.Access`, `Incursa.Platform.Access.AspNetCore`, `Incursa.Platform.Access.WorkOS`, `Incursa.Platform.Webhooks.WorkOS`, and existing `Incursa.Platform.Audit.WorkOS` packages, but broader auth/widget/webhook/vendor surfaces remain incubating
- `incubating/cloudflare/` contributed the public `Incursa.Platform.Dns` / `Incursa.Platform.Dns.Cloudflare` and `Incursa.Platform.CustomDomains` / `Incursa.Platform.CustomDomains.Cloudflare` capability families, but broader storage, probe, load-balancing, and non-promoted vendor surfaces remain incubating

Current deferrals:

- the remaining WorkOS code is still not being promoted as a standalone identity layer 2 library in this pass; session selection, onboarding middleware, widgets, management clients, profile/session enrichment, and the old vendor-owned webhook pipeline remain in `incubating/workos/`

Rules:

- keep the code buildable when practical
- do not delete provenance or tests unless they are pure caches/output
- do not pack or publish projects from `incubating/` by default
- promote code out of `incubating/` only after it has a clear public package boundary
