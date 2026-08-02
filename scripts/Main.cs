using Godot;
using System.Collections.Generic;

public partial class Main : Node2D
{
	public Rect2 IslandRect = new(40, 40, 2320, 1520);

	private Hero _hero;
	private Camera2D _cam;
	private SpawnDirector _spawner;
	private Hud _hud;
	private CardPopup _popup;
	private float _timeLeft;
	private bool _choosing;
	private bool _ended;
	private readonly RandomNumberGeneratorRng _rng = new();

	private int _goalMinute = 1;
	private bool _goalDone;
	private int _elitesThisMinute;
	private float _minuteMark;

	public override void _Ready()
	{
		ShowHeroSelect();
	}

	private void ShowHeroSelect()
	{
		ClearWorld();
		var select = new HeroSelect();
		AddChild(select);
		select.HeroPicked += id =>
		{
			var heroId = (HeroId)id;
			if (Game.Instance.StarterHero == null)
				Game.Instance.ChooseStarter(heroId);
			else if (!Game.Instance.IsHeroUnlocked(heroId))
				return;
			Game.Instance.SelectedHero = heroId;
			select.QueueFree();
			StartRun(heroId);
		};
	}

	private void ClearWorld()
	{
		foreach (var c in GetChildren())
		{
			if (c is Game) continue;
			((Node)c).QueueFree();
		}
		_hero = null;
		_spawner = null;
		_hud = null;
		_popup = null;
		_ended = false;
		_choosing = false;
	}

	private void StartRun(HeroId heroId)
	{
		ClearWorld();
		Game.Instance.ResetRunStats();
		_timeLeft = Game.RunDuration;
		_goalMinute = 1;
		_goalDone = false;
		_elitesThisMinute = 0;
		_minuteMark = 60f;

		// 岛背景
		QueueRedraw();

		_hero = new Hero();
		AddChild(_hero);
		_hero.GlobalPosition = IslandRect.GetCenter();
		_hero.Setup(heroId);
		_hero.Died += OnHeroDied;
		_hero.HpChanged += (hp, max) => { if (_hud != null) _hud.HpLabel.Text = $"生命 {hp:0}/{max:0}"; };
		_hero.XpChanged += (lv, xp, need) => { if (_hud != null) _hud.XpLabel.Text = $"英雄 Lv{lv}  XP {xp:0}/{need:0}"; };

		_cam = new Camera2D { PositionSmoothingEnabled = true, PositionSmoothingSpeed = 8 };
		_hero.AddChild(_cam);
		_cam.MakeCurrent();

		_spawner = new SpawnDirector { World = this, Island = IslandRect };
		AddChild(_spawner);
		_spawner.EliteSpawned += OnEliteSpawned;
		_spawner.MinuteEventFired += m =>
		{
			_hud.MsgLabel.Text = $"第 {m} 分钟精英冲击！";
		};

		_hud = new Hud();
		AddChild(_hud);
		_hud.AdPressed += OnAdPressed;
		_hud.RefreshSlots(_hero.Loadout);
		_hud.HpLabel.Text = $"生命 {_hero.Hp:0}/{_hero.MaxHp:0}";

		// 开局选卡（暂停）
		string starterId = CardCatalog.StarterCardId(heroId);
		var starter = CardCatalog.Get(starterId);
		OpenCardPick("开局装备", new List<CardDef> { starter }, forcedSingle: true, afterStarter: true);
	}

	private void OnAdPressed()
	{
		if (Game.Instance.TryUnlockAdSlot())
		{
			_hud.MsgLabel.Text = "本局槽位 +1（广告占位）";
			_hud.RefreshSlots(_hero.Loadout);
		}
	}

	private void OnEliteSpawned(Enemy elite)
	{
		elite.Died += OnEnemyDied;
	}

	public override void _Draw()
	{
		DrawRect(IslandRect, new Color(0.18f, 0.28f, 0.2f));
		DrawRect(IslandRect, new Color(0.35f, 0.55f, 0.4f), false, 4);
		// 简单障碍
		DrawRect(new Rect2(600, 400, 120, 80), new Color(0.25f, 0.22f, 0.18f));
		DrawRect(new Rect2(1400, 900, 160, 60), new Color(0.25f, 0.22f, 0.18f));
		DrawRect(new Rect2(1000, 600, 80, 140), new Color(0.25f, 0.22f, 0.18f));
	}

	public override void _Process(double delta)
	{
		if (_hero == null || _ended || _choosing) return;
		float dt = (float)delta;

		// 钳制英雄在岛内
		var p = _hero.GlobalPosition;
		p.X = Mathf.Clamp(p.X, IslandRect.Position.X + 20, IslandRect.End.X - 20);
		p.Y = Mathf.Clamp(p.Y, IslandRect.Position.Y + 20, IslandRect.End.Y - 20);
		_hero.GlobalPosition = p;

		_timeLeft -= dt;
		_hud.SetTime(_timeLeft);
		Game.Instance.HighestItemLevelSum = _hero.Loadout.LevelSum();

		// 分钟目标刷新
		if (_spawner.Elapsed >= _minuteMark)
		{
			if (_goalDone) Game.Instance.MinuteGoalsCompleted += 1;
			_goalMinute += 1;
			_goalDone = false;
			_elitesThisMinute = 0;
			_minuteMark += 60f;
			_hud.GoalLabel.Text = _goalMinute <= 5
				? $"分钟目标: 击杀 1 精英 (本分钟 {_elitesThisMinute})"
				: "分钟目标: 完成";
		}
		else
		{
			_hud.GoalLabel.Text = $"分钟目标: 击杀 1 精英 {( _goalDone ? "完成" : $"({_elitesThisMinute}/1)")}";
		}

		// 拾取高亮大件
		foreach (var n in GetTree().GetNodesInGroup("big_drops"))
		{
			if (n is BigItemDrop drop && IsInstanceValid(drop) &&
				_hero.GlobalPosition.DistanceTo(drop.GlobalPosition) < 28)
			{
				OpenCardPick("精英掉落", new List<CardDef> { drop.Card }, forcedSingle: true);
				drop.QueueFree();
				break;
			}
		}

		// 升级
		while (_hero.TryLevelUp())
		{
			var opts = CardCatalog.RollOptions(Game.Instance.SelectedHero, _hero.Loadout, 3, _rng);
			if (opts.Count == 0) break;
			OpenCardPick("升级选择", opts);
			break; // OpenCardPick pauses; next levels after resume via queue
		}

		if (_timeLeft <= 0)
			EndRun(true);
	}

	private readonly Queue<List<CardDef>> _pendingPicks = new();

	private void OpenCardPick(string title, List<CardDef> options, bool forcedSingle = false, bool afterStarter = false)
	{
		if (options == null || options.Count == 0) return;
		if (_choosing)
		{
			_pendingPicks.Enqueue(options);
			return;
		}
		_choosing = true;
		GetTree().Paused = true;
		_popup = new CardPopup();
		AddChild(_popup);
		_popup.Setup(title, options);
		_popup.Chosen += id =>
		{
			var card = CardCatalog.Get(id);
			if (card != null)
			{
				_hero.Loadout.ApplyCard(card, _hero, this);
				_hud.RefreshSlots(_hero.Loadout);
			}
			_popup.QueueFree();
			_popup = null;
			_choosing = false;
			GetTree().Paused = false;

			if (afterStarter)
			{
				// 开局后再给一次三选一扩构筑
				var opts = CardCatalog.RollOptions(Game.Instance.SelectedHero, _hero.Loadout, 3, _rng);
				OpenCardPick("额外起步卡", opts);
				return;
			}

			// 处理连升
			if (_pendingPicks.Count > 0)
			{
				OpenCardPick("升级选择", _pendingPicks.Dequeue());
				return;
			}
			while (_hero.TryLevelUp())
			{
				var opts = CardCatalog.RollOptions(Game.Instance.SelectedHero, _hero.Loadout, 3, _rng);
				if (opts.Count == 0) break;
				OpenCardPick("升级选择", opts);
				return;
			}
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_hero == null || _ended) return;
		foreach (var n in GetTree().GetNodesInGroup("enemies"))
		{
			if (n is Enemy e && IsInstanceValid(e) && !e.HasMeta("wired"))
			{
				e.SetMeta("wired", true);
				e.Died += OnEnemyDied;
			}
		}
	}

	private void OnEnemyDied(Enemy enemy)
	{
		if (_ended || _hero == null) return;
		Game.Instance.KillCount += 1;
		_hero.AddXp(enemy.XpValue);
		if (enemy.IsElite)
		{
			Game.Instance.EliteKills += 1;
			_elitesThisMinute += 1;
			_goalDone = true;
			SpawnBigDrop(enemy.GlobalPosition);
		}
	}

	private void SpawnBigDrop(Vector2 pos)
	{
		var opts = CardCatalog.RollOptions(Game.Instance.SelectedHero, _hero.Loadout, 6, _rng);
		if (opts.Count == 0) return;
		var drop = new BigItemDrop { Card = opts[0] };
		AddChild(drop);
		drop.GlobalPosition = pos;
	}

	private void OnHeroDied()
	{
		EndRun(false);
	}

	private void EndRun(bool victory)
	{
		if (_ended) return;
		_ended = true;
		Game.Instance.RunActive = false;
		GetTree().Paused = false;

		int score = Game.Instance.KillCount * 2
			+ Game.Instance.EliteKills * 25
			+ Game.Instance.MinuteGoalsCompleted * 40
			+ (int)(Game.Instance.HighestItemLevelSum * 8)
			+ (victory ? (int)(_hero.Hp / _hero.MaxHp * 50) : 0);

		string rank = score >= 600 ? "S" : score >= 400 ? "A" : score >= 220 ? "B" : "C";
		int gain = rank switch { "S" => 4, "A" => 3, "B" => 2, _ => 1 };
		if (!victory) gain = Mathf.Max(1, gain - 1);
		Game.Instance.AddMetaFromScore(gain);

		var result = new ResultScreen();
		AddChild(result);
		result.Setup(victory, score, rank, gain, Game.Instance.KillCount, Game.Instance.EliteKills, Game.Instance.MinuteGoalsCompleted);
		result.ContinuePressed += () =>
		{
			result.QueueFree();
			ShowHeroSelect();
		};
	}
}
