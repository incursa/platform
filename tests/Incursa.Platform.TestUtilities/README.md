# Incursa.Platform.TestUtilities

Shared test helpers for packages built on Incursa Platform.

This package is intended for automated test projects, not production applications. It includes:

- behavior-test base classes for inbox, outbox, scheduler, and lease implementations
- lightweight fakes and sample discovery helpers
- test logging utilities and monotonic clock helpers

Install in a test project:

```bash
dotnet add package Incursa.Platform.TestUtilities
```

Typical companion packages:

- `Incursa.Platform`
- `Shouldly`
- `xunit.v3`

The APIs in this package are optimized for test composition and may change alongside platform test surface evolution.
