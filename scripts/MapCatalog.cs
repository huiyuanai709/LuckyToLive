using Godot;

public enum MapId
{
	Island = 0,
	Apocalypse = 1,
	Wilderness = 2,
	Desert = 3,
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
	public int TreeCount;
	public int BushCount;
	public int GrassCount;
	public int FlowerCount;
	public int ExtraCount;
	public float InlandInset; // 内陆地相对岸线的内缩
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
		TreeCount = 48,
		BushCount = 36,
		GrassCount = 160,
		FlowerCount = 40,
		ExtraCount = 18,
		InlandInset = 28f,
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
		TreeCount = 28,
		BushCount = 22,
		GrassCount = 40,
		FlowerCount = 0,
		ExtraCount = 24,
		InlandInset = 18f,
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
		TreeCount = 72,
		BushCount = 55,
		GrassCount = 200,
		FlowerCount = 28,
		ExtraCount = 30,
		InlandInset = 22f,
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
		TreeCount = 22,
		BushCount = 30,
		GrassCount = 50,
		FlowerCount = 0,
		ExtraCount = 36,
		InlandInset = 12f,
	};
}
