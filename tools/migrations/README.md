# Incursa.Platform.SchemaMigrations.Cli

`Incursa.Platform.SchemaMigrations.Cli` is a CLI / dotnet tool for applying Incursa Platform schema migrations to SQL Server and PostgreSQL.

## Install

```bash
dotnet tool install --global Incursa.Platform.SchemaMigrations.Cli
```

## Usage

```bash
incursa-schema --provider sqlserver --connection-string "<connection string>" --schema infra
incursa-schema --provider postgres --connection-string "<connection string>" --schema infra
```

Optional flags:
- `--include-control-plane`: also apply the control-plane bundle
- `--schema`: override the default schema name (`infra`)

## Examples

```bash
incursa-schema -p sqlserver -c "Server=.;Database=Incursa;Trusted_Connection=True;" -s infra
incursa-schema -p postgres -c "Host=localhost;Database=incursa;Username=postgres;Password=postgres" --include-control-plane
```

## Repository

- https://github.com/incursa/platform