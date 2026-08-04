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

	private float _contactCd;
	private AnimatedSprite2D _sprite;
	private UnitSpriteAnim _anim;
	private Vector2 _spriteBaseScale;
	private float _summonCd;

	public override void _Ready()
	{
		AddToGroup("enemies");
		var shape = new CollisionShape2D();
		shape.Shape = new CircleShape2D { Radius = IsElite ? 18 : 12 };
		AddChild(shape);

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
	}

	public void ConfigureElite(float hpMul, string affix)
	{
		IsElite = true;
		Affix = affix;
		MaxHp = 90f * hpMul;
		Hp = MaxHp;
		Speed = 55f;
		ContactDamage = 14f;
		XpValue = 18f;
		if (affix == "冲刺") Speed = 95f;
		if (affix == "护盾") { MaxHp *= 1.4f; Hp = MaxHp; }
		// AddChild 已触发 _Ready（当时还不是精英），此处补刷贴图与动效幅度
		ApplyVisual();
	}

	private void ApplyVisual()
	{
		_spriteBaseScale = new Vector2(IsElite ? 0.48f : 0.38f, IsElite ? 0.48f : 0.38f);
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

		if (Affix == "召唤")
		{
			_summonCd -= dt;
			if (_summonCd <= 0)
			{
				_summonCd = 6f;
				SpawnMinion();
			}
		}

		Vector2 toHero = hero.GlobalPosition - GlobalPosition;
		float dist = toHero.Length();
		// 英雄圆半径 14 + 怪 12/18 ≈ 26/32；停步与接触距离必须大于该分离距离，否则贴身也打不到
		float bodyR = IsElite ? 18f : 12f;
		float contactRange = 14f + bodyR + 4f;
		float stopRange = contactRange - 2f;
		bool moving = false;
		if (dist > stopRange)
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
			_anim?.PlayAttack(0.24f);
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

	private void SpawnMinion()
	{
		var e = new Enemy();
		GetParent().AddChild(e);
		e.GlobalPosition = GlobalPosition + new Vector2(20, 0);
		e.ConfigureBasic(0.5f, 1.1f);
		e.XpValue = 2f;
	}

	public void TakeDamage(float amount)
	{
		Hp -= amount;
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
			DrawCircle(Vector2.Zero, IsElite ? 16 : 10, c);
		}
		float w = IsElite ? 28f : 20f;
		float pct = Mathf.Clamp(Hp / MaxHp, 0, 1);
		DrawRect(new Rect2(-w / 2, -24, w, 4), new Color(0.25f, 0, 0));
		DrawRect(new Rect2(-w / 2, -24, w * pct, 4), new Color(0.2f, 1f, 0.3f));
		if (IsElite && !string.IsNullOrEmpty(Affix))
		{
			DrawCircle(new Vector2(0, -30), 3, new Color(1f, 0.9f, 0.2f));
		}
	}
}
