using Godot;

public partial class Pet : CharacterBody2D
{
	public SlotItem Item;
	public Hero OwnerHero;
	private float _cd;
	private float _packLeapCd;

	public static Pet Spawn(Node2D world, Hero hero, SlotItem item)
	{
		var pet = new Pet();
		world.AddChild(pet);
		pet.OwnerHero = hero;
		pet.Item = item;
		pet.GlobalPosition = hero.GlobalPosition + new Vector2(-30, 20);
		return pet;
	}

	public void ApplyItem(SlotItem item) => Item = item;

	/// <summary>围猎号令：陷阱触发时瞬移扑咬（CD 1.2s）。</summary>
	public void TryPackLeap(Enemy target)
	{
		if (target == null || !IsInstanceValid(target) || Item == null) return;
		if (_packLeapCd > 0f) return;
		_packLeapCd = 1.2f;
		GlobalPosition = target.GlobalPosition;
		target.TakeDamage(Item.Damage);
	}

	public override void _Ready()
	{
		AddToGroup("pets");
		var shape = new CollisionShape2D();
		shape.Shape = new CircleShape2D { Radius = 10 };
		AddChild(shape);
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (_packLeapCd > 0f) _packLeapCd -= dt;
		if (OwnerHero == null || !IsInstanceValid(OwnerHero)) { QueueFree(); return; }

		Enemy target = null;
		float best = 220f;
		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is not Enemy e || !IsInstanceValid(e)) continue;
			float d = GlobalPosition.DistanceTo(e.GlobalPosition);
			if (d < best) { best = d; target = e; }
		}

		Vector2 dest = target != null
			? target.GlobalPosition
			: OwnerHero.GlobalPosition + new Vector2(-28, 18);

		Vector2 dir = dest - GlobalPosition;
		if (dir.Length() > 12f)
		{
			Velocity = dir.Normalized() * 210f;
			MoveAndSlide();
		}

		_cd -= dt;
		if (target != null && best <= Item.Range + target.BodyRadius && _cd <= 0)
		{
			_cd = 1f / Mathf.Max(0.2f, Item.FireRate);
			target.TakeDamage(Item.Damage);
		}
	}

	public override void _Draw()
	{
		DrawCircle(new Vector2(0, -4), 12, new Color(0.75f, 0.55f, 0.35f));
		DrawCircle(new Vector2(-4, -8), 3, Colors.Black);
		DrawCircle(new Vector2(4, -8), 3, Colors.Black);
	}
}
