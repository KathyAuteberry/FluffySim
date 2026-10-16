# Headless simulator tests

Scenarios advance a fake clock at 60 frames per simulated second, without waiting for
wall-clock time. Large assignment sweeps can still execute millions of frames. The catalog
also runs 40 random seeds per strategy, configured in `test.runsettings`.

Run ordinary Debug tests when stepping through the code:

```sh
dotnet test tests/AnoMech.Tests -c Debug
```

For a faster full run, enable optimization while retaining the DEBUG code paths. This Linux
command isolates output and intermediates from ordinary Debug and installed Release files:

```sh
dotnet test tests/AnoMech.Tests -c Debug -p:Optimize=true \
  -p:OutputPath="$PWD/tests/AnoMech.Tests/bin/fast-Debug/" \
  -p:IntermediateOutputPath=obj/fast-tests-Debug/
```

Run from the repository root and use the existing `DALAMUD_HOME` setting for the SDK.
Keep test output inside the checkout: the assembly/version test locates the version props
by walking upward from the test binary. Optimization reduces debugger
stepping fidelity. It does not select Release or remove DEBUG-only code. Push/PR CI builds
optimized Debug binaries and runs them with `--no-build`. Tag publication uses the isolated
`fast-Debug` output and `fast-tests-Debug` intermediates shown above, so its scenario test
build cannot replace the ordinary Debug artifact used by safety checks. Safety checks still
inspect ordinary Debug and optimized TRACE artifacts.

Fall of Faith runs all 16 element patterns against 70 representative orders, one for every
set of four tethered roles. A coverage check requires every role in every tether position.
The inexpensive assignment-model test still checks all 1,680 ordered assignments. This
avoids repeating all 1,680 full simulations for each of the 16 element patterns.

Tests share static `Natives` and `Plugin` services. Do not enable thread-level NUnit
parallelism without first isolating those services. Separate test processes isolate them.

Use `--filter FullyQualifiedName~PracticeInvulnerabilityControlsTests` for focused UI checks,
and `--logger 'trx;LogFileName=tests.trx'` to capture per-case timings. UI number formatting
assertions follow the current culture; decimal-comma environments are supported.
