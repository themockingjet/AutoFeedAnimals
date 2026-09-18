# Icon Brief

This brief tells Copilot (or any icon generator) what must stay
consistent across all mods and what may vary for this specific mod. See
the shared brand kit in [valheim-mod-brand/](/home/dev/github/valheim/valheim-mod-brand) for the full system,
reusable prompts, and templates.

## Brand system

- Canvas: 256x256 PNG
- Style: Nordic fantasy, low-poly, hand-painted, Valheim-adjacent
- Composition: centered object, three-quarter perspective
- Frame: reuse the shared rounded dark-metal rune frame
- Lighting: warm upper-left light with cool blue shadow
- Text: none
- Transparency: none

## Mod identity

- Mod name: `AutoFeedAnimals`
- Category: `farming / automation`
- Primary subject: a calm tamed boar lowering its head toward a glowing food
  pile
- Supporting objects: a shallow wooden feeding trough, barley and carrots,
  subtle linked rune marks suggesting an ownership-safe routine
- Accent color: harvest gold with muted leaf green
- Mood: reliable, pastoral, practical, quietly magical
- Avoid: logos from Valheim, copied game UI, readable text, clutter,
  photorealism, extra characters, unrelated weapons.

## Examples

### SmelterAutoFeed

```md
## Mod identity
- Mod name: SmelterAutoFeed
- Category: automation
- Primary subject: a stone smelter
- Supporting objects: ore entering one side, coal entering another
- Accent color: ember orange
- Mood: reliable, warm, mechanical, magical
```

### Veinmine

```md
## Mod identity
- Mod name: Veinmine
- Category: resource gathering
- Primary subject: a pickaxe breaking a glowing ore vein
- Supporting objects: copper/gold fragments, subtle crack lines
- Accent color: warm copper
- Mood: forceful, efficient, mineral-rich
```

## Package icon requirements

- Package icon must be an original 256x256 PNG.
- Reuse the shared mod-brand visual system where available.
- Preserve the standard frame, lighting direction, and background
  treatment.
- Vary only the central subject and approved accent palette according to
  this brief.
- Do not use text, copied Valheim artwork, third-party game assets,
  logos, screenshots, or trademarks.
- Verify the final `Thunderstore/icon.png` is exactly 256x256 before
  packaging.
