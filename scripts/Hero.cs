using Godot;
using System.Collections.Generic;

public partial class Hero : CharacterBody2D
{
	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void HpChangedEventHandler(float hp, float maxHp);
	[Signal] public delegate void XpChangedEventHandler(int level, float xp, float need);
	[Signal] public delegate void FrenzyChangedEventHandler(int streak, float mul);

	public HeroId HeroType;
	public float MaxHp = 100f;
	public float Hp = 100f;
	public float MoveSpeed = 180f;
	public float RegenPerSec;
	public int Level = 1;
	public float Xp;
	public Loadout Loadout = new();

	/// <summary>精英掉落拾取半径；磁吸被动会拉高。</summary>
	public float PickupRange = 28f;
	/// <summary>造成伤害时按比例回血（0~1）。</summary>
	public float Lifesteal;
	/// <summary>连杀狂热伤害倍率（英雄武器）。</summary>
	public float DamageMul = 1f;
	public int KillStreak { get; private set; }

	private readonly Dictionary<string, float> _cooldowns = new();
	private readonly Dictionary<string, BeamEmitter> _beams = new();
	private Enemy _target;
	private AnimatedSprite2D _sprite;
	private UnitSpriteAnim _anim;
	private readonly Vector2 _spriteBaseScale = new(0.58f, 0.58f);
	/// <summary>最后一次移动方向；站住时射线保持该朝向。</summary>
	private Vector2 _facing = Vector2.Right;

	private float _invulnLeft;
	private float _dashCd;
	private float _dashLeft;
	private Vector2 _dashVel;
	private float _streakWindow;
	private const float DashDistance = 130f;
	private const float DashDuration = 0.14f;
	private const float DashCooldown = 1.6f;
	private const float StreakGap = 1.8f;

	public bool IsInvulnerable => _invulnLeft > 0f || _dashLeft > 0f;

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
		_anim?.SetArtFacesRight(CharacterArt.HeroArtFacesRight(HeroType));
		_anim?.RefreshMultiFrameFlag();
		_sprite.Play(CharacterArt.AnimIdle);
		QueueRedraw();
	}

	public float XpToNext() => 12f + Level * 8f;

	public void AddXp(float amount)
	{
		if (amount <= 0f) return;
		Xp += amount;
		FloatingText.ShowXp(GlobalPosition, amount);
		EmitSignal(SignalName.XpChanged, Level, Xp, XpToNext());
	}

	public bool TryLevelUp()
	{
		if (Xp < XpToNext()) return false;
		Xp -= XpToNext();
		Level += 1;
		RestoreFullHp();
		_invulnLeft = Mathf.Max(_invulnLeft, 0.85f);
		EmitSignal(SignalName.XpChanged, Level, Xp, XpToNext());
		return true;
	}

	/// <summary>升级 / 强效回复：生命回满并飘字。</summary>
	public void RestoreFullHp()
	{
		float missing = MaxHp - Hp;
		Hp = MaxHp;
		if (missing > 0.5f)
			FloatingText.ShowHeal(GlobalPosition, missing);
		EmitSignal(SignalName.HpChanged, Hp, MaxHp);
		QueueRedraw();
	}

	public void TakeDamage(float amount)
	{
		if (amount <= 0f || IsInvulnerable) return;
		Hp -= amount;
		FloatingText.ShowDamage(GlobalPosition, amount);
		_anim?.PlayHit();
		EmitSignal(SignalName.HpChanged, Hp, MaxHp);
		QueueRedraw();
		if (Hp <= 0f)
		{
			Hp = 0;
			EmitSignal(SignalName.Died);
		}
	}

	public void Heal(float amount, bool showFloat = false)
	{
		if (amount <= 0f) return;
		float before = Hp;
		Hp = Mathf.Min(MaxHp, Hp + amount);
		float gained = Hp - before;
		if (showFloat && gained > 1f)
			FloatingText.ShowHeal(GlobalPosition, gained);
		EmitSignal(SignalName.HpChanged, Hp, MaxHp);
	}

	/// <summary>击杀推进连杀窗口；超时清零。返回当前倍率。</summary>
	public float RegisterKill()
	{
		if (_streakWindow > 0f)
			KillStreak += 1;
		else
			KillStreak = 1;
		_streakWindow = StreakGap;
		float mul = FrenzyMulForStreak(KillStreak);
		if (!Mathf.IsEqualApprox(mul, DamageMul))
		{
			DamageMul = mul;
			EmitSignal(SignalName.FrenzyChanged, KillStreak, DamageMul);
		}
		else if (KillStreak >= 3)
		{
			EmitSignal(SignalName.FrenzyChanged, KillStreak, DamageMul);
		}
		return DamageMul;
	}

	public static float FrenzyMulForStreak(int streak)
	{
		if (streak >= 12) return 1.55f;
		if (streak >= 8) return 1.35f;
		if (streak >= 5) return 1.2f;
		if (streak >= 3) return 1.1f;
		return 1f;
	}

	public float ScaleDamage(float baseDamage) => baseDamage * Mathf.Max(0.1f, DamageMul);

	public void OnDealtDamage(float amount)
	{
		if (Lifesteal <= 0f || amount <= 0f) return;
		Heal(amount * Lifesteal, showFloat: amount * Lifesteal >= 2f);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (_invulnLeft > 0f) _invulnLeft -= dt;
		if (_dashCd > 0f) _dashCd -= dt;

		if (_streakWindow > 0f)
		{
			_streakWindow -= dt;
			if (_streakWindow <= 0f && KillStreak > 0)
			{
				KillStreak = 0;
				if (DamageMul > 1f)
				{
					DamageMul = 1f;
					EmitSignal(SignalName.FrenzyChanged, 0, 1f);
				}
			}
		}

		Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		bool moving = input.LengthSquared() > 0.0001f;
		if (moving) _facing = input.Normalized();

		TryStartDash(input);

		if (_dashLeft > 0f)
		{
			_dashLeft -= dt;
			Velocity = _dashVel;
			MoveAndSlide();
		}
		else
		{
			Velocity = input * MoveSpeed;
			MoveAndSlide();
		}

		if (RegenPerSec > 0) Heal(RegenPerSec * dt);

		AcquireTarget();
		TickWeapons(dt);
		TickBeams(dt);
		TickAnim(dt, moving || _dashLeft > 0f);
		if (_sprite?.SpriteFrames == null) QueueRedraw();
	}

	private void TryStartDash(Vector2 input)
	{
		if (_dashLeft > 0f || _dashCd > 0f) return;
		if (!Input.IsActionJustPressed("dash")) return;
		Vector2 dir = input.LengthSquared() > 0.0001f ? input.Normalized() : _facing;
		if (dir.LengthSquared() < 0.0001f) dir = Vector2.Right;
		_facing = dir;
		_dashLeft = DashDuration;
		_dashCd = DashCooldown;
		_dashVel = dir * (DashDistance / DashDuration);
		_invulnLeft = Mathf.Max(_invulnLeft, DashDuration + 0.08f);
		_anim?.PlayAttack(0.16f);
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
		if (dist > item.Range + 20f + (_target?.BodyRadius ?? 0f) * 0.35f
			&& item.WeaponStyle is not ("slash" or "charge")) return false;

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
		float dmg = ScaleDamage(item.Damage);
		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy e || !IsInstanceValid(e)) continue;
			// 命中按怪物体型外扩，避免精英放大后近战「够不着中心」
			if (GlobalPosition.DistanceTo(e.GlobalPosition) <= range + e.BodyRadius * 0.45f)
			{
				e.TakeDamage(dmg);
				OnDealtDamage(dmg);
			}
		}
	}

	private void FireProjectile(SlotItem item)
	{
		var p = new Projectile();
		GetParent().AddChild(p);
		p.GlobalPosition = GlobalPosition;
		p.Setup(_target, item, this);
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
		// 冲刺冷却：脚下短条
		if (_dashCd > 0f)
		{
			float ready = 1f - Mathf.Clamp(_dashCd / DashCooldown, 0f, 1f);
			DrawRect(new Rect2(-10, 18, 20, 3), new Color(0.15f, 0.15f, 0.2f, 0.7f));
			DrawRect(new Rect2(-10, 18, 20 * ready, 3), new Color(0.45f, 0.85f, 1f, 0.9f));
		}
		if (IsInvulnerable)
			DrawArc(Vector2.Zero, 22f, 0f, Mathf.Tau, 24, new Color(0.6f, 0.95f, 1f, 0.55f), 2f);
	}
}
