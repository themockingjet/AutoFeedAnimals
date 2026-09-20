# Auto Feed Animals Architecture

## Purpose

Optionally assists hungry tameable animals, including animals still being
tamed, with eligible food from accessible nearby chests while preserving
Valheim's native food eligibility and consumption behavior. Manual feeding and
breeding remain available when the feature is disabled.

## Runtime flow

1. `AutoFeedAnimalsPlugin` is loaded by BepInEx.
2. `AutoFeedAnimalsSettings` registers local range, pathing, protection,
   exclusion, and display settings.
3. Harmony gates native `MonsterAI.UpdateConsumeItem` to tame, hungry,
	locally owned animals.
4. `MonsterAIUpdateConsumeItemPatch` preserves native ground feeding and
   delegates chest feeding to `AnimalFeedService`; Valheim retains native
   item-removal callbacks and ground-food behavior.
5. Harmony patches are removed when the plugin is destroyed.

Performance details and profiling scenarios are documented in
[PERFORMANCE.md](PERFORMANCE.md).

## Components

Document each service, patch, and integration as it is added:

| Component | Responsibility | Native API boundary |
| --- | --- | --- |
| `AutoFeedAnimalsPlugin` | Plugin lifecycle, service construction, Harmony registration, and cleanup | BepInEx, Harmony |
| `AutoFeedAnimalsSettings` | Config.Bind, typed settings, and setting-change propagation | BepInEx.Configuration |
| `AnimalFeedService` | Owner checks, chest selection, direct inventory consumption, and protection decisions | `MonsterAI`, `Tameable`, `Container`, `Inventory` |
| `FeedContainerRegistry` | Container registration, membership/content revisions, event-driven advisory food hints, restoration refresh, player-container filtering, nearby lookup, and access checks | `Container`, `ZNetView`, `ZDO`, `Inventory` |
| `FeedFilter` | Comma-separated food and animal exclusions | Configuration values |
| `ChestFeedState` | Per-animal chest target, timers, reusable nearby-container buffer, membership/content snapshots, retry backoff, short-lived path result, and food templates | Local runtime state |
| `FeedPerformanceMetrics` | Debug-only counters for chest search, validation, and event-driven hint cost | BepInEx logging |
| `MonsterAIUpdateConsumeItemPatch` | Preserves native ground feeding and delegates chest feeding | `MonsterAI` |
| `ContainerLifecyclePatches` | Tracks container creation, destruction, and inventory changes | `Container` |
| `FeedContainerProtectionPatch` | Prevents untamed tameable animals damaging registered feed containers while preserving native damage from other enemies | `WearNTear`, `HitData` |
| `TameableHoverTextPatch` | Adds the optional acclimatizing progress display | `Tameable` |

## State and ownership

Animal and chest feeding is owner-authoritative. Only a peer that owns both
the animal and source chest, and passes `Container.CheckAccess`, may withdraw
food. The mod never claims ownership or writes tame feeding ZDO data. The
native consume callback remains responsible for the fed/taming timer.

Performance mitigation state is local and advisory. Registry revisions may
reset a failed-search backoff, but they never authorize a withdrawal or replace
the fresh inventory, ownership, access, range, path, or native food checks.
Container-change events update per-container content revisions and invalidate a
short-lived item-name hint. The hint is refreshed lazily by an authoritative
owner during candidate selection, so non-owner peers do not scan inventories
just because a chest changed. The hint can skip a candidate inventory snapshot
only when no known item name matches the animal's native food templates;
unknown or expired hints fall back to a fresh snapshot.

## Out of scope

- Feeding from containers without valid local ownership and
  `Container.CheckAccess`.
- Custom food allow-lists, direct ZDO writes, and ownership claims.
- Changing native ground-food radius or interval. Chest feeding has its own
	synchronized range and movement settings.