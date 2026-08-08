# Design v1：协同进化 + Boss 分钟 + 轻量重随

锁定范围：在现有「5 分钟岛上槽位构筑」上，补三块内容——**协同进化卡**、**Boss 分钟高潮**、**每局 1 次重随**。  
不在本版：商人、诅咒、元素反应、联机、永久技能树、广告 SDK。

参考循环：Vampire Survivors（进化）、Brotato（构筑张力）、Soulstone Survivors（Boss 波）。

---

## 1. 目标体验

| 分钟 | 玩家感受 |
|------|----------|
| 0–1 | 开局起步，认清武器手感 |
| 1–2 | 槽位成型，开始瞄协同材料 |
| 2–3 | **小 Boss（潮汐守卫）**，检验火力 |
| 3–4 | 冲进化 / 补洞；可用一次重随 |
| 4–5 | **终局 Boss（岛主）**，存活即胜 |

核心一句话：每局不只是「堆数值」，而是「有没有凑成那一组」。

---

## 2. 协同进化（Synergy）

### 2.1 规则

- 进化是一张特殊 **Upgrade 卡**（`CardKind.Upgrade`，`IsNewItem = false`）。
- 出现条件：Loadout **同时拥有**配方中的全部材料 ItemId，且目标武器尚未进化。
- 抽取：在 `CardCatalog.RollOptions` 中，满足条件时以 **高权重** 塞进候选池（见 2.4）。
- 选中后：
  1. 主武器 `ItemId` 变为进化体 Id（或挂 `EvolvedFrom` + 改 `WeaponStyle` / 数值）。
  2. **副材料不删槽**（避免惩罚建筑/宠物位）；副材料保留，但该协同本局不可再触发。
  3. HUD 槽位名刷新为进化名；可有一次短提示（MsgLabel）。
- 每英雄 **2 条** 主协同，另加 **1 条** 跨英雄公共协同。总计 7 条，首版先做满。

### 2.2 卡表（首版）

| SynergyId | 英雄 | 材料 A（主） | 材料 B | 进化名 | 效果摘要 |
|-----------|------|-------------|--------|--------|----------|
| `syn_w_cleave` | 战士 | `w_slash` | `w_charge` | 裂阵斩 | 裂斩改为大弧 AOE；伤害×1.35，范围+40；冲锋刃保留 |
| `syn_w_bastion` | 战士 | `w_shield_wall` | `w_heal_totem` | 堡垒之誓 | 盾墙反伤×1.5；战旗同时给范围内 **+10% 伤害**（光环） |
| `syn_m_frostfire` | 法师 | `m_ice` | `m_fire` | 霜火陨星 | 弹道改为冰火球：减速 0.5 + 溅射 70；伤害取两者较高侧×1.2 |
| `syn_m_prism` | 法师 | `m_beam` | `m_ice_field` | 棱镜射线 | 射线条数上限 3→4；命中附带短暂减速；寒冰阵保留 |
| `syn_h_pack` | 猎人 | `h_pet` | `h_trap` | 围猎号令 | 狼宠伤害×1.4；陷阱触发时狼瞬移咬一次（内置 CD 1.2s） |
| `syn_h_storm` | 猎人 | `h_pierce` | `h_frost` | 霜暴连矢 | 穿透箭每发分裂 1 支减速副箭；穿透数+1 |
| `syn_p_bloodrush` | 公共 | `p_vamp` | `p_speed` | 血疾 | 移速再+15；击杀回 2 HP；连杀 Frenzy 阈值各档 -1（3→2…） |

说明：

- 战士堡垒以建筑为主武器「质变」，进化标记挂在 `w_shield_wall` 上（主材料）。
- 猎人围猎主材料为 `h_pet`。
- `syn_p_bloodrush` 主材料 `p_vamp`；两被动都要在槽里。

### 2.3 数据形状（建议）

```csharp
public sealed class SynergyDef
{
    public string Id;              // syn_m_frostfire
    public HeroId? Hero;           // null = 公共
    public string[] Requires;      // 材料 ItemId
    public string PrimaryItemId;   // 被进化的主件
    public string ResultNameKey;   // i18n
    public string ResultDescKey;
    public string EvolveStat;      // 路由到 Loadout 的进化分支
}
```

进化卡以现有 `CardDef` 表达：

- `Id = "evo_m_frostfire"`（选卡用）
- `Kind = Upgrade`, `IsNewItem = false`
- `GrantsItemId = PrimaryItemId`
- `UpgradeStat = "evolve:<SynergyId>"`（或独立字段 `SynergyId`）

`Loadout` 增加 `HashSet<string> CompletedSynergies`，防止重复。

### 2.4 抽取权重

在 `RollOptions` 末尾（已有 pool 之后）：

1. 扫描 `SynergyCatalog`，条件满足且未完成 → 生成对应进化 `CardDef`。
2. 若进化候选非空：三选一里 **至少占 1 格**（若 pool 不足 3 则优先填进化）。
3. 槽满时进化卡仍可出现（它是 Upgrade，不占新槽）。

### 2.5 UI

- Kind 标签：沿用「升级」，或新增 `CardKind` 显示为「进化」（推荐后者，文案 `ui.kind.evolve`）。
- 卡面 Desc 写清材料：「需要：冰矢 + 火球」。
- 首次凑齐材料时可选 Msg：「协同可进化：霜火陨星」（不强制弹窗，免打断）。

---

## 3. Boss 分钟

### 3.1 时间轴（改 `SpawnDirector`）

| 时间 | 事件 | 行为 |
|------|------|------|
| 60s / 120s / 180s / 240s | 保留分钟精英波 | 现有 `QueueEliteWave`，波次略减（见下） |
| **150s（2:30）** | **潮汐守卫**（小 Boss） | 停止普通刷怪 8s；生成 1 只；计为精英（算分钟目标） |
| **270s（4:30）** | **岛主**（终局 Boss） | 停止普通刷怪至死亡或开局结束；生成 1 只；击杀额外分 |
| 原 270s 的 `QueueEliteWave(4)` | **删除** | 避免与岛主叠爆 |

分钟精英波微调：`1 + minute/2` → `1`（minute 1–2）、`2`（minute 3–4），把压力让给 Boss。

### 3.2 Boss 定义

复用 `Enemy`，新增 `ConfigureBoss(string bossId, float hpMul)`：

| BossId | 出场 | HP 基准 | 体型 | 技能组合（复用 Affix 逻辑） | XP / 掉落 |
|--------|------|---------|------|-----------------------------|-----------|
| `tide_guard` | 2:30 | 420 × 时间系数 | 半径 52 | `melee` 冲锋 + 短冷却 | XP 40；掉 1 大件（必出进化相关或高价值升级） |
| `island_lord` | 4:30 | 900 × 时间系数 | 半径 64 | `orbit` + `fire_ground` 交替；接触伤 18 | XP 80；掉 1 大件；击杀 +80 结算分 |

约束：

- 同时场上最多 1 Boss（`IsBoss` 标记）。
- Boss 死亡不刷第二只；超时未击杀岛主 → 仍按存活判定胜负（现有 5:00 胜利规则不变）。
- Boss 算 `EliteKills` / 分钟目标精英数。
- HUD：`ui.hud.boss_tide` / `ui.hud.boss_lord` 提示；可选简易 Boss 血条（首版用 MsgLabel + 精英血色即可，血条为 P1.1）。

### 3.3 地图差异（轻量）

`MapId` 只换皮肤与名字，不换机制：

| Map | 小 Boss 显示名 | 终局 Boss 显示名 |
|-----|----------------|------------------|
| Island | 潮汐守卫 | 岛主 |
| Apocalypse | 烬核守卫 | 废土领主 |
| Wilderness | 棘藤守卫 | 密林霸主 |
| Desert | 沙暴守卫 | 烈日领主 |

逻辑 Id 仍是 `tide_guard` / `island_lord`。

---

## 4. 轻量重随（Reroll）

### 4.1 规则

- 每局 **1 次** 免费重随（`Game` 本局计数 `RerollsLeft`，开局 = 1）。
- 仅在升级三选一 / 精英大件弹窗中可用；开局强制单卡不可重随。
- 重随：销毁当前 3 张，按同一 `RollOptions` 再抽；**允许重复出现**同一张（简化）；进化卡权重规则仍生效。
- UI：CardPopup 底部按钮「重随 (1)」→ 点完变灰。

### 4.2 不做

- 广告换重随、元货币买重随、锁定单卡——留给后续。

---

## 5. 结算与分数

在现有公式上追加：

```
score += CompletedSynergies.Count * 50
score += IslandLordKilled ? 80 : 0
score += TideGuardKilled ? 30 : 0
```

Rank 阈值不变；元货币换算不变。  
ResultScreen 可加两行小字：协同数、Boss 击杀（P1）。

---

## 6. 实现切片（建议 PR 顺序）

| 切片 | 内容 | 验收 |
|------|------|------|
| **A** | `SynergyCatalog` + RollOptions 注入进化卡 + Loadout 进化分支（可先做数值版，特效简陋） | 凑齐材料必能抽到进化；选中后主件数值变化且不重复 |
| **B** | `ConfigureBoss` + 2:30 / 4:30 时间轴；删旧 270s 四精英波 | 两场 Boss 稳定出现；场上敌人数不爆 |
| **C** | CardPopup 重随 ×1 | 每局仅一次；开局卡无按钮 |
| **D** | i18n 文案 + 结算加分 + HUD 提示 | 中英齐全；分数可见 |

切片 A→B→C 可串行；D 随各片补齐。

---

## 7. 非目标（明确砍掉）

- 商人 / 祭坛 / 宝箱事件  
- 诅咒卡、Ban 卡  
- 真实元素反应（冰火进化用静态数值模拟即可）  
- 宠物 AI 模式切换  
- 新英雄、新地图机制  
- 音效包（Boss 出场可后续加）

---

## 8. 验收清单（v1 Done）

- [ ] 7 条协同均可在对应英雄/公共下触发一次进化  
- [ ] 进化不新占槽、不删除副材料、本局不重复  
- [ ] 2:30 小 Boss、4:30 终局 Boss 各出现一次  
- [ ] 终局 Boss 与旧「270s 四精英」不同时存在  
- [ ] 每局恰好 1 次重随机会  
- [ ] `dotnet build` 通过；手动一局能看到进化与 Boss 提示  

---

## 9. 数值初值（平衡用，可调）

- 进化伤害倍率落在 **1.25–1.45**，避免秒杀 Boss。  
- 潮汐守卫战斗时长目标：**15–25s**（中等构筑）。  
- 岛主战斗时长目标：**25–40s**；打不死也能靠走位熬到 5:00。  
- 重随不改变经验曲线，只改善坏随。
