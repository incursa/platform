# Definitions

This folder contains definition inputs and generated contract-support files used by `Incursa.Integrations.ElectronicNotary.Proof`.

Do not hand-edit generated code; change the source definition files in this folder and then regenerate the outputs.

## What Lives Here

- definition files that shape generated IDs, enums, and DTO contracts
- generated code that provides compile-time safety for the proof package

## Why It Exists

The proof integration has a large vendor-specific contract surface. Keeping those definitions centralized makes the generated transport types easier to review and evolve without scattering literal identifiers across the package.
