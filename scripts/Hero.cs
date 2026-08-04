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
	private readonly Dictionary<string, BeamEmitter> _beams = new();
	private Enemy _target;
	private AnimatedSprite2D _sprite;
	private UnitSpriteAnim _anim;
	private readonly Vector2 _spriteBaseScale = new(0.58f, 0.58f);
	/// <summary>最后一次移动方向；站住时射线保持该朝向。</summary>
	private Vector2 _facing = Vector2.Right;

	public override void _Ready()
	{
		AddToGroup("hero");
		var shape = new CollisionShape2D();
		shape.Shape = new CircleShape2D { Radius = 14 };
		AddChild(shape);

		_sprite = new AnimatedSprite2D
		{
			Scale = _spriteBaseScale,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};
		AddChild(_sprite);
		_anim = new UnitSpriteAnim(_sprite, _spriteBaseScale);
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
		_sprite.SpriteFrames = CharacterArt.ForHero(HeroType);
		_sprite.Play(CharacterArt.AnimIdle);
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
		_anim?.PlayHit();
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
		bool moving = input.LengthSquared() > 0.0001f;
		if (moving) _facing = input.Normalized();

		if (RegenPerSec > 0) Heal(RegenPerSec * dt);

		AcquireTarget();
		TickWeapons(dt);
		TickBeams(dt);
		TickAnim(dt, moving);
		if (_sprite?.SpriteFrames == null) QueueRedraw();
	}

	private void TickAnim(float dt, bool moving)
	{
		if (_anim == null) return;
		_anim.SetMoving(moving);
		_anim.SetFacingX(_facing.X);
		_anim.SetWalkHz(7.5f + MoveSpeed / 60f);
		_anim.Update(dt);
	}

	/// <summary>持续射线自成节奏（持续/冷却），不走 TryFire 的攻速冷却。</summary>
	private void TickBeams(float dt)
	{
		foreach (var item in Loadout.Slots)
		{
			if (item.Kind != CardKind.Weapon || item.WeaponStyle != "beam") continue;
			if (!_beams.TryGetValue(item.ItemId, out var beam) || !IsInstanceValid(beam))
			{
				beam = new BeamEmitter();
				AddChild(beam);
				beam.Setup(item);
				_beams[item.ItemId] = beam;
			}
			beam.Tick(dt, _facing);
		}
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
			if (item.WeaponStyle == "beam") continue; // 由 TickBeams 驱动
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

		Vector2 toTarget = _target.GlobalPosition - GlobalPosition;
		if (toTarget.LengthSquared() > 0.0001f)
			_anim?.SetFacingX(toTarget.X);

		switch (item.WeaponStyle)
		{
			case "slash":
				MeleeHit(item, item.Range);
				break;
			case "charge":
				MeleeHit(item, item.Range);
				break;
			default:
				FireProjectile(item);
				break;
		}
		_anim?.PlayAttack(item.WeaponStyle is "slash" or "charge" ? 0.28f : 0.18f);
		return true;
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

	private void FireProjectile(SlotItem item)
	{
		var p = new Projectile();
		GetParent().AddChild(p);
		p.GlobalPosition = GlobalPosition;
		p.Setup(_target, item);
	}

	public override void _Draw()
	{
		bool hasFrames = _sprite?.SpriteFrames != null
			&& _sprite.SpriteFrames.HasAnimation(CharacterArt.AnimIdle)
			&& _sprite.SpriteFrames.GetFrameCount(CharacterArt.AnimIdle) > 0;
		if (!hasFrames)
		{
			Color body = HeroType switch
			{
				HeroId.Warrior => new Color(0.85f, 0.45f, 0.25f),
				HeroId.Mage => new Color(0.45f, 0.4f, 0.9f),
				_ => new Color(0.35f, 0.7f, 0.4f),
			};
			if (_anim != null && _anim.HitFlash) body = new Color(1f, 0.4f, 0.4f);
			DrawCircle(new Vector2(0, -6), 16, body);
			DrawCircle(new Vector2(0, 10), 12, body.Darkened(0.15f));
		}
		float w = 28f;
		DrawRect(new Rect2(-w / 2, -34, w, 4), new Color(0.2f, 0, 0));
		DrawRect(new Rect2(-w / 2, -34, w * (Hp / MaxHp), 4), new Color(0.2f, 0.9f, 0.3f));
	}
}
