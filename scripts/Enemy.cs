using Godot;

public partial class Enemy : CharacterBody2D
{
	[Signal] public delegate void DiedEventHandler(Enemy enemy);

	public float Speed = 70f;
	public float MaxHp = 30f;
	public float Hp = 30f;
	public float ContactDamage = 8f;
	public float ContactCooldown = 0.7f;
	public float XpValue = 4f;
	public bool IsElite;
	public string Affix = "";
	public float SlowTimer;
	public float SlowFactor = 1f;

	/// <summary>碰撞 / 贴身判定半径；精英在 ConfigureElite 后会放大。</summary>
	public float BodyRadius { get; private set; } = 12f;

	private float _contactCd;
	private AnimatedSprite2D _sprite;
	private UnitSpriteAnim _anim;
	private Vector2 _spriteBaseScale;
	private CollisionShape2D _colShape;
	private float _summonCd;
	private float _skillCd;

	// 近战冲锋
	private bool _charging;
	private float _chargeT;
	private float _chargeCd;
	private Vector2 _chargeDir = Vector2.Right;

	public override void _Ready()
	{
		AddToGroup("enemies");
		_colShape = new CollisionShape2D();
		_colShape.Shape = new CircleShape2D { Radius = BodyRadius };
		AddChild(_colShape);

		_sprite = new AnimatedSprite2D
		{
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};
		AddChild(_sprite);
		ApplyVisual();
	}

	public void ConfigureBasic(float hpMul, float spdMul)
	{
		MaxHp *= hpMul;
		Hp = MaxHp;
		Speed *= spdMul;
		XpValue = 3f + hpMul;
		BodyRadius = 12f;
		ApplyBodyRadius();
		ApplyVisual();
	}

	public void ConfigureElite(float hpMul, string affix)
	{
		IsElite = true;
		Affix = affix;
		MaxHp = 90f * hpMul;
		Hp = MaxHp;
		Speed = 55f;
		ContactDamage = 14f;
		ContactCooldown = 0.7f;
		XpValue = 18f;
		// 体型放大好几倍，远距离也能一眼认出精英
		BodyRadius = 40f;
		ApplyBodyRadius();

		switch (affix)
		{
			case "melee":
				// 近战形：攻速快，离远会冲锋
				Speed = 78f;
				ContactDamage = 11f;
				ContactCooldown = 0.28f;
				_chargeCd = 1.2f;
				break;
			case "orbit":
				// 远程技能形：绕身旋转球，可走位躲
				Speed = 42f;
				ContactDamage = 6f;
				ContactCooldown = 0.9f;
				break;
			case "fire_ground":
				// 远程技能形：往玩家脚下放火，示警后可躲
				Speed = 46f;
				ContactDamage = 8f;
				ContactCooldown = 0.85f;
				_skillCd = 1.8f;
				break;
			case "dash":
				// 兼容旧词条：当作近战冲锋
				Affix = "melee";
				Speed = 78f;
				ContactDamage = 11f;
				ContactCooldown = 0.28f;
				_chargeCd = 1.2f;
				break;
			case "ranged":
				// 兼容旧词条：当作旋转球
				Affix = "orbit";
				Speed = 42f;
				ContactDamage = 6f;
				ContactCooldown = 0.9f;
				break;
			case "shield":
				MaxHp *= 1.4f;
				Hp = MaxHp;
				break;
			case "summon":
				_summonCd = 3f;
				break;
		}

		// AddChild 已触发 _Ready（当时还不是精英），此处补刷贴图与动效幅度
		ApplyVisual();

		if (Affix == "orbit")
			SpawnOrbitBalls(3);
	}

	private void ApplyBodyRadius()
	{
		if (_colShape?.Shape is CircleShape2D circle)
			circle.Radius = BodyRadius;
	}

	private void ApplyVisual()
	{
		// 基础 ~0.52；精英约 2.5× 基础显示尺度，显著更大
		float s = IsElite ? 1.45f : 0.52f;
		_spriteBaseScale = new Vector2(s, s);
		if (_sprite != null)
		{
			_sprite.SpriteFrames = CharacterArt.ForEnemy(IsElite);
			_sprite.Scale = _spriteBaseScale;
			_sprite.Play(CharacterArt.AnimIdle);
		}
		if (_anim == null)
			_anim = new UnitSpriteAnim(_sprite, _spriteBaseScale);
		else
			_anim.SetBaseScale(_spriteBaseScale);
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (SlowTimer > 0)
		{
			SlowTimer -= dt;
			if (SlowTimer <= 0) SlowFactor = 1f;
		}

		var hero = GetTree().GetFirstNodeInGroup("hero") as Hero;
		if (hero == null || !IsInstanceValid(hero)) return;

		if (Affix == "summon")
		{
			_summonCd -= dt;
			if (_summonCd <= 0)
			{
				_summonCd = 6f;
				SpawnMinion();
			}
		}

		if (Affix == "fire_ground")
			TickFireGround(dt, hero);

		Vector2 toHero = hero.GlobalPosition - GlobalPosition;
		float dist = toHero.Length();
		// 英雄圆半径 14 + 怪体半径；停步与接触距离必须大于该分离距离，否则贴身也打不到
		float contactRange = 14f + BodyRadius + 4f;
		float stopRange = contactRange - 2f;
		bool moving = false;

		if (Affix == "melee" && TickMeleeCharge(dt, toHero, dist, contactRange, hero))
		{
			moving = true;
		}
		else if (dist > stopRange)
		{
			Velocity = toHero.Normalized() * Speed * SlowFactor;
			MoveAndSlide();
			moving = true;
		}
		else
		{
			Velocity = Vector2.Zero;
		}

		_contactCd -= dt;
		if (dist < contactRange && _contactCd <= 0f)
		{
			_contactCd = ContactCooldown;
			hero.TakeDamage(ContactDamage);
			_anim?.PlayAttack(Affix == "melee" ? 0.16f : 0.24f);
		}

		if (_anim != null)
		{
			_anim.SetMoving(moving);
			if (toHero.LengthSquared() > 0.0001f)
				_anim.SetFacingX(toHero.X);
			_anim.SetWalkHz(6.5f + Speed / 40f);
			_anim.Update(dt);
		}
		QueueRedraw();
	}

	/// <summary>
	/// 近战冲锋：距离够远时短促冲刺；返回 true 表示本帧已接管移动。
	/// </summary>
	private bool TickMeleeCharge(float dt, Vector2 toHero, float dist, float contactRange, Hero hero)
	{
		if (_charging)
		{
			_chargeT -= dt;
			Velocity = _chargeDir * 340f * SlowFactor;
			MoveAndSlide();
			// 冲锋途中碰到也结算一次接触伤
			if (dist < contactRange + 8f && _contactCd <= 0f)
			{
				_contactCd = ContactCooldown * 0.85f;
				hero.TakeDamage(ContactDamage * 1.15f);
				_anim?.PlayAttack(0.2f);
			}
			if (_chargeT <= 0f)
				_charging = false;
			return true;
		}

		_chargeCd -= dt;
		if (_chargeCd <= 0f && dist > 150f)
		{
			_charging = true;
			_chargeT = 0.48f;
			_chargeDir = toHero.LengthSquared() > 0.0001f ? toHero.Normalized() : Vector2.Right;
			_chargeCd = 3.6f;
			_anim?.PlayAttack(0.35f);
			return true;
		}
		return false;
	}

	private void TickFireGround(float dt, Hero hero)
	{
		_skillCd -= dt;
		if (_skillCd > 0f) return;
		_skillCd = 4.2f;
		var zone = new FireZone();
		GetParent().AddChild(zone);
		// 落在英雄当前位置，示警后才开始烧，留出走位空间
		zone.GlobalPosition = hero.GlobalPosition;
		_anim?.PlayAttack(0.3f);
	}

	private void SpawnOrbitBalls(int count)
	{
		float spin = 2.55f;
		for (int i = 0; i < count; i++)
		{
			var ball = new EnemyOrbitBall
			{
				OwnerEnemy = this,
				Angle = Mathf.Tau * i / count,
				OrbitRadius = BodyRadius + 56f,
				SpinSpeed = spin,
				Damage = 9f,
			};
			GetParent().CallDeferred(Node.MethodName.AddChild, ball);
		}
	}

	private void SpawnMinion()
	{
		var e = new Enemy();
		GetParent().AddChild(e);
		e.GlobalPosition = GlobalPosition + new Vector2(28, 0);
		e.ConfigureBasic(0.5f, 1.1f);
		e.XpValue = 2f;
	}

	public void TakeDamage(float amount)
	{
		if (amount <= 0f) return;
		Hp -= amount;
		FloatingText.ShowDamage(GlobalPosition, amount);
		_anim?.PlayHit();
		QueueRedraw();
		if (Hp <= 0f)
		{
			EmitSignal(SignalName.Died, this);
			QueueFree();
		}
	}

	public void ApplySlow(float factor, float duration)
	{
		SlowFactor = Mathf.Min(SlowFactor, factor);
		SlowTimer = Mathf.Max(SlowTimer, duration);
	}

	public override void _Draw()
	{
		bool hasFrames = _sprite?.SpriteFrames != null
			&& _sprite.SpriteFrames.HasAnimation(CharacterArt.AnimIdle)
			&& _sprite.SpriteFrames.GetFrameCount(CharacterArt.AnimIdle) > 0;
		if (!hasFrames)
		{
			Color c = IsElite ? new Color(0.95f, 0.45f, 0.15f) : new Color(0.85f, 0.25f, 0.35f);
			if (_anim != null && _anim.HitFlash) c = new Color(1f, 0.4f, 0.4f);
			DrawCircle(Vector2.Zero, IsElite ? 34 : 10, c);
		}

		float w = IsElite ? 56f : 20f;
		float barY = IsElite ? -58f : -24f;
		float pct = Mathf.Clamp(Hp / MaxHp, 0, 1);
		DrawRect(new Rect2(-w / 2, barY, w, IsElite ? 6f : 4f), new Color(0.25f, 0, 0));
		DrawRect(new Rect2(-w / 2, barY, w * pct, IsElite ? 6f : 4f), new Color(0.2f, 1f, 0.3f));

		if (IsElite && !string.IsNullOrEmpty(Affix))
		{
			Color mark = Affix switch
			{
				"melee" => new Color(1f, 0.35f, 0.2f),
				"orbit" => new Color(0.7f, 0.35f, 1f),
				"fire_ground" => new Color(1f, 0.55f, 0.1f),
				"shield" => new Color(0.45f, 0.75f, 1f),
				"summon" => new Color(0.4f, 1f, 0.45f),
				_ => new Color(1f, 0.9f, 0.2f),
			};
			DrawCircle(new Vector2(0, barY - 8f), 5, mark);
			// 冲锋预警：即将冲刺时闪一下
			if (Affix == "melee" && !_charging && _chargeCd < 0.35f)
				DrawArc(Vector2.Zero, BodyRadius + 10f, 0f, Mathf.Tau, 20, new Color(1f, 0.3f, 0.15f, 0.55f), 2.5f);
		}
	}
}
