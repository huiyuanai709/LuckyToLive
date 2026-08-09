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
	private bool _tideWarned;

	public override void _Ready()
	{
		// 树木等装饰与角色按 Y 排序遮挡；UI 为 CanvasLayer，不受影响
		YSortEnabled = true;
		ShowHeroSelect();
	}

	private void ShowHeroSelect()
	{
		ClearWorld();
		// 云测 / 无指针环境：LUCKY_AUTOSTART=mage|warrior|hunter 跳过选人
		string auto = OS.GetEnvironment("LUCKY_AUTOSTART");
		if (!string.IsNullOrEmpty(auto))
		{
			var heroId = auto.Trim().ToLowerInvariant() switch
			{
				"mage" or "1" => HeroId.Mage,
				"warrior" or "0" => HeroId.Warrior,
				_ => HeroId.Hunter,
			};
			Game.Instance.SelectedHero = heroId;
			if (Game.Instance.StarterHero == null)
				Game.Instance.ChooseStarter(heroId);
			CallDeferred(MethodName.StartRun, (int)heroId);
			return;
		}
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
		_tideWarned = false;

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
		_hero.FrenzyChanged += (streak, mul) =>
		{
			if (_hud == null || _hero == null) return;
			int tipAt = Mathf.Max(1, 3 - _hero.FrenzyThresholdBonus);
			_hud.MsgLabel.Text = streak >= tipAt
				? I18n.T("ui.hud.frenzy", streak, $"{mul:0.00}")
				: "";
		};

		_cam = new Camera2D { PositionSmoothingEnabled = true, PositionSmoothingSpeed = 8 };
		_hero.AddChild(_cam);
		_cam.MakeCurrent();

		_spawner = new SpawnDirector { World = this, Island = IslandRect };
		AddChild(_spawner);
		_spawner.EnemySpawned = WireEnemy;
		_spawner.MinuteEventFired += m =>
		{
			_hud.MsgLabel.Text = I18n.T("ui.hud.elite_wave", m);
		};
		_spawner.BossSpawned += bossId =>
		{
			if (_hud == null) return;
			_hud.MsgLabel.Text = I18n.T("ui.hud.boss_spawn", I18n.BossName(bossId));
		};
		_spawner.EliteProgressChanged += (prog, need) =>
		{
			_hud?.SetEliteProgress(prog, need);
		};
		_spawner.EliteChargeReady += () =>
		{
			if (_hud == null) return;
			_hud.MsgLabel.Text = I18n.T("ui.hud.elite_ready");
		};
		_spawner.EliteSpawned += _ =>
		{
			if (_hud == null || _spawner == null) return;
			_hud.SetEliteProgress(_spawner.KillProgress, _spawner.KillThreshold);
		};

		_hud = new Hud();
		AddChild(_hud);
		_hud.AdPressed += OnAdPressed;
		_hud.RefreshSlots(_hero.Loadout);
		_hud.HpLabel.Text = I18n.T("ui.hud.hp", $"{_hero.Hp:0}", $"{_hero.MaxHp:0}");
		_hud.SetEliteProgress(_spawner.KillProgress, _spawner.KillThreshold);
		if (I18n.Instance != null)
			I18n.Instance.LocaleChanged += OnLocaleChanged;

		// 开局选卡（暂停）；自动开局时直接发牌，方便无指针环境验证
		string starterId = CardCatalog.StarterCardId(heroId);
		var starter = CardCatalog.Get(starterId);
		if (!string.IsNullOrEmpty(OS.GetEnvironment("LUCKY_AUTOSTART")))
		{
			if (starter != null)
				_hero.Loadout.ApplyCard(starter, _hero, this);
			var bonus = CardCatalog.RollOptions(heroId, _hero.Loadout, 1, _rng);
			if (bonus.Count > 0)
				_hero.Loadout.ApplyCard(bonus[0], _hero, this);
			// 云测可视化：强制给一座建筑 + 一只宝箱，方便截图验收
			if (!string.IsNullOrEmpty(OS.GetEnvironment("LUCKY_DEBUG_VISUAL")))
				SpawnDebugVisuals(heroId);
			_hud.RefreshSlots(_hero.Loadout);
			return;
		}
		OpenCardPick(I18n.T("ui.card.starter"), new List<CardDef> { starter }, forcedSingle: true, afterStarter: true);
	}

	private void SpawnDebugVisuals(HeroId heroId)
	{
		string buildId = heroId switch
		{
			HeroId.Warrior => "w_turret",
			HeroId.Hunter => "h_camp",
			_ => "m_fire_turret",
		};
		var buildCard = CardCatalog.Get(buildId);
		if (buildCard != null && !_hero.Loadout.HasItem(buildCard.GrantsItemId))
			_hero.Loadout.ApplyCard(buildCard, _hero, this);

		var dropCard = CardCatalog.Get(CardCatalog.StarterCardId(heroId));
		if (dropCard == null)
		{
			var rolled = CardCatalog.RollOptions(heroId, _hero.Loadout, 1, _rng);
			if (rolled.Count > 0) dropCard = rolled[0];
		}
		if (dropCard != null)
		{
			var drop = new BigItemDrop { Card = dropCard };
			AddChild(drop);
			drop.GlobalPosition = _hero.GlobalPosition + new Vector2(70, 20);
		}
	}

	private void OnLocaleChanged(string _)
	{
		if (_ended || _hero == null || _hud == null) return;
		_hud.HpLabel.Text = I18n.T("ui.hud.hp", $"{_hero.Hp:0}", $"{_hero.MaxHp:0}");
		_hud.XpLabel.Text = I18n.T("ui.hud.xp", _hero.Level, $"{_hero.Xp:0}", $"{_hero.XpToNext():0}");
		_hud.RefreshSlots(_hero.Loadout);
		_hud.SetTime(_timeLeft);
		if (_spawner != null)
			_hud.SetEliteProgress(_spawner.KillProgress, _spawner.KillThreshold);
	}

	private void OnAdPressed()
	{
		if (Game.Instance.TryUnlockAdSlot())
		{
			_hud.MsgLabel.Text = I18n.T("ui.hud.ad_gained");
			_hud.RefreshSlots(_hero.Loadout);
		}
	}

	private void WireEnemy(Enemy enemy)
	{
		if (enemy == null || !IsInstanceValid(enemy) || enemy.HasMeta("wired")) return;
		enemy.SetMeta("wired", true);
		enemy.Died += OnEnemyDied;
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

		// 潮汐缩圈：危险区半透明覆盖
		float edge = SafeEdgeMargin();
		if (edge > 22f)
		{
			var safe = IslandRect.Grow(-edge);
			var danger = new Color(0.15f, 0.35f, 0.7f, 0.22f);
			DrawRect(new Rect2(IslandRect.Position, new Vector2(IslandRect.Size.X, edge)), danger);
			DrawRect(new Rect2(IslandRect.Position.X, IslandRect.End.Y - edge, IslandRect.Size.X, edge), danger);
			DrawRect(new Rect2(IslandRect.Position.X, IslandRect.Position.Y + edge, edge, IslandRect.Size.Y - edge * 2f), danger);
			DrawRect(new Rect2(IslandRect.End.X - edge, IslandRect.Position.Y + edge, edge, IslandRect.Size.Y - edge * 2f), danger);
			DrawRect(safe, new Color(0.4f, 0.75f, 1f, 0.55f), false, 2f);
		}
	}

	/// <summary>最后 60 秒岸线内收，逼近中央交战。</summary>
	private float SafeEdgeMargin()
	{
		float elapsed = _spawner?.Elapsed ?? 0f;
		if (elapsed < 240f) return 20f;
		float t = Mathf.Clamp((elapsed - 240f) / 60f, 0f, 1f);
		return 20f + t * 160f;
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

		// 钳制英雄在岛内；最后一分钟潮汐缩圈
		float edge = SafeEdgeMargin();
		var p = _hero.GlobalPosition;
		p.X = Mathf.Clamp(p.X, IslandRect.Position.X + edge, IslandRect.End.X - edge);
		p.Y = Mathf.Clamp(p.Y, IslandRect.Position.Y + edge, IslandRect.End.Y - edge);
		_hero.GlobalPosition = p;
		if (!_tideWarned && edge > 24f && _hud != null)
		{
			_tideWarned = true;
			_hud.MsgLabel.Text = I18n.T("ui.hud.tide_shrink");
		}
		if (edge > 22f)
			QueueRedraw();

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

		// 精英击杀进度阈值随时间变化，每帧对齐 HUD
		_hud.SetEliteProgress(_spawner.KillProgress, _spawner.KillThreshold);

		// 拾取高亮大件
		float pickup = _hero.PickupRange;
		foreach (var n in GetTree().GetNodesInGroup("big_drops"))
		{
			if (n is not BigItemDrop drop || !IsInstanceValid(drop)) continue;
			float dist = _hero.GlobalPosition.DistanceTo(drop.GlobalPosition);
			// 磁吸：较远时缓慢拉近，进入拾取半径再开卡
			if (dist < pickup * 2.4f && dist > pickup)
			{
				drop.GlobalPosition = drop.GlobalPosition.MoveToward(_hero.GlobalPosition, 420f * dt);
				continue;
			}
			if (dist < pickup)
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
	private string _popupTitle;
	private bool _popupAfterStarter;

	private void OpenCardPick(string title, List<CardDef> options, bool forcedSingle = false, bool afterStarter = false)
	{
		if (options == null || options.Count == 0) return;
		if (_choosing)
		{
			_pendingPicks.Enqueue(options);
			return;
		}
		_choosing = true;
		_popupTitle = title;
		_popupAfterStarter = afterStarter;
		GetTree().Paused = true;
		_popup = new CardPopup();
		AddChild(_popup);
		bool allowReroll = !forcedSingle && !afterStarter && options.Count > 1;
		_popup.Setup(title, options, allowReroll);
		_popup.RerollPressed += OnCardReroll;
		_popup.Chosen += id =>
		{
			var card = CardCatalog.Get(id);
			if (card != null)
			{
				_hero.Loadout.ApplyCard(card, _hero, this);
				Game.Instance.SynergiesCompleted = _hero.Loadout.CompletedSynergies.Count;
				_hud.RefreshSlots(_hero.Loadout);
				TryShowSynergyReadyTip();
				if (card.Kind == CardKind.Evolve)
					_hud.MsgLabel.Text = I18n.T("ui.hud.evolved", card.LocalizedName);
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

	private void OnCardReroll()
	{
		if (_popup == null || !Game.Instance.TryConsumeReroll()) return;
		var opts = CardCatalog.RollOptions(Game.Instance.SelectedHero, _hero.Loadout, 3, _rng);
		if (opts.Count == 0) return;
		_popup.Rebuild(opts);
	}

	private void TryShowSynergyReadyTip()
	{
		if (_hero == null || _hud == null) return;
		var ready = SynergyCatalog.ReadySynergies(Game.Instance.SelectedHero, _hero.Loadout);
		if (ready.Count == 0) return;
		_hud.MsgLabel.Text = I18n.T("ui.hud.synergy_ready", ready[0].Name);
	}

	private void OnEnemyDied(Enemy enemy)
	{
		if (_ended || _hero == null) return;
		Game.Instance.KillCount += 1;
		float xp = enemy.XpValue;
		float mul = _hero.RegisterKill();
		// 连杀略加经验，强化「清群→升级回满」的节奏
		if (mul > 1f) xp *= 1f + (mul - 1f) * 0.5f;
		_hero.AddXp(xp);
		// 普通怪叠精英刷新进度；满则刷精英
		_spawner?.RegisterKill(enemy);
		if (enemy.IsBoss)
		{
			if (enemy.BossId == "tide_guard") Game.Instance.TideGuardKilled = true;
			if (enemy.BossId == "island_lord") Game.Instance.IslandLordKilled = true;
		}
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

		Game.Instance.SynergiesCompleted = _hero?.Loadout?.CompletedSynergies.Count ?? 0;
		int score = Game.Instance.KillCount * 2
			+ Game.Instance.EliteKills * 25
			+ Game.Instance.MinuteGoalsCompleted * 40
			+ (int)(Game.Instance.HighestItemLevelSum * 8)
			+ Game.Instance.SynergiesCompleted * 50
			+ (Game.Instance.TideGuardKilled ? 30 : 0)
			+ (Game.Instance.IslandLordKilled ? 80 : 0)
			+ (victory ? (int)(_hero.Hp / _hero.MaxHp * 50) : 0);

		string rank = score >= 600 ? "S" : score >= 400 ? "A" : score >= 220 ? "B" : "C";
		int gain = rank switch { "S" => 4, "A" => 3, "B" => 2, _ => 1 };
		if (!victory) gain = Mathf.Max(1, gain - 1);
		Game.Instance.AddMetaFromScore(gain);

		var result = new ResultScreen();
		AddChild(result);
		result.Setup(
			victory, score, rank, gain,
			Game.Instance.KillCount, Game.Instance.EliteKills, Game.Instance.MinuteGoalsCompleted,
			Game.Instance.SynergiesCompleted,
			(Game.Instance.TideGuardKilled ? 1 : 0) + (Game.Instance.IslandLordKilled ? 1 : 0));
		result.ContinuePressed += () =>
		{
			result.QueueFree();
			ShowHeroSelect();
		};
	}
}
