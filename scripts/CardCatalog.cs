using System.Collections.Generic;
using System.Linq;

public enum CardKind
{
	Weapon,
	Building,
	Pet,
	Passive,
	Upgrade,
}

public class CardDef
{
	public string Id;
	public string Name;
	public string Desc;
	public CardKind Kind;
	public HeroId? Hero;          // null = 公共
	public string GrantsItemId;   // 新品写入槽位的 id；升级则为目标 item id
	public bool IsNewItem;
	public string WeaponStyle;    // slash/pierce/ice_arrow/fireball/beam/charge
	public string BuildingStyle;  // turret_phys/turret_fire/slow_field/heal_totem/shield_wall/trap
	public string PetStyle;       // wolf
	public string UpgradeStat;    // damage/rate/range/special/hp/speed/regen
	public string ProjectileTexture;   // 覆盖弹道贴图：res://assets/projectiles/{name}.png 的 name
	public float[] BeamAnglesDeg;      // 覆盖射线角度（相对朝向，度）；null = 用默认规则
	public int BeamRaysAdd;            // 升级时额外增加的射线数（走默认角度规则）
	public float BeamLengthAdd;        // 升级时额外增加的射线长度
}

public class SlotItem
{
	public string ItemId;
	public string Name;
	public CardKind Kind;
	public int Level = 1;
	public string WeaponStyle;
	public string BuildingStyle;
	public string PetStyle;
	public float Damage = 10f;
	public float FireRate = 1f;
	public float Range = 140f;
	public int Pierce = 1;
	public float SlowFactor = 1f;
	public float Splash = 0f;
	public Building BuildingRef;
	public Pet PetRef;
	public string UpgradeStatHolder;

	// —— 弹道 / 射线表现与机制 ——
	/// <summary>覆盖贴图名；空则按 WeaponStyle / BuildingStyle 取默认。</summary>
	public string ProjectileTexture;
	/// <summary>射线激活持续时间（秒）。</summary>
	public float BeamDuration = 2.5f;
	/// <summary>射线冷却时间（秒），从持续结束开始计。</summary>
	public float BeamCooldown = 3.0f;
	/// <summary>射线条数（默认角度规则按此数量排布）。</summary>
	public int BeamRays = 1;
	/// <summary>覆盖角度列表（相对朝向，度）；非空时忽略默认规则。</summary>
	public float[] BeamAnglesDeg;

	/// <summary>单件武器射线条数上限；满后不再抽出增加射线的升级卡。</summary>
	public const int MaxBeamRays = 3;

	/// <summary>实际生效的射线条数（有覆盖角度时以角度表为准）。</summary>
	public int EffectiveBeamRays =>
		BeamAnglesDeg != null && BeamAnglesDeg.Length > 0
			? BeamAnglesDeg.Length
			: System.Math.Max(1, BeamRays);
}

public static class CardCatalog
{
	private static readonly List<CardDef> All = new()
	{
		// —— 战士 ——
		new CardDef { Id = "w_slash", Name = "裂斩", Desc = "近战挥击，伤害附近敌人", Kind = CardKind.Weapon, Hero = HeroId.Warrior, GrantsItemId = "w_slash", IsNewItem = true, WeaponStyle = "slash" },
		new CardDef { Id = "w_charge", Name = "冲锋刃", Desc = "周期性向前斩击更远", Kind = CardKind.Weapon, Hero = HeroId.Warrior, GrantsItemId = "w_charge", IsNewItem = true, WeaponStyle = "charge" },
		new CardDef { Id = "w_shield_wall", Name = "盾墙", Desc = "阻挡并反伤靠近的敌人", Kind = CardKind.Building, Hero = HeroId.Warrior, GrantsItemId = "w_shield_wall", IsNewItem = true, BuildingStyle = "shield_wall" },
		new CardDef { Id = "w_heal_totem", Name = "战旗", Desc = "范围内缓慢回血", Kind = CardKind.Building, Hero = HeroId.Warrior, GrantsItemId = "w_heal_totem", IsNewItem = true, BuildingStyle = "heal_totem" },
		new CardDef { Id = "w_turret", Name = "矛塔", Desc = "物理炮塔自动射击", Kind = CardKind.Building, Hero = HeroId.Warrior, GrantsItemId = "w_turret", IsNewItem = true, BuildingStyle = "turret_phys" },
		new CardDef { Id = "up_w_slash_dmg", Name = "裂斩·锋刃", Desc = "裂斩伤害提升", Kind = CardKind.Upgrade, Hero = HeroId.Warrior, GrantsItemId = "w_slash", IsNewItem = false, UpgradeStat = "damage" },
		new CardDef { Id = "up_w_slash_rate", Name = "裂斩·连斩", Desc = "裂斩攻速提升", Kind = CardKind.Upgrade, Hero = HeroId.Warrior, GrantsItemId = "w_slash", IsNewItem = false, UpgradeStat = "rate" },
		new CardDef { Id = "up_w_charge", Name = "冲锋·破阵", Desc = "冲锋刃伤害与距离提升", Kind = CardKind.Upgrade, Hero = HeroId.Warrior, GrantsItemId = "w_charge", IsNewItem = false, UpgradeStat = "special" },
		new CardDef { Id = "up_w_wall", Name = "盾墙·加固", Desc = "盾墙生命与反伤提升", Kind = CardKind.Upgrade, Hero = HeroId.Warrior, GrantsItemId = "w_shield_wall", IsNewItem = false, UpgradeStat = "special" },
		new CardDef { Id = "up_w_totem", Name = "战旗·鼓舞", Desc = "战旗回血加强", Kind = CardKind.Upgrade, Hero = HeroId.Warrior, GrantsItemId = "w_heal_totem", IsNewItem = false, UpgradeStat = "special" },
		new CardDef { Id = "up_w_turret", Name = "矛塔·强化", Desc = "矛塔伤害提升", Kind = CardKind.Upgrade, Hero = HeroId.Warrior, GrantsItemId = "w_turret", IsNewItem = false, UpgradeStat = "damage" },

		// —— 法师 ——
		new CardDef { Id = "m_ice", Name = "冰矢", Desc = "冰系射击并减速", Kind = CardKind.Weapon, Hero = HeroId.Mage, GrantsItemId = "m_ice", IsNewItem = true, WeaponStyle = "ice_arrow" },
		new CardDef { Id = "m_fire", Name = "火球", Desc = "火系爆炸伤害", Kind = CardKind.Weapon, Hero = HeroId.Mage, GrantsItemId = "m_fire", IsNewItem = true, WeaponStyle = "fireball" },
		new CardDef { Id = "m_beam", Name = "元素射线", Desc = "伸出固定长度射线，随移动方向扫射；有持续与冷却", Kind = CardKind.Weapon, Hero = HeroId.Mage, GrantsItemId = "m_beam", IsNewItem = true, WeaponStyle = "beam" },
		new CardDef { Id = "m_ice_field", Name = "寒冰阵", Desc = "减速场建筑", Kind = CardKind.Building, Hero = HeroId.Mage, GrantsItemId = "m_ice_field", IsNewItem = true, BuildingStyle = "slow_field" },
		new CardDef { Id = "m_fire_turret", Name = "火法塔", Desc = "火焰炮塔", Kind = CardKind.Building, Hero = HeroId.Mage, GrantsItemId = "m_fire_turret", IsNewItem = true, BuildingStyle = "turret_fire" },
		new CardDef { Id = "up_m_ice", Name = "冰矢·深寒", Desc = "冰矢伤害与减速提升", Kind = CardKind.Upgrade, Hero = HeroId.Mage, GrantsItemId = "m_ice", IsNewItem = false, UpgradeStat = "special" },
		new CardDef { Id = "up_m_fire", Name = "火球·爆炎", Desc = "火球伤害与溅射提升", Kind = CardKind.Upgrade, Hero = HeroId.Mage, GrantsItemId = "m_fire", IsNewItem = false, UpgradeStat = "special" },
		new CardDef { Id = "up_m_beam", Name = "射线·聚焦", Desc = "射线伤害与射程提升", Kind = CardKind.Upgrade, Hero = HeroId.Mage, GrantsItemId = "m_beam", IsNewItem = false, UpgradeStat = "beam_focus" },
		new CardDef { Id = "up_m_beam_cross", Name = "射线·十字", Desc = "左右各增一条射线，形成十字", Kind = CardKind.Upgrade, Hero = HeroId.Mage, GrantsItemId = "m_beam", IsNewItem = false, UpgradeStat = "beam_rays", BeamAnglesDeg = new[] { 0f, 90f, -90f } },
		new CardDef { Id = "up_m_field", Name = "寒冰阵·扩大", Desc = "减速场范围提升", Kind = CardKind.Upgrade, Hero = HeroId.Mage, GrantsItemId = "m_ice_field", IsNewItem = false, UpgradeStat = "range" },
		new CardDef { Id = "up_m_fturret", Name = "火法塔·烈焰", Desc = "火法塔伤害提升", Kind = CardKind.Upgrade, Hero = HeroId.Mage, GrantsItemId = "m_fire_turret", IsNewItem = false, UpgradeStat = "damage" },

		// —— 猎人 ——
		new CardDef { Id = "h_pierce", Name = "穿透箭", Desc = "穿透多名敌人", Kind = CardKind.Weapon, Hero = HeroId.Hunter, GrantsItemId = "h_pierce", IsNewItem = true, WeaponStyle = "pierce" },
		new CardDef { Id = "h_frost", Name = "冰箭", Desc = "箭矢附带减速", Kind = CardKind.Weapon, Hero = HeroId.Hunter, GrantsItemId = "h_frost", IsNewItem = true, WeaponStyle = "ice_arrow" },
		new CardDef { Id = "h_pet", Name = "召唤狼宠", Desc = "宠物占一槽，自动撕咬", Kind = CardKind.Pet, Hero = HeroId.Hunter, GrantsItemId = "h_pet", IsNewItem = true, PetStyle = "wolf" },
		new CardDef { Id = "h_trap", Name = "捕兽夹", Desc = "陷阱建筑，触发伤害并减速", Kind = CardKind.Building, Hero = HeroId.Hunter, GrantsItemId = "h_trap", IsNewItem = true, BuildingStyle = "trap" },
		new CardDef { Id = "h_camp", Name = "营地哨塔", Desc = "猎人炮塔", Kind = CardKind.Building, Hero = HeroId.Hunter, GrantsItemId = "h_camp", IsNewItem = true, BuildingStyle = "turret_phys" },
		new CardDef { Id = "up_h_pierce", Name = "穿透·连射", Desc = "穿透数与伤害提升", Kind = CardKind.Upgrade, Hero = HeroId.Hunter, GrantsItemId = "h_pierce", IsNewItem = false, UpgradeStat = "special" },
		new CardDef { Id = "up_h_frost", Name = "冰箭·霜冻", Desc = "冰箭伤害与减速提升", Kind = CardKind.Upgrade, Hero = HeroId.Hunter, GrantsItemId = "h_frost", IsNewItem = false, UpgradeStat = "special" },
		new CardDef { Id = "up_h_pet", Name = "狼宠·野性", Desc = "宠物伤害与攻速提升", Kind = CardKind.Upgrade, Hero = HeroId.Hunter, GrantsItemId = "h_pet", IsNewItem = false, UpgradeStat = "special" },
		new CardDef { Id = "up_h_trap", Name = "捕兽夹·锋利", Desc = "陷阱伤害提升", Kind = CardKind.Upgrade, Hero = HeroId.Hunter, GrantsItemId = "h_trap", IsNewItem = false, UpgradeStat = "damage" },
		new CardDef { Id = "up_h_camp", Name = "哨塔·校准", Desc = "哨塔射程与伤害提升", Kind = CardKind.Upgrade, Hero = HeroId.Hunter, GrantsItemId = "h_camp", IsNewItem = false, UpgradeStat = "special" },

		// —— 公共 ——
		new CardDef { Id = "p_hp", Name = "强体", Desc = "最大生命 +25", Kind = CardKind.Passive, Hero = null, GrantsItemId = "p_hp", IsNewItem = true, UpgradeStat = "hp" },
		new CardDef { Id = "p_speed", Name = "疾步", Desc = "移动速度提升", Kind = CardKind.Passive, Hero = null, GrantsItemId = "p_speed", IsNewItem = true, UpgradeStat = "speed" },
		new CardDef { Id = "p_regen", Name = "再生", Desc = "缓慢回复生命", Kind = CardKind.Passive, Hero = null, GrantsItemId = "p_regen", IsNewItem = true, UpgradeStat = "regen" },
		new CardDef { Id = "up_p_hp", Name = "强体·再锻", Desc = "再 +20 最大生命", Kind = CardKind.Upgrade, Hero = null, GrantsItemId = "p_hp", IsNewItem = false, UpgradeStat = "hp" },
	};

	public static CardDef Get(string id) => All.FirstOrDefault(c => c.Id == id);

	public static string StarterCardId(HeroId hero) => hero switch
	{
		HeroId.Warrior => "w_slash",
		HeroId.Mage => "m_ice",
		HeroId.Hunter => "h_pierce",
		_ => "h_pierce",
	};

	public static List<CardDef> RollOptions(HeroId hero, Loadout loadout, int count, RandomNumberGeneratorRng rng)
	{
		bool full = loadout.IsFull;
		var pool = All.Where(c =>
		{
			if (c.Hero != null && c.Hero != hero) return false;
			if (!IsUpgradeStillUseful(c, loadout)) return false;
			if (full)
			{
				if (c.IsNewItem) return false;
				return loadout.HasItem(c.GrantsItemId);
			}
			if (!c.IsNewItem) return loadout.HasItem(c.GrantsItemId);
			if (c.Kind == CardKind.Passive) return !loadout.HasItem(c.GrantsItemId);
			return !loadout.HasItem(c.GrantsItemId);
		}).ToList();

		if (pool.Count == 0)
		{
			pool = All.Where(c =>
				!c.IsNewItem && c.Hero == hero && loadout.HasItem(c.GrantsItemId)
				&& IsUpgradeStillUseful(c, loadout)).ToList();
		}

		rng.Shuffle(pool);
		return pool.Take(System.Math.Min(count, pool.Count)).ToList();
	}

	/// <summary>
	/// 已达上限的升级不再进入抽取池（例如射线条数已满时隐藏 beam_rays）。
	/// 后续新增「再增加射线」类卡也会走同一判定。
	/// </summary>
	public static bool IsUpgradeStillUseful(CardDef card, Loadout loadout)
	{
		if (card == null || card.IsNewItem) return true;
		if (card.UpgradeStat != "beam_rays") return true;

		var item = loadout.GetItem(card.GrantsItemId);
		if (item == null) return true;
		return item.EffectiveBeamRays < SlotItem.MaxBeamRays;
	}
}

/// <summary>薄封装，避免与 Godot RNG 命名冲突。</summary>
public class RandomNumberGeneratorRng
{
	private readonly Godot.RandomNumberGenerator _rng = new();
	public RandomNumberGeneratorRng() => _rng.Randomize();
	public float Randf() => _rng.Randf();
	public int RandiRange(int min, int max) => _rng.RandiRange(min, max);
	public void Shuffle<T>(List<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = RandiRange(0, i);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}
