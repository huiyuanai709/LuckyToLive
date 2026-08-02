# 塔防 Roguelike 原型 (Godot 4.x + C#)

## 如何运行
1. 安装 **.NET SDK 8.0**（Godot C# 需要）：https://dotnet.microsoft.com/download
2. 安装 **Godot 4.x 的 .NET 版**（不是标准版，官网下载页会分开列出 "Standard" 和 ".NET"）：https://godotengine.org/download
3. 打开 Godot（.NET 版），选择"导入" -> 选中本文件夹下的 `project.godot`
4. 首次打开时 Godot 会提示生成/编译 C# 项目（或者你可以先在命令行 `dotnet build` 一次）
5. 点击运行(F5)，主场景是 `scenes/Main.tscn`

如果 Godot 提示找不到 C# 支持，检查一下菜单栏"编辑器 -> 编辑器设置"里 Mono/dotnet 路径是否正确，或确认下载的是 .NET 版 Godot。

## 核心玩法（和 GDScript 版一致）
- 敌人沿固定路径行走，到达终点扣生命
- 空地上点"建塔"按钮花金币造塔
- 每击杀 5 个敌人获得 1 个技能点；有技能点时点击场上的塔弹出**升级选择窗口**
- 升级窗口默认 3 个随机选项（伤害/攻速/射程/溅射/连锁闪电/减速）
- 击杀敌人有几率掉落道具，点击拾取：
  - 金色 = 直接获得金币
  - 紫色 = **"接下来几次升级选项 +1"**（消耗型加成：拿到后接下来 3 次打开升级窗口选项数变成 4，用完为止）
  - 红色 = 全场塔伤害永久+5
  - 青色 = 全场塔攻速永久+0.1

## 代码结构
```
project.godot            — 项目配置，Game.cs 注册为自动加载单例
TDProject.csproj         — .NET 项目文件（net8.0, Godot.NET.Sdk）
scenes/Main.tscn          — 唯一场景，挂载 Main.cs，其余 UI/节点由代码动态生成
scripts/Game.cs           — 全局状态：Gold/Lives/Wave/SkillPoints/BonusChoiceCharges
                            （提供 Game.Instance 静态引用，方便其它脚本访问）
scripts/Main.cs           — 主循环：建塔、刷怪、UI、点击处理、升级弹窗触发
scripts/Tower.cs          — 塔：自动索敌、开火、ApplyUpgrade() 应用技能升级
scripts/Enemy.cs          — 敌人：沿路径移动、掉血、血条绘制
scripts/Projectile.cs     — 子弹：命中判定，处理 splash/chain/slow 三种特殊效果
scripts/ItemDrop.cs       — 掉落道具（4种类型，用颜色区分）
scripts/UpgradePopup.cs   — 升级选择弹窗（动态生成按钮，选项数由外部传入）
```

所有游戏对象都是纯代码绘制（`_Draw()` 画圆/矩形），没有用美术资源，方便你后续替换成 Sprite2D/贴图。

## 和 GDScript 版的差异说明
- 逻辑与数值完全一致，只是语言换成了 C#
- Godot 里的 signal 在 C# 中变成强类型的 event（如 `Tower.TowerLeveled`），用 `+=` 订阅
- `Game` 单例除了走 `/root/Game` 路径查找，也暴露了 `Game.Instance` 静态属性，代码里访问更方便

## 可以扩展的方向
- 塔的种类目前只有一种基础塔，可以加"塔类型"选择（法师塔/弓箭塔/炮塔等）
- 现在技能只挂在单个塔上，可以加"全局天赋树"
- 掉落道具目前是立即生效，可以改成"背包+主动使用"
- 波次目前只是线性增长血量，可以加 Boss 波、精英怪、多路径分支
- 建塔位置目前是固定几个点，可以改成网格系统 + 任意位置放置

## 已知限制（原型阶段）
- 没有存档/读档
- 没有音效和美术资源
- 数值未做详细平衡测试
