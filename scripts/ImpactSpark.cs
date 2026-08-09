using Godot;

/// <summary>
/// 命中火花：从命中点向外炸开的短促放射线 + 芯部亮点，纯 _Draw 自淡出。
/// 与 MeleeSwing 同款写法，不依赖粒子材质/贴图，是命中反馈里最基础的一层视觉。
/// </summary>
public partial class ImpactSpark : Node2D
{
	private Color _color = Colors.White;
	private float _life;
	private float _maxLife;
	private float _radius = 16f;
	private const int Rays = 6;

	public void Setup(Color color, float scale)
	{
		_color = color;
		_maxLife = 0.22f;
		_life = _maxLife;
		_radius = 16f * Mathf.Clamp(scale, 0.5f, 2.8f);
		ZIndex = 55;
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
		float r = _radius * (0.35f + t * 1.2f);
		for (int i = 0; i < Rays; i++)
		{
			float a = Mathf.Tau * i / Rays + t * 0.5f;
			var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
			DrawLine(dir * r * 0.3f, dir * r, new Color(_color, alpha), 2.8f);
		}
		DrawCircle(Vector2.Zero, r * 0.32f, new Color(_color, alpha * 0.85f));
	}
}
