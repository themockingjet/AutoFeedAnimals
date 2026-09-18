# Auto Feed Animals

Optionally feeds tame, hungry animals directly from eligible food stored in
accessible nearby chests. Animals walk to the chest and consume one item from
its inventory without ejecting food onto the ground.

## Features

- Uses each animal's native food eligibility, hunger, pathing, and consumption
	flow.
- Runs only on the owning peer and leaves native search timing and radius in
	Valheim's control.
- Honors chest ownership and access rules and does not consume through walls
	unless `Feeding/Ignore Pathing` is enabled.

## Installation

Install with your mod manager, or drop the packaged DLL into `BepInEx/plugins`.

## Configuration

The configuration file is `BepInEx/config/str.autofeedanimals.cfg`.

- `Feeding/Enable Auto Feeder`: `true` by default for new configuration files.
- `Feeding/Feed Range (Meters)`: `5` by default, bounded from `1` to `60`.
- `Feeding/Ignore Pathing`: `false` by default; opt in to remote feeding
  without a walkable path.
- `Feeding/Protect Feed Containers`: `true` by default.
- `Feeding/Disallow Feed`: empty by default; comma-separated food names.
- `Feeding/Disallow Animal`: empty by default; comma-separated animal names.

Local interface settings:

- `Interface/Show Animal Stats`: `true` by default. When pointing at an
  acclimatizing untamed animal, the display is simplified to `Animal (%)`.

## Compatibility

Requires BepInExPack Valheim and ServerSync-compatible peers. ServerSync is
merged into the plugin release. Install the same version on the server and
clients. MultiUserChest access behavior is honored; validate both together on
a dedicated server before release.

## Credits

Built with BepInEx, Harmony, and ServerSync.