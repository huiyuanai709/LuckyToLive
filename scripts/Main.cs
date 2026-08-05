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
	private MapTheme _mapTheme = MapCatalog.Island;
	private readonly RandomNumberGeneratorRng _rng = new();

	private int _goalMinute = 1;
	private bool _goalDone;
	private int _elitesThisMinute;
	private float _minuteMark;

	public override void _Ready()
	{
		// 树木等装饰与角色按 Y 排序遮挡；UI 为 CanvasLayer，不受影响
		YSortEnabled = true;
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
		if (I18n.Instance != null)
			I18n.Instance.LocaleChanged -= OnLocaleChanged;
		FloatingText.ClearAll();
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
		_mapTheme = MapCatalog.Get(Game.Instance?.SelectedMap ?? MapId.Island);
	}

	private void StartRun(HeroId heroId)
	{
		ClearWorld();
		FloatingText.Prewarm(300);
		Game.Instance.ResetRunStats();
		_mapTheme = MapCatalog.Get(Game.Instance.SelectedMap);
		_timeLeft = Game.RunDuration;
		_goalMinute = 1;
		_goalDone = false;
		_elitesThisMinute = 0;
		_minuteMark = 60f;

		// 岛背景 + 环境装饰（树木 / 草丛 / 岩石等）
		QueueRedraw();
		IslandDecor.Spawn(this, IslandRect, _mapTheme);

		_hero = new Hero();
		AddChild(_hero);
		_hero.GlobalPosition = IslandRect.GetCenter();
		_hero.Setup(heroId);
		_hero.Died += OnHeroDied;
		_hero.HpChanged += (hp, max) =>
		{
			if (_hud != null) _hud.HpLabel.Text = I18n.T("ui.hud.hp", $"{hp:0}", $"{max:0}");
		};
		_hero.XpChanged += (lv, xp, need) =>
		{
			if (_hud != null) _hud.XpLabel.Text = I18n.T("ui.hud.xp", lv, $"{xp:0}", $"{need:0}");
		};

		_cam = new Camera2D { PositionSmoothingEnabled = true, PositionSmoothingSpeed = 8 };
		_hero.AddChild(_cam);
		_cam.MakeCurrent();

		_spawner = new SpawnDirector { World = this, Island = IslandRect };
		AddChild(_spawner);
		_spawner.EliteSpawned += OnEliteSpawned;
		_spawner.MinuteEventFired += m =>
		{
			_hud.MsgLabel.Text = I18n.T("ui.hud.elite_wave", m);
		};

		_hud = new Hud();
		AddChild(_hud);
		_hud.AdPressed += OnAdPressed;
		_hud.RefreshSlots(_hero.Loadout);
		_hud.HpLabel.Text = I18n.T("ui.hud.hp", $"{_hero.Hp:0}", $"{_hero.MaxHp:0}");
		if (I18n.Instance != null)
			I18n.Instance.LocaleChanged += OnLocaleChanged;

		// 开局选卡（暂停）
		string starterId = CardCatalog.StarterCardId(heroId);
		var starter = CardCatalog.Get(starterId);
		OpenCardPick(I18n.T("ui.card.starter"), new List<CardDef> { starter }, forcedSingle: true, afterStarter: true);
	}

	private void OnLocaleChanged(string _)
	{
		if (_ended || _hero == null || _hud == null) return;
		_hud.HpLabel.Text = I18n.T("ui.hud.hp", $"{_hero.Hp:0}", $"{_hero.MaxHp:0}");
		_hud.XpLabel.Text = I18n.T("ui.hud.xp", _hero.Level, $"{_hero.Xp:0}", $"{_hero.XpToNext():0}");
		_hud.RefreshSlots(_hero.Loadout);
		_hud.SetTime(_timeLeft);
	}

	private void OnAdPressed()
	{
		if (Game.Instance.TryUnlockAdSlot())
		{
			_hud.MsgLabel.Text = I18n.T("ui.hud.ad_gained");
			_hud.RefreshSlots(_hero.Loadout);
		}
	}

	private void OnEliteSpawned(Enemy elite)
	{
		elite.Died += OnEnemyDied;
	}

	public override void _Draw()
	{
		DrawOceanAndIsland();
	}

	private void DrawOceanAndIsland()
	{
		var theme = _mapTheme ?? MapCatalog.Get(Game.Instance?.SelectedMap ?? MapId.Island);

		// 海域：铺开到岛屿外足够大，镜头跟随时仍可见水面
		var ocean = IslandRect.Grow(900);
		DrawRect(ocean, theme.Ocean);
		// 近岸浅水环
		DrawRect(IslandRect.Grow(36), theme.Shallow);

		// 沙滩 / 岸线
		var shoreTex = EnvironmentArt.Load(theme.ShoreGround);
		if (shoreTex != null)
			DrawTiled(shoreTex, IslandRect);
		else
			DrawRect(IslandRect, theme.ShoreFallback);

		// 内陆主体（内缩露出岸边）
		var inlandRect = IslandRect.Grow(-theme.InlandInset);
		var inlandTex = EnvironmentArt.Load(theme.InlandGround);
		if (inlandTex != null)
			DrawTiled(inlandTex, inlandRect);
		else
			DrawRect(inlandRect, theme.InlandFallback);

		// 地表斑块：与主题障碍阵列对齐，强化阻隔物可读性
		var patchTex = EnvironmentArt.Load(theme.PatchGround);
		var dirtPatches = theme.GroundPatches is { Length: > 0 }
			? theme.GroundPatches
			: new[]
			{
				new Rect2(600, 400, 140, 100),
				new Rect2(1400, 880, 180, 90),
				new Rect2(980, 580, 120, 160),
			};
		foreach (var r in dirtPatches)
		{
			if (patchTex != null) DrawTiled(patchTex, r);
			else DrawRect(r, theme.ShoreFallback.Darkened(0.15f));
		}

		// 岛屿描边
		DrawRect(IslandRect, theme.Border, false, 3);
		DrawRect(IslandRect.Grow(2), theme.BorderGlow, false, 1.5f);
	}

	private void DrawTiled(Texture2D tex, Rect2 area)
	{
		int tw = tex.GetWidth();
		int th = tex.GetHeight();
		if (tw <= 0 || th <= 0) return;

		float startX = area.Position.X;
		float startY = area.Position.Y;
		float endX = area.End.X;
		float endY = area.End.Y;

		for (float y = startY; y < endY; y += th)
		{
			for (float x = startX; x < endX; x += tw)
			{
				float w = Mathf.Min(tw, endX - x);
				float h = Mathf.Min(th, endY - y);
				var dst = new Rect2(x, y, w, h);
				var src = new Rect2(0, 0, w, h);
				DrawTextureRectRegion(tex, dst, src);
			}
		}
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
				? I18n.T("ui.hud.goal_minute", _elitesThisMinute)
				: I18n.T("ui.hud.goal_complete");
		}
		else
		{
			_hud.GoalLabel.Text = _goalDone
				? I18n.T("ui.hud.goal_done")
				: I18n.T("ui.hud.goal_progress", _elitesThisMinute);
		}

		// 拾取高亮大件
		foreach (var n in GetTree().GetNodesInGroup("big_drops"))
		{
			if (n is BigItemDrop drop && IsInstanceValid(drop) &&
				_hero.GlobalPosition.DistanceTo(drop.GlobalPosition) < 28)
			{
				OpenCardPick(I18n.T("ui.card.elite_drop"), new List<CardDef> { drop.Card }, forcedSingle: true);
				drop.QueueFree();
				break;
			}
		}

		// 升级
		while (_hero.TryLevelUp())
		{
			var opts = CardCatalog.RollOptions(Game.Instance.SelectedHero, _hero.Loadout, 3, _rng);
			if (opts.Count == 0) break;
			OpenCardPick(I18n.T("ui.card.upgrade"), opts);
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
				OpenCardPick(I18n.T("ui.card.bonus"), opts);
				return;
			}

			// 处理连升
			if (_pendingPicks.Count > 0)
			{
				OpenCardPick(I18n.T("ui.card.upgrade"), _pendingPicks.Dequeue());
				return;
			}
			while (_hero.TryLevelUp())
			{
				var opts = CardCatalog.RollOptions(Game.Instance.SelectedHero, _hero.Loadout, 3, _rng);
				if (opts.Count == 0) break;
				OpenCardPick(I18n.T("ui.card.upgrade"), opts);
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
