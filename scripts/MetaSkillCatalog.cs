using System.Collections.Generic;
using System.Linq;

/// <summary>局外升级解锁的主动技能定义（局内按键释放，带冷却）。</summary>
public class MetaSkillDef
{
	public string Id;
	public HeroId Hero;
	/// <summary>需要的局外角色等级（含）。</summary>
	public int UnlockLevel;
	/// <summary>冷却秒数。</summary>
	public float Cooldown;
	/// <summary>技能槽位：0=主技能(Q)，1=副技能(E)。</summary>
	public int Slot;
}

public static class MetaSkillCatalog
{
	public const int MaxMetaLevel = 10;
	/// <summary>每级局外升级提供的初始攻击倍率增量。</summary>
	public const float AttackBonusPerLevel = 0.10f;

	private static readonly List<MetaSkillDef> All = new()
	{
		new MetaSkillDef { Id = "sk_war_cry", Hero = HeroId.Warrior, UnlockLevel = 3, Cooldown = 12f, Slot = 0 },
		new MetaSkillDef { Id = "sk_iron_guard", Hero = HeroId.Warrior, UnlockLevel = 6, Cooldown = 20f, Slot = 1 },
		new MetaSkillDef { Id = "sk_frost_nova", Hero = HeroId.Mage, UnlockLevel = 3, Cooldown = 13f, Slot = 0 },
		new MetaSkillDef { Id = "sk_arc_blink", Hero = HeroId.Mage, UnlockLevel = 6, Cooldown = 14f, Slot = 1 },
		new MetaSkillDef { Id = "sk_volley", Hero = HeroId.Hunter, UnlockLevel = 3, Cooldown = 12f, Slot = 0 },
		new MetaSkillDef { Id = "sk_snare", Hero = HeroId.Hunter, UnlockLevel = 6, Cooldown = 16f, Slot = 1 },
	};

	public static MetaSkillDef Get(string id) =>
		All.FirstOrDefault(s => s.Id == id);

	public static IEnumerable<MetaSkillDef> ForHero(HeroId hero) =>
		All.Where(s => s.Hero == hero).OrderBy(s => s.Slot);

	public static IEnumerable<MetaSkillDef> UnlockedFor(HeroId hero, int metaLevel) =>
		ForHero(hero).Where(s => metaLevel >= s.UnlockLevel);

	public static MetaSkillDef GetSlot(HeroId hero, int slot) =>
		All.FirstOrDefault(s => s.Hero == hero && s.Slot == slot);

	public static float AttackMulForLevel(int metaLevel) =>
		1f + (System.Math.Max(1, metaLevel) - 1) * AttackBonusPerLevel;

	public static int UpgradeCost(int currentLevel)
	{
		if (currentLevel >= MaxMetaLevel) return 0;
		return 2 + (currentLevel - 1); // Lv1→2:2, Lv2→3:3, …
	}

	/// <summary>下一技能解锁等级；已满则返回 0。</summary>
	public static int NextSkillUnlockLevel(HeroId hero, int metaLevel)
	{
		foreach (var s in ForHero(hero))
		{
			if (metaLevel < s.UnlockLevel)
				return s.UnlockLevel;
		}
		return 0;
	}
}
