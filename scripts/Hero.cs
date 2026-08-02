using Godot;
using System.Collections.Generic;

public partial class Hero : CharacterBody2D
{
	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void HpChangedEventHandler(float hp, float maxHp);
	[Signal] public delegate void XpChangedEventHandler(int level, float xp, float need);

	public HeroId HeroType;
	public float MaxHp = 100f;
	public float Hp = 100f;
	public float MoveSpeed = 180f;
	public float RegenPerSec;
	public int Level = 1;
	public float Xp;
	public Loadout Loadout = new();

	private readonly Dictionary<string, float> _cooldowns = new();
	private Enemy _target;
	private Sprite2D _sprite;
	private float _beamFlash;

	public override void _Ready()
	{
		AddToGroup("hero");
		var shape = new CollisionShape2D();
		shape.Shape = new CircleShape2D { Radius = 14 };
		AddChild(shape);

		_sprite = new Sprite2D
		{
			Scale = new Vector2(0.42f, 0.42f),
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};
		AddChild(_sprite);
		ApplySprite();
		EmitSignal(SignalName.HpChanged, Hp, MaxHp);
		EmitSignal(SignalName.XpChanged, Level, Xp, XpToNext());
	}

	public void Setup(HeroId id)
	{
		HeroType = id;
		switch (id)
		{
			case HeroId.Warrior: MaxHp = 140; MoveSpeed = 170; break;
			case HeroId.Mage: MaxHp = 90; MoveSpeed = 165; break;
			case HeroId.Hunter: MaxHp = 100; MoveSpeed = 195; break;
		}
		Hp = MaxHp;
		ApplySprite();
	}

	private void ApplySprite()
	{
		if (_sprite == null) return;
		string path = HeroType switch
		{
			HeroId.Warrior => "res://assets/characters/hero_warrior.png",
			HeroId.Mage => "res://assets/characters/hero_mage.png",
			_ => "res://assets/characters/hero_hunter.png",
		};
		if (ResourceLoader.Exists(path))
			_sprite.Texture = GD.Load<Texture2D>(path);
		QueueRedraw();
	}

	public float XpToNext() => 12f + Level * 8f;

	public void AddXp(float amount)
	{
		Xp += amount;
		EmitSignal(SignalName.XpChanged, Level, Xp, XpToNext());
	}

	public bool TryLevelUp()
	{
		if (Xp < XpToNext()) return false;
		Xp -= XpToNext();
		Level += 1;
		EmitSignal(SignalName.XpChanged, Level, Xp, XpToNext());
		return true;
	}

	public void TakeDamage(float amount)
	{
		Hp -= amount;
		EmitSignal(SignalName.HpChanged, Hp, MaxHp);
		QueueRedraw();
		if (Hp <= 0f)
		{
			Hp = 0;
			EmitSignal(SignalName.Died);
		}
	}

	public void Heal(float amount)
	{
		Hp = Mathf.Min(MaxHp, Hp + amount);
		EmitSignal(SignalName.HpChanged, Hp, MaxHp);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = input * MoveSpeed;
		MoveAndSlide();

		if (RegenPerSec > 0) Heal(RegenPerSec * dt);

		AcquireTarget();
		TickWeapons(dt);
		if (_beamFlash > 0) { _beamFlash -= dt; QueueRedraw(); }
	}

	private void AcquireTarget()
	{
		_target = null;
		float best = float.MaxValue;
		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy e || !IsInstanceValid(e)) continue;
			float d = GlobalPosition.DistanceTo(e.GlobalPosition);
			if (d < best)
			{
				best = d;
				_target = e;
			}
		}
	}

	private void TickWeapons(float dt)
	{
		foreach (var item in Loadout.Slots)
		{
			if (item.Kind != CardKind.Weapon) continue;
			_cooldowns.TryGetValue(item.ItemId, out float cd);
			cd -= dt;
			if (cd <= 0f)
			{
				if (TryFire(item))
					cd = 1f / Mathf.Max(0.15f, item.FireRate);
			}
			_cooldowns[item.ItemId] = cd;
		}
	}

	private bool TryFire(SlotItem item)
	{
		if (_target == null || !IsInstanceValid(_target)) return false;
		float dist = GlobalPosition.DistanceTo(_target.GlobalPosition);
		if (dist > item.Range + 20f && item.WeaponStyle is not ("slash" or "charge")) return false;

		switch (item.WeaponStyle)
		{
			case "slash":
				MeleeHit(item, item.Range);
				return true;
			case "charge":
				MeleeHit(item, item.Range);
				return true;
			case "beam":
				FireBeam(item);
				return true;
			default:
				FireProjectile(item);
				return true;
		}
	}

	private void MeleeHit(SlotItem item, float range)
	{
		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy e || !IsInstanceValid(e)) continue;
			if (GlobalPosition.DistanceTo(e.GlobalPosition) <= range)
				e.TakeDamage(item.Damage);
		}
	}

	private void FireBeam(SlotItem item)
	{
		if (_target == null) return;
		_beamFlash = 0.08f;
		_target.TakeDamage(item.Damage);
		QueueRedraw();
	}

	private void FireProjectile(SlotItem item)
	{
		var p = new Projectile();
		GetParent().AddChild(p);
		p.GlobalPosition = GlobalPosition;
		p.Setup(_target, item);
	}

	public override void _Draw()
	{
		if (_sprite?.Texture == null)
		{
			Color body = HeroType switch
			{
				HeroId.Warrior => new Color(0.85f, 0.45f, 0.25f),
				HeroId.Mage => new Color(0.45f, 0.4f, 0.9f),
				_ => new Color(0.35f, 0.7f, 0.4f),
			};
			DrawCircle(new Vector2(0, -6), 16, body);
			DrawCircle(new Vector2(0, 10), 12, body.Darkened(0.15f));
		}
		float w = 28f;
		DrawRect(new Rect2(-w / 2, -34, w, 4), new Color(0.2f, 0, 0));
		DrawRect(new Rect2(-w / 2, -34, w * (Hp / MaxHp), 4), new Color(0.2f, 0.9f, 0.3f));

		if (_beamFlash > 0 && _target != null && IsInstanceValid(_target))
		{
			DrawLine(Vector2.Zero, ToLocal(_target.GlobalPosition), new Color(0.7f, 0.4f, 1f, 0.85f), 3f);
		}
	}
}
