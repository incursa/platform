# Incursa.Integrations.Stripe

Reusable Stripe billing primitives for Incursa applications.

This package provides:

- `IStripeBillingClient` for checkout, billing portal, webhook, and subscription access flows
- `StripeBillingOptions` for API key and webhook configuration
- DI registration helpers for ASP.NET Core and background services

The package is intended to keep Stripe-specific integration concerns behind a focused adapter surface so consuming applications can depend on their own billing workflows rather than direct Stripe SDK usage.
