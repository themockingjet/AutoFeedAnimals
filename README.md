# Auto Feed Animals

Auto Feed Animals optionally feeds tame, hungry animals directly from eligible
food in accessible nearby chests without ejecting food onto the ground. It
preserves native food eligibility, spherical range, pathing, hunger, and
item-consumption effects.

Manual feeding and breeding remain available when automatic feeding is off.

## Getting started

Make targets automatically load `$HOME/.config/valheim-dev/env.sh` when it
exists. Source that file manually only when using the variables from direct
shell commands outside Make.

```bash
make preflight
make build
make package
make verify-release
```

The package is written to `release/<manifest-name>-<version>.zip` and includes
the built plugin DLL and standard Thunderstore metadata. If a mod has
explicitly approved runtime DLLs, pass them as a space-separated
`PACKAGE_DLLS` list.

## Deploy to a test server

The template includes a temporary test-server installer. It copies every
root-level DLL from the verified release ZIP into a separate `local-*` plugin
directory under the active BepInEx release. It preserves whether the server and
maintenance timer were running, never starts or stops either one, and does not
modify the managed Hexium manifest.

For a test server on the current host:

```bash
make deploy-test-server TEST_SERVER=local
```

For an SSH-accessible test server:

```bash
make deploy-test-server TEST_SERVER=admin@test-server
```

The installer is deploy-only. To deploy several mods before restarting once,
stop the server and timer yourself, run the command from each mod repository,
then start them once:

```bash
sudo systemctl stop valheim-restart.timer
sudo systemctl stop valheim.service

make deploy-test-server TEST_SERVER=local
# Repeat from each mod repository.

sudo systemctl start valheim.service
sudo systemctl start valheim-restart.timer
```

The helper never calls `systemctl`; the mod files are loaded on the single
restart at the end.

The SSH account must be able to run the configured sudo command. For a
non-default SSH port, pass `TEST_SERVER_SSH_OPTIONS="-p 2222"`. The default
remote paths match `valheim-project`:

- plugin root: `/opt/valheim/modpack/current/BepInEx/plugins`
- server unit: `valheim.service`
- maintenance timer: `valheim-restart.timer`

When running directly as root, set `TEST_SERVER_SUDO=` to skip sudo.

Remove the temporary install with:

```bash
make remove-test-server TEST_SERVER=local
```

Use `TEST_PLUGIN_DIR=local-OtherName` when several local builds need to be
tested independently. The helper refuses to touch a directory that does not
start with `local-` unless `TEST_ALLOW_NONLOCAL_PLUGIN_DIR=1` is explicitly
provided.

If the shared references are not installed yet:

```bash
make setup-references
source "$HOME/.config/valheim-dev/env.sh"
```

## Configuration
## Configuration

All settings are local BepInEx configuration values. Install the same plugin
version and matching configuration on peers that should use the same behavior.

## Project layout

- `src/auto-feed-animals/`: plugin source and project file.
- `Thunderstore/`: package metadata and icon.
- `docs/`: architecture and compatibility notes.
- `scripts/`: setup, build, package, and release verification commands.
- `release/`: generated local build and package output; do not commit it.

`Thunderstore/icon.png` is the original 256x256 package icon used by releases.

## Before editing

Read [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md) and
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Document ownership,
multiplayer, and native API constraints before adding patches.

## Generated and private files

Do not commit Valheim game DLLs, BepInEx reference DLLs, `bin/`, `obj/`, or
generated release archives. The local reference cache belongs under
`$HOME/valheim-dev`, not in this repository.
