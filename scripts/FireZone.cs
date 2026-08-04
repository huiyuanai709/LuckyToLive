using Godot;

/// <summary>
/// 精英「脚下火」：先在落点示警，再燃烧一段时间，可躲开。
/// </summary>
public partial class FireZone : Node2D
{
	public float Radius = 54f;
	public float Damage = 7f;
	public float Telegraph = 0.75f;
	public float BurnDuration = 2.4f;
	public float TickInterval = 0.35f;

	private float _age;
	private float _tickCd;
	private bool _burning;

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_age += dt;

		if (!_burning)
		{
			if (_age >= Telegraph)
			{
				_burning = true;
				_tickCd = 0f;
			}
		}
		else
		{
			_tickCd -= dt;
			if (_tickCd <= 0f)
			{
				_tickCd = TickInterval;
				var hero = GetTree().GetFirstNodeInGroup("hero") as Hero;
				if (hero != null && IsInstanceValid(hero)
					&& GlobalPosition.DistanceTo(hero.GlobalPosition) <= Radius + 10f)
				{
					hero.TakeDamage(Damage);
				}
			}

			if (_age >= Telegraph + BurnDuration)
			{
				QueueFree();
				return;
			}
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_burning)
		{
			// 示警：空心圈 + 半透明底，给玩家反应时间
			float pulse = 0.55f + 0.35f * Mathf.Sin(_age * 14f);
			DrawCircle(Vector2.Zero, Radius, new Color(1f, 0.35f, 0.1f, 0.18f * pulse));
			DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 28, new Color(1f, 0.55f, 0.15f, 0.9f), 3f);
			DrawArc(Vector2.Zero, Radius * 0.55f, 0f, Mathf.Tau, 20, new Color(1f, 0.8f, 0.2f, 0.5f), 1.5f);
			return;
		}

		float lifeLeft = (Telegraph + BurnDuration) - _age;
		float fade = Mathf.Clamp(lifeLeft / 0.6f, 0.35f, 1f);
		float flicker = 0.85f + 0.15f * Mathf.Sin(_age * 18f);
		DrawCircle(Vector2.Zero, Radius, new Color(1f, 0.25f, 0.05f, 0.42f * fade * flicker));
		DrawCircle(Vector2.Zero, Radius * 0.62f, new Color(1f, 0.55f, 0.1f, 0.55f * fade));
		DrawCircle(Vector2.Zero, Radius * 0.28f, new Color(1f, 0.9f, 0.35f, 0.7f * fade));
	}
}
