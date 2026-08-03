# Card pick seam — manual acceptance checklist

Primary seam: `CardPopup.Setup(title, options)` (Card pick presentation).

Run in Godot play mode (F5). Pause-driven picks already route through this popup.

## A. Happy path (icons present)

1. Start a run, level up (or force a multi-option pick).
2. Confirm each option shows, top → bottom:
   - Card Icon (~112 px, consistent size across options)
   - Card name
   - Card Description
   - Chinese Card Kind Label (`武器` / `建筑` / `升级` / `被动` / `宠物`) — not English `[Weapon]` etc.
3. Confirm icons are transparent cutouts on the button (no solid white box).
4. Confirm upgrade vs base side by side (when offered) use distinct icons and the `升级` label.

## B. Missing-icon fallback

1. Temporarily rename one shipped icon, e.g. `assets/cards/w_slash.png` → `w_slash.png.bak`.
2. Open a pick that includes that Card.
3. Expect: that option is text-only (name + description + kind label); other options unchanged; no crash.
4. Restore the file.

## C. Chosen contract

1. Click an option.
2. Expect: popup closes / run resumes as today; chosen Card id is applied (starter / level-up / elite drop all still work).
3. No new caller API required — `Setup` + `Chosen(cardId)` unchanged.

## D. Catalog completeness

Every catalog Card id must have `res://assets/cards/{id}.png`:

| Id | Name |
|----|------|
| w_slash | 裂斩 |
| w_charge | 冲锋刃 |
| w_shield_wall | 盾墙 |
| w_heal_totem | 战旗 |
| w_turret | 矛塔 |
| up_w_slash_dmg | 裂斩·锋刃 |
| up_w_slash_rate | 裂斩·连斩 |
| up_w_charge | 冲锋·破阵 |
| up_w_wall | 盾墙·加固 |
| up_w_totem | 战旗·鼓舞 |
| up_w_turret | 矛塔·强化 |
| m_ice | 冰矢 |
| m_fire | 火球 |
| m_beam | 元素射线 |
| m_ice_field | 寒冰阵 |
| m_fire_turret | 火法塔 |
| up_m_ice | 冰矢·深寒 |
| up_m_fire | 火球·爆炎 |
| up_m_beam | 射线·聚焦 |
| up_m_field | 寒冰阵·扩大 |
| up_m_fturret | 火法塔·烈焰 |
| h_pierce | 穿透箭 |
| h_frost | 冰箭 |
| h_pet | 召唤狼宠 |
| h_trap | 捕兽夹 |
| h_camp | 营地哨塔 |
| up_h_pierce | 穿透·连射 |
| up_h_frost | 冰箭·霜冻 |
| up_h_pet | 狼宠·野性 |
| up_h_trap | 捕兽夹·锋利 |
| up_h_camp | 哨塔·校准 |
| p_hp | 强体 |
| p_speed | 疾步 |
| p_regen | 再生 |
| up_p_hp | 强体·再锻 |

Quick check (PowerShell from repo root):

```powershell
$ids = @('w_slash','w_charge','w_shield_wall','w_heal_totem','w_turret','up_w_slash_dmg','up_w_slash_rate','up_w_charge','up_w_wall','up_w_totem','up_w_turret','m_ice','m_fire','m_beam','m_ice_field','m_fire_turret','up_m_ice','up_m_fire','up_m_beam','up_m_field','up_m_fturret','h_pierce','h_frost','h_pet','h_trap','h_camp','up_h_pierce','up_h_frost','up_h_pet','up_h_trap','up_h_camp','p_hp','p_speed','p_regen','up_p_hp')
$ids | Where-Object { -not (Test-Path "assets/cards/$_.png") }
```

Expect empty output.

## E. Surfaces

- [ ] Starter single-Card pick shows icon when present
- [ ] Level-up three-pick shows icons
- [ ] Elite-drop pick shows icon when present

## Notes

- No automated test harness in-repo; this checklist locks the Card pick presentation seam until one exists.
- Offline AI generation produced opaque RGB; edge-connected near-white → alpha was applied as a one-time content fix so cutouts match hero portraits (generator could not emit true alpha). Spot-check silhouettes; regenerate individual icons if highlights were damaged.
