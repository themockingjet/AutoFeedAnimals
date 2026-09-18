# Changelog

## [Unreleased]

## 0.1.2

### Changed

- Reduced repeated empty-chest work with reusable nearby-container buffers and
  bounded chest-search retry backoff; container changes reset the backoff.
- Reduced redundant chest path checks while retaining fresh validation before
  inventory mutation.
- Added event-driven per-container food-hint invalidation and scoped content
  revisions; hints refresh lazily on the authoritative owner and stale or
  unknown hints fall back to fresh inventory validation.

### ServerSync/ConfigSync

- Advanced the minimum required plugin version to `0.1.2`; synchronized
  settings and the version handshake remain unchanged. ServerSync remains
  merged into the release DLL.

## 0.1.1

### Added

- Added owner-authoritative chest-backed feeding for tame animals using native
  food eligibility, inventory access, pathing, direct item removal, and
  consumption without spawning ground drops.
- Added synchronized feed range, pathing, container protection, food
  exclusion, and animal exclusion settings.
- Auto-feeding is enabled by default for new configuration files.
- Existing loaded containers are discovered during startup/restoration instead
  of requiring food to be removed and re-added.
- Hungry untamed animals can now consume eligible chest food through the native
  taming flow.
- Added a toggleable local taming-progress display beside untamed animal names.
- Fixed restored player chests being skipped by relaxed creator-ZDO checks and
  shortened nearby-container refreshes for elevated or late-loaded chests.

### Fixed

- Preserved native damage from non-tameable enemies while feed-container
  protection is enabled; only untamed tameable animals are blocked.

### ServerSync/ConfigSync

- Added synchronized spaced `Feeding/*` settings, including `Ignore Pathing`;
  the version handshake is unchanged. ServerSync remains merged into the
  release DLL.

## 0.1.0

- Initial release of the Auto Feed Animals plugin.
- Added synchronized spaced feeding settings.
- Added an owner-authoritative Harmony integration around native animal feeding
  and its native spherical food search.

### ServerSync/ConfigSync

- Settings and the minimum required plugin version use ServerSync's handshake.
- `ServerSync.dll` is a build-time dependency merged into the release DLL.