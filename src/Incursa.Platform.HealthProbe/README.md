# Incursa.Platform.HealthProbe

Runs in-process health checks from the host DI container using standardized health buckets.

## Command

```text
health [live|ready|dep]
health list
```

Options:

- `--timeout <seconds>`: timeout for the whole execution (default 2s)
- `--json`: emit JSON payload
- `--include-data`: include filtered check `data` entries in output

Default bucket: `ready`

Exit codes:

- `0`: healthy
- `1`: non-healthy
- `2`: misconfiguration/exception
