# Routine and extended Debug validation

`dotnet test tests/AnoMech.Tests -c Debug` uses `test.runsettings`: four catalog seeds per strategy and excludes `Extended` tests. Routine coverage retains representative Darklit full patterns, Apocalypse axis/direction runs, all original scenario catalog entries, CT/P5 roles, new P2 geometry matrices, failure cases, native startup contracts and lifecycle checks.

Extended coverage retains exhaustive Darklit graph/wing and hard-invisible patterns, all Apocalypse water permutations, full DD pattern matrices, P2 hidden-party seat/seed loops, and expensive multiplayer replay/link interruption suites. No tests were deleted.

Run everything explicitly (Debug only):

```sh
dotnet test tests/AnoMech.Tests -c Debug --settings tests/AnoMech.Tests/extended.runsettings
```

Run only extended stress: `dotnet test tests/AnoMech.Tests -c Debug --settings tests/AnoMech.Tests/extended.runsettings --filter 'TestCategory=Extended'`.
The default settings filter is combined with command-line filters. To opt into Extended coverage, explicitly select `extended.runsettings`; a command-line filter alone cannot override the routine exclusion. Use isolated optimized Debug output/intermediate directories for long runs; never optimize installed output or build Release.

The previous optimized full report had 14,976 passes in 7m49. Per-case durations summed by class: LinkInterruption 90.88s (66 cases), DebugBotReplay 69.57s (40), Apocalypse 61.71s (2,624), ScenarioCatalog 41.80s (50 cases, each with a hidden 40-seed loop), Darklit 25.14s (2,352), HardPattern 19.02s (1,962). These are summed test durations, not independent wall-time benchmarks. DD's full loops also repeat expensive ice-route searches; moving them to Extended preserves stress coverage while routine DD smoke, receive fixtures and negative mechanics remain.

Repair validation: final routine run executed **7,835 cases in 2m17** (0 failures), compared with the older **14,976-case 7m49** full stress baseline. One additional mirror-slot delay regression was added after the full run and passed in the separate 36-case focused run (3s). The retained Extended cases and reduced catalog seed loops account for the smaller routine workload; this is not an identical-workload benchmark.
