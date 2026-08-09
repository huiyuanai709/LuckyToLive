using Godot;

/// <summary>
/// 可破坏掩体：阻挡走位，吃英雄伤害后碎裂清路。
/// </summary>
public partial class DestructibleCover : StaticBody2D
{
	public float MaxHp = 40f;
	public float Hp = 40f;
	public float BodyRadius = 22f;

	private Sprite2D _sprite;
	private CollisionShape2D _col;
	private float _hitFlash;
	private bool _broken;

	public static DestructibleCover Create(string kind, Vector2 pos, float scale, Texture2D tex, bool feetAnchor)
	{
		var cover = new DestructibleCover();
		cover.Position = pos;
		cover.CollisionLayer = 1;
		cover.CollisionMask = 0;
		cover.BodyRadius = Mathf.Clamp(18f * scale, 14f, 36f);
		cover.MaxHp = 28f + cover.BodyRadius * 1.1f;
		cover.Hp = cover.MaxHp;

		cover._sprite = new Sprite2D
		{
			Texture = tex,
			TextureFilter = TextureFilterEnum.Nearest,
			Scale = new Vector2(scale, scale),
			Centered = true,
		};
		if (feetAnchor && tex != null)
			cover._sprite.Offset = new Vector2(0, -tex.GetHeight() * 0.5f + 6f);
		cover.AddChild(cover._sprite);

		cover._col = new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = cover.BodyRadius },
			Position = new Vector2(0, -2f),
		};
		cover.AddChild(cover._col);
		return cover;
	}

	public override void _Ready()
	{
		AddToGroup("island_decor");
		AddToGroup("island_obstacles");
		AddToGroup("destructibles");
	}

	public override void _Process(double delta)
	{
		if (_hitFlash <= 0f) return;
		_hitFlash -= (float)delta;
		if (_sprite != null)
			_sprite.Modulate = _hitFlash > 0f
				? new Color(1.4f, 0.7f, 0.6f)
				: Colors.White;
	}

	public void TakeDamage(float amount)
	{
		if (_broken || amount <= 0f) return;
		Hp -= amount;
		_hitFlash = 0.12f;
		FloatingText.ShowDamage(GlobalPosition + new Vector2(0, -18), amount);
		if (Hp <= 0f)
			BreakApart();
	}

	private void BreakApart()
	{
		if (_broken) return;
		_broken = true;
		RemoveFromGroup("destructibles");
		RemoveFromGroup("island_obstacles");
		if (_col != null) _col.Disabled = true;
		CollisionLayer = 0;

		// 碎裂：缩一下再消失
		var tw = CreateTween();
		if (_sprite != null)
		{
			tw.TweenProperty(_sprite, "scale", _sprite.Scale * 1.15f, 0.08f);
			tw.TweenProperty(_sprite, "modulate:a", 0f, 0.18f);
		}
		tw.TweenCallback(Callable.From(QueueFree));
	}
}
