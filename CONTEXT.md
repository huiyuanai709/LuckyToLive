# LuckyToLive

Roguelike pickup / loadout game: heroes choose Cards that grant or upgrade items in the run.

## Language

**Card**:
A selectable offer in a pick (weapon, building, pet, passive, or upgrade), defined by identity, name, description, and kind.
_Avoid_: Option, choice, reward (unless referring to the pick moment itself)

**Card Icon**:
The chibi (Q-version) illustration that visually represents one Card in UI; each Card identity has its own icon, including upgrade Cards. In a pick, it sits above the Card name and Card Description. Art language matches the existing hero chibi look (big-head cartoon, clean outlines, cel shading, transparent cutout), depicting the Card's symbolic item or effect rather than a full hero body.
_Avoid_: Runtime-generated image, shared base-item art for upgrades, emoji, placeholder glyph, full-bleed button background art, mismatched flat UI-icon style

**Card Description**:
The short Chinese text on a Card that explains what it does.
_Avoid_: Flavor text, tooltip, lore (unless a separate field is introduced later)

**Icon Brief**:
The package used to commission a Card Icon: Card name, Card Description, kind, and relevant style tags (weapon/building/pet/upgrade stat as applicable).
_Avoid_: Prompt-only, raw Desc alone

**Card Kind Label**:
The Chinese UI label for a Card's kind shown on the pick button: 武器, 建筑, 升级, 被动, or 宠物.
_Avoid_: Raw English enum text like `[Weapon]` / `[Upgrade]`
