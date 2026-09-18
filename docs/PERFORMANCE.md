# Performance Notes

## Heat map

### Native ground feeding — highest baseline cost

Valheim's `MonsterAI.FindClosestConsumableItem` allocates a
`Physics.OverlapSphere` result for each native search, then evaluates nearby
item colliders, native food eligibility, distance, and path state.
AutoFeedAnimals does not add another ground-food scan or replace that native
behavior.

The cost is driven primarily by:

$$O(A \times I)$$

where $A$ is the number of animals performing native searches and $I$ is the
number of nearby item colliders.

### Chest candidate searches — highest mod-side cost

Chest searches run from the native consume flow, not from a second global
update loop. A search can still visit every nearby candidate chest and call
`Inventory.GetAllItems()` for each candidate:

$$O(A \times C \times S \times (V + P))$$

where:

- $A$ is the number of eligible owner animals;
- $C$ is the number of nearby candidate containers;
- $S$ is the chest-search frequency after retry backoff;
- $V$ is inventory enumeration work;
- $P$ is path-check work when pathing is enabled.

Empty and inaccessible chests are therefore important profiling cases. The
mod does not cache mutable inventory contents as authoritative state; every
candidate snapshot and final consumption snapshot is intentionally fresh.

### Path checks and target movement

`BaseAI.HavePath(Vector3)` is used when selecting a chest and while moving to a
selected target. A valid selection result is reused for only a short movement
window, and a fresh path check remains required before inventory mutation.
`Ignore Pathing=true` skips these calls but does not skip ownership, access,
range, or food eligibility checks.

### Registry and allocation work

The registry performs a world-wide `FindObjectsByType<Container>` refresh at
initialization and at ten-second restoration intervals. Each animal refreshes
its nearby-container list at most every five seconds, but that list is now
filled in place rather than allocated on every refresh. The registry revision
also invalidates failed-search backoff when containers are created, changed, or
destroyed.

### Event-driven inventory hints

`Container.OnContainerChanged` updates a per-container content revision and
invalidates an advisory set of non-empty item shared names. The hint is
refreshed lazily by the authoritative owner during candidate selection, so a
second modded peer does not scan the inventory merely because it received the
same event. During candidate selection, a chest whose hint has no overlap with
the animal's native food templates can be skipped without another
`Inventory.GetAllItems()` snapshot. Hints are refreshed at most every five
seconds as a safety fallback for missed events, world restoration, and other
inventory integrations.

The hint is never authoritative: disallow lists, `MonsterAI.CanConsume`, access,
ownership, range, pathing, and the final fresh inventory snapshot still run
through the existing path. This keeps event-driven work from changing native
food eligibility or allowing stale inventory state to remove an item.

## Applied mitigations

- Native ground-food behavior, radius, and interval remain untouched.
- No second global update loop or scheduled native consume call is introduced.
- Nearby-container lists are reused per animal.
- Failed chest searches use a bounded retry backoff: 0.5, 1, 2, 4, then
  5-second additional delays on top of the native search interval.
- Container lifecycle and inventory-change notifications reset that backoff and
  force the next nearby-container refresh.
- Content changes are scoped to nearby containers instead of resetting retry
  state for every animal on every chest event.
- Event-driven food-name hints are invalidated on every peer but refreshed only
  when an authoritative owner needs them; they skip candidate inventory
  snapshots when no native food name can match, while unknown and expired hints
  fall back to a fresh snapshot.
- A selected path result is reused for at most 0.25 seconds and is revalidated
  freshly before removing food.
- Native food templates are cached per animal instead of rebuilding the
  species-food map for every chest search.
- Inventory contents are enumerated once per candidate chest and once again for
  final consumption validation; no mutable inventory cache is authoritative.
- Deny-list CSV strings are parsed once at startup instead of per animal and
  per search.
- Range checks use squared 3D distance where a square root is unnecessary.
- Live range/pathing changes clear per-animal chest targets and reusable caches
  once; they do not create a recurring update loop.

When BepInEx debug logging is enabled, the service emits a 30-second chest
feeding counter report containing search attempts, failed searches, successful
feeds, nearby refreshes, candidate containers, inventory snapshots, and path
checks, plus content-change checks, relevant content changes, hint refreshes,
and hint skips. These counters are intended for before/after profiling and are
not a new gameplay setting.

## Profiling scenarios

Profile the dedicated server with:

1. 10, 50, and 100 tame animals with empty chests.
2. 100 hungry animals sharing one chest with several food stacks.
3. 100 hungry animals with multiple nearby chests.
4. Dense loose ground food, measuring native `OverlapSphere` allocations.
5. `Ignore Pathing=false` with reachable and unreachable chest layouts.
6. `Ignore Pathing=true` with the same layouts.
7. MultiUserChest enabled with simultaneous chest access.

Record frame time, allocation spikes, chest inventory correctness, debug
counter values, and whether animals keep native ground feeding behavior. The
expected result is materially fewer repeated inventory snapshots for empty
chests and fewer redundant path checks without delayed response after a chest
change. A chest-side slowdown should be addressed by reducing
candidate/container work, not by bypassing ownership, access checks, or native
food eligibility.