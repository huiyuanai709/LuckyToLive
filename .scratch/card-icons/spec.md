Status: ready-for-agent

# Spec: Card Icons on Card pick UI

## Problem Statement

When a player opens a Card pick, each option is text-only. It is hard to tell Cards apart at a glance—especially upgrade Cards that share a lineage with a base item—and the pick feels visually flat next to the existing chibi hero art.

## Solution

Every Card gets its own offline AI-commissioned chibi Card Icon. On the Card pick UI, each option shows the Card Icon above the Card name, Card Description, and a Chinese Card Kind Label. Icons ship as square transparent PNGs and load by Card identity convention; if an icon is missing, the option still works as text-only.

## User Stories

1. As a player, I want each Card option to show a Card Icon, so that I can recognize offers faster than reading text alone.
2. As a player, I want the Card Icon to sit above the Card name and Card Description, so that the layout matches the familiar hero-select pattern.
3. As a player, I want to still read the Card name, so that I know exactly which Card I am choosing.
4. As a player, I want to still read the Card Description, so that I understand the effect before committing.
5. As a player, I want a Chinese Card Kind Label (武器 / 建筑 / 升级 / 被动 / 宠物), so that I can tell upgrades from new items without decoding English enums.
6. As a player, I want upgrade Cards to have their own Card Icons, so that “裂斩·锋刃” does not look identical to “裂斩”.
7. As a player, I want Card Icons to match the existing hero chibi art language, so that the pick UI feels like the same game.
8. As a player, I want Card Icons to depict the Card’s symbolic item or effect rather than a full hero body, so that weapons, buildings, pets, and passives stay readable at small size.
9. As a player, I want Card Icons on a transparent cutout, so that they sit cleanly on the option button without a white box.
10. As a player, I want Card Icons sized consistently across options, so that a three-pick row does not look uneven.
11. As a player, I want choosing a Card to behave exactly as today (pause, apply, resume), so that visuals do not change run rules.
12. As a player, I want the starter single-Card pick to show a Card Icon when available, so that early moments are not text-only exceptions.
13. As a player, I want level-up and elite-drop picks to show Card Icons when available, so that every pick surface is consistent.
14. As a player, I want missing Card Icons to fall back to text-only without crashing, so that a partial asset set still lets me play.
15. As a content author, I want each Card identity to map to exactly one Card Icon asset by convention, so that I do not maintain a separate path field per Card.
16. As a content author, I want Icon Briefs built from Card name, Card Description, kind, and style tags, so that commissioned art stays on-brief.
17. As a content author, I want offline AI generation (not runtime generation), so that picks stay instant and deterministic.
18. As a content author, I want source Card Icons at roughly 256×256 square PNG, so that UI can scale them down without mush.
19. As a content author, I want UI display around 96–128 px, so that icons fit the existing option button footprint.
20. As a content author, I want alpha used only for cutout transparency (not whole-icon fade), so that icons stay vivid.
21. As a content author, I want transparency enforced by generation constraints and spot-check/regenerate, so that we do not depend on an auto-matte script that can eat highlights.
22. As a content author, I want the first delivery to include a Card Icon for every Card currently in the catalog, so that no pick option is randomly icon-less in a complete build.
23. As a developer, I want Card pick construction to remain the single UI entry for options, so that callers do not need a new API to show icons.
24. As a developer, I want the existing Chosen(cardId) signal contract preserved, so that run flow code does not change.
25. As a developer, I want icon loading to follow the same “exists then load texture” pattern used for hero portraits, so that asset absence is safe.
26. As a QA tester, I want a three-option pick with all icons present to show three consistent chibi icons plus Chinese labels, so that I can accept the happy path visually.
27. As a QA tester, I want a pick where one icon file is deliberately missing to show text-only for that option only, so that fallback is proven.
28. As a QA tester, I want upgrade vs base Card options side by side to show distinct Card Icons and the 升级 label, so that lineage confusion is reduced.
29. As a player, I do not want whole-button background illustrations with overlaid text, so that descriptions stay readable.
30. As a player, I do not want English `[Weapon]` / `[Upgrade]` labels on the button, so that the UI stays in the game’s Chinese voice.
31. As a release owner, I want Card Icons shipped with the game assets (not fetched at pick time), so that offline play and pause-menu picks remain reliable.
32. As an art reviewer, I want failed transparent-background generations regenerated rather than auto-punched, so that silhouette quality stays intentional.
33. As a future content author, I want adding a new Card to imply adding `cards/{id}` art by the same convention, so that the pipeline stays obvious.
34. As a player, I want icon and text hierarchy clear enough that I am not forced to open another tooltip to choose, so that picks stay snappy under time pressure.
35. As a developer, I want no per-Card icon path schema on Card definitions for this delivery, so that catalog data stays focused on gameplay fields.

## Implementation Decisions

- Respect ADR: Card Icons are offline AI PNGs with transparent square canvases; one icon per Card identity including upgrades; load by identity convention; missing files fall back to text-only; art matches hero chibi language; transparency via constraints + spot-check, not auto-matte.
- Modify the Card pick popup construction so each option is a vertical composition: Card Icon (when present) → Card name → Card Description → Chinese Card Kind Label. Keep the option clickable as a single control that emits the existing Chosen signal with the Card id.
- Do not change the Card pick caller contract: callers still pass a title and a list of Card definitions; apply/loadout behavior stays outside the popup.
- Resolve Card Icon resources by convention from Card identity (folder of card icons, filename equals Card id, PNG). No new icon-path field on Card definitions for this delivery.
- When the resource is absent, omit the icon node and still show name, description, and kind label.
- Display size roughly 96–128 px; preserve aspect; prefer the same texture filtering approach used for other chibi UI portraits so edges stay crisp.
- Source assets: ~256×256 square PNG, transparent cutout alpha only (not overall translucency).
- Commission one Card Icon per catalog Card for the first delivery, using an Icon Brief of name + Card Description + kind + relevant style tags; style aligned to existing hero chibi art; subject is symbolic item/effect, not a full hero.
- Map Card Kind Label as: Weapon→武器, Building→建筑, Upgrade→升级, Passive→被动, Pet→宠物.
- Do not introduce runtime image generation, shared base-item icons for upgrades, or automated background-removal post-processing in this delivery.
- Domain language for this work lives in the project glossary; keep UI and asset work consistent with Card / Card Icon / Card Description / Icon Brief / Card Kind Label.

## Testing Decisions

- Good tests assert external behavior at the Card pick presentation boundary only: given Cards and whether icon resources exist, what the player-facing option shows and that Chosen still returns the correct Card id. Do not assert internal path-string construction, node tree shape, or AI prompt text.
- Primary seam: Card pick display via the existing popup setup entry (highest existing seam). Ideal number of seams: one.
- Cover at least: (a) all offered Cards have icons → each option shows icon + name + description + Chinese kind label; (b) one missing icon → that option is text-only, others unchanged, no crash; (c) activation still emits Chosen with the selected Card id.
- Asset completeness for first delivery may be checked as an adjunct to the same seam: every catalog Card identity has a loadable Card Icon resource.
- Prior art: there is no automated test suite in the repo today; hero select already demonstrates safe texture load + portrait-above-text layout. Until a test harness exists, acceptance is manual/play-mode against the seam above; any future automated tests should lock that same seam rather than inventing lower ones.

## Out of Scope

- Runtime or online AI image generation during a pick.
- Automatic background-removal / alpha-punch scripts.
- Sharing one icon across a base item and its upgrade Cards.
- Adding icon path fields to Card definitions, icon atlases, or a separate icon manifest.
- Redesigning Card pick layout beyond icon + text stack (e.g. full-bleed card art, hover tooltips, animations).
- Showing Card Icons on HUD slots, world drops, or non-pick surfaces.
- Changing Card rolling, loadout application, pause behavior, or economy.
- Localization beyond the agreed Chinese Card Kind Labels.
- New automated test framework adoption beyond what is needed to express the seam (optional follow-up).

## Further Notes

- First delivery intentionally generates the full current catalog set (~35 Cards) so a complete build is not randomly icon-sparse; regenerate individually when transparency or style fails spot-check.
- If generation tooling cannot emit true alpha, treat that as a content defect and regenerate—do not silently ship opaque white boxes.
- Glossary and ADR already capture domain terms and the offline-icon decision; implement against those rather than inventing parallel vocabulary.
