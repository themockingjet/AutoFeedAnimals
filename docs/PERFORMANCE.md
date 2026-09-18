# Performance Notes

## Hot spots

### Native ground feeding

Valheim's `MonsterAI.FindClosestConsumableItem` allocates a
`Physics.OverlapSphere` result for each native search, then evaluates nearby
item colliders, native food eligibility, distance, and path state. AutoFeedAnimals
does not add another ground-food scan or replace that native behavior.

The cost is driven primarily by:

$$O(A \times I)$$

where $A$ is the number of animals performing native searches and $I$ is the
number of nearby item colliders.

### Chest feeding

Chest searches run at each animal's native consume-search interval, not every
frame. The container registry is refreshed at startup, on container inventory
changes, and periodically during world restoration. Each animal refreshes its
nearby-container list at most every 5 seconds. Chest selection then checks
ownership, access, range, pathing when enabled, and inventory contents.

The remaining chest-side cost is one `Inventory.GetAllItems()` snapshot per
candidate chest during a search. This preserves correct concurrent inventory
validation and avoids stale cached item counts; do not cache mutable inventory
contents as authoritative state.

## Applied optimizations

- No second global update loop.
- No temporary `ItemDrop` spawn for chest food.
- Nearby containers are cached per animal and refreshed periodically.
- Native food templates are cached per animal instead of rebuilding the
  species-food map for every chest search.
- Inventory contents are enumerated once per candidate chest rather than once
  per food template.
- Deny-list CSV strings are parsed once at startup instead of per animal and
  per search.
- Range checks use squared 3D distance where a square root is unnecessary.
- `FindObjectsByType` runs only during container-registry initialization.
- Live range/pathing changes clear per-animal chest targets and caches once;
  they do not create a recurring update loop.

## Profiling scenarios

Profile the dedicated server with:

1. 10, 50, and 100 tame animals with empty chests.
2. 100 hungry animals sharing one chest with several food stacks.
3. 100 hungry animals with multiple nearby chests.
4. Dense loose ground food, measuring native `OverlapSphere` allocations.
5. `Ignore Pathing=false` with reachable and unreachable chest layouts.
6. `Ignore Pathing=true` with the same layouts.
7. MultiUserChest enabled with simultaneous chest access.

Record frame time, allocation spikes, chest inventory correctness, and whether
animals keep native ground feeding behavior. A chest-side slowdown should be
addressed by reducing candidate/container work, not by bypassing ownership,
access checks, or native food eligibility.