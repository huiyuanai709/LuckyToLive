using Godot;

/// <summary>
/// 精英「旋转球」：绕精英旋转，碰到英雄造成伤害，可走位躲开。
/// </summary>
public partial class EnemyOrbitBall : Node2D
{
	public Enemy OwnerEnemy;
	public float Angle;
	public float OrbitRadius = 96f;
	public float SpinSpeed = 2.6f;
	public float Damage = 9f;
	public float HitRadius = 16f;

	private float _hitCd;

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (OwnerEnemy == null || !IsInstanceValid(OwnerEnemy))
		{
			QueueFree();
			return;
		}

		Angle += SpinSpeed * dt;
		GlobalPosition = OwnerEnemy.GlobalPosition + Vector2.Right.Rotated(Angle) * OrbitRadius;

		_hitCd -= dt;
		var hero = GetTree().GetFirstNodeInGroup("hero") as Hero;
		if (hero != null && IsInstanceValid(hero) && _hitCd <= 0f
			&& GlobalPosition.DistanceTo(hero.GlobalPosition) <= HitRadius + 14f)
		{
			_hitCd = 0.55f;
			hero.TakeDamage(Damage);
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		// 外环 + 内核，远距离也容易辨认旋转轨迹
		DrawCircle(Vector2.Zero, 12f, new Color(0.55f, 0.2f, 0.95f, 0.85f));
		DrawCircle(Vector2.Zero, 7f, new Color(0.95f, 0.65f, 1f, 0.95f));
		DrawArc(Vector2.Zero, 14f, 0f, Mathf.Tau, 16, new Color(0.8f, 0.4f, 1f, 0.45f), 2f);
	}
}
