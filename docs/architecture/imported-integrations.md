# Integration ownership

Public provider implementations are owned by [incursa/integrations-public](https://github.com/incursa/integrations-public). Local clone paths, source-worktree state, commits, and non-public implementation details are not part of the platform contract.

## Platform ownership

`platform` owns provider-neutral contracts and orchestration for access, custom domains, DNS, email, storage, observability, webhooks, hosting, and related shared capabilities.

## Integration ownership

Public integration repositories own vendor protocol calls, provider-specific configuration, concrete storage behavior, provider schema artifacts, and focused provider tests. Proprietary implementations and customer workflow remain outside public repositories.

## Boundary rules

- `platform` must not depend on provider implementation repositories.
- Provider repositories may depend on the public platform contracts they implement.
- Source movement does not authorize a package rename, version reset, publication, or compatibility claim.
- CI uses declared dependencies; local overrides must be explicit and must not assume a machine-specific sibling path.
