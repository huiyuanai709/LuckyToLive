using System.Collections.Generic;
using System.Linq;

public sealed class SynergyDef
{
	public string Id;
	public string EvolveCardId;
	public HeroId? Hero;
	public string[] Requires;
	public string PrimaryItemId;
	public string EvolveStat;
	public string Name;
	public string Desc;
}

/// <summary>协同进化配方与进化卡生成。</summary>
public static class SynergyCatalog
{
	public static readonly List<SynergyDef> All = new()
	{
		new SynergyDef
		{
			Id = "syn_w_cleave", EvolveCardId = "evo_w_cleave", Hero = HeroId.Warrior,
			Requires = new[] { "w_slash", "w_charge" }, PrimaryItemId = "w_slash",
			EvolveStat = "evolve:syn_w_cleave",
			Name = "裂阵斩", Desc = "需要：裂斩 + 冲锋刃。裂斩大弧 AOE，伤害与范围提升",
		},
		new SynergyDef
		{
			Id = "syn_w_bastion", EvolveCardId = "evo_w_bastion", Hero = HeroId.Warrior,
			Requires = new[] { "w_shield_wall", "w_heal_totem" }, PrimaryItemId = "w_shield_wall",
			EvolveStat = "evolve:syn_w_bastion",
			Name = "堡垒之誓", Desc = "需要：盾墙 + 战旗。盾墙反伤提升；战旗赋予伤害光环",
		},
		new SynergyDef
		{
			Id = "syn_m_frostfire", EvolveCardId = "evo_m_frostfire", Hero = HeroId.Mage,
			Requires = new[] { "m_ice", "m_fire" }, PrimaryItemId = "m_ice",
			EvolveStat = "evolve:syn_m_frostfire",
			Name = "霜火陨星", Desc = "需要：冰矢 + 火球。冰火球：减速并大范围爆炸",
		},
		new SynergyDef
		{
			Id = "syn_m_prism", EvolveCardId = "evo_m_prism", Hero = HeroId.Mage,
			Requires = new[] { "m_beam", "m_ice_field" }, PrimaryItemId = "m_beam",
			EvolveStat = "evolve:syn_m_prism",
			Name = "棱镜射线", Desc = "需要：元素射线 + 寒冰阵。射线上限+1，命中附带减速",
		},
		new SynergyDef
		{
			Id = "syn_h_pack", EvolveCardId = "evo_h_pack", Hero = HeroId.Hunter,
			Requires = new[] { "h_pet", "h_trap" }, PrimaryItemId = "h_pet",
			EvolveStat = "evolve:syn_h_pack",
			Name = "围猎号令", Desc = "需要：狼宠 + 捕兽夹。狼宠伤害提升；陷阱触发时狼扑咬",
		},
		new SynergyDef
		{
			Id = "syn_h_storm", EvolveCardId = "evo_h_storm", Hero = HeroId.Hunter,
			Requires = new[] { "h_pierce", "h_frost" }, PrimaryItemId = "h_pierce",
			EvolveStat = "evolve:syn_h_storm",
			Name = "霜暴连矢", Desc = "需要：穿透箭 + 冰箭。穿透+1，并分裂减速副箭",
		},
		new SynergyDef
		{
			Id = "syn_p_bloodrush", EvolveCardId = "evo_p_bloodrush", Hero = null,
			Requires = new[] { "p_vamp", "p_speed" }, PrimaryItemId = "p_vamp",
			EvolveStat = "evolve:syn_p_bloodrush",
			Name = "血疾", Desc = "需要：嗜血 + 疾步。移速提升，击杀回血，连杀更容易触发",
		},
	};

	public static SynergyDef Get(string id) => All.FirstOrDefault(s => s.Id == id);

	public static SynergyDef GetByEvolveCard(string cardId) =>
		All.FirstOrDefault(s => s.EvolveCardId == cardId);

	public static bool IsReady(SynergyDef syn, Loadout loadout)
	{
		if (syn == null || loadout == null) return false;
		if (loadout.CompletedSynergies.Contains(syn.Id)) return false;
		foreach (string req in syn.Requires)
		{
			if (!loadout.HasItem(req)) return false;
		}
		return true;
	}

	public static List<SynergyDef> ReadySynergies(HeroId hero, Loadout loadout)
	{
		return All.Where(s =>
		{
			if (s.Hero != null && s.Hero != hero) return false;
			return IsReady(s, loadout);
		}).ToList();
	}

	public static CardDef ToEvolveCard(SynergyDef syn) => new()
	{
		Id = syn.EvolveCardId,
		Name = syn.Name,
		Desc = syn.Desc,
		Kind = CardKind.Evolve,
		Hero = syn.Hero,
		GrantsItemId = syn.PrimaryItemId,
		IsNewItem = false,
		UpgradeStat = syn.EvolveStat,
		SynergyId = syn.Id,
	};

	public static List<CardDef> ReadyEvolveCards(HeroId hero, Loadout loadout) =>
		ReadySynergies(hero, loadout).Select(ToEvolveCard).ToList();
}
