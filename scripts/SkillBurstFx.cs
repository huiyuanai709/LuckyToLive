using Godot;

/// <summary>局内主动技能释放时的短时视觉环（不参与碰撞）。</summary>
public partial class SkillBurstFx : Node2D
{
	public float Radius = 80f;
	public Color RingColor = new(0.6f, 0.85f, 1f, 0.85f);
	public float Life = 0.35f;

	private float _age;

	public override void _Process(double delta)
	{
		_age += (float)delta;
		if (_age >= Life)
		{
			QueueFree();
			return;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		float t = Mathf.Clamp(_age / Life, 0f, 1f);
		float r = Radius * (0.55f + 0.55f * t);
		float a = 1f - t;
		var fill = new Color(RingColor.R, RingColor.G, RingColor.B, 0.18f * a);
		var ring = new Color(RingColor.R, RingColor.G, RingColor.B, 0.85f * a);
		DrawCircle(Vector2.Zero, r, fill);
		DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 36, ring, 3.5f);
	}
}
