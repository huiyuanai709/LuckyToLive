using Godot;
using System.Collections.Generic;
using System.Linq;

public class Loadout
{
	public readonly List<SlotItem> Slots = new();
	public readonly HashSet<string> CompletedSynergies = new();

	public bool IsFull => Slots.Count >= Game.Instance.AvailableSlotsThisRun;
	public int Count => Slots.Count;

	public bool HasItem(string itemId) => Slots.Any(s => s.ItemId == itemId);

	public SlotItem GetItem(string itemId) => Slots.FirstOrDefault(s => s.ItemId == itemId);

	public SlotItem ApplyCard(CardDef card, Hero hero, Node2D world)
	{
		if (card.Kind == CardKind.Evolve || (card.UpgradeStat != null && card.UpgradeStat.StartsWith("evolve:")))
			return ApplyEvolve(card, hero);

		if (!card.IsNewItem)
		{
			var existing = GetItem(card.GrantsItemId);
			if (existing == null) return null;
			UpgradeItem(existing, card.UpgradeStat, card);
			SyncEntityStats(existing);
			return existing;
		}

		if (card.Kind == CardKind.Passive)
		{
			if (HasItem(card.GrantsItemId))
			{
				var p = GetItem(card.GrantsItemId);
				UpgradeItem(p, card.UpgradeStat, card);
				ApplyPassive(p, hero);
				return p;
			}
			var passive = new SlotItem
			{
				ItemId = card.GrantsItemId,
				Name = card.Name,
				Kind = CardKind.Passive,
				UpgradeStatHolder = card.UpgradeStat,
			};
			Slots.Add(passive);
			ApplyPassive(passive, hero);
			return passive;
		}

		if (IsFull) return null;

		var item = CreateFromCard(card);
		Slots.Add(item);

		if (item.Kind == CardKind.Building)
		{
			item.BuildingRef = Building.SpawnNear(world, hero, item);
		}
		else if (item.Kind == CardKind.Pet)
		{
			item.PetRef = Pet.Spawn(world, hero, item);
		}

		return item;
	}

	private SlotItem ApplyEvolve(CardDef card, Hero hero)
	{
		var syn = !string.IsNullOrEmpty(card.SynergyId)
			? SynergyCatalog.Get(card.SynergyId)
			: SynergyCatalog.GetByEvolveCard(card.Id);
		if (syn == null || CompletedSynergies.Contains(syn.Id)) return null;
		if (!SynergyCatalog.IsReady(syn, this)) return null;

		var primary = GetItem(syn.PrimaryItemId);
		if (primary == null) return null;

		primary.Level += 1;
		primary.EvolveCardId = syn.EvolveCardId;
		primary.Name = syn.Name;
		ApplyEvolveStats(syn.Id, primary, hero);
		CompletedSynergies.Add(syn.Id);
		SyncEntityStats(primary);

		// 堡垒：给战旗挂伤害光环标记
		if (syn.Id == "syn_w_bastion")
		{
			var banner = GetItem("w_heal_totem");
			if (banner != null)
			{
				banner.DamageAuraBonus = 0.10f;
				SyncEntityStats(banner);
			}
		}

		// 围猎：陷阱也记标记，方便触发
		if (syn.Id == "syn_h_pack")
		{
			var trap = GetItem("h_trap");
			if (trap != null) trap.PackHunt = true;
			primary.PackHunt = true;
			SyncEntityStats(primary);
		}

		return primary;
	}

	private static void ApplyEvolveStats(string synId, SlotItem primary, Hero hero)
	{
		switch (synId)
		{
			case "syn_w_cleave":
				primary.Damage *= 1.35f;
				primary.Range += 40f;
				break;
			case "syn_w_bastion":
				primary.Damage *= 1.5f;
				break;
			case "syn_m_frostfire":
			{
				float fireDmg = 16f;
				var fire = hero?.Loadout?.GetItem("m_fire");
				if (fire != null) fireDmg = fire.Damage;
				primary.Damage = Mathf.Max(primary.Damage, fireDmg) * 1.2f;
				primary.Splash = Mathf.Max(primary.Splash, 70f);
				primary.SlowFactor = Mathf.Min(primary.SlowFactor, 0.5f);
				primary.WeaponStyle = "frostfire";
				break;
			}
			case "syn_m_prism":
				primary.BeamRaysCap = 4;
				if (primary.EffectiveBeamRays < 4)
				{
					primary.BeamRays = Mathf.Min(4, primary.BeamRays + 1);
					primary.BeamAnglesDeg = null;
				}
				primary.SlowFactor = Mathf.Min(primary.SlowFactor, 0.55f);
				primary.Damage *= 1.25f;
				break;
			case "syn_h_pack":
				primary.Damage *= 1.4f;
				primary.PackHunt = true;
				break;
			case "syn_h_storm":
				primary.Pierce += 1;
				primary.SplitArrow = true;
				primary.Damage *= 1.25f;
				break;
			case "syn_p_bloodrush":
				if (hero != null)
				{
					hero.MoveSpeed += 15f;
					hero.KillHealOnKill += 2f;
					hero.FrenzyThresholdBonus += 1;
				}
				break;
		}
	}

	private static SlotItem CreateFromCard(CardDef card)
	{
		var item = new SlotItem
		{
			ItemId = card.GrantsItemId,
			Name = card.Name,
			Kind = card.Kind,
			WeaponStyle = card.WeaponStyle,
			BuildingStyle = card.BuildingStyle,
			PetStyle = card.PetStyle,
			Level = 1,
			ProjectileTexture = card.ProjectileTexture,
			BeamAnglesDeg = card.BeamAnglesDeg,
		};
		switch (card.WeaponStyle)
		{
			case "slash":
				item.Damage = 14; item.FireRate = 1.4f; item.Range = 55; break;
			case "charge":
				item.Damage = 22; item.FireRate = 0.55f; item.Range = 110; break;
			case "pierce":
				item.Damage = 11; item.FireRate = 1.1f; item.Range = 220; item.Pierce = 3; break;
			case "ice_arrow":
				item.Damage = 10; item.FireRate = 1.0f; item.Range = 200; item.SlowFactor = 0.55f; break;
			case "fireball":
				item.Damage = 16; item.FireRate = 0.75f; item.Range = 180; item.Splash = 50; break;
			case "beam":
				// 持续射线：伤害为每次 tick 的量，tick 间隔由 BeamEmitter.TickInterval 全局决定
				item.Damage = 8; item.Range = 240;
				item.BeamDuration = 2.5f; item.BeamCooldown = 3.0f; item.BeamRays = 1;
				break;
		}
		switch (card.BuildingStyle)
		{
			case "turret_phys":
				item.Damage = 9; item.FireRate = 1.2f; item.Range = 150; break;
			case "turret_fire":
				item.Damage = 12; item.FireRate = 0.9f; item.Range = 140; item.Splash = 35; break;
			case "slow_field":
				item.Range = 90; item.SlowFactor = 0.5f; break;
			case "heal_totem":
				item.Range = 100; item.Damage = 3; break;
			case "shield_wall":
				item.Range = 40; item.Damage = 6; break;
			case "trap":
				item.Damage = 18; item.Range = 28; item.SlowFactor = 0.4f; break;
		}
		if (card.PetStyle == "wolf")
		{
			item.Damage = 8; item.FireRate = 1.3f; item.Range = 40;
		}
		return item;
	}

	private static void UpgradeItem(SlotItem item, string stat, CardDef card = null)
	{
		item.Level += 1;
		// 升级成长对齐敌人约 5 分钟 ~4–5× 血量缩放：每张升级卡应有明显体感（~30–50% 首升）。
		switch (stat)
		{
			case "beam_focus":
				item.Damage += 4f + item.Level * 1.5f;
				item.Range += 28f + (card?.BeamLengthAdd ?? 0f);
				break;
			case "beam_rays":
			{
				int before = item.EffectiveBeamRays;
				int cap = item.BeamRaysCap;
				if (before >= cap) break;

				if (card?.BeamAnglesDeg != null && card.BeamAnglesDeg.Length > 0)
				{
					int n = Mathf.Min(card.BeamAnglesDeg.Length, cap);
					var angles = new float[n];
					System.Array.Copy(card.BeamAnglesDeg, angles, n);
					item.BeamAnglesDeg = angles;
					item.BeamRays = n;
				}
				else
				{
					int add = Mathf.Max(1, card?.BeamRaysAdd ?? 1);
					item.BeamRays = Mathf.Min(cap, before + add);
					item.BeamAnglesDeg = null;
				}
				item.Range += card?.BeamLengthAdd ?? 0f;
				item.Damage += 3f + item.Level;
				break;
			}
			case "damage":
				item.Damage += 6f + item.Level * 2f;
				break;
			case "rate":
				item.FireRate += 0.22f;
				break;
			case "range":
				item.Range += 26f;
				break;
			case "special":
				item.Damage += 5f + item.Level;
				item.FireRate += 0.12f;
				item.Range += 14f;
				if (item.Pierce > 0) item.Pierce += 1;
				if (item.Splash > 0) item.Splash += 12f;
				if (item.SlowFactor < 1f) item.SlowFactor = Mathf.Max(0.25f, item.SlowFactor - 0.08f);
				break;
			case "hp":
			case "speed":
			case "regen":
			case "vamp":
			case "magnet":
				break;
		}
	}

	private static void SyncEntityStats(SlotItem item)
	{
		if (item.BuildingRef != null && GodotObject.IsInstanceValid(item.BuildingRef))
			item.BuildingRef.ApplyItem(item);
		if (item.PetRef != null && GodotObject.IsInstanceValid(item.PetRef))
			item.PetRef.ApplyItem(item);
	}

	private static void ApplyPassive(SlotItem item, Hero hero)
	{
		switch (item.ItemId)
		{
			case "p_hp":
				hero.MaxHp += item.Level == 1 ? 25 : 20;
				hero.Hp = Mathf.Min(hero.Hp + 25, hero.MaxHp);
				break;
			case "p_speed":
				hero.MoveSpeed += 30;
				break;
			case "p_regen":
				hero.RegenPerSec += 1.2f;
				break;
			case "p_vamp":
				hero.Lifesteal += item.Level == 1 ? 0.05f : 0.03f;
				break;
			case "p_magnet":
				hero.PickupRange += item.Level == 1 ? 48f : 28f;
				break;
		}
	}

	public float LevelSum() => Slots.Sum(s => s.Level);
}
