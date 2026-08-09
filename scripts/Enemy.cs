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
	public bool IsBoss;
	public string BossId = "";
	public string Affix = "";
	public float SlowTimer;
	public float SlowFactor = 1f;
	private bool _bossAlsoFireGround;

	/// <summary>碰撞 / 贴身判定半径；精英在 ConfigureElite 后会放大。</summary>
	public float BodyRadius { get; private set; } = 12f;

	private float _contactCd;
	private AnimatedSprite2D _sprite;
	private UnitSpriteAnim _anim;
	private Vector2 _spriteBaseScale;
	private CollisionShape2D _colShape;
	private float _summonCd;
	private float _skillCd;
	private Hero _hero;
	private float _cosmeticDrawCd;
	private bool _chargeWarnDrawn;

	// 近战冲锋
	private bool _charging;
	private float _chargeT;
	private float _chargeCd;
	private Vector2 _chargeDir = Vector2.Right;

	// 击退：独立于追击/冲锋的短促位移，命中方向的力随时间衰减
	private Vector2 _knockbackVel = Vector2.Zero;
	private const float KnockbackDecay = 750f;

	// 狂怒(berserk)：残血越战越勇，基于配置时记录的基础值实时插值
	private float _berserkBaseSpeed;
	private float _berserkBaseContactCd;

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
			case "berserk":
				// 残血越战越勇：速度/攻速随生命降低而线性提升
				Speed = 62f;
				ContactDamage = 10f;
				ContactCooldown = 0.6f;
				_berserkBaseSpeed = Speed;
				_berserkBaseContactCd = ContactCooldown;
				break;
			case "splitter":
				// 死亡时裂成两只弱化分身，血量略降作为补偿
				Speed = 50f;
				ContactDamage = 9f;
				ContactCooldown = 0.75f;
				MaxHp *= 0.82f;
				Hp = MaxHp;
				break;
		}

		// AddChild 已触发 _Ready（当时还不是精英），此处补刷贴图与动效幅度
		ApplyVisual();

		if (Affix == "orbit")
			SpawnOrbitBalls(3);
	}

	/// <summary>Boss：复用精英词条逻辑，体型与血量更高。</summary>
	public void ConfigureBoss(string bossId, float hpMul)
	{
		IsElite = true;
		IsBoss = true;
		BossId = bossId ?? "";
		_bossAlsoFireGround = false;

		switch (BossId)
		{
			case "tide_guard":
				Affix = "melee";
				MaxHp = 420f * hpMul;
				Hp = MaxHp;
				Speed = 72f;
				ContactDamage = 13f;
				ContactCooldown = 0.32f;
				XpValue = 40f;
				BodyRadius = 52f;
				_chargeCd = 1.0f;
				break;
			case "island_lord":
				Affix = "orbit";
				_bossAlsoFireGround = true;
				MaxHp = 900f * hpMul;
				Hp = MaxHp;
				Speed = 48f;
				ContactDamage = 18f;
				ContactCooldown = 0.75f;
				XpValue = 80f;
				BodyRadius = 64f;
				_skillCd = 2.0f;
				break;
			default:
				Affix = "melee";
				MaxHp = 500f * hpMul;
				Hp = MaxHp;
				Speed = 60f;
				ContactDamage = 14f;
				XpValue = 50f;
				BodyRadius = 56f;
				break;
		}

		ApplyBodyRadius();
		ApplyVisual();
		if (Affix == "orbit")
			SpawnOrbitBalls(IsBoss ? 4 : 3);
	}

	private void ApplyBodyRadius()
	{
		if (_colShape?.Shape is CircleShape2D circle)
			circle.Radius = BodyRadius;
	}

	private void ApplyVisual()
	{
		// 基础 ~0.52；精英约 2.5× 基础显示尺度；Boss 再大一圈
		float s = IsBoss ? 1.85f : IsElite ? 1.45f : 0.52f;
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
		_anim?.SetArtFacesRight(CharacterArt.EnemyArtFacesRight(IsElite));
		_anim?.RefreshMultiFrameFlag();
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

		if (_hero == null || !IsInstanceValid(_hero))
			_hero = GetTree().GetFirstNodeInGroup("hero") as Hero;
		if (_hero == null || !IsInstanceValid(_hero)) return;

		if (Affix == "summon")
		{
			_summonCd -= dt;
			if (_summonCd <= 0)
			{
				_summonCd = 6f;
				// 场上已挤时不再召唤，避免后期叠怪卡顿
				if (GetTree().GetNodesInGroup("enemies").Count < SpawnDirector.MaxAliveEnemies)
					SpawnMinion();
			}
		}

		if (Affix == "fire_ground" || _bossAlsoFireGround)
			TickFireGround(dt, _hero);

		if (Affix == "berserk" && _berserkBaseSpeed > 0f)
		{
			float missingPct = 1f - Mathf.Clamp(Hp / MaxHp, 0f, 1f);
			Speed = _berserkBaseSpeed * (1f + missingPct * 1.1f);
			ContactCooldown = Mathf.Max(0.14f, _berserkBaseContactCd * (1f - missingPct * 0.5f));
		}

		Vector2 toHero = _hero.GlobalPosition - GlobalPosition;
		float dist = toHero.Length();
		// 英雄圆半径 14 + 怪体半径；停步与接触距离必须大于该分离距离，否则贴身也打不到
		float contactRange = 14f + BodyRadius + 4f;
		float stopRange = contactRange - 2f;
		bool moving = false;

		if (Affix == "melee" && TickMeleeCharge(dt, toHero, dist, contactRange, _hero))
		{
			moving = true;
		}
		else if (dist > stopRange)
		{
			float terrainMul = TerrainBrush.SampleMul(GetTree(), GlobalPosition, forHero: false);
			Velocity = toHero.Normalized() * Speed * SlowFactor * terrainMul;
			MoveAndSlide();
			moving = true;
		}
		else
		{
			Velocity = Vector2.Zero;
		}

		// 击退：与追击/冲锋位移分开结算的一次额外 MoveAndSlide，命中方向短促推开
		if (_knockbackVel.LengthSquared() > 1f)
		{
			Velocity = _knockbackVel;
			MoveAndSlide();
			moving = true;
			_knockbackVel = _knockbackVel.MoveToward(Vector2.Zero, KnockbackDecay * dt);
		}

		_contactCd -= dt;
		if (dist < contactRange && _contactCd <= 0f)
		{
			_contactCd = ContactCooldown;
			_hero.TakeDamage(ContactDamage);
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

		// 血条/受击只在状态变化时重绘；精英词条特效降频，避免百级怪每帧 _Draw
		bool chargeWarn = Affix == "melee" && !_charging && _chargeCd < 0.35f;
		if (chargeWarn != _chargeWarnDrawn)
		{
			_chargeWarnDrawn = chargeWarn;
			QueueRedraw();
		}
		else if (IsElite && (Affix == "fire_ground" || Affix == "melee"))
		{
			_cosmeticDrawCd -= dt;
			if (_cosmeticDrawCd <= 0f)
			{
				_cosmeticDrawCd = 0.12f;
				QueueRedraw();
			}
		}
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
		SpawnDirector.Active?.Register(e);
	}

	/// <summary>
	/// 统一伤害入口：所有伤害来源（近战/弹道/建筑/宠物/射线）都走这里，命中震屏/粒子/
	/// 音效只需在此处理一次。knockbackForce 大于 0 时才会推开，默认无击退（建筑/射线等
	/// 持续伤害源不必额外指定）。
	/// </summary>
	public void TakeDamage(float amount, Vector2 knockbackDir = default, float knockbackForce = 0f, Color? fxColor = null)
	{
		if (amount <= 0f) return;
		Hp -= amount;
		FloatingText.ShowDamage(GlobalPosition, amount);
		_anim?.PlayHit();
		QueueRedraw();

		if (knockbackForce > 0f && knockbackDir.LengthSquared() > 0.0001f)
			ApplyKnockback(knockbackDir.Normalized(), knockbackForce);

		bool frenzied = _hero != null && IsInstanceValid(_hero) && _hero.DamageMul > 1f;
		float frenzyFxMul = frenzied ? Mathf.Max(1f, _hero.FrenzyFxMul) : 1f;
		Color spark = fxColor ?? new Color(1f, 0.5f, 0.35f);
		if (frenzied) spark = spark.Lerp(new Color(1f, 0.9f, 0.25f), 0.6f); // 狂热连杀：命中特效偏金色
		float fxScale = (0.7f + Mathf.Min(amount / 30f, 1f) * 0.7f) * frenzyFxMul;
		CombatFx.ImpactBurst(GetParent(), GlobalPosition, spark, fxScale);
		ProceduralSfx.Play(amount >= 18f ? "hit_heavy" : "hit_light", GlobalPosition, 0.1f);

		// 只在较重的命中/精英身上震屏：高频小震屏会让 Main.Shake() 调用过密，也容易晕；
		// 阈值门控避免屏幕在快速连打时抖个不停。狂热只轻微放大震屏，避免血怒叠到刺眼。
		if (amount >= 22f || IsElite)
		{
			float dmgShakeMul = Mathf.Clamp(amount / 22f, 0.6f, 1.25f);
			float shakeFrenzy = frenzied ? Mathf.Lerp(1f, frenzyFxMul, 0.35f) : 1f;
			CombatFx.Shake((IsElite ? 5f : 3f) * dmgShakeMul * shakeFrenzy, IsElite ? 0.12f : 0.09f);
		}

		if (Hp <= 0f)
		{
			ProceduralSfx.Play("enemy_death", GlobalPosition);
			if (IsElite) CombatFx.Shake(8f, 0.18f);
			if (Affix == "splitter") SpawnSplit();
			EmitSignal(SignalName.Died, this);
			QueueFree();
		}
	}

	/// <summary>命中方向的短促推力；体型越大越抗击退，精英/Boss 手感更"扎实"。</summary>
	public void ApplyKnockback(Vector2 dir, float force)
	{
		float sizeMul = Mathf.Clamp(16f / BodyRadius, 0.12f, 1f);
		_knockbackVel += dir * force * sizeMul;
		const float maxSpeed = 480f;
		if (_knockbackVel.Length() > maxSpeed)
			_knockbackVel = _knockbackVel.Normalized() * maxSpeed;
	}

	/// <summary>裂生(splitter) 词条：死亡时补两只弱化分身，复用 SpawnMinion 同款配置。</summary>
	private void SpawnSplit()
	{
		for (int i = 0; i < 2; i++)
		{
			var e = new Enemy();
			GetParent().AddChild(e);
			e.GlobalPosition = GlobalPosition + new Vector2(i == 0 ? -22 : 22, 6);
			e.ConfigureBasic(0.55f, 1.15f);
			e.XpValue = 3f;
			SpawnDirector.Active?.Register(e);
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
			var theme = MapCatalog.Get(Game.Instance?.SelectedMap ?? MapId.Island);
			Color c = IsElite ? theme.EliteFallback : theme.BasicFallback;
			if (_anim != null && _anim.HitFlash) c = new Color(1f, 0.4f, 0.4f);
			DrawCircle(Vector2.Zero, IsElite ? 34 : 10, c);
		}

		float w = IsBoss ? 72f : IsElite ? 56f : 20f;
		float barY = IsBoss ? -72f : IsElite ? -58f : -24f;
		float pct = Mathf.Clamp(Hp / MaxHp, 0, 1);
		float barH = IsBoss ? 8f : IsElite ? 6f : 4f;
		DrawRect(new Rect2(-w / 2, barY, w, barH), new Color(0.25f, 0, 0));
		DrawRect(new Rect2(-w / 2, barY, w * pct, barH), IsBoss ? new Color(1f, 0.55f, 0.15f) : new Color(0.2f, 1f, 0.3f));

		if (IsElite && !string.IsNullOrEmpty(Affix))
		{
			Color mark = IsBoss
				? new Color(1f, 0.75f, 0.2f)
				: Affix switch
			{
				"melee" => new Color(1f, 0.35f, 0.2f),
				"orbit" => new Color(0.7f, 0.35f, 1f),
				"fire_ground" => new Color(1f, 0.55f, 0.1f),
				"shield" => new Color(0.45f, 0.75f, 1f),
				"summon" => new Color(0.4f, 1f, 0.45f),
				"berserk" => new Color(1f, 0.15f, 0.15f),
				"splitter" => new Color(0.5f, 1f, 0.65f),
				_ => new Color(1f, 0.9f, 0.2f),
			};
			// 词条色环贴身描边：远距离也能和贴图一起认出精英类型
			float ringR = BodyRadius + 6f;
			DrawArc(Vector2.Zero, ringR, 0f, Mathf.Tau, 28, new Color(mark.R, mark.G, mark.B, 0.55f), 3.2f);
			DrawCircle(new Vector2(0, barY - 8f), 5, mark);
			// 冲锋预警：即将冲刺时闪一下
			if (Affix == "melee" && !_charging && _chargeCd < 0.35f)
				DrawArc(Vector2.Zero, BodyRadius + 10f, 0f, Mathf.Tau, 20, new Color(1f, 0.3f, 0.15f, 0.55f), 2.5f);
			if (Affix == "shield")
				DrawArc(Vector2.Zero, BodyRadius + 14f, 0f, Mathf.Tau, 24, new Color(0.55f, 0.85f, 1f, 0.4f), 2f);
			if (Affix == "fire_ground")
			{
				for (int i = 0; i < 3; i++)
				{
					float a = i * Mathf.Tau / 3f + (float)Time.GetTicksMsec() * 0.004f;
					DrawCircle(Vector2.Right.Rotated(a) * (BodyRadius + 18f), 3.5f, new Color(1f, 0.45f, 0.1f, 0.7f));
				}
			}
		}
	}
}
