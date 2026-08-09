using Godot;

public partial class Building : Node2D
{
	public SlotItem Item;
	public float Hp = 80f;
	private float _cd;
	private float _tick;
	private Texture2D _tex;
	/// <summary>建筑贴图在世界中的目标显示直径（像素）。原先 64 相对英雄过小。</summary>
	private float _visualSize = 118f;

	public static Building SpawnNear(Node2D world, Hero hero, SlotItem item)
	{
		var b = new Building();
		world.AddChild(b);
		b.Item = item;
		b.GlobalPosition = FindSpot(world, hero.GlobalPosition);
		b.ApplyItem(item);
		return b;
	}

	private static Vector2 FindSpot(Node2D world, Vector2 origin)
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		var main = world as Main;
		Rect2 bounds = main?.IslandRect ?? new Rect2(80, 80, 2240, 1440);
		for (int i = 0; i < 24; i++)
		{
			float ang = rng.Randf() * Mathf.Tau;
			float rad = 60 + rng.Randf() * 80;
			Vector2 p = origin + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
			p.X = Mathf.Clamp(p.X, bounds.Position.X + 40, bounds.End.X - 40);
			p.Y = Mathf.Clamp(p.Y, bounds.Position.Y + 40, bounds.End.Y - 40);
			bool blocked = false;
			foreach (var n in world.GetTree().GetNodesInGroup("buildings"))
			{
				if (n is Node2D other && other.GlobalPosition.DistanceTo(p) < 48) { blocked = true; break; }
			}
			if (!blocked) return p;
		}
		return origin + new Vector2(70, 0);
	}

	public void ApplyItem(SlotItem item)
	{
		Item = item;
		Hp = 60 + item.Level * 20;
		_tex = LoadArt(item);
		_visualSize = VisualSizeFor(item);
		QueueRedraw();
	}

	private static Texture2D LoadArt(SlotItem item)
	{
		if (item == null || string.IsNullOrEmpty(item.ItemId)) return null;
		string path = $"res://assets/cards/{item.ItemId}.png";
		return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
	}

	public override void _Ready()
	{
		AddToGroup("buildings");
		if (Item != null) _tex = LoadArt(Item);
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_cd -= dt;
		_tick -= dt;
		switch (Item.BuildingStyle)
		{
			case "turret_phys":
			case "turret_fire":
				if (_cd <= 0) TryShoot();
				break;
			case "slow_field":
				ApplyAuraSlow();
				break;
			case "heal_totem":
			{
				var hero = GetTree().GetFirstNodeInGroup("hero") as Hero;
				if (hero != null && GlobalPosition.DistanceTo(hero.GlobalPosition) <= Item.Range)
				{
					if (Item.DamageAuraBonus > 0f)
						hero.AuraDamageMul = Mathf.Max(hero.AuraDamageMul, 1f + Item.DamageAuraBonus);
					if (_tick <= 0)
					{
						_tick = 0.5f;
						hero.Heal(Item.Damage * 0.5f);
					}
				}
				break;
			}
			case "shield_wall":
				DamageNearby(dt, true);
				break;
			case "trap":
				TriggerTrap();
				break;
		}
		// 建筑外观静态，不必每帧重绘
	}

	private void TryShoot()
	{
		Enemy best = null;
		float bestD = Item.Range;
		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy e || !IsInstanceValid(e)) continue;
			float d = GlobalPosition.DistanceTo(e.GlobalPosition) - e.BodyRadius * 0.35f;
			if (d < bestD) { bestD = d; best = e; }
		}
		if (best == null) return;
		_cd = 1f / Mathf.Max(0.2f, Item.FireRate);
		var p = new Projectile();
		GetParent().AddChild(p);
		p.GlobalPosition = GlobalPosition;
		p.Setup(best, Item);
	}

	private void ApplyAuraSlow()
	{
		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is Enemy e && IsInstanceValid(e)
				&& GlobalPosition.DistanceTo(e.GlobalPosition) <= Item.Range + e.BodyRadius * 0.35f)
				e.ApplySlow(Item.SlowFactor, 0.35f);
		}
	}

	private void DamageNearby(float dt, bool reflect)
	{
		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy e || !IsInstanceValid(e)) continue;
			if (GlobalPosition.DistanceTo(e.GlobalPosition) > Item.Range + e.BodyRadius * 0.4f) continue;
			if (reflect && _cd <= 0)
			{
				e.TakeDamage(Item.Damage);
				_cd = 0.45f;
			}
		}
	}

	private void TriggerTrap()
	{
		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy e || !IsInstanceValid(e)) continue;
			if (GlobalPosition.DistanceTo(e.GlobalPosition) > Item.Range + e.BodyRadius * 0.4f) continue;
			e.TakeDamage(Item.Damage);
			e.ApplySlow(Item.SlowFactor, 1.5f);
			if (Item.PackHunt)
				NotifyPackHunt(e);
			QueueFree();
			return;
		}
	}

	private void NotifyPackHunt(Enemy target)
	{
		foreach (var n in GetTree().GetNodesInGroup("pets"))
		{
			if (n is Pet pet && IsInstanceValid(pet) && pet.Item != null && pet.Item.PackHunt)
				pet.TryPackLeap(target);
		}
	}

	public override void _Draw()
	{
		if (Item == null) return;

		// 光环类建筑：先画范围圈，再叠贴图
		if (Item.BuildingStyle is "slow_field" or "heal_totem")
		{
			Color aura = Item.BuildingStyle == "slow_field"
				? new Color(0.4f, 0.75f, 1f, 0.35f)
				: new Color(0.3f, 0.9f, 0.5f, 0.28f);
			DrawCircle(Vector2.Zero, Item.Range, aura);
		}

		if (_tex != null)
		{
			Vector2 size = _tex.GetSize();
			float scale = _visualSize / Mathf.Max(size.X, size.Y);
			Vector2 draw = size * scale;
			DrawTextureRect(_tex, new Rect2(-draw / 2f, draw), false);
			return;
		}

		Color c = Item.BuildingStyle switch
		{
			"turret_fire" => new Color(0.95f, 0.4f, 0.15f),
			"slow_field" => new Color(0.4f, 0.75f, 1f, 0.35f),
			"heal_totem" => new Color(0.3f, 0.9f, 0.5f),
			"shield_wall" => new Color(0.7f, 0.7f, 0.85f),
			"trap" => new Color(0.8f, 0.6f, 0.2f),
			_ => new Color(0.55f, 0.55f, 0.6f),
		};
		// 光环范围已在上方画过；此处只画实体占位（放大，与有贴图时观感接近）
		if (Item.BuildingStyle is not ("slow_field" or "heal_totem"))
			DrawRect(new Rect2(-28, -28, 56, 56), c);
		DrawCircle(Vector2.Zero, 12, c.Lightened(0.3f));
	}

	private float VisualSizeFor(SlotItem item)
	{
		if (item == null) return _visualSize;
		return item.BuildingStyle switch
		{
			"trap" => 88f,
			"shield_wall" => 130f,
			"turret_phys" or "turret_fire" => 124f,
			"heal_totem" => 112f,
			"slow_field" => 100f,
			_ => _visualSize,
		};
	}
}
