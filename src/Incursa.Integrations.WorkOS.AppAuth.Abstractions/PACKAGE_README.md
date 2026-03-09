# WorkOS AppAuth Source Folder

This is a source folder compiled into `Incursa.Integrations.WorkOS`. It is not a separately published package.

## What Lives Here

- normalized WorkOS claims accessors
- organization-selection and organization-context contracts
- app-auth option types used by the broader WorkOS runtime package

## Why It Is A Source Folder

These types are implementation support for the main WorkOS runtime package. They remain vendor-specific and host-agnostic, but they are not intended to be versioned as a separate public package.

## Related Package

- `Incursa.Integrations.WorkOS`
