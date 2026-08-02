using Godot;
using System.Collections.Generic;

public enum HeroId
{
	Warrior = 0,
	Mage = 1,
	Hunter = 2,
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

	// 本局
	public int AdSlotsUnlocked;
	public int SlotCapacity => BaseSlots + AdSlotsUnlocked;
	public int KillCount;
	public int EliteKills;
	public int MinuteGoalsCompleted;
	public float HighestItemLevelSum;
	public bool RunActive;

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
		Save();
		return true;
	}

	public void ChooseStarter(HeroId id)
	{
		StarterHero = id;
		UnlockedHeroes.Clear();
		UnlockedHeroes.Add(id);
		SelectedHero = id;
		Save();
	}

	public void ResetRunStats()
	{
		AdSlotsUnlocked = 0;
		KillCount = 0;
		EliteKills = 0;
		MinuteGoalsCompleted = 0;
		HighestItemLevelSum = 0;
		RunActive = true;
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
		if (StarterHero != null) cfg.SetValue("meta", "starter", (int)StarterHero.Value);
		cfg.SetValue("meta", "currency", MetaCurrency);
		var arr = new Godot.Collections.Array();
		foreach (var h in UnlockedHeroes) arr.Add((int)h);
		cfg.SetValue("meta", "unlocked", arr);
		cfg.Save(SavePath);
	}

	public void Load()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(SavePath) != Error.Ok) return;
		if (cfg.HasSectionKey("meta", "starter"))
			StarterHero = (HeroId)(int)cfg.GetValue("meta", "starter");
		MetaCurrency = (int)cfg.GetValue("meta", "currency", 0);
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
		if (StarterHero != null) SelectedHero = StarterHero.Value;
	}
}
