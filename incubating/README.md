# Incubating

`incubating/` preserves staged code that is excluded from the public, releasable package surface.

Use this area for:

- provider code that does not meet the public package support boundary
- integrations that mix reusable infrastructure with product or workflow assumptions
- staging imports from sibling repos during consolidation

Do not treat `incubating/` as a synonym for vendor-specific. Public layer 1 vendor adapters belong in `src/` as packable packages once their boundary is clean, even when they remain provider-specific.

Rules:

- keep the code buildable when practical
- do not delete provenance or tests unless they are pure caches/output
- do not pack or publish projects from `incubating/` by default
- promote code out of `incubating/` only after it has a clear public package boundary
- keep inventory and promotion status in work items rather than this README
