# WorkOS Core Source Folder

This is a source folder compiled into `Incursa.Integrations.WorkOS`. It is not a separately published package.

## What Lives Here

- deterministic API key authentication result modeling
- permission-to-scope mapping and provider-side authorization helpers
- API key management orchestration
- OAuth client-credentials token acquisition and caching
- WorkOS webhook signature validation and coordination helpers
- claim enrichment services for organization, role, and permission projection

## Why It Is A Source Folder

These types are implementation support for the main WorkOS runtime package. They are intentionally vendor-specific and should not be consumed as a standalone public package.
