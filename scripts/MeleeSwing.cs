using Godot;

/// <summary>
/// 近战攻击的可见挥砍表现：裂斩(slash) 沿朝向窄弧扫光，冲锋刃(charge) 是更宽的
/// 圆形冲击波。纯 _Draw 自淡出，无贴图依赖，风格与 Enemy/Hero 的程序化 _Draw 一致。
/// </summary>
public partial class MeleeSwing : Node2D
{
	private const float ConeHalfAngleDeg = 50f;

	private bool _heavy;
	private Vector2 _dir = Vector2.Right;
	private float _range = 60f;
	private float _life;
	private float _maxLife;
	private Color _color;

	public void Setup(string weaponStyle, Vector2 dir, float range, bool heavy)
	{
		_heavy = heavy;
		_dir = dir.LengthSquared() > 0.0001f ? dir.Normalized() : Vector2.Right;
		_range = range;
		_maxLife = heavy ? 0.22f : 0.14f;
		_life = _maxLife;
		_color = heavy ? new Color(1f, 0.55f, 0.25f) : new Color(0.9f, 0.95f, 1f);
		ZIndex = 40;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		_life -= (float)delta;
		if (_life <= 0f)
		{
			QueueFree();
			return;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		float t = 1f - Mathf.Clamp(_life / _maxLife, 0f, 1f);
		float alpha = 1f - t;
		float baseAngle = _dir.Angle();

		if (_heavy)
		{
			// 冲锋刃：全周冲击波环，随时间外扩淡出
			float r = _range * (0.55f + t * 0.6f);
			DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 28, new Color(_color, alpha * 0.55f), 6f);
			DrawArc(Vector2.Zero, r * 0.7f, 0f, Mathf.Tau, 24, new Color(_color, alpha * 0.35f), 3f);
		}
		else
		{
			// 裂斩：沿朝向 ±ConeHalfAngleDeg 的扇形斩击轨迹，与收窄后的命中判定一致
			float half = Mathf.DegToRad(ConeHalfAngleDeg);
			int segs = 14;
			var pts = new Vector2[segs + 2];
			pts[0] = Vector2.Zero;
			for (int i = 0; i <= segs; i++)
			{
				float a = baseAngle - half + 2f * half * (i / (float)segs);
				pts[i + 1] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * _range * (0.85f + t * 0.25f);
			}
			var fill = new Color(_color, alpha * 0.7f);
			var colors = new Color[pts.Length];
			for (int i = 0; i < colors.Length; i++) colors[i] = fill;
			DrawPolygon(pts, colors);
			DrawArc(Vector2.Zero, _range, baseAngle - half, baseAngle + half, segs, new Color(_color, alpha), 3f);
		}
	}
}
