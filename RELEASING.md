# Maintaining and releasing FluffySim

**Local builds and tests are Debug-only. Never write installed Release output.**
Dalamud watches it and can reload/freeze the running client. Only clean GitHub Actions
runners build Release. Use Conventional Commit messages for every new commit.

## Development and history

`master` is FluffySim's trunk/publication bookmark; plogons independently publishes master.
The initial six-parent integration combines upstream, original Evernow PR 35/38/50 tips,
the complete FRU/DSR feature/fix branch and a separate CI branch. `plkqvn` is an empty
`chore: integration commit`; its child `xznrxk` prepares 2.0.0.0 and is local master.
An empty working area follows. All features and CI are ancestors of the versioned release.
The old four-parent layout is historical, not the final release topology.

Local workspace setup (jj configuration is not cloned):

```sh
jj config set --repo 'revset-aliases."trunk()"' master
jj config set --repo revsets.log '@ | heads(::@ & merges())::@ | parents(heads(::@ & merges()))'
```

Everyday: edit → Debug checks → Conventional Commit on the intended branch → inspect and
advance the reviewed bookmark. Inherit the user-level restack alias without a repo override;
inspect `jj config list` before use and
never restack published ancestry. Upstream updates fetch/inspect/merge/test new changes on
published history as described in [AGENTS.md](AGENTS.md). Recovery starts with `jj op log`
and `jj --at-operation <id> log`; preserve current work before any targeted recovery.

```sh
dotnet build -c Debug
dotnet test tests/AnoMech.Tests -c Debug
dotnet test tests/AnoMech.Relay.Tests -c Debug
python -m unittest discover -s tools/tests -p test_package_release.py
```

Run both Debug safety backends using [tests/SafetyTests/README.md](tests/SafetyTests/README.md).
Native acceptance still requires all nine encounters, Easy/Hard, Pause, visibility, God Mode,
local abilities, offline FRU/DSR, party HUD and safe Leave/BGM restoration.
Local packaging dry-run only:
`python tools/package_release.py --input AnoMech/bin/x64/Debug --output /tmp/fluffysim-package-check`.
Never distribute this Debug ZIP. The packager never builds or mutates its input.

## First publication (explicit owner authorization required)

Prepared version: **2.0.0.0**. `FluffySim.Version.props` contains `2.0.0`; assembly/file/tag
versions append `.0`. Increase major for breaking milestones, minor for features, patch for
fixes; revision stays zero. No public release or live manifest update has been performed.

1. Review and publish plogons/master from its own directory with
   `jj git push --remote origin --bookmark master`. Its bootstrap is `[]`, plus updater/tools.
2. In FluffySim, inspect `master` and confirm its release-preparation parent is the complete
   integration merge. Run `jj git push --remote origin --bookmark master --dry-run`, then
   publish that bookmark only with owner authorization. It already includes the feature
   branch and CI; no private-label override is needed for the Conventionally named merge.
3. Inspect `jj status`, `jj log` and both bookmark lists again. Preserve the empty working area.
4. Set GitHub default branches to master for FluffySim and master for plogons. Enable Actions.
   Configure FluffySim's `PLOGONS_TOKEN` secret: a fine-grained token restricted to plogons
   with Contents read/write. Its branch rules must permit the metadata update. The release
   job requests Contents write in FluffySim; the standard GITHUB_TOKEN cannot write plogons.
   Keep tokens out of source and shell history. GitHub App authentication is a future option,
   not implemented here. [Token scope reference](https://docs.github.com/en/actions/concepts/security/github_token).
5. With explicit release approval, run in FluffySim:
   `jj tag set v2.0.0.0 -r master`, inspect `jj tag list`, then
   `jj git push --remote origin --tag v2.0.0.0`. Installed jj 0.46 supports tags natively.
   Never move published tags. Recheck status and bookmarks afterward.

## Recurring release pipeline

Bump version props, update changelog, Debug-test, commit and publish the reviewed integrated
master. Tag `vX.Y.Z.0`. The workflow verifies version, master ancestry and all nine custom scenario registrations, runs plugin/relay
and both safety backends, builds Release on its isolated runner and packages declared runtime
dependencies plus selected public documentation. Preserve matching complete source at the
tag and AGPL attribution; GitHub provides tagged source archives.

The workflow extracts the matching CHANGELOG.md entry with tools/release_notes.py and uses
it as the GitHub release body; it does not substitute generated PR notes.

The public release is created before metadata changes. Its downloaded ZIP supplies the
manifest; the updater validates identity/API/version and constructs exact release URLs in
plogons/mostly-harmless.json. Other plugins survive; versions never decrease.

Subscription: https://raw.githubusercontent.com/KathyAuteberry/plogons/master/mostly-harmless.json

## Failed publication

Inspect Actions logs first. Test/package failures publish nothing. If the release exists but
metadata failed, preserve its tag/ZIP, fix credentials or metadata state and dispatch the same
tag: `gh workflow run publish-fluffysim.yml --ref v2.0.0.0`. Reruns reuse the existing asset and
never replace it. Missing existing assets deliberately fail. Incorrect binaries require a
higher version, never a moved tag or replacement ZIP.

Metadata push races fetch the latest plogons/master and recompute up to three times. A newer
version or inconsistent equal-version entry fails without changing live metadata. Manual
recovery: fetch plogons with jj, preserve current work, create a clean change on master@origin,
download the exact public ZIP, run its updater/tests, inspect diff and commit with a
Conventional Commit message. Advance master and publish only with owner authorization.
Never roll back other plugins or force-push over concurrent work.
