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

		var title = new Label
		{
			Text = Game.Instance.StarterHero == null
				? I18n.T("ui.hero_select.title_starter")
				: I18n.T("ui.hero_select.title_play"),
			Position = new Vector2(280, 36),
		};
		title.AddThemeColorOverride("font_color", Colors.White);
		AddChild(title);

		var currency = new Label
		{
			Text = I18n.T("ui.hero_select.currency", Game.Instance.MetaCurrency),
			Position = new Vector2(280, 62),
		};
		AddChild(currency);

		var hint = new Label
		{
			Text = I18n.T("ui.hero_select.hint"),
			Position = new Vector2(280, 84),
		};
		AddChild(hint);

		var langBtn = new Button
		{
			Text = I18n.T("ui.hero_select.lang", I18n.Instance?.LocaleDisplayName() ?? "中文"),
			Position = new Vector2(980, 36),
			Size = new Vector2(200, 36),
		};
		langBtn.Pressed += () => I18n.Instance?.ToggleLocale();
		AddChild(langBtn);

		BuildMapRow();

		var row = new HBoxContainer { Position = new Vector2(120, 280) };
		AddChild(row);

		foreach (HeroId id in System.Enum.GetValues(typeof(HeroId)))
			row.AddChild(MakeCard(id));
	}

	private void BuildMapRow()
	{
		var mapTitle = new Label
		{
			Text = I18n.T("ui.map_select.title"),
			Position = new Vector2(120, 118),
		};
		mapTitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.9f, 1f));
		AddChild(mapTitle);

		var mapRow = new HBoxContainer
		{
			Position = new Vector2(120, 148),
		};
		mapRow.AddThemeConstantOverride("separation", 12);
		AddChild(mapRow);

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
		panel.AddChild(box);

		var name = new Label { Text = I18n.MapName(id) };
		name.AddThemeColorOverride("font_color", Colors.White);
		box.AddChild(name);

		var desc = new Label
		{
			Text = I18n.MapDesc(id),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		desc.CustomMinimumSize = new Vector2(180, 40);
		desc.AddThemeColorOverride("font_color", new Color(0.8f, 0.82f, 0.88f));
		box.AddChild(desc);

		var btn = new Button
		{
			Text = selected ? I18n.T("ui.map_select.selected") : I18n.T("ui.map_select.pick"),
			Disabled = selected,
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
			};
			box.AddChild(tex);
		}

		var lbl = new Label { Text = $"{name}\n\n{desc}" };
		lbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		lbl.CustomMinimumSize = new Vector2(200, 100);
		box.AddChild(lbl);

		var btn = new Button();
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
