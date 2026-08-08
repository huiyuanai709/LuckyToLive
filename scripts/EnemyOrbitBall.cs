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

	public override void _Ready() => QueueRedraw();

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

		// 球体贴图/色块静态；位置变化不需要每帧 _Draw
	}

	public override void _Draw()
	{
		// 紫晶刺球：菱形核 + 尖刺，避免再画成双圆史莱姆
		var core = new Color(0.72f, 0.35f, 1f, 0.95f);
		var glow = new Color(0.95f, 0.7f, 1f, 0.55f);
		var tip = new Color(0.55f, 0.2f, 0.95f, 0.9f);
		DrawCircle(Vector2.Zero, 11f, glow);
		DrawColoredPolygon(new[]
		{
			new Vector2(0, -10), new Vector2(8, 0), new Vector2(0, 10), new Vector2(-8, 0),
		}, core);
		for (int i = 0; i < 6; i++)
		{
			float a = i * Mathf.Tau / 6f + Angle * 0.35f;
			var dir = Vector2.Right.Rotated(a);
			DrawLine(dir * 6f, dir * 15f, tip, 2.2f);
		}
		DrawCircle(new Vector2(-2, -2), 2.2f, new Color(1f, 0.9f, 1f, 0.85f));
	}
}
