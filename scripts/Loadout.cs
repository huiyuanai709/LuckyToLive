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
			UpgradeItem(existing, card.UpgradeStat);
			SyncEntityStats(existing);
			return existing;
		}

		if (card.Kind == CardKind.Passive)
		{
			if (HasItem(card.GrantsItemId))
			{
				var p = GetItem(card.GrantsItemId);
				UpgradeItem(p, card.UpgradeStat);
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
				item.Damage = 8; item.FireRate = 4.0f; item.Range = 240; break;
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

	private static void UpgradeItem(SlotItem item, string stat)
	{
		item.Level += 1;
		switch (stat)
		{
			case "damage":
				item.Damage += 4 + item.Level; break;
			case "rate":
				item.FireRate += 0.15f; break;
			case "range":
				item.Range += 18; break;
			case "special":
				item.Damage += 3;
				item.FireRate += 0.08f;
				item.Range += 10;
				if (item.Pierce > 0) item.Pierce += 1;
				if (item.Splash > 0) item.Splash += 8;
				if (item.SlowFactor < 1f) item.SlowFactor = Mathf.Max(0.3f, item.SlowFactor - 0.05f);
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
