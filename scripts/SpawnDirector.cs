using Godot;

public partial class SpawnDirector : Node
{
	public Rect2 Island;
	public Node2D World;
	public float Elapsed;

	private float _spawnCd = 1.2f;
	private float _density = 1f;
	private int _lastMinuteEvent = -1;
	private float _bonusEliteCd = 40f;
	private readonly RandomNumberGeneratorRng _rng = new();
	private static readonly string[] Affixes = { "冲刺", "护盾", "召唤", "远程" };

	[Signal] public delegate void EliteSpawnedEventHandler(Enemy elite);
	[Signal] public delegate void MinuteEventFiredEventHandler(int minute);

	public override void _Process(double delta)
	{
		if (World == null || !Game.Instance.RunActive) return;
		float dt = (float)delta;
		Elapsed += dt;
		_density = 1f + Elapsed / 60f * 0.85f;
		if (Elapsed > 240f) _density += 1.2f;

		_spawnCd -= dt;
		if (_spawnCd <= 0)
		{
			_spawnCd = Mathf.Max(0.35f, 1.3f / _density);
			SpawnBasic();
		}

		_bonusEliteCd -= dt;
		if (_bonusEliteCd <= 0)
		{
			_bonusEliteCd = 50f;
			SpawnElite();
		}

		int minute = (int)(Elapsed / 60f);
		if (minute >= 1 && minute <= 4 && minute != _lastMinuteEvent && Elapsed >= minute * 60f)
		{
			_lastMinuteEvent = minute;
			SpawnEliteWave(1 + minute / 2);
			EmitSignal(SignalName.MinuteEventFired, minute);
		}

		if (Elapsed >= 270f && Elapsed < 271f)
			SpawnEliteWave(4);
	}

	private void SpawnBasic()
	{
		var e = new Enemy();
		World.AddChild(e);
		e.GlobalPosition = EdgePoint();
		float hpMul = 1f + Elapsed / 80f;
		e.ConfigureBasic(hpMul, 1f + Elapsed / 400f);
		if (e.Affix == "" && _rng.Randf() < 0.05f) { } // keep simple
		Wire(e);
	}

	public Enemy SpawnElite()
	{
		var e = new Enemy();
		World.AddChild(e);
		e.GlobalPosition = EdgePoint();
		string affix = Affixes[_rng.RandiRange(0, Affixes.Length - 1)];
		e.ConfigureElite(1f + Elapsed / 100f, affix);
		// 远程词条：提高接触伤害伪装为压迫（简化）
		if (affix == "远程") e.ContactDamage = 10f;
		Wire(e);
		EmitSignal(SignalName.EliteSpawned, e);
		return e;
	}

	private void SpawnEliteWave(int count)
	{
		for (int i = 0; i < count; i++) SpawnElite();
	}

	private void Wire(Enemy e)
	{
		e.Died += enemy =>
		{
			// Main listens via group/callback separately
		};
	}

	private Vector2 EdgePoint()
	{
		int side = _rng.RandiRange(0, 3);
		float x = Island.Position.X;
		float y = Island.Position.Y;
		float w = Island.Size.X;
		float h = Island.Size.Y;
		return side switch
		{
			0 => new Vector2(x + _rng.Randf() * w, y + 10),
			1 => new Vector2(x + _rng.Randf() * w, y + h - 10),
			2 => new Vector2(x + 10, y + _rng.Randf() * h),
			_ => new Vector2(x + w - 10, y + _rng.Randf() * h),
		};
	}
}
