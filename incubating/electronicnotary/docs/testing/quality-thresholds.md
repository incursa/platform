# Quality Thresholds

## Current Baselines

- Coverage gate:
- Line: `>= 45%`
- Branch: `>= 30%`
- Mutation gate (scoped critical files):
- Break threshold: `>= 55%`

## Ratchet Policy

- Only increase thresholds after 2 consecutive green PR cycles.
- Default ratchet step:
- Coverage: +5 percentage points per cycle.
- Mutation: +5 percentage points per cycle.
- Never reduce thresholds without a documented compatibility/quality decision entry.

## Planned Ratchet Targets

- Near-term target: line `65%`, branch `55%`.
- Mid-term target: line `75%`, branch `65%`.
- Scoped mutation target: `70%+` once survivor triage stabilizes.
