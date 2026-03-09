# Incubating

`incubating/` preserves code that may still be useful but is not part of the public, releasable `Incursa.Platform` package surface today.

Use this area for:

- provider code that has not been split into clean capability packages yet
- integrations that mix reusable infrastructure with product or workflow assumptions
- staging imports from sibling repos during consolidation

Rules:

- keep the code buildable when practical
- do not delete provenance or tests unless they are pure caches/output
- do not pack or publish projects from `incubating/` by default
- promote code out of `incubating/` only after it has a clear public package boundary
