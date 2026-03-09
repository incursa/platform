# Incubating

`incubating/` preserves code that may still be useful but is not part of the public, releasable package surface today.

Use this area for:

- provider code that is not yet supportable as a public package
- integrations that mix reusable infrastructure with product or workflow assumptions
- staging imports from sibling repos during consolidation

Do not treat `incubating/` as a synonym for vendor-specific. Public layer 1 vendor adapters belong in `src/` as packable packages once their boundary is clean, even when they remain provider-specific.

Current status:

- there are no active vendor package families intentionally left under `incubating/` after the current promotion pass
- public vendor-specific packages now live in `src/` under `Incursa.Integrations.*`

Rules:

- keep the code buildable when practical
- do not delete provenance or tests unless they are pure caches/output
- do not pack or publish projects from `incubating/` by default
- promote code out of `incubating/` only after it has a clear public package boundary
