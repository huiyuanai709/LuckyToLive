using Godot;
using System.Collections.Generic;

public enum HeroId
{
	Warrior = 0,
	Mage = 1,
	Hunter = 2,
}

public enum DifficultyId
{
	Normal = 0,
	Hard = 1,
	Nightmare = 2,
}

/// <summary>全局元进度与本局共享状态。</summary>
public partial class Game : Node
{
	public static Game Instance { get; private set; }

	public const int BaseSlots = 5;
	public const int MaxAdSlots = 2;
	public const float RunDuration = 300.0f;

	public HeroId? StarterHero;
	public HashSet<HeroId> UnlockedHeroes = new();
	public int MetaCurrency;
	public HeroId SelectedHero = HeroId.Hunter;
	public MapId SelectedMap = MapId.Island;
	public DifficultyId SelectedDifficulty = DifficultyId.Normal;
	/// <summary>本局角色名（创建角色时输入，暗黑式开局）。</summary>
	public string CharacterName = "";

	/// <summary>各英雄局外等级（从 1 起）。</summary>
	public Dictionary<HeroId, int> HeroMetaLevels = new();

	// 本局
	public int AdSlotsUnlocked;
	public int SlotCapacity => BaseSlots + AdSlotsUnlocked;
	public int KillCount;
	public int EliteKills;
	public int MinuteGoalsCompleted;
	public float HighestItemLevelSum;
	public bool RunActive;
	/// <summary>每局免费重随次数。</summary>
	public int RerollsLeft;
	public bool TideGuardKilled;
	public bool IslandLordKilled;
	public int SynergiesCompleted;

	private const string SavePath = "user://save.cfg";

	public override void _Ready()
	{
		Instance = this;
		Load();
	}

	public int AvailableSlotsThisRun => SlotCapacity;

	public bool IsHeroUnlocked(HeroId id)
	{
		if (StarterHero == null) return true;
		return UnlockedHeroes.Contains(id);
	}

	public int UnlockCost(HeroId id)
	{
		if (StarterHero == null) return 0;
		if (UnlockedHeroes.Contains(id)) return 0;
		int unlockedCount = UnlockedHeroes.Count;
		return unlockedCount <= 1 ? 3 : 6;
	}

	public bool TryUnlockHero(HeroId id)
	{
		if (IsHeroUnlocked(id)) return true;
		int cost = UnlockCost(id);
		if (MetaCurrency < cost) return false;
		MetaCurrency -= cost;
		UnlockedHeroes.Add(id);
		EnsureHeroLevel(id);
		Save();
		return true;
	}

	public void ChooseStarter(HeroId id)
	{
		StarterHero = id;
		UnlockedHeroes.Clear();
		UnlockedHeroes.Add(id);
		SelectedHero = id;
		EnsureHeroLevel(id);
		Save();
	}

	public void SetCharacterName(string name)
	{
		CharacterName = (name ?? "").Trim();
		if (CharacterName.Length > 16)
			CharacterName = CharacterName.Substring(0, 16);
		Save();
	}

	public int GetHeroMetaLevel(HeroId id)
	{
		EnsureHeroLevel(id);
		return HeroMetaLevels[id];
	}

	public float GetMetaAttackMul(HeroId id) =>
		MetaSkillCatalog.AttackMulForLevel(GetHeroMetaLevel(id));

	public int MetaUpgradeCost(HeroId id) =>
		MetaSkillCatalog.UpgradeCost(GetHeroMetaLevel(id));

	public bool CanUpgradeHero(HeroId id)
	{
		if (StarterHero != null && !IsHeroUnlocked(id)) return false;
		int lv = GetHeroMetaLevel(id);
		if (lv >= MetaSkillCatalog.MaxMetaLevel) return false;
		return MetaCurrency >= MetaUpgradeCost(id);
	}

	public bool TryUpgradeHero(HeroId id)
	{
		if (StarterHero != null && !IsHeroUnlocked(id)) return false;
		int lv = GetHeroMetaLevel(id);
		if (lv >= MetaSkillCatalog.MaxMetaLevel) return false;
		int cost = MetaUpgradeCost(id);
		if (MetaCurrency < cost) return false;
		MetaCurrency -= cost;
		HeroMetaLevels[id] = lv + 1;
		Save();
		return true;
	}

	private void EnsureHeroLevel(HeroId id)
	{
		if (!HeroMetaLevels.ContainsKey(id))
			HeroMetaLevels[id] = 1;
	}

	public float DiffSpawnMul => SelectedDifficulty switch
	{
		DifficultyId.Hard => 1.40f,
		DifficultyId.Nightmare => 1.85f,
		_ => 1f,
	};

	public float DiffHpMul => SelectedDifficulty switch
	{
		DifficultyId.Hard => 1.45f,
		DifficultyId.Nightmare => 2.0f,
		_ => 1f,
	};

	public float DiffAtkMul => SelectedDifficulty switch
	{
		DifficultyId.Hard => 1.30f,
		DifficultyId.Nightmare => 1.65f,
		_ => 1f,
	};

	/// <summary>结算货币倍率（困难/噩梦略增）。</summary>
	public float DiffRewardMul => SelectedDifficulty switch
	{
		DifficultyId.Hard => 1.5f,
		DifficultyId.Nightmare => 2.0f,
		_ => 1f,
	};

	public void ResetRunStats()
	{
		AdSlotsUnlocked = 0;
		KillCount = 0;
		EliteKills = 0;
		MinuteGoalsCompleted = 0;
		HighestItemLevelSum = 0;
		RerollsLeft = 1;
		TideGuardKilled = false;
		IslandLordKilled = false;
		SynergiesCompleted = 0;
		RunActive = true;
	}

	public bool TryConsumeReroll()
	{
		if (RerollsLeft <= 0) return false;
		RerollsLeft -= 1;
		return true;
	}

	public bool TryUnlockAdSlot()
	{
		if (AdSlotsUnlocked >= MaxAdSlots) return false;
		AdSlotsUnlocked += 1;
		return true;
	}

	public void AddMetaFromScore(int amount)
	{
		MetaCurrency += Mathf.Max(0, amount);
		Save();
	}

	public void Save()
	{
		var cfg = new ConfigFile();
		cfg.Load(SavePath); // 保留 settings 等其它分区（如语言）
		if (StarterHero != null) cfg.SetValue("meta", "starter", (int)StarterHero.Value);
		cfg.SetValue("meta", "currency", MetaCurrency);
		if (!string.IsNullOrEmpty(CharacterName))
			cfg.SetValue("meta", "character_name", CharacterName);
		cfg.SetValue("meta", "difficulty", (int)SelectedDifficulty);
		var arr = new Godot.Collections.Array();
		foreach (var h in UnlockedHeroes) arr.Add((int)h);
		cfg.SetValue("meta", "unlocked", arr);

		var levels = new Godot.Collections.Dictionary();
		foreach (var kv in HeroMetaLevels)
			levels[((int)kv.Key).ToString()] = kv.Value;
		cfg.SetValue("meta", "hero_levels", levels);
		cfg.Save(SavePath);
	}

	public void Load()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(SavePath) != Error.Ok) return;
		if (cfg.HasSectionKey("meta", "starter"))
			StarterHero = (HeroId)(int)cfg.GetValue("meta", "starter");
		MetaCurrency = (int)cfg.GetValue("meta", "currency", 0);
		CharacterName = (string)cfg.GetValue("meta", "character_name", "");
		SelectedDifficulty = (DifficultyId)(int)cfg.GetValue("meta", "difficulty", (int)DifficultyId.Normal);
		UnlockedHeroes.Clear();
		if (cfg.HasSectionKey("meta", "unlocked"))
		{
			var arr = (Godot.Collections.Array)cfg.GetValue("meta", "unlocked");
			foreach (Variant v in arr) UnlockedHeroes.Add((HeroId)(int)v);
		}
		else if (StarterHero != null)
		{
			UnlockedHeroes.Add(StarterHero.Value);
		}

		HeroMetaLevels.Clear();
		if (cfg.HasSectionKey("meta", "hero_levels"))
		{
			var levels = (Godot.Collections.Dictionary)cfg.GetValue("meta", "hero_levels");
			foreach (Variant key in levels.Keys)
			{
				if (int.TryParse(key.AsString(), out int id))
					HeroMetaLevels[(HeroId)id] = Mathf.Clamp((int)levels[key], 1, MetaSkillCatalog.MaxMetaLevel);
			}
		}
		foreach (HeroId id in System.Enum.GetValues(typeof(HeroId)))
			EnsureHeroLevel(id);

		if (StarterHero != null) SelectedHero = StarterHero.Value;
	}
}
