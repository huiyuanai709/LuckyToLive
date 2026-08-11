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

		var dim = new ColorRect { Color = new Color(0.08f, 0.1f, 0.14f, 0.95f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		var langBtn = new Button
		{
			Text = I18n.T("ui.hero_select.lang", I18n.Instance?.LocaleDisplayName() ?? "中文"),
			Position = new Vector2(1040, 24),
			Size = new Vector2(200, 36),
		};
		langBtn.Pressed += () => I18n.Instance?.ToggleLocale();
		AddChild(langBtn);

		var root = new VBoxContainer();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.Alignment = BoxContainer.AlignmentMode.Center;
		root.AddThemeConstantOverride("separation", 16);
		root.OffsetLeft = 40;
		root.OffsetRight = -40;
		root.OffsetTop = 24;
		root.OffsetBottom = -24;
		AddChild(root);

		if (_step == Step.CreateCharacter)
			BuildCreateCharacter(root);
		else
			BuildSelectMap(root);
	}

	private void BuildCreateCharacter(VBoxContainer root)
	{
		var title = new Label
		{
			Text = I18n.T("ui.char_create.title"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		title.AddThemeColorOverride("font_color", Colors.White);
		title.AddThemeFontSizeOverride("font_size", 28);
		root.AddChild(title);

		var currency = new Label
		{
			Text = I18n.T("ui.hero_select.currency", Game.Instance.MetaCurrency),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		root.AddChild(currency);

		var hint = new Label
		{
			Text = I18n.T("ui.char_create.hint"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		root.AddChild(hint);

		var nameCenter = new CenterContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		root.AddChild(nameCenter);

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
		root.AddChild(heroTitle);

		var heroCenter = new CenterContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		root.AddChild(heroCenter);

		var heroRow = new HBoxContainer();
		heroRow.AddThemeConstantOverride("separation", 16);
		heroCenter.AddChild(heroRow);

		foreach (HeroId id in System.Enum.GetValues(typeof(HeroId)))
			heroRow.AddChild(MakeCard(id));

		var nextCenter = new CenterContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		root.AddChild(nextCenter);

		var nextBtn = new Button
		{
			Text = I18n.T("ui.char_create.next"),
			CustomMinimumSize = new Vector2(220, 44),
		};
		nextBtn.Pressed += TryAdvanceToMapSelect;
		nextCenter.AddChild(nextBtn);

		CallDeferred(MethodName.FocusNameEdit);
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
		{
			// 首局：选定即记为起始英雄
			Game.Instance.ChooseStarter(heroId);
		}
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

	private void BuildSelectMap(VBoxContainer root)
	{
		var title = new Label
		{
			Text = I18n.T("ui.map_select.title"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		title.AddThemeColorOverride("font_color", Colors.White);
		title.AddThemeFontSizeOverride("font_size", 28);
		root.AddChild(title);

		string heroName = I18n.HeroName(Game.Instance.SelectedHero);
		var summary = new Label
		{
			Text = I18n.T("ui.map_select.character", Game.Instance.CharacterName, heroName),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		summary.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.55f));
		root.AddChild(summary);

		var hint = new Label
		{
			Text = I18n.T("ui.map_select.hint"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		root.AddChild(hint);

		BuildMapSection(root);

		var actions = new CenterContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		root.AddChild(actions);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 16);
		actions.AddChild(row);

		var backBtn = new Button
		{
			Text = I18n.T("ui.map_select.back"),
			CustomMinimumSize = new Vector2(160, 44),
		};
		backBtn.Pressed += () =>
		{
			_step = Step.CreateCharacter;
			Rebuild();
		};
		row.AddChild(backBtn);

		var startBtn = new Button
		{
			Text = I18n.T("ui.map_select.start"),
			CustomMinimumSize = new Vector2(220, 44),
		};
		startBtn.Pressed += () => EmitSignal(SignalName.HeroPicked, (int)Game.Instance.SelectedHero);
		row.AddChild(startBtn);
	}

	private void BuildMapSection(VBoxContainer root)
	{
		var mapCenter = new CenterContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		root.AddChild(mapCenter);

		var mapRow = new HBoxContainer();
		mapRow.AddThemeConstantOverride("separation", 12);
		mapCenter.AddChild(mapRow);

		foreach (MapId mapId in MapCatalog.All)
			mapRow.AddChild(MakeMapCard(mapId));
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

		var panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(220, 300);
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
			ContentMarginTop = 10,
			ContentMarginBottom = 10,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
		};
		panel.AddThemeStyleboxOverride("panel", style);

		var box = new VBoxContainer();
		box.Alignment = BoxContainer.AlignmentMode.Center;
		box.AddThemeConstantOverride("separation", 8);
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
				CustomMinimumSize = new Vector2(96, 96),
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			};
			box.AddChild(tex);
		}

		var lbl = new Label
		{
			Text = $"{name}\n\n{desc}",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(200, 100),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		box.AddChild(lbl);

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
		return panel;
	}
}
