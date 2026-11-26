# Repository boundary summary

## Platform ownership

`platform` owns provider-neutral abstractions, models, orchestration, hosting adapters, shared test utilities, analyzers, and helper CLIs.

The source tree and [eng/package-catalog.json](../eng/package-catalog.json) are authoritative for the exact packable project inventory. Documentation should describe package families and boundaries rather than copying a point-in-time project count.

## Provider ownership

Public provider implementations, their database dialects, vendor protocols, provider schemas, and focused provider tests live in [incursa/integrations-public](https://github.com/incursa/integrations-public). Proprietary providers belong in non-public integration repositories whose details are outside the public contract.

## Dependency direction

Integration repositories may depend on `platform`; `platform` must not depend on them. CI and releases consume declared package dependencies. Contributors may use explicit local overrides for coordinated work, but durable docs and project files must not assume a machine-specific sibling checkout.

## Incubating code

`incubating/` is a non-publishable staging area. Code moves into the public package surface only after its provider-neutral or public-provider boundary, support policy, tests, and package identity are explicit.
