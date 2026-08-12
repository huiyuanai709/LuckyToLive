using Godot;

/// <summary>
/// 暗黑式开局流程：先创建角色（命名 + 选英雄），再选择关卡。
/// </summary>
public partial class HeroSelect : CanvasLayer
{
	[Signal] public delegate void HeroPickedEventHandler(int heroId);
	[Signal] public delegate void RefreshRequestedEventHandler();

	private enum Step
	{
		CreateCharacter,
		SelectMap,
	}

	private Step _step = Step.CreateCharacter;
	private string _draftName = "";
	private HeroId? _draftHero;
	private LineEdit _nameEdit;
	private Label _nameError;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_draftName = Game.Instance?.CharacterName ?? "";
		if (Game.Instance != null && Game.Instance.StarterHero != null)
			_draftHero = Game.Instance.SelectedHero;
		if (I18n.Instance != null)
			I18n.Instance.LocaleChanged += OnLocaleChanged;
		Rebuild();
	}

	public override void _ExitTree()
	{
		if (I18n.Instance != null)
			I18n.Instance.LocaleChanged -= OnLocaleChanged;
	}

	private void OnLocaleChanged(string _) => Rebuild();

	public void Rebuild()
	{
		foreach (var c in GetChildren())
			((Node)c).QueueFree();
		_nameEdit = null;
		_nameError = null;

		var dim = new ColorRect
		{
			Color = new Color(0.08f, 0.1f, 0.14f, 0.95f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(root);

		if (_step == Step.CreateCharacter)
			BuildCreateCharacter(root);
		else
			BuildSelectMap(root);

		// 最后添加，保证盖在内容之上可点
		var langBtn = new Button
		{
			Text = I18n.T("ui.hero_select.lang", I18n.Instance?.LocaleDisplayName() ?? "中文"),
			Position = new Vector2(1040, 24),
			Size = new Vector2(200, 36),
		};
		langBtn.Pressed += () => I18n.Instance?.ToggleLocale();
		AddChild(langBtn);
	}

	private void BuildCreateCharacter(Control root)
	{
		var content = new VBoxContainer();
		content.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		content.OffsetLeft = 40;
		content.OffsetRight = -40;
		content.OffsetTop = 20;
		content.OffsetBottom = -64; // 留给底部「下一步」
		content.Alignment = BoxContainer.AlignmentMode.Center;
		content.AddThemeConstantOverride("separation", 8);
		content.MouseFilter = Control.MouseFilterEnum.Ignore;
		root.AddChild(content);

		var title = new Label
		{
			Text = I18n.T("ui.char_create.title"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		title.AddThemeColorOverride("font_color", Colors.White);
		title.AddThemeFontSizeOverride("font_size", 28);
		content.AddChild(title);

		var currency = new Label
		{
			Text = I18n.T("ui.hero_select.currency", Game.Instance.MetaCurrency),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		content.AddChild(currency);

		var hint = new Label
		{
			Text = I18n.T("ui.char_create.hint"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		content.AddChild(hint);

		var nameCenter = new CenterContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		content.AddChild(nameCenter);

		var nameBox = new VBoxContainer();
		nameBox.CustomMinimumSize = new Vector2(420, 0);
		nameBox.AddThemeConstantOverride("separation", 6);
		nameCenter.AddChild(nameBox);

		var nameLabel = new Label
		{
			Text = I18n.T("ui.char_create.name_label"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.9f, 1f));
		nameBox.AddChild(nameLabel);

		_nameEdit = new LineEdit
		{
			Text = _draftName,
			PlaceholderText = I18n.T("ui.char_create.name_placeholder"),
			MaxLength = 16,
			Alignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(420, 40),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			// Web/mobile: need the OS keyboard; experimentalVK in index.html must also be on.
			VirtualKeyboardEnabled = true,
			VirtualKeyboardShowOnFocus = true,
			VirtualKeyboardType = LineEdit.VirtualKeyboardTypeEnum.Default,
		};
		_nameEdit.TextChanged += text =>
		{
			_draftName = text ?? "";
			if (_nameError != null)
				_nameError.Visible = false;
		};
		_nameEdit.TextSubmitted += _ => TryAdvanceToMapSelect();
		nameBox.AddChild(_nameEdit);

		_nameError = new Label
		{
			Text = I18n.T("ui.char_create.name_required"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Visible = false,
		};
		_nameError.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.4f));
		nameBox.AddChild(_nameError);

		var heroTitle = new Label
		{
			Text = Game.Instance.StarterHero == null
				? I18n.T("ui.hero_select.title_starter")
				: I18n.T("ui.char_create.pick_hero"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		heroTitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.9f, 1f));
		content.AddChild(heroTitle);

		var heroCenter = new CenterContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		content.AddChild(heroCenter);

		var heroRow = new HBoxContainer();
		heroRow.AddThemeConstantOverride("separation", 16);
		heroCenter.AddChild(heroRow);

		foreach (HeroId id in System.Enum.GetValues(typeof(HeroId)))
			heroRow.AddChild(MakeCard(id));

		var nextBtn = new Button
		{
			Text = I18n.T("ui.char_create.next"),
			AnchorLeft = 0.5f,
			AnchorRight = 0.5f,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			OffsetLeft = -110,
			OffsetRight = 110,
			OffsetTop = -56,
			OffsetBottom = -12,
		};
		nextBtn.Pressed += TryAdvanceToMapSelect;
		root.AddChild(nextBtn);

		// Desktop: auto-focus is fine. Touch/web: browsers only open the keyboard
		// inside a user gesture — auto GrabFocus leaves the field focused with no
		// keyboard, so a later tap often fails to re-trigger it.
		if (!IsTouchOriented())
			CallDeferred(MethodName.FocusNameEdit);
	}

	private static bool IsTouchOriented()
	{
		if (OS.HasFeature("mobile") || OS.HasFeature("web_android") || OS.HasFeature("web_ios"))
			return true;
		if (!string.IsNullOrEmpty(OS.GetEnvironment("LUCKY_TOUCH_CONTROLS")))
			return true;
		return DisplayServer.IsTouchscreenAvailable();
	}

	private void FocusNameEdit()
	{
		_nameEdit?.GrabFocus();
		_nameEdit?.SelectAll();
	}

	private void TryAdvanceToMapSelect()
	{
		string name = (_nameEdit?.Text ?? _draftName).Trim();
		_draftName = name;
		if (string.IsNullOrEmpty(name))
		{
			if (_nameError != null)
			{
				_nameError.Text = I18n.T("ui.char_create.name_required");
				_nameError.Visible = true;
			}
			_nameEdit?.GrabFocus();
			return;
		}

		if (_draftHero == null)
		{
			if (_nameError != null)
			{
				_nameError.Text = I18n.T("ui.char_create.hero_required");
				_nameError.Visible = true;
			}
			return;
		}

		var heroId = _draftHero.Value;
		if (Game.Instance.StarterHero == null)
			Game.Instance.ChooseStarter(heroId);
		else if (!Game.Instance.IsHeroUnlocked(heroId))
		{
			if (_nameError != null)
			{
				_nameError.Text = I18n.T("ui.hero_select.not_enough");
				_nameError.Visible = true;
			}
			return;
		}

		Game.Instance.SetCharacterName(name);
		Game.Instance.SelectedHero = heroId;
		_step = Step.SelectMap;
		Rebuild();
	}

	private void BuildSelectMap(Control root)
	{
		var content = new VBoxContainer();
		content.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		content.OffsetLeft = 40;
		content.OffsetRight = -40;
		content.OffsetTop = 24;
		content.OffsetBottom = -72;
		content.Alignment = BoxContainer.AlignmentMode.Center;
		content.AddThemeConstantOverride("separation", 14);
		content.MouseFilter = Control.MouseFilterEnum.Ignore;
		root.AddChild(content);

		var title = new Label
		{
			Text = I18n.T("ui.map_select.title"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		title.AddThemeColorOverride("font_color", Colors.White);
		title.AddThemeFontSizeOverride("font_size", 28);
		content.AddChild(title);

		string heroName = I18n.HeroName(Game.Instance.SelectedHero);
		var summary = new Label
		{
			Text = I18n.T("ui.map_select.character", Game.Instance.CharacterName, heroName),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		summary.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.55f));
		content.AddChild(summary);

		var hint = new Label
		{
			Text = I18n.T("ui.map_select.hint"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		content.AddChild(hint);

		var mapCenter = new CenterContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		content.AddChild(mapCenter);

		var mapRow = new HBoxContainer();
		mapRow.AddThemeConstantOverride("separation", 12);
		mapCenter.AddChild(mapRow);

		foreach (MapId mapId in MapCatalog.All)
			mapRow.AddChild(MakeMapCard(mapId));

		var diffTitle = new Label
		{
			Text = I18n.T("ui.diff.title"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		diffTitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.9f, 1f));
		content.AddChild(diffTitle);

		var diffCenter = new CenterContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		content.AddChild(diffCenter);

		var diffRow = new HBoxContainer();
		diffRow.AddThemeConstantOverride("separation", 12);
		diffCenter.AddChild(diffRow);

		foreach (DifficultyId diff in System.Enum.GetValues(typeof(DifficultyId)))
			diffRow.AddChild(MakeDiffCard(diff));

		var backBtn = new Button
		{
			Text = I18n.T("ui.map_select.back"),
			AnchorLeft = 0.5f,
			AnchorRight = 0.5f,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			OffsetLeft = -200,
			OffsetRight = -20,
			OffsetTop = -56,
			OffsetBottom = -12,
		};
		backBtn.Pressed += () =>
		{
			_step = Step.CreateCharacter;
			Rebuild();
		};
		root.AddChild(backBtn);

		var startBtn = new Button
		{
			Text = I18n.T("ui.map_select.start"),
			AnchorLeft = 0.5f,
			AnchorRight = 0.5f,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 20,
			OffsetRight = 200,
			OffsetTop = -56,
			OffsetBottom = -12,
		};
		startBtn.Pressed += () => EmitSignal(SignalName.HeroPicked, (int)Game.Instance.SelectedHero);
		root.AddChild(startBtn);
	}

	private Control MakeDiffCard(DifficultyId id)
	{
		bool selected = Game.Instance.SelectedDifficulty == id;
		var panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(200, 108);
		Color accent = id switch
		{
			DifficultyId.Hard => new Color(0.95f, 0.65f, 0.25f),
			DifficultyId.Nightmare => new Color(0.9f, 0.35f, 0.4f),
			_ => new Color(0.45f, 0.8f, 0.55f),
		};
		var style = new StyleBoxFlat
		{
			BgColor = selected
				? new Color(0.16f, 0.18f, 0.22f, 0.95f)
				: new Color(0.12f, 0.14f, 0.18f, 0.95f),
			BorderColor = selected ? accent : new Color(0.35f, 0.38f, 0.45f),
			BorderWidthLeft = selected ? 3 : 1,
			BorderWidthTop = selected ? 3 : 1,
			BorderWidthRight = selected ? 3 : 1,
			BorderWidthBottom = selected ? 3 : 1,
			ContentMarginLeft = 10,
			ContentMarginRight = 10,
			ContentMarginTop = 8,
			ContentMarginBottom = 8,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
		};
		panel.AddThemeStyleboxOverride("panel", style);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 4);
		box.Alignment = BoxContainer.AlignmentMode.Center;
		panel.AddChild(box);

		var name = new Label
		{
			Text = I18n.DifficultyName(id),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		name.AddThemeColorOverride("font_color", accent);
		box.AddChild(name);

		var desc = new Label
		{
			Text = I18n.DifficultyDesc(id),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(180, 40),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		desc.AddThemeColorOverride("font_color", new Color(0.8f, 0.82f, 0.88f));
		box.AddChild(desc);

		var btn = new Button
		{
			Text = selected ? I18n.T("ui.map_select.selected") : I18n.T("ui.map_select.pick"),
			Disabled = selected,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		btn.Pressed += () =>
		{
			Game.Instance.SelectedDifficulty = id;
			Game.Instance.Save();
			Rebuild();
		};
		box.AddChild(btn);
		return panel;
	}

	private Control MakeMapCard(MapId id)
	{
		bool selected = Game.Instance.SelectedMap == id;
		var theme = MapCatalog.Get(id);

		var panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(200, 108);
		var style = new StyleBoxFlat
		{
			BgColor = selected
				? theme.InlandFallback.Lightened(0.08f)
				: new Color(0.12f, 0.14f, 0.18f, 0.95f),
			BorderColor = selected ? theme.BorderGlow : new Color(0.35f, 0.38f, 0.45f),
			BorderWidthLeft = selected ? 3 : 1,
			BorderWidthTop = selected ? 3 : 1,
			BorderWidthRight = selected ? 3 : 1,
			BorderWidthBottom = selected ? 3 : 1,
			ContentMarginLeft = 10,
			ContentMarginRight = 10,
			ContentMarginTop = 8,
			ContentMarginBottom = 8,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
		};
		panel.AddThemeStyleboxOverride("panel", style);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 4);
		box.Alignment = BoxContainer.AlignmentMode.Center;
		panel.AddChild(box);

		var name = new Label
		{
			Text = I18n.MapName(id),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		name.AddThemeColorOverride("font_color", Colors.White);
		box.AddChild(name);

		var desc = new Label
		{
			Text = I18n.MapDesc(id),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(180, 40),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		desc.AddThemeColorOverride("font_color", new Color(0.8f, 0.82f, 0.88f));
		box.AddChild(desc);

		var btn = new Button
		{
			Text = selected ? I18n.T("ui.map_select.selected") : I18n.T("ui.map_select.pick"),
			Disabled = selected,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		btn.Pressed += () =>
		{
			Game.Instance.SelectedMap = id;
			Rebuild();
		};
		box.AddChild(btn);

		return panel;
	}

	private Control MakeCard(HeroId id)
	{
		bool unlocked = Game.Instance.IsHeroUnlocked(id);
		bool firstPick = Game.Instance.StarterHero == null;
		bool selected = _draftHero == id;
		int cost = Game.Instance.UnlockCost(id);
		int metaLv = Game.Instance.GetHeroMetaLevel(id);
		float atkMul = Game.Instance.GetMetaAttackMul(id);
		int upCost = Game.Instance.MetaUpgradeCost(id);
		bool maxed = metaLv >= MetaSkillCatalog.MaxMetaLevel;

		var panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(210, 300);
		var style = new StyleBoxFlat
		{
			BgColor = selected
				? new Color(0.18f, 0.22f, 0.3f, 0.95f)
				: new Color(0.1f, 0.12f, 0.16f, 0.7f),
			BorderColor = selected ? new Color(0.95f, 0.8f, 0.35f) : new Color(0.3f, 0.33f, 0.4f),
			BorderWidthLeft = selected ? 3 : 1,
			BorderWidthTop = selected ? 3 : 1,
			BorderWidthRight = selected ? 3 : 1,
			BorderWidthBottom = selected ? 3 : 1,
			ContentMarginLeft = 10,
			ContentMarginRight = 10,
			ContentMarginTop = 8,
			ContentMarginBottom = 8,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
		};
		panel.AddThemeStyleboxOverride("panel", style);

		var box = new VBoxContainer();
		box.Alignment = BoxContainer.AlignmentMode.Center;
		box.AddThemeConstantOverride("separation", 5);
		panel.AddChild(box);

		string name = I18n.HeroName(id);
		string desc = I18n.HeroDesc(id);

		string path = id switch
		{
			HeroId.Warrior => "res://assets/characters/hero_warrior.png",
			HeroId.Mage => "res://assets/characters/hero_mage.png",
			_ => "res://assets/characters/hero_hunter.png",
		};
		if (ResourceLoader.Exists(path))
		{
			var tex = new TextureRect
			{
				Texture = GD.Load<Texture2D>(path),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				CustomMinimumSize = new Vector2(72, 72),
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			};
			box.AddChild(tex);
		}

		var lbl = new Label
		{
			Text = $"{name}\n{desc}",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(200, 56),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		box.AddChild(lbl);

		var metaLbl = new Label
		{
			Text = I18n.T("ui.meta.level_atk", metaLv, $"{atkMul:0.00}"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		metaLbl.AddThemeColorOverride("font_color", new Color(0.95f, 0.82f, 0.45f));
		box.AddChild(metaLbl);

		var skillBits = new System.Text.StringBuilder();
		foreach (var sk in MetaSkillCatalog.ForHero(id))
		{
			bool open = metaLv >= sk.UnlockLevel;
			string skName = I18n.SkillName(sk.Id);
			if (skillBits.Length > 0) skillBits.Append('\n');
			skillBits.Append(open
				? I18n.T("ui.meta.skill_unlocked", skName)
				: I18n.T("ui.meta.skill_locked", sk.UnlockLevel, skName));
		}
		var skillLbl = new Label
		{
			Text = skillBits.ToString(),
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(200, 36),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		skillLbl.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.9f));
		skillLbl.AddThemeFontSizeOverride("font_size", 12);
		box.AddChild(skillLbl);

		var btn = new Button();
		btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		if (firstPick || unlocked)
		{
			btn.Text = selected
				? I18n.T("ui.char_create.selected", name)
				: I18n.T("ui.char_create.pick", name);
			btn.Disabled = selected;
			btn.Pressed += () =>
			{
				_draftHero = id;
				if (_nameEdit != null)
					_draftName = _nameEdit.Text ?? "";
				Rebuild();
			};
		}
		else
		{
			btn.Text = I18n.T("ui.hero_select.unlock", cost);
			btn.Pressed += () =>
			{
				if (Game.Instance.TryUnlockHero(id))
				{
					_draftHero = id;
					Rebuild();
				}
				else
					btn.Text = I18n.T("ui.hero_select.not_enough");
			};
		}
		box.AddChild(btn);

		if (!firstPick && unlocked)
		{
			var upBtn = new Button
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};
			if (maxed)
			{
				upBtn.Text = I18n.T("ui.meta.max_level");
				upBtn.Disabled = true;
			}
			else
			{
				upBtn.Text = I18n.T("ui.meta.upgrade", upCost);
				upBtn.Pressed += () =>
				{
					if (Game.Instance.TryUpgradeHero(id))
						Rebuild();
					else
						upBtn.Text = I18n.T("ui.hero_select.not_enough");
				};
			}
			box.AddChild(upBtn);
		}

		return panel;
	}
}