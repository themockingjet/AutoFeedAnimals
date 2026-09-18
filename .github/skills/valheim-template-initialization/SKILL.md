---
name: valheim-template-initialization
description: Convert a fresh copy of valheim-mod-template into one named Valheim BepInEx plugin by replacing every identity and starter-behavior placeholder. Use before implementing the first real feature in a newly copied template; do not use for changes to an already initialized mod.
---

# Initialize a Valheim Mod from This Template

## Scope

Apply this skill only in a new copy of `valheim-mod-template`. Do not modify
the canonical template repository. Do not begin feature implementation until
the initialization checklist and placeholder-residue check both pass.

The result must be one internally consistent mod identity. A partial rename is
not acceptable.

## Required identity record

Obtain or choose these values before changing files. Do not derive one value
from an incompatible spelling of another.

| Field | Required format | Example |
| --- | --- | --- |
| Display name | Player-facing name; spaces allowed | `Smelter Auto Feed` |
| Assembly name | PascalCase C# identifier; no spaces | `SmelterAutoFeed` |
| Root namespace | Valid PascalCase C# namespace | `SmelterAutoFeed` |
| Project slug | Lowercase letters, numbers, and hyphens | `smelter-auto-feed` |
| Plugin GUID | Stable reverse-domain lowercase identifier | `str.smelterautofeed` |
| Thunderstore package name | Thunderstore-compatible package identity | `SmelterAutoFeed` |
| Initial version | Semantic version without a `v` prefix | `0.1.0` |
| Project website | Absolute HTTPS project URL | `https://github.com/owner/SmelterAutoFeed` |
| One-sentence description | Player-facing description of actual behavior | `Feeds nearby containers into compatible production stations.` |
| Authority model | Exactly `client-only`, `server-authoritative`, or `owner-authoritative` | `owner-authoritative` |
| ServerSync decision | Exactly `required` or `not used`, with a reason | `not used: settings are client-local` |

The plugin GUID is a permanent compatibility identifier. Do not use
`com.example.*`, rename it after publishing, or reuse another mod's GUID.

## Required filesystem rename

From the copied template root, rename all three project paths as one coherent
operation:

```text
valheim-mod-name.sln
src/valheim-mod-name/
src/valheim-mod-name/valheim-mod-name.csproj
```

Use the selected assembly name for the solution file and the selected project
slug for both source directory and project file:

```text
<AssemblyName>.sln
src/<project-slug>/
src/<project-slug>/<project-slug>.csproj
```

After moving files, update the solution project display name and project path.
Replace the template solution project GUID with a newly generated GUID so the
new project does not collide with the template or another copied mod in a
solution. Preserve that new GUID consistently in every solution configuration
entry.

Delete generated template outputs before the first build:

```text
src/<project-slug>/bin/
src/<project-slug>/obj/
release/
```

Do not commit generated outputs, plugin binaries, PDBs, ZIP files, hashes, game
assemblies, BepInEx references, or Steam content.

## Mandatory source replacements

Complete every item below before adding a feature.

| Location | Required replacement |
| --- | --- |
| Project file | Set `<AssemblyName>` and `<RootNamespace>` to the chosen values. |
| Plugin source filename | Rename `ValheimModNamePlugin.cs` to `<AssemblyName>Plugin.cs`. |
| Plugin source | Replace the namespace, plugin class name, `PluginGuid`, `PluginName`, and `PluginVersion`. |
| `Properties/AssemblyInfo.cs` | Replace company, product, copyright, assembly version, and file version. Use `<version>.0` for assembly/file versions when the selected version has three components. |
| Root README | Replace every starter instruction, project name, path, archive name, and template-only claim with accurate documentation for the initialized mod. |
| `Thunderstore/manifest.json` | Replace `name`, `version_number`, `website_url`, and `description`. Keep `denikson-BepInExPack_Valheim-5.4.2202` unless the target runtime explicitly requires a different loader dependency. |
| `Thunderstore/README.md` | Replace the title, feature list, configuration, compatibility, and credits with accurate mod content. |
| `Thunderstore/CHANGELOG.md` | Replace starter entries with a real `0.1.0` initial-release entry. Remove all sample `Replace` text. Keep an empty `[Unreleased]` section only if the repository's release process requires it. |
| `docs/ARCHITECTURE.md` | Replace the title, purpose, runtime flow, component table, state/ownership section, and out-of-scope section with actual design decisions. |
| `docs/COMPATIBILITY.md` | State the actual supported Valheim baseline, dependencies, authority model, dedicated-server support, config synchronization decision, known conflicts, and verification matrix. |
| `docs/ICON_BRIEF.md` | Fill every `REPLACE WITH` field with the new mod's concrete visual identity. |
| `.github/copilot-instructions.md` | Replace the template title and purpose. State the final plugin GUID, assembly name, namespace, package name, version, authority model, config-sync decision, and release-content rules. |

## Starter plugin decision

The template's starter plugin contains example configuration and optional
ServerSync `ConfigSync` behavior. Choose exactly one path:

### `ServerSync decision = required`

Keep ServerSync only when synchronized configuration or a version handshake is
part of the declared mod behavior. Then:

1. Replace `EnableFeature` with an actual mod setting or remove it.
2. Define every synchronized setting's name, default, valid range, authority,
   and player-visible effect.
3. State whether a version mismatch warns or rejects a client.
4. Keep the manifest, compatibility documentation, agent instructions, and
   changelog accurate about whether ServerSync is merged or a runtime
   dependency.

### `ServerSync decision = not used`

Remove all ServerSync/ConfigSync code and references that are not needed:

1. Remove `using ServerSync;`.
2. Remove `_configSync` and all handshake configuration entries.
3. Remove ServerSync-specific documentation, changelog text, and dependency
   claims.
4. Update the project/release configuration only after verifying no build or
   package step still references ServerSync.

Do not retain an example feature, example setting, example handshake, or
template-only log message in an initialized mod.

## Documentation and packaging truthfulness

All documentation must describe implemented behavior, not future intentions or
template mechanics. Specifically:

- State whether clients, servers, or both must install the mod.
- State the valid owner or server responsible for every networked mutation.
- List all actual configuration settings and their defaults.
- List actual dependencies and known incompatibilities.
- Do not claim a release command, test-server command, ServerSync merge, or
  package layout exists unless it is present and verified in the initialized
  repository.
- Replace `Thunderstore/icon.png` with an original, non-placeholder 256x256
  PNG before packaging.

## Required validation

Run these checks after all replacements, excluding generated directories:

```bash
rg -n \
  'ValheimModName|valheim-mod-name|com\.example\.valheimmodname|<REPLACE|Replace this|example feature|starter plugin|placeholder' \
  --glob '!.github/skills/valheim-template-initialization/**' \
  --glob '!.github/agents/**' \
  --glob '!**/bin/**' \
  --glob '!**/obj/**' \
  --glob '!release/**' \
  .
```

The command must return no unresolved identity or starter-behavior matches.
The excluded skill is the initialization procedure itself. The excluded icon
agent is generic reusable guidance, not mod identity. Neither exclusion permits
an unresolved identity or starter behavior in the initialized mod's source,
metadata, or player-facing documentation.

Then:

1. Run the repository's documented preflight/build command.
2. Confirm the built assembly name equals the chosen assembly name.
3. Run the repository's documented package and release-verification commands
   when those commands exist.
4. Inspect the ZIP and confirm its metadata matches the identity record.
5. Confirm no forbidden Valheim, Unity, BepInEx, or Harmony reference DLLs are
   in the release ZIP.
6. Record any unavailable build tool or incomplete template automation as a
   blocker; do not claim initialization is complete without the residue scan
   passing.
