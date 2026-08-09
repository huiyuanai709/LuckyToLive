using Godot;

/// <summary>
/// 可踩踏的地形区：草丛 / 灌丛减速。不阻挡碰撞，只改移速。
/// </summary>
public partial class TerrainBrush : Node2D
{
	public float Radius = 54f;
	/// <summary>敌人移速倍率。</summary>
	public float EnemySlow = 0.62f;
	/// <summary>英雄移速倍率（轻减速，保留走位空间）。</summary>
	public float HeroSlow = 0.88f;
	public Color Tint = new(0.35f, 0.7f, 0.35f, 0.22f);

	public override void _Ready()
	{
		AddToGroup("terrain_brush");
		ZIndex = -2;
		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, Radius, Tint);
		DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 28, new Color(Tint.R, Tint.G, Tint.B, 0.45f), 1.5f);
	}

	/// <summary>采样点上的移速倍率（多块叠加取最慢）。</summary>
	public static float SampleMul(SceneTree tree, Vector2 worldPos, bool forHero)
	{
		if (tree == null) return 1f;
		float mul = 1f;
		foreach (var n in tree.GetNodesInGroup("terrain_brush"))
		{
			if (n is not TerrainBrush brush || !GodotObject.IsInstanceValid(brush)) continue;
			if (worldPos.DistanceTo(brush.GlobalPosition) > brush.Radius) continue;
			float local = forHero ? brush.HeroSlow : brush.EnemySlow;
			mul = Mathf.Min(mul, local);
		}
		return mul;
	}
}
