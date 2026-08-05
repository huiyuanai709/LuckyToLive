using Godot;

public enum MapId
{
	Island = 0,
	Apocalypse = 1,
	Wilderness = 2,
	Desert = 3,
}

/// <summary>障碍物阵列形状：组成可读的阻隔带，而不是零散单点。</summary>
public enum BarrierShape
{
	/// <summary>不规则堆叠（岩堆 / 废墟堆）。</summary>
	Clump = 0,
	/// <summary>水平 barricade。</summary>
	LineH = 1,
	/// <summary>竖直 barricade。</summary>
	LineV = 2,
	/// <summary>L 形掩体。</summary>
	LShape = 3,
	/// <summary>弧形围挡（开口朝外）。</summary>
	Arc = 4,
}

/// <summary>一组主题障碍物：中心、形状与规模。</summary>
public sealed class BarrierSpec
{
	public Vector2 Center;
	public BarrierShape Shape;
	public float SpanX = 120f;
	public float SpanY = 80f;
	public int Count = 5;
	public float ScaleMin = 1.15f;
	public float ScaleMax = 1.55f;
	/// <summary>为空则用主题 BlockKinds。</summary>
	public string[] Kinds;
}

/// <summary>地图主题：地表色、装饰物与敌人贴图约定。</summary>
public sealed class MapTheme
{
	public MapId Id;
	public Color Ocean;
	public Color Shallow;
	public Color ShoreFallback;
	public Color InlandFallback;
	public Color Border;
	public Color BorderGlow;
	public string ShoreGround;   // EnvironmentArt 名；空则用 ShoreFallback
	public string InlandGround;
	public string PatchGround;
	public string BasicEnemy;    // CharacterArt 敌人名（无 _sheet 后缀）
	public string EliteEnemy;
	public Color BasicFallback;
	public Color EliteFallback;
	public ulong DecorSeed;
	public string[] TreeKinds;
	public string[] BushKinds;
	public string[] RockKinds;
	public string[] GrassKinds;
	public string[] FlowerKinds;
	public string[] ScatterExtra; // mushroom / cactus_small / bone_pile 等
	/// <summary>可阻挡移动的硬障碍种类（岩石 / 树干 / 仙人掌等）。</summary>
	public string[] BlockKinds;
	/// <summary>主题障碍阵列：决定走位空间。</summary>
	public BarrierSpec[] Barriers;
	/// <summary>障碍脚下的地表斑块（与 Barriers 对齐）。</summary>
	public Rect2[] GroundPatches;
	public int TreeCount;
	public int BushCount;
	public int GrassCount;
	public int FlowerCount;
	public int ExtraCount;
	public int ScatterRockCount;
	public float InlandInset; // 内陆地相对岸线的内缩
	public float ClearRadius; // 出生点清空半径
}

public static class MapCatalog
{
	public static readonly MapId[] All =
	{
		MapId.Island, MapId.Apocalypse, MapId.Wilderness, MapId.Desert,
	};

	public static MapTheme Get(MapId id) => id switch
	{
		MapId.Apocalypse => Apocalypse,
		MapId.Wilderness => Wilderness,
		MapId.Desert => Desert,
		_ => Island,
	};

	public static readonly MapTheme Island = new()
	{
		Id = MapId.Island,
		Ocean = new Color(0.14f, 0.32f, 0.48f),
		Shallow = new Color(0.22f, 0.48f, 0.58f),
		ShoreFallback = new Color(0.78f, 0.70f, 0.45f),
		InlandFallback = new Color(0.22f, 0.42f, 0.28f),
		Border = new Color(0.40f, 0.62f, 0.45f),
		BorderGlow = new Color(0.55f, 0.78f, 0.55f),
		ShoreGround = "ground_sand",
		InlandGround = "ground_grass",
		PatchGround = "ground_dirt",
		BasicEnemy = "enemy_basic",
		EliteEnemy = "enemy_elite",
		BasicFallback = new Color(0.85f, 0.25f, 0.35f),
		EliteFallback = new Color(0.95f, 0.45f, 0.15f),
		DecorSeed = 0x151A11D0,
		TreeKinds = new[] { "tree_round", "tree_pine" },
		BushKinds = new[] { "bush" },
		RockKinds = new[] { "rock", "stump" },
		GrassKinds = new[] { "grass_tuft", "grass_tuft_b" },
		FlowerKinds = new[] { "flower_yellow", "flower_red" },
		ScatterExtra = new[] { "mushroom" },
		BlockKinds = new[] { "rock", "stump" },
		// 绿岛：几处岩堆 + 岸边树篱，留出中央开阔与斜向走位带
		Barriers = new[]
		{
			B(660, 440, BarrierShape.Clump, 140, 100, 7, 1.25f, 1.7f),
			B(1480, 930, BarrierShape.LineH, 200, 70, 6, 1.2f, 1.6f),
			B(1040, 670, BarrierShape.LineV, 70, 180, 6, 1.2f, 1.55f),
			B(420, 1100, BarrierShape.Clump, 150, 90, 5, 1.15f, 1.5f, "rock", "stump"),
			B(1880, 360, BarrierShape.Arc, 160, 120, 6, 1.2f, 1.55f),
			B(520, 280, BarrierShape.LineH, 160, 50, 4, 1.1f, 1.4f, "stump", "rock"),
			B(1760, 1180, BarrierShape.LShape, 140, 130, 6, 1.15f, 1.5f),
		},
		GroundPatches = new[]
		{
			new Rect2(600, 400, 140, 100),
			new Rect2(1400, 880, 180, 90),
			new Rect2(980, 580, 120, 160),
			new Rect2(420, 1100, 160, 80),
			new Rect2(1800, 320, 120, 100),
			new Rect2(1680, 1120, 150, 120),
		},
		TreeCount = 36,
		BushCount = 28,
		GrassCount = 140,
		FlowerCount = 36,
		ExtraCount = 14,
		ScatterRockCount = 8,
		InlandInset = 28f,
		ClearRadius = 170f,
	};

	public static readonly MapTheme Apocalypse = new()
	{
		Id = MapId.Apocalypse,
		Ocean = new Color(0.08f, 0.10f, 0.12f),
		Shallow = new Color(0.16f, 0.14f, 0.12f),
		ShoreFallback = new Color(0.28f, 0.26f, 0.22f),
		InlandFallback = new Color(0.22f, 0.20f, 0.18f),
		Border = new Color(0.45f, 0.28f, 0.22f),
		BorderGlow = new Color(0.70f, 0.25f, 0.18f),
		ShoreGround = "ground_rubble",
		InlandGround = "ground_ash",
		PatchGround = "ground_rubble",
		BasicEnemy = "enemy_zombie",
		EliteEnemy = "enemy_tyrant",
		BasicFallback = new Color(0.45f, 0.62f, 0.38f),
		EliteFallback = new Color(0.35f, 0.28f, 0.42f),
		DecorSeed = 0xA90CA179,
		TreeKinds = new[] { "tree_dead", "tree_burnt" },
		BushKinds = new[] { "bush_dead" },
		RockKinds = new[] { "rock_rubble", "bone_pile" },
		GrassKinds = new[] { "grass_tuft", "grass_tuft_b" },
		FlowerKinds = System.Array.Empty<string>(),
		ScatterExtra = new[] { "rock_rubble", "bone_pile" },
		BlockKinds = new[] { "rock_rubble", "bone_pile", "tree_burnt" },
		// 废土：碎石路障横切走廊，形成掩体巷战
		Barriers = new[]
		{
			B(560, 520, BarrierShape.LineH, 220, 60, 7, 1.3f, 1.75f, "rock_rubble"),
			B(1500, 520, BarrierShape.LineH, 220, 60, 7, 1.3f, 1.75f, "rock_rubble"),
			B(1040, 900, BarrierShape.LineH, 260, 55, 8, 1.25f, 1.65f, "rock_rubble", "bone_pile"),
			B(780, 1100, BarrierShape.LineV, 55, 160, 5, 1.2f, 1.55f, "rock_rubble"),
			B(1600, 1100, BarrierShape.LineV, 55, 160, 5, 1.2f, 1.55f, "rock_rubble"),
			B(400, 320, BarrierShape.Clump, 130, 110, 6, 1.2f, 1.6f, "bone_pile", "rock_rubble"),
			B(1900, 1280, BarrierShape.Clump, 140, 100, 6, 1.2f, 1.6f, "bone_pile", "tree_burnt"),
			B(1200, 340, BarrierShape.LShape, 150, 140, 7, 1.25f, 1.65f, "rock_rubble", "tree_burnt"),
		},
		GroundPatches = new[]
		{
			new Rect2(480, 470, 260, 100),
			new Rect2(1420, 470, 260, 100),
			new Rect2(940, 850, 300, 90),
			new Rect2(740, 1020, 100, 200),
			new Rect2(1560, 1020, 100, 200),
			new Rect2(340, 280, 150, 130),
			new Rect2(1840, 1220, 160, 120),
			new Rect2(1140, 280, 170, 160),
		},
		TreeCount = 18,
		BushCount = 16,
		GrassCount = 28,
		FlowerCount = 0,
		ExtraCount = 12,
		ScatterRockCount = 6,
		InlandInset = 18f,
		ClearRadius = 180f,
	};

	public static readonly MapTheme Wilderness = new()
	{
		Id = MapId.Wilderness,
		Ocean = new Color(0.10f, 0.28f, 0.34f),
		Shallow = new Color(0.18f, 0.42f, 0.40f),
		ShoreFallback = new Color(0.42f, 0.38f, 0.22f),
		InlandFallback = new Color(0.14f, 0.32f, 0.18f),
		Border = new Color(0.28f, 0.48f, 0.28f),
		BorderGlow = new Color(0.40f, 0.70f, 0.38f),
		ShoreGround = "ground_forest_dirt",
		InlandGround = "ground_wild",
		PatchGround = "ground_forest_dirt",
		BasicEnemy = "enemy_beast",
		EliteEnemy = "enemy_direbeast",
		BasicFallback = new Color(0.70f, 0.42f, 0.18f),
		EliteFallback = new Color(0.72f, 0.22f, 0.12f),
		DecorSeed = 0xB11D50BE,
		TreeKinds = new[] { "tree_wild", "tree_wild_pine", "tree_round", "tree_pine" },
		BushKinds = new[] { "bush_wild", "bush" },
		RockKinds = new[] { "rock", "stump" },
		GrassKinds = new[] { "grass_tuft", "grass_tuft_b" },
		FlowerKinds = new[] { "flower_yellow", "mushroom" },
		ScatterExtra = new[] { "mushroom", "stump" },
		BlockKinds = new[] { "tree_wild", "tree_wild_pine", "stump", "rock" },
		// 密林：树篱围出林间小径，中央仍可周旋
		Barriers = new[]
		{
			B(480, 480, BarrierShape.Arc, 180, 150, 8, 0.85f, 1.15f, "tree_wild", "tree_wild_pine"),
			B(1880, 480, BarrierShape.Arc, 180, 150, 8, 0.85f, 1.15f, "tree_wild", "tree_pine"),
			B(480, 1180, BarrierShape.Arc, 180, 140, 7, 0.85f, 1.15f, "tree_wild_pine", "tree_round"),
			B(1880, 1180, BarrierShape.Arc, 180, 140, 7, 0.85f, 1.15f, "tree_wild", "tree_round"),
			B(1040, 380, BarrierShape.LineH, 240, 50, 6, 0.9f, 1.2f, "tree_wild", "stump"),
			B(760, 900, BarrierShape.Clump, 120, 110, 6, 1.1f, 1.45f, "rock", "stump"),
			B(1480, 900, BarrierShape.Clump, 120, 110, 6, 1.1f, 1.45f, "rock", "stump"),
			B(1200, 1240, BarrierShape.LineH, 200, 45, 5, 0.9f, 1.15f, "tree_wild_pine", "stump"),
		},
		GroundPatches = new[]
		{
			new Rect2(400, 420, 180, 150),
			new Rect2(1800, 420, 180, 150),
			new Rect2(400, 1120, 180, 140),
			new Rect2(1800, 1120, 180, 140),
			new Rect2(940, 340, 260, 80),
			new Rect2(700, 850, 140, 120),
			new Rect2(1420, 850, 140, 120),
			new Rect2(1120, 1200, 220, 70),
		},
		TreeCount = 40,
		BushCount = 42,
		GrassCount = 180,
		FlowerCount = 24,
		ExtraCount = 20,
		ScatterRockCount = 6,
		InlandInset = 22f,
		ClearRadius = 190f,
	};

	public static readonly MapTheme Desert = new()
	{
		Id = MapId.Desert,
		Ocean = new Color(0.55f, 0.42f, 0.22f), // 沙海 / 干涸边
		Shallow = new Color(0.72f, 0.58f, 0.32f),
		ShoreFallback = new Color(0.88f, 0.74f, 0.42f),
		InlandFallback = new Color(0.82f, 0.68f, 0.38f),
		Border = new Color(0.78f, 0.55f, 0.28f),
		BorderGlow = new Color(0.95f, 0.78f, 0.40f),
		ShoreGround = "ground_dune",
		InlandGround = "ground_dune",
		PatchGround = "ground_cracked",
		BasicEnemy = "enemy_sandfiend",
		EliteEnemy = "enemy_scarab",
		BasicFallback = new Color(0.90f, 0.70f, 0.28f),
		EliteFallback = new Color(0.55f, 0.38f, 0.14f),
		DecorSeed = 0xDE5E47A1,
		TreeKinds = new[] { "cactus" },
		BushKinds = new[] { "bush_dry" },
		RockKinds = new[] { "rock_sand", "bone_pile" },
		GrassKinds = new[] { "bush_dry" },
		FlowerKinds = System.Array.Empty<string>(),
		ScatterExtra = new[] { "cactus_small", "bone_pile", "rock_sand" },
		BlockKinds = new[] { "rock_sand", "cactus", "bone_pile" },
		// 沙漠：沙岩脊与仙人掌丛点缀干裂地，留出宽阔绕行空间
		Barriers = new[]
		{
			B(620, 420, BarrierShape.LineH, 180, 55, 5, 1.3f, 1.7f, "rock_sand"),
			B(1680, 420, BarrierShape.LineH, 180, 55, 5, 1.3f, 1.7f, "rock_sand"),
			B(1040, 720, BarrierShape.Clump, 130, 120, 7, 1.25f, 1.65f, "rock_sand", "bone_pile"),
			B(480, 1000, BarrierShape.Arc, 150, 130, 6, 1.05f, 1.4f, "cactus", "cactus_small"),
			B(1860, 1000, BarrierShape.Arc, 150, 130, 6, 1.05f, 1.4f, "cactus", "rock_sand"),
			B(1280, 1180, BarrierShape.LineV, 50, 160, 5, 1.2f, 1.55f, "rock_sand", "bone_pile"),
			B(760, 1280, BarrierShape.Clump, 120, 90, 5, 1.15f, 1.5f, "bone_pile", "rock_sand"),
			B(1520, 280, BarrierShape.LShape, 130, 120, 6, 1.1f, 1.45f, "cactus", "rock_sand"),
		},
		GroundPatches = new[]
		{
			new Rect2(560, 380, 200, 80),
			new Rect2(1620, 380, 200, 80),
			new Rect2(980, 670, 150, 140),
			new Rect2(420, 950, 160, 140),
			new Rect2(1800, 950, 160, 140),
			new Rect2(1240, 1100, 90, 180),
			new Rect2(700, 1240, 140, 100),
			new Rect2(1460, 240, 150, 140),
		},
		TreeCount = 10,
		BushCount = 22,
		GrassCount = 36,
		FlowerCount = 0,
		ExtraCount = 20,
		ScatterRockCount = 8,
		InlandInset = 12f,
		ClearRadius = 175f,
	};

	private static BarrierSpec B(
		float x, float y, BarrierShape shape,
		float spanX, float spanY, int count,
		float scaleMin, float scaleMax,
		params string[] kinds)
	{
		return new BarrierSpec
		{
			Center = new Vector2(x, y),
			Shape = shape,
			SpanX = spanX,
			SpanY = spanY,
			Count = count,
			ScaleMin = scaleMin,
			ScaleMax = scaleMax,
			Kinds = kinds is { Length: > 0 } ? kinds : null,
		};
	}
}
