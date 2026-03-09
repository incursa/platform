# Incursa Platform Documentation Index

Welcome to the comprehensive documentation for the Incursa Platform. This guide will help you understand and effectively use all platform components.

## 🚀 Getting Started

- [**Getting Started Guide**](GETTING_STARTED.md) - **Start here if you're new!**
- [Main README](../README.md) - Project overview and quick setup

## 🎯 Core Concepts

### Time Abstractions
- [Time Abstractions Guide](time-abstractions.md) - TimeProvider and IMonotonicClock
- [Monotonic Clock Usage](monotonic-clock-guide.md) - Stable timing for timeouts and measurements

### Modularity and Engines
- [Modularity Quick Start](modularity-quickstart.md) - Plug modules into a host and expose engines
- [Engine Contracts Overview](engine-overview.md) - Manifests, descriptors, and discovery
- [Module Engine Architecture](module-engine-architecture.md) - End-to-end engine and adapter design

### Work Queue Pattern
- [Platform Primitives Overview](platform-primitives-overview.md) - Inbox, outbox, fanout, and fan-in end-to-end
- [Work Queue Pattern](work-queue-pattern.md) - Claim-ack-abandon semantics
- [Work Queue Implementation](work-queue-implementation.md) - Technical details

### Observability
- [Observability Guide](observability/README.md) - Audit, operations, and observability conventions

## 📤 Outbox Pattern

### Core Documentation
- [Outbox Quick Start](outbox-quickstart.md) - Get started with outbox pattern
- [Outbox API Reference](outbox-api-reference.md) - Complete API documentation
- [Outbox Examples](outbox-examples.md) - Real-world usage examples

### Multi-Tenant Scenarios
- [Outbox Router Guide](OutboxRouter.md) - Multi-database routing
- [Dynamic Outbox Configuration](dynamic-outbox-example.md) - Runtime discovery
- [Multi-Outbox Guide](multi-outbox-guide.md) - Advanced patterns

### Implementation Details
- [Outbox Router Implementation](OutboxRouterImplementation.md) - Internal architecture
- [Multi-Outbox README](MULTI_OUTBOX_README.md) - Design decisions
- [Work Queue Implementation](work-queue-implementation.md) - Technical details

## 📥 Inbox Pattern

### Core Documentation
- [Inbox Quick Start](inbox-quickstart.md) - Get started with inbox pattern
- [Inbox API Reference](inbox-api-reference.md) - Complete API documentation
- [Inbox Examples](inbox-examples.md) - Real-world usage examples

### Multi-Tenant Scenarios
- [Inbox Router Guide](InboxRouter.md) - Multi-database routing
- [Dynamic Inbox Configuration](dynamic-inbox-example.md) - Runtime discovery

## 🔒 Distributed Locking

- [Lease System v2](lease-v2-usage.md) - Distributed locks with automatic renewal
- [Lease Examples](lease-examples.md) - Common patterns and use cases

## 🏢 Multi-Tenant Patterns

- [Multi-Database Pattern](multi-database-pattern.md) - Comprehensive guide
- [Schema Configuration](schema-configuration.md) - Database schema management

## 📖 Additional Resources

- [Implementation Summary](IMPLEMENTATION_SUMMARY.md) - High-level implementation overview
- [API Reference](api-reference.md) - Complete public interfaces and signatures
- [Operational Troubleshooting](operational-troubleshooting.md) - Runbook for common production issues
- [Testing Operating Model](testing-operating-model.md) - Workbench quality contract, artifact paths, and advisory workflow
- [Testing Known Issues](testing-known-issues.md) - Current `KnownIssue` lane status and guidance

## 🔍 Quick Links by Task

### I want to...
- **Send messages reliably** → [Outbox Quick Start](outbox-quickstart.md)
- **Process messages idempotently** → [Inbox Quick Start](inbox-quickstart.md)
- **Measure timeouts accurately** → [Monotonic Clock Guide](monotonic-clock-guide.md)
- **Implement distributed locking** → [Lease System v2](lease-v2-usage.md)
- **Support multiple tenants** → [Multi-Database Pattern](multi-database-pattern.md)
- **Configure database schema** → [Schema Configuration](schema-configuration.md)

## 📝 Documentation Conventions

Throughout this documentation:
- **Code examples** are tested against the latest codebase
- **Namespaces** use `Incursa.Platform`
- **Configuration** examples use C# and appsettings.json
- **Database** examples use SQL Server syntax

## 🤝 Contributing

Found an issue or want to improve the documentation? See our [contribution guidelines](../CONTRIBUTING.md).

---

**Last Updated:** 2026-03-08
**Platform Version:** 1.0.0+
