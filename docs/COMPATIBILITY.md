# AutoFeedAnimals Compatibility

## Supported versions

- Valheim: current stable version represented by the local reference cache.
- BepInEx: 5.x.
- Runtime: .NET Framework 4.8.
- ServerSync: not used. Configuration is local to each BepInEx process.

Update these values when the reference cache or supported game version changes.

## Multiplayer

- Side: both; authoritative work runs only on the local owner of the animal.
- Ownership: the animal's `ZNetView` owner may initiate native consumption;
  non-owners perform no mutation and never claim ownership.
- Dedicated server: supported. Native feeding runs on whichever peer owns the
  animal, including the dedicated server when it owns that object. No
  client-side fallback or custom RPC is used.
- Food source: eligible items consumed directly from accessible nearby chest
  inventories. Private and warded access remains enforced.

## Configuration

The following settings are local to each installed plugin instance:

- `Feeding/Enable Auto Feeder`, default `true` for new configuration files.
- `Feeding/Feed Range (Meters)`, default `5`, bounded from `1` to `60`.
- `Feeding/Ignore Pathing`, default `false`; when enabled, chest movement and
  the native path requirement are skipped for remote feeding within the
  configured range. It does not bypass range.
- `Feeding/Protect Feed Containers`, default `true`.
- `Feeding/Disallow Feed`, default empty.
- `Feeding/Disallow Animal`, default empty.

Local interface setting is not synchronized:

- `Interface/Show Animal Stats`, default `true`; simplifies native
  acclimatizing untamed animal hover text to `Animal (percentage%)`.

Install the same release and matching configuration on peers that should use
the same feeding behavior. There is no version handshake or automatic config
propagation between peers. A value such as `Carrot` in `Disallow Animal` is a
filter entry.

Synchronized feeding changes are applied live. A range or pathing change also
clears cached chest targets before the next chest search.

## Dependencies

- BepInExPack Valheim 5.4.2202 or compatible.
- No runtime DLL beyond the plugin is required.

## Known conflicts

MultiUserChest may participate through its access behavior. AutoFeedAnimals
uses native `Container.CheckAccess` and does not patch container RPCs. Both
mods must be validated together for simultaneous chest access, ownership, and
food withdrawal. Its inventory-change event also refreshes the advisory food
hint lazily on the authoritative owner; a five-second safety refresh remains in
place if another integration changes inventory without raising the event.

## Verification matrix

Before release, verify at minimum:

- Single-player startup.
- Dedicated-server startup if supported.
- A client joining a server with the mod installed.
- A client joining without the mod when that mode is supported.
- The gameplay path affected by every Harmony patch.
- With `Protect Feed Containers=true`, untamed tameable animals are blocked from
  damaging registered feed containers, while non-tameable hostile characters
  still use Valheim's native damage path.
