# LuckyToLive（Open Island Roguelike）

开放岛屿生存 Roguelike 原型（Godot 4.x + C#）。在限时岛屿上操控英雄杀怪升级、构筑武器 / 建筑 / 宠物槽位，撑过 5 分钟或倒下结算。

## 语言

选英雄界面与局内 HUD 右上角可切换 **中文 / English**；偏好写入本地存档 `user://save.cfg` 的 `settings.locale`。文案表见 `assets/i18n/translations.csv`（Godot TranslationServer）。

Web 导出没有系统字体回退，因此项目默认 UI 字体为 `assets/fonts/NotoSansSC-Game.ttf`（Noto Sans SC 子集，含中英）。若新增大量中文文案后出现缺字，用同目录 `regen_subset.py` 从完整 OTF 重新生成子集。

## 如何运行

### 依赖

1. 安装 **.NET SDK 10.0+**（根目录 `global.json` 会锁定）：https://dotnet.microsoft.com/download  
2. 安装 **Godot 4.7.x 的 .NET 版**（不要选 Standard）：https://godotengine.org/download  
3. （可选，仅浏览器导出）安装 wasm 工具：`dotnet workload install wasm-tools`

### Godot 编辑器（日常改场景 / 资源）

1. 用 Godot（.NET 版）导入本目录下的 `project.godot`  
2. 首次打开若提示编译 C#，确认通过；也可先执行 `dotnet build`  
3. 运行（F5），主场景为 `scenes/Main.tscn`

若提示无 C# 支持，请确认下载的是 .NET 版 Godot，并检查「编辑器 → 编辑器设置」中的 dotnet 路径。

### 2dog 主机（桌面 / Web）

本仓库已用 [2dog](https://2dog.dev/getting-started.html) 接入 .NET 宿主，游戏内容仍在仓库根目录：

```text
TDProject.csproj          — 游戏程序集（Godot.NET.Sdk / net10.0）
TDProject.2dog/           — 桌面 Generic Host
TDProject.web/            — 浏览器 WebAssembly Host
export_presets.cfg        — 2dog 导出预设（Web + Desktop）
```

```bash
# 桌面运行（嵌入 libgodot）
dotnet run --project TDProject.2dog

# 浏览器发布（静态站输出到 TDProject.web/AppBundle/）
dotnet workload install wasm-tools   # 一次性
dotnet publish TDProject.web
dotnet tool install -g dotnet-serve  # 一次性
dotnet serve --directory TDProject.web/AppBundle -z -b
```

### GitHub Pages

推送到 `main`（或手动跑 Actions）会发布 Web 包：`.github/workflows/pages.yml`。

一次性设置：仓库 **Settings → Pages → Build and deployment → Source: GitHub Actions**。  
站点地址一般为 `https://<user>.github.io/LuckyToLive/`。

本地 / CDN 部署建议关掉预压缩旁路（Pages workflow 已这样配置）：

```bash
dotnet publish TDProject.web -p:TwoDogWebPrecompress=false
```

详见 [2dog Web 文档](https://2dog.dev/web.html)。

## 核心玩法

- **选英雄**：战士 / 法师 / 猎人；首次进入选定「起始英雄」并解锁，其余英雄用元货币解锁  
- **操作**：WASD / 方向键在岛屿内移动；武器与宠物自动攻击  
- **时限**：单局 5 分钟（`Game.RunDuration`）；到时存活即胜利，英雄阵亡则失败  
- **槽位构筑**：默认 5 槽（本局最多再 +2，HUD 上「广告」按钮为占位解锁）；可装配武器、建筑、宠物、被动  
- **升级选卡**：击杀获经验，升级时三选一；槽满后主要出现已有物品的升级卡  
- **精英掉落**：精英死亡掉「大件」高亮道具，靠近自动拾取并获得一张卡  
- **分钟目标**：每分钟击杀至少 1 精英可计入目标完成数，影响结算分数  
- **结算**：按击杀 / 精英 / 分钟目标 / 装备等级和评分（S/A/B/C），获得元货币用于解锁英雄  

### 英雄概览

| 英雄 | 特点 | 起始卡 |
|------|------|--------|
| 战士 | 高生命、近战裂斩 / 冲锋、盾墙与战旗 | 裂斩 |
| 法师 | 较低生命、冰矢 / 火球 / 射线、减速场与火塔 | 冰矢 |
| 猎人 | 高移速、穿透箭 / 冰箭、狼宠与陷阱 | 穿透箭 |

## 代码结构

```
project.godot              — 项目名 Open Island Roguelike；Game 为 Autoload
TDProject.csproj           — Godot.NET.Sdk / net10.0（2dog 游戏程序集）
TDProject.2dog/            — 2dog 桌面宿主
TDProject.web/             — 2dog 浏览器宿主（dotnet publish → AppBundle）
TDProject.slnx             — 解决方案（web 默认不参与普通 build）
scenes/Main.tscn           — 主场景，挂载 Main.cs
assets/characters|enemies  — 英雄与敌人贴图
assets/environment         — 岛屿地面 / 树木 / 草丛 / 岩石等环境贴图
scripts/
  I18n.cs                  — 国际化 Autoload（CSV → TranslationServer，中英切换）
  Game.cs                  — 元进度（货币、解锁英雄、存档）与本局统计
  Main.cs                  — 选人、开局、计时、选卡流程、结算；岛屿地面绘制
  EnvironmentArt.cs        — 环境贴图加载（res://assets/environment/）
  IslandDecor.cs           — 开局铺设主题障碍（可碰撞）与树木 / 草丛等装饰
  MapCatalog.cs            — 地图主题（地表、敌人、障碍阵列）
  Hero.cs                  — 英雄移动、生命、经验、武器开火
  Loadout.cs               — 槽位列表；应用卡牌生成建筑/宠物/被动
  CardCatalog.cs           — 卡牌定义与三选一抽取池
  SpawnDirector.cs         — 刷怪密度、精英与分钟冲击波
  Enemy.cs / Projectile.cs — 敌人与投射物
  Building.cs / Pet.cs     — 建筑与宠物实体
  BigItemDrop.cs           — 精英大件掉落
  ui/HeroSelect.cs         — 选英雄界面（含语言切换）
  ui/Hud.cs                — HUD（时间、生命、经验、槽位、广告槽、语言切换）
  ui/CardPopup.cs          — 暂停时的选卡弹窗
  ui/ResultScreen.cs       — 结算界面
assets/i18n/translations.csv — 中英对照文案表
```

多数 UI 与实体由代码动态创建；岛屿海域 / 沙滩 / 草地 / 泥土在 `Main._Draw()` 中铺贴，树木与草丛等由 `IslandDecor` 在开局生成。

## 可扩展方向

- 真实广告 SDK 接入「本局 +1 槽」  
- 更丰富的精英词条（冲刺 / 护盾 / 召唤 / 远程已有占位）  
- 音效、粒子与更高清手绘环境替换  
- 数值平衡与更多协同 / 商人事件  

已落地（见 `docs/design-synergy-boss-v1.md`）：7 条协同进化、2:30/4:30 Boss、每局 1 次重随。

## 已知限制（原型）

- 广告解锁槽位仅为本地占位，无真实广告  
- 无完整音效与关卡编辑器内容  
- 存档仅本地 `user://save.cfg`（起始英雄、解锁、元货币）  
