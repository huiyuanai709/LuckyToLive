# 03 — 按「Card pick 展示缝」做验收（含缺图与齐全性）

**What to build:** Verify the Card pick presentation seam against the spec: with all icons present, each option shows icon + name + description + Chinese kind label; with one icon deliberately missing, only that option falls back to text-only and nothing crashes; choosing still returns the correct Card id. Also confirm catalog completeness—every Card identity has a Card Icon. No test framework required; produce a repeatable manual acceptance checklist (or automate the same seam later if a harness appears).

**Blocked by:** 01 — Card pick 显示 Card Icon（含回退与中文 Kind）; 02 — 全目录 Card Icon 出齐并抽检透明底

**Status:** ready-for-agent

- [x] Happy path: multi-option pick shows consistent Card Icons and Chinese kind labels
- [x] Missing-icon path: one absent asset → text-only for that option only; no crash
- [x] Chosen still delivers the selected Card id
- [x] Completeness: every catalog Card id has a loadable Card Icon

Acceptance checklist: [acceptance-checklist.md](../acceptance-checklist.md)
