using Godot;
using System;

public partial class SpawnDirector : Node
{
	public Rect2 Island;
	public Node2D World;
	public float Elapsed;
	/// <summary>新怪生成时回调（用于 Main 接线 Died 等，避免每帧扫 group）。</summary>
	public Action<Enemy> EnemySpawned;

	/// <summary>当前局刷怪器；召唤物等旁路生成时用来补接线。</summary>
	public static SpawnDirector Active { get; private set; }

	/// <summary>场上存活敌人软上限；超过后只补精英波，停普通刷怪。</summary>
	public const int MaxAliveEnemies = 64;

	private float _spawnCd = 1.2f;
	private float _density = 1f;
	private int _lastMinuteEvent = -1;
	private int _pendingEliteWave;
	private float _pendingEliteCd;
	private float _basicSuppressLeft;
	private bool _tideSpawned;
	private bool _lordSpawned;
	private Enemy _activeBoss;
	private readonly RandomNumberGeneratorRng _rng = new();
	// melee=快攻+冲锋；orbit=旋转球；fire_ground=脚下火（可躲）；shield/summon 保留；
	// berserk=残血越战越勇；splitter=死亡裂成两只弱化分身
	private static readonly string[] Affixes = { "melee", "orbit", "fire_ground", "shield", "summon", "berserk", "splitter" };

	/// <summary>普通怪击杀叠加的精英刷新进度（精英/Boss 不计）。</summary>
	public int KillProgress { get; private set; }

	/// <summary>进度满值；随时间略升，避免后期密度过高时精英过于频繁。</summary>
	public int KillThreshold => Mathf.Max(10, 14 + (int)(Elapsed / 60f) * 3);

	[Signal] public delegate void EliteSpawnedEventHandler(Enemy elite);
	[Signal] public delegate void MinuteEventFiredEventHandler(int minute);
	[Signal] public delegate void BossSpawnedEventHandler(string bossId);
	[Signal] public delegate void EliteProgressChangedEventHandler(int progress, int threshold);
	[Signal] public delegate void EliteChargeReadyEventHandler();

	public override void _EnterTree() => Active = this;

	public override void _ExitTree()
	{
		if (Active == this) Active = null;
	}

	public override void _Process(double delta)
	{
		if (World == null || !Game.Instance.RunActive) return;
		float dt = (float)delta;
		Elapsed += dt;
		if (_basicSuppressLeft > 0f) _basicSuppressLeft -= dt;

		// 后期加密度，但不再在最后一分钟陡增到刷怪爆炸
		_density = 1f + Elapsed / 60f * 0.75f;
		if (Elapsed > 240f) _density += 0.55f;

		bool suppressBasic = _basicSuppressLeft > 0f
			|| (_activeBoss != null && IsInstanceValid(_activeBoss) && _activeBoss.BossId == "island_lord");

		_spawnCd -= dt;
		if (_spawnCd <= 0)
		{
			// 地板 0.5s，避免软件渲染下敌人数把帧率打崩
			_spawnCd = Mathf.Max(0.5f, 1.35f / _density);
			if (!suppressBasic && AliveEnemyCount() < MaxAliveEnemies)
				SpawnBasic();
		}

		int minute = (int)(Elapsed / 60f);
		if (minute >= 1 && minute <= 4 && minute != _lastMinuteEvent && Elapsed >= minute * 60f)
		{
			_lastMinuteEvent = minute;
			// minute 1–2 → 1；3–4 → 2（压力让给 Boss）
			int wave = minute <= 2 ? 1 : 2;
			QueueEliteWave(wave);
			EmitSignal(SignalName.MinuteEventFired, minute);
		}

		// 2:30 潮汐守卫；4:30 岛主（取代旧 270s 四精英波）
		if (!_tideSpawned && Elapsed >= 150f)
		{
			_tideSpawned = true;
			_basicSuppressLeft = Mathf.Max(_basicSuppressLeft, 8f);
			SpawnBoss("tide_guard");
		}
		if (!_lordSpawned && Elapsed >= 270f)
		{
			_lordSpawned = true;
			SpawnBoss("island_lord");
		}

		// 精英波错峰生成，避免同一帧塞进多个大体积精英
		if (_pendingEliteWave > 0)
		{
			_pendingEliteCd -= dt;
			if (_pendingEliteCd <= 0f)
			{
				_pendingEliteCd = 0.55f;
				_pendingEliteWave--;
				SpawnElite();
			}
		}

		if (_activeBoss != null && !IsInstanceValid(_activeBoss))
			_activeBoss = null;
	}

	private int AliveEnemyCount()
	{
		if (World == null) return 0;
		return World.GetTree().GetNodesInGroup("enemies").Count;
	}

	/// <summary>
	/// 击杀普通怪叠精英进度；满则刷新一只精英（溢出可连刷）。
	/// 分钟波 / Boss 时间轴不变。
	/// </summary>
	public void RegisterKill(Enemy enemy)
	{
		if (enemy == null || enemy.IsElite || enemy.IsBoss) return;
		KillProgress += 1;
		bool charged = TrySpawnFromKillProgress();
		EmitSignal(SignalName.EliteProgressChanged, KillProgress, KillThreshold);
		if (charged)
			EmitSignal(SignalName.EliteChargeReady);
	}

	private bool TrySpawnFromKillProgress()
	{
		int guard = 8;
		bool fired = false;
		while (KillProgress >= KillThreshold && guard-- > 0)
		{
			KillProgress -= KillThreshold;
			fired = true;
			if (AliveEnemyCount() < MaxAliveEnemies + 8)
				SpawnElite();
			else
				QueueEliteWave(1);
		}
		return fired;
	}

	private void SpawnBasic()
	{
		var e = new Enemy();
		World.AddChild(e);
		e.GlobalPosition = EdgePoint();
		float hpMul = 1f + Elapsed / 80f;
		e.ConfigureBasic(hpMul, 1f + Elapsed / 400f);
		Wire(e);
	}

	public Enemy SpawnElite()
	{
		var e = new Enemy();
		World.AddChild(e);
		e.GlobalPosition = EdgePoint();
		string affix = Affixes[_rng.RandiRange(0, Affixes.Length - 1)];
		e.ConfigureElite(1f + Elapsed / 100f, affix);
		Wire(e);
		EmitSignal(SignalName.EliteSpawned, e);
		return e;
	}

	public Enemy SpawnBoss(string bossId)
	{
		if (_activeBoss != null && IsInstanceValid(_activeBoss))
			return _activeBoss;

		var e = new Enemy();
		World.AddChild(e);
		e.GlobalPosition = EdgePoint();
		float hpMul = 1f + Elapsed / 120f;
		e.ConfigureBoss(bossId, hpMul);
		_activeBoss = e;
		Wire(e);
		EmitSignal(SignalName.EliteSpawned, e);
		EmitSignal(SignalName.BossSpawned, bossId);
		return e;
	}

	private void QueueEliteWave(int count)
	{
		_pendingEliteWave += count;
		if (_pendingEliteCd > 0.55f)
			_pendingEliteCd = 0.15f;
	}

	public void Register(Enemy e) => EnemySpawned?.Invoke(e);

	private void Wire(Enemy e) => Register(e);

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
