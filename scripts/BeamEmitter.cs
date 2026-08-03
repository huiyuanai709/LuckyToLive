using Godot;

/// <summary>
/// 持续射线：从人物身上伸出固定长度的射线，随移动方向转向，
/// 激活期间按固定 tick 间隔对重叠敌人造成伤害，结束后进入冷却。
/// 升级可增加不同角度的射线数。
/// </summary>
public partial class BeamEmitter : Node2D
{
	/// <summary>全局伤害 tick 间隔（秒）；卡牌只改每次 tick 的 Damage。</summary>
	public const float TickInterval = 0.25f;
	/// <summary>命中判定的射线半宽（像素）。</summary>
	private const float HitHalfWidth = 12f;
	/// <summary>光束条绘制厚度（像素）。</summary>
	private const float DrawThickness = 18f;

	public SlotItem Item;

	private float _activeLeft;
	private float _cooldownLeft;
	private float _tickLeft;
	private Vector2 _aim = Vector2.Right;
	private Texture2D _tex;

	public bool IsActive => _activeLeft > 0f;

	public void Setup(SlotItem item)
	{
		Item = item;
		_tex = ProjectileArt.ForBeam(item);
		QueueRedraw();
	}

	/// <summary>由 Hero 每帧驱动；aim 为当前朝向（跟移动方向，站住保持最后朝向）。</summary>
	public void Tick(float dt, Vector2 aim)
	{
		if (Item == null) return;
		if (aim.LengthSquared() > 0.0001f) _aim = aim.Normalized();

		if (_activeLeft > 0f)
		{
			_activeLeft -= dt;
			_tickLeft -= dt;
			if (_tickLeft <= 0f)
			{
				_tickLeft = TickInterval;
				DamageOverlapping();
			}
			if (_activeLeft <= 0f)
			{
				_activeLeft = 0f;
				_cooldownLeft = Mathf.Max(0.05f, Item.BeamCooldown);
			}
			QueueRedraw();
			return;
		}

		_cooldownLeft -= dt;
		if (_cooldownLeft <= 0f)
		{
			_activeLeft = Mathf.Max(0.05f, Item.BeamDuration);
			_tickLeft = 0f; // 开火瞬间先跳一次
		}
		QueueRedraw();
	}

	/// <summary>当前所有射线相对世界的朝向（已含 _aim 基准）。</summary>
	private float[] AnglesRad()
	{
		float baseAng = _aim.Angle();
		float[] offsets = Item.BeamAnglesDeg;
		if (offsets == null || offsets.Length == 0)
		{
			// 默认规则：1 条向前；2 条前后；3 条起按 360° 均分
			int n = Mathf.Max(1, Item.BeamRays);
			offsets = new float[n];
			if (n == 1) offsets[0] = 0f;
			else if (n == 2) { offsets[0] = 0f; offsets[1] = 180f; }
			else for (int i = 0; i < n; i++) offsets[i] = 360f * i / n;
		}
		var result = new float[offsets.Length];
		for (int i = 0; i < offsets.Length; i++)
			result[i] = baseAng + Mathf.DegToRad(offsets[i]);
		return result;
	}

	private void DamageOverlapping()
	{
		float len = Mathf.Max(20f, Item.Range);
		float[] angles = AnglesRad();
		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy e || !IsInstanceValid(e)) continue;
			Vector2 rel = e.GlobalPosition - GlobalPosition;
			float dist = rel.Length();
			if (dist > len + HitHalfWidth) continue;

			foreach (float ang in angles)
			{
				Vector2 dir = Vector2.Right.Rotated(ang);
				float along = rel.Dot(dir);
				if (along < -HitHalfWidth || along > len) continue;
				float perp = Mathf.Abs(rel.Dot(new Vector2(-dir.Y, dir.X)));
				if (perp > HitHalfWidth) continue;
				e.TakeDamage(Item.Damage);
				if (Item.SlowFactor < 1f) e.ApplySlow(Item.SlowFactor, 0.6f);
				break; // 同一 tick 内一只怪只吃一条射线
			}
		}
	}

	public override void _Draw()
	{
		if (Item == null || _activeLeft <= 0f) return;
		float len = Mathf.Max(20f, Item.Range);
		// 结束前淡出，给出「即将收回」的读数
		float alpha = Mathf.Clamp(_activeLeft / 0.25f, 0.35f, 1f);
		var tint = new Color(1f, 1f, 1f, alpha);

		foreach (float ang in AnglesRad())
		{
			if (_tex != null)
			{
				DrawSetTransform(Vector2.Zero, ang, Vector2.One);
				DrawTextureRect(_tex, new Rect2(0, -DrawThickness / 2f, len, DrawThickness), false, tint);
				DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
			}
			else
			{
				Vector2 end = Vector2.Right.Rotated(ang) * len;
				DrawLine(Vector2.Zero, end, new Color(0.7f, 0.4f, 1f, 0.85f * alpha), 4f);
			}
		}
	}
}
