# 02 — 全目录 Card Icon 出齐并抽检透明底

**What to build:** Every Card in the catalog has its own offline chibi Card Icon (including upgrade Cards), commissioned from an Icon Brief (name, description, kind, style tags), matching existing hero chibi art language and depicting symbolic item/effect—not a full hero. Square transparent PNG sources at the agreed size; transparency enforced by generation constraints and spot-check/regenerate (no auto-matte). After this ticket, a complete build’s Card picks are not randomly icon-sparse.

**Blocked by:** 01 — Card pick 显示 Card Icon（含回退与中文 Kind）

**Status:** ready-for-agent

- [x] Every catalog Card identity has a loadable Card Icon asset
- [x] Upgrade Cards have distinct icons from their base Cards
- [x] Icons match hero chibi style and use transparent cutout alpha (spot-checked; failures regenerated)
- [x] Icons display correctly at the pick UI size locked in ticket 01
