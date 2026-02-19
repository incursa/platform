# Incursa Platform Schema Migrations CLI

Run the latest Incursa Platform schema migrations against SQL Server or Postgres.

## Usage

```
incursa-schema --provider sqlserver --connection-string "<connection string>" --schema infra
incursa-schema --provider postgres --connection-string "<connection string>" --schema infra
```

Optional flags:
- `--include-control-plane`: also apply the control-plane bundle to the same database.
- `--schema`: override the default schema name (`infra`).

## Examples

```
incursa-schema -p sqlserver -c "Server=.;Database=Incursa;Trusted_Connection=True;" -s infra
incursa-schema -p postgres -c "Host=localhost;Database=incursa;Username=postgres;Password=postgres" --include-control-plane
```
