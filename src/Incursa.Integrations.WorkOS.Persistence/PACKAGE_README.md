# WorkOS Persistence Source Folder

This is a source folder compiled into `Incursa.Integrations.WorkOS`. It is not a separately published package.

## What Lives Here

- in-memory persistence helpers for development and tests
- key-value-backed mapping and state storage
- idempotency storage used by webhook processing flows
- persistence extension points used by the broader WorkOS runtime package

## Why It Is A Source Folder

These components support the main WorkOS runtime package and stay internal to that published surface. They are not intended to become a generic storage abstraction or a separate persistence package.
