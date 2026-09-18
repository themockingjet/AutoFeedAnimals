# AutoFeedAnimals Agent Instructions

## Purpose

AutoFeedAnimals optionally feeds tame, hungry animals from eligible food in
accessible nearby chests. It is intended for players who want production
automation without replacing manual feeding or breeding. The plugin extends
Valheim's `MonsterAI` consumption flow without maintaining a food allow-list.

- Plugin GUID: `str.autofeedanimals`
- Assembly: `AutoFeedAnimals`
- Namespace: `AutoFeedAnimals`
- Thunderstore package: `AutoFeedAnimals`
- Version: `0.1.2`

## Compatibility

- Target Valheim version: current stable version used by the local reference cache.
- Loader: BepInEx 5.
- Runtime target: .NET Framework 4.8.
- Development SDK: .NET 8 or later.
- Build-time publicizer: `BepInEx.AssemblyPublicizer.MSBuild`.
- Config synchronization: required ServerSync `ConfigSync` for all `Feeding/*`
  settings, with a version handshake.
- Multiplayer: owner-authoritative. Only the valid animal `ZNetView` owner
  allows native feeding; the mod never claims ownership.

## Critical game rules

- Only mutate networked objects through their valid ownership and RPC flows.
- Prefer native Valheim APIs over direct ZDO or serialized-state edits.
- Do not add hard-coded game data when the native API can discover it.
- Do not claim remote ownership without an explicit, approved ownership flow.

## Build and release

- Source the shared environment:
  `source "$HOME/.config/valheim-dev/env.sh"`.
- Run `make preflight` before building or releasing.
- Run `make build`.
- Run `make package`.
- Run `make verify-release`.
- Keep `ServerSync.dll` outside the repository; release builds merge it into
  the plugin with ILRepack.
- Release ZIPs must contain the plugin DLL, runtime DLL dependencies,
  `manifest.json`, `README.md`, `CHANGELOG.md`, and `icon.png`.

## Changelog

- Keep `Thunderstore/CHANGELOG.md` in a Keep a Changelog-style format with
  the newest release first.
- Use an `[Unreleased]` section while work is in progress. When publishing,
  rename it to the exact version in `Thunderstore/manifest.json` and start a
  new `[Unreleased]` section.
- Record concise, user-visible changes under `Added`, `Changed`, `Fixed`, or
  `Removed`. Include compatibility changes, configuration migrations, and
  multiplayer behavior changes when they affect users; do not list routine
  internal refactors or build noise.
- If the mod uses ServerSync or ConfigSync, add a `ServerSync/ConfigSync`
  entry that explains which settings are synchronized, whether the version
  handshake or minimum required version changed, and whether ServerSync is a
  runtime dependency or merged into the release DLL. Do not imply that
  settings are synchronized when the mod has no such integration; omit the
  section when it is not applicable.
- Do not rewrite or remove previous release notes. Update the changelog in
  the same change that updates the manifest version or user-facing behavior.

## Icon generation

- Fill in `docs/ICON_BRIEF.md` with this mod's subject/palette before
  requesting an icon.
- Use the `icon-generator` agent (`.github/agents/icon-generator.md`)
  to assemble a brand-consistent prompt and generate `Thunderstore/icon.png`.
- The shared brand system lives in the `valheim-mod-brand` repo/folder;
  do not invent a different frame, palette set, or lighting scheme.
- The generated subject must pass the anti-sameness checklist in
  `valheim-mod-brand/ICON_SYSTEM.md` — no plain recolored placeholder
  shapes as final art.

## Scope discipline

- Do not modify other repositories.
- Do not commit Valheim game assemblies or Steam content.
- Do not publish a ZIP containing a placeholder or unverified DLL.
- Update this file when compatibility or multiplayer rules change.