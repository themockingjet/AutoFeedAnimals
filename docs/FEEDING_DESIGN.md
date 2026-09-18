# Animal Feeding Design

## Goal

Auto-feeding is an optional, separately enabled feature. When enabled, tame
hungry animals can draw eligible food from accessible nearby chests; the item
is consumed directly from the chest when the animal reaches it. Disabling it
leaves manual feeding and breeding unchanged.

## Verified native flow

The current reference cache contains these relevant APIs in
`assembly_valheim.dll`:

- `Tameable.IsTamed()`, `IsHungry()`, and `OnConsumedItem(ItemDrop)`.
- `MonsterAI.CanConsume(ItemDrop.ItemData)`.
- `MonsterAI.UpdateConsumeItem(Humanoid, float)`, which finds a nearby world
  drop, calls `ItemDrop.RemoveOne()`, and invokes its consumption callback.
- `Container.GetInventory()`, `Container.CheckAccess(long)`, and
  `Inventory.RemoveOneItem(ItemDrop.ItemData)` for the chest source.
- `MonsterAI.FindClosestConsumableItem(float)`, which uses allocating
  `Physics.OverlapSphere`, then evaluates each candidate's item component,
  eligibility, distance, and path.

The implementation must not maintain a food allow-list or write the tame
feeding ZDO value directly. It must use `IsTamed`, `IsHungry`, `CanConsume`,
and the native consumption callback so species-specific food, hunger duration,
effects, and taming behavior stay owned by Valheim.

`Tameable.UseItem(Humanoid, ItemDrop.ItemData)` is not the general food path
in this game build and must not be used as one.

## Authority and sources

Classification: owner-authoritative. A feed operation may run only when the
local peer owns the animal's `ZNetView`; it must never call
`ClaimOwnership()`.

Version 1 uses accessible chests as the source and handles the chest branch in
`MonsterAI.UpdateConsumeItem`. Hungry tameable animals, including animals
still being tamed, may use the chest path. The animal keeps a chest target,
walks to it unless `Ignore Pathing=true`, then the mod revalidates access and
native food eligibility, removes exactly one item through `Inventory`, and
invokes the native consume callback and effects. No temporary ground item is
spawned. Ground-food targets continue through Valheim's untouched native path.

## Scheduler rules

`UpdateConsumeItem` has its own per-animal search timer and controls movement,
pathing, item removal, and the callback. A separate scheduler must not invoke
it; doing so can alter native timing and causes an additional allocating
`Physics.OverlapSphere` query per scheduled animal.

When no eligible chest food is found, the chest branch adds a bounded retry
backoff of 0.5, 1, 2, 4, then 5 seconds on top of the native search interval.
Container creation and destruction invalidate nearby-container discovery.
Inventory changes update a per-container revision and reset backoff only for
animals whose cached nearby set includes that chest. This reduces repeated
empty-chest work without introducing a global update loop; native ground-food
searches continue on Valheim's own schedule.

Container changes also invalidate a short-lived advisory set of non-empty item
shared names. The authoritative owner refreshes that hint lazily during
candidate selection; non-owner peers do not scan inventories solely because
they received the event. Candidate selection may skip a chest when that hint
has no overlap with the animal's native food templates, but unknown or expired
hints fall back to a fresh inventory snapshot. The hint never replaces
`Container.CheckAccess`, ownership checks, `MonsterAI.CanConsume`, or the final
fresh inventory validation.

The native search radius is prefab-defined. Do not expose an independent
`SearchRadius` setting by mutating `MonsterAI.m_consumeSearchRange`: that would
change native AI behavior and still leave the scan allocation cost unbounded.
For chest candidates, AutoFeedAnimals calls native `BaseAI.HavePath(Vector3)`
with the chest position before selecting a walking target. This rejects a chest
with no walkable route while allowing a route around a wall. The check is made
again before direct consumption. A successful selection path result may be
reused for at most 0.25 seconds while moving, then it is recalculated.
`Ignore Pathing=true` skips the path check and allows direct consumption within
the native search radius.

The native per-animal search interval and prefab-defined radius remain in
control. AutoFeedAnimals does not add a second scheduler or a global search
budget; doing so would duplicate native work and impose an arbitrary policy on
large herds.

The gate checks tame status and ownership; native `UpdateConsumeItem` remains
the authority for hunger, and native `CanConsume` remains the authority for
eligibility. Never call `Inventory.RemoveOneItem`, `ItemDrop.RemoveOne`, or
`Tameable.OnConsumedItem` from mod code.

### Performance boundary

Native query cost is proportional to hungry animals times candidate item
colliders within each animal's radius. Large shared pens with loose food can
therefore produce repeated overlapping sphere queries. Bound the design by
retaining the native radius, avoiding duplicated scans/raycast checks, and
profiling dense pens before release. The mod intentionally does not impose a
second global search loop or per-frame budget.

## MultiUserChest compatibility

AutoFeedAnimals uses `Container.CheckAccess` and the native inventory API, but
does not patch chest access or invent a chest RPC. MultiUserChest may alter
`CheckAccess` to permit multiple users; that decision is honored. The local
peer must still own both the animal and chest, so no remote ownership is
claimed. Validate concurrent chest use with MultiUserChest on a dedicated
server.

## Required configuration

All gameplay-affecting values are synchronized with the existing
`ServerSync.ConfigSync` integration:

- `Feeding/Enable Auto Feeder` (default `true`)
- `Feeding/Feed Range (Meters)` (default `5`, bounded from `1` to `60`)
- `Feeding/Ignore Pathing` (default `false`)
- `Feeding/Protect Feed Containers` (default `true`)
- `Feeding/Disallow Feed` (default empty, comma-separated food names)
- `Feeding/Disallow Animal` (default empty, comma-separated animal names)

Local interface settings:

- `Interface/Show Animal Stats` (default `true`)

The native ground-food search remains untouched. A player-built container is
recognized as a feed container when its loaded `Container` component, player
piece identity, valid ZDO, and inventory are available. Existing
containers are discovered at startup and rechecked periodically during world
and inventory restoration; `Container.OnContainerChanged` also refreshes its
registration. Chest discovery uses the synchronized `Feed Range (Meters)` 3D
radius and cached container registry.
`Ignore Pathing=false` requires a walkable route and movement to the chest;
`Ignore Pathing=true` consumes directly within the configured spherical range;
it never bypasses the range check, including for a previously selected chest.
The deny lists
are additional player controls and do not replace native `CanConsume`.

When compatible ServerSync is available, the version handshake remains
required because these settings change authoritative gameplay behavior.

The plugin detects the known incompatible ServerSync `ZRoutedRpc.Everybody`
field shape and skips ConfigSync registration instead of allowing runtime
`MissingFieldException` errors during setting changes. Local runtime settings
remain functional until a compatible ServerSync build is installed.

Runtime changes from ServerSync or Configuration Manager are applied through
`SettingChanged` callbacks. Changing `Feed Range (Meters)` clears existing
chest targets and nearby-container caches, so the new value is used without a
restart.

## Validation matrix

Before release, validate in single-player and a dedicated server with an owner
and non-owner client:

1. A hungry tame animal walks to an accessible nearby chest, withdraws one
  eligible item directly from its inventory, and receives the native fed
  duration/effects without a ground drop.
2. Untamed, satiated, or food-ineligible animals consume nothing.
  Untamed hungry animals with eligible chest food do consume and progress
  through native taming.
3. A chest with no native walkable path supplies no food by default.
4. The same chest may supply food when `Ignore Pathing=true`.
5. A ground food item without a native path consumes nothing.
6. A dense large herd is profiled against native AI behavior without an added
  mod search loop.
7. A non-owner performs no mutation and never claims ownership.
8. Manual feeding and native breeding continue to work with auto-feeding off.
9. Private, warded, and inaccessible containers remain untouched.
10. With MultiUserChest installed, authorized concurrent chest use remains
  functional and unauthorized chests do not supply food.
11. A non-player/default container is ignored, while a player-built chest,
  cart, or barrel is eligible.
12. `DisallowFeed` and `DisallowAnimal` prevent only automatic chest feeding.
13. An untamed tameable animal cannot damage a registered feed container while
  `ProtectFeedContainers=true`; other hostile characters retain native container
  damage.
14. Pointing at an acclimatizing untamed animal shows only its name and native
  percentage, for example `Boar (23%)`; disabling the interface toggle keeps
  Valheim's native hover text unchanged.
