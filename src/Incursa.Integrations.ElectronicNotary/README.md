# Incursa.Integrations.ElectronicNotary

This folder is the landing page for the Electronic Notary layer 1 integration family.

Use it as the starting point when you are browsing the vendor-specific proof and webhook packages in the repository.

## Family Map

- `Incursa.Integrations.ElectronicNotary`: root package and family anchor
- `Incursa.Integrations.ElectronicNotary.Abstractions`: shared contracts for the family
- `Incursa.Integrations.ElectronicNotary.Proof`: proof API client and vendor DTO surface
- `Incursa.Integrations.ElectronicNotary.Proof.AspNetCore`: ASP.NET Core webhook and healing integration

## Relationship To Layer 2

This family is currently intentionally vendor-shaped. It does not define a provider-neutral notarization or signature capability.

## See Also

- `PACKAGE_README.md` for the NuGet package overview
