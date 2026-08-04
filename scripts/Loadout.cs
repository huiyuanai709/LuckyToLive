using Godot;
using System.Collections.Generic;
using System.Linq;

public class Loadout
{
	public readonly List<SlotItem> Slots = new();

	public bool IsFull => Slots.Count >= Game.Instance.AvailableSlotsThisRun;
	public int Count => Slots.Count;

	public bool HasItem(string itemId) => Slots.Any(s => s.ItemId == itemId);

	public SlotItem GetItem(string itemId) => Slots.FirstOrDefault(s => s.ItemId == itemId);

	public SlotItem ApplyCard(CardDef card, Hero hero, Node2D world)
	{
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
				if (before >= SlotItem.MaxBeamRays) break; // 已满则不再加条数（抽取侧也应已过滤）

				if (card?.BeamAnglesDeg != null && card.BeamAnglesDeg.Length > 0)
				{
					// 覆盖式：采用卡牌角度，但不超过上限
					int n = Mathf.Min(card.BeamAnglesDeg.Length, SlotItem.MaxBeamRays);
					var angles = new float[n];
					System.Array.Copy(card.BeamAnglesDeg, angles, n);
					item.BeamAnglesDeg = angles;
					item.BeamRays = n;
				}
				else
				{
					// 默认规则：只加条数，角度由 Beam.DefaultAngles 均分/对称推出
					int add = Mathf.Max(1, card?.BeamRaysAdd ?? 1);
					item.BeamRays = Mathf.Min(SlotItem.MaxBeamRays, before + add);
					item.BeamAnglesDeg = null;
				}
				item.Range += card?.BeamLengthAdd ?? 0f;
				item.Damage += 3f + item.Level;
				break;
			}
			case "damage":
				// 纯伤害：高成长，后续升级仍有感
				item.Damage += 6f + item.Level * 2f;
				break;
			case "rate":
				item.FireRate += 0.22f;
				break;
			case "range":
				item.Range += 26f;
				break;
			case "special":
				// 多数武器唯一升级路径：伤害为主，附带攻速/射程/特效
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
		}
	}

	public float LevelSum() => Slots.Sum(s => s.Level);
}
