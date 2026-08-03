# 01 — Card pick 显示 Card Icon（含回退与中文 Kind）

**What to build:** When a player opens a Card pick, each option shows a Card Icon above the Card name, Card Description, and a Chinese Card Kind Label (武器 / 建筑 / 升级 / 被动 / 宠物). Icons load by Card identity convention. If an icon is missing, that option stays text-only and the pick still works. Choosing still reports the Card id as today. Ship a small set of sample Card Icons so the layout is playable end-to-end.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [x] Card pick options use icon-above-text layout (name, description, Chinese kind label)
- [x] Card Icon appears when the conventional asset for that Card id exists
- [x] Missing icon falls back to text-only without crash; Chosen still returns the correct Card id
- [x] At least a few sample Card Icons are present so a real pick can demo the happy path
