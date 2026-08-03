using Godot;
using System.Collections.Generic;

public partial class Projectile : Node2D
{
	public Enemy Target;
	public float Damage;
	public float Speed = 420f;
	public int PierceLeft = 1;
	public float Splash;
	public float SlowFactor = 1f;
	public float SlowDuration;
	public Color Tint = new(1, 0.9f, 0.3f);
	private readonly HashSet<Enemy> _hit = new();
	private Vector2 _dir = Vector2.Right;
	private float _life = 2.5f;
	private Texture2D _tex;
	private bool _rotates;
	/// <summary>贴图在世界中的目标显示直径（像素）。</summary>
	private float _visualSize = 26f;

	public void Setup(Enemy target, SlotItem item)
	{
		Target = target;
		Damage = item.Damage;
		PierceLeft = Mathf.Max(1, item.Pierce);
		Splash = item.Splash;
		if (item.SlowFactor < 1f)
		{
			SlowFactor = item.SlowFactor;
			SlowDuration = 1.6f + item.Level * 0.2f;
		}
		Tint = item.WeaponStyle switch
		{
			"ice_arrow" => new Color(0.4f, 0.8f, 1f),
			"fireball" => new Color(1f, 0.45f, 0.15f),
			"pierce" => new Color(0.6f, 1f, 0.5f),
			_ => Tint,
		};
		_tex = ProjectileArt.ForProjectile(item);
		_rotates = ProjectileArt.RotatesWithVelocity(item);
		_visualSize = Splash > 0 ? 34f : 26f;
		if (target != null && IsInstanceValid(target))
			_dir = (target.GlobalPosition - GlobalPosition).Normalized();
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_tex == null)
		{
			DrawCircle(Vector2.Zero, Splash > 0 ? 6 : 4, Tint);
			return;
		}
		Vector2 size = _tex.GetSize();
		float scale = _visualSize / Mathf.Max(size.X, size.Y);
		Vector2 draw = size * scale;
		DrawTextureRect(_tex, new Rect2(-draw / 2f, draw), false);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_life -= dt;
		if (_life <= 0) { QueueFree(); return; }

		if (Target != null && IsInstanceValid(Target) && PierceLeft <= 1 && Splash <= 0)
			_dir = (Target.GlobalPosition - GlobalPosition).Normalized();

		GlobalPosition += _dir * Speed * dt;
		// 箭类朝速度方向；火球等近似圆形的保持不旋转
		if (_rotates) Rotation = _dir.Angle();

		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy e || !IsInstanceValid(e) || _hit.Contains(e)) continue;
			if (GlobalPosition.DistanceTo(e.GlobalPosition) > 14f) continue;
			Hit(e);
			if (PierceLeft <= 0) { QueueFree(); return; }
		}
	}

	private void Hit(Enemy e)
	{
		_hit.Add(e);
		if (Splash > 0)
		{
			foreach (var n in GetTree().GetNodesInGroup("enemies"))
			{
				if (n is Enemy other && IsInstanceValid(other) &&
					other.GlobalPosition.DistanceTo(e.GlobalPosition) <= Splash)
				{
					other.TakeDamage(Damage * (other == e ? 1f : 0.65f));
					if (SlowFactor < 1f) other.ApplySlow(SlowFactor, SlowDuration);
				}
			}
			PierceLeft = 0;
			return;
		}

		e.TakeDamage(Damage);
		if (SlowFactor < 1f) e.ApplySlow(SlowFactor, SlowDuration);
		PierceLeft -= 1;
	}
}
