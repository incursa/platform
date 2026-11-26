# Provider Quality Baseline

Provider implementation quality baselines moved to `integrations-public` as part of the repository split.

This `platform` repository no longer carries:

- `Incursa.Platform.SqlServer`
- `Incursa.Platform.Postgres`
- `Incursa.Platform.InMemory`
- provider-focused workflows and quality ratchets tied to those packages

Use [`incursa/integrations-public`](https://github.com/incursa/integrations-public) for provider quality contracts and ratcheting guidance.
