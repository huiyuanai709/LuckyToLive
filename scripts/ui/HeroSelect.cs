using Godot;

public partial class HeroSelect : CanvasLayer
{
	[Signal] public delegate void HeroPickedEventHandler(int heroId);
	[Signal] public delegate void RefreshRequestedEventHandler();

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
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

		var title = new Label
		{
			Text = Game.Instance.StarterHero == null
				? I18n.T("ui.hero_select.title_starter")
				: I18n.T("ui.hero_select.title_play"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		title.AddThemeColorOverride("font_color", Colors.White);
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
			Text = I18n.T("ui.hero_select.hint"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		root.AddChild(hint);

		BuildMapSection(root);

		var heroRow = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		heroRow.AddThemeConstantOverride("separation", 16);
		root.AddChild(heroRow);

		foreach (HeroId id in System.Enum.GetValues(typeof(HeroId)))
			heroRow.AddChild(MakeCard(id));
	}

	private void BuildMapSection(VBoxContainer root)
	{
		var mapTitle = new Label
		{
			Text = I18n.T("ui.map_select.title"),
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		mapTitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.9f, 1f));
		root.AddChild(mapTitle);

		var mapRow = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		mapRow.AddThemeConstantOverride("separation", 12);
		root.AddChild(mapRow);

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
		int cost = Game.Instance.UnlockCost(id);

		var box = new VBoxContainer();
		box.CustomMinimumSize = new Vector2(220, 280);
		box.Alignment = BoxContainer.AlignmentMode.Center;
		box.AddThemeConstantOverride("separation", 8);

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
			btn.Text = firstPick
				? I18n.T("ui.hero_select.pick", name)
				: I18n.T("ui.hero_select.play", name);
			btn.Pressed += () => EmitSignal(SignalName.HeroPicked, (int)id);
		}
		else
		{
			btn.Text = I18n.T("ui.hero_select.unlock", cost);
			btn.Pressed += () =>
			{
				if (Game.Instance.TryUnlockHero(id))
					Rebuild();
				else
					btn.Text = I18n.T("ui.hero_select.not_enough");
			};
		}
		box.AddChild(btn);
		return box;
	}
}
