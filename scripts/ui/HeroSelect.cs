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
			Position = new Vector2(280, 60),
		};
		title.AddThemeColorOverride("font_color", Colors.White);
		AddChild(title);

		var currency = new Label
		{
			Text = I18n.T("ui.hero_select.currency", Game.Instance.MetaCurrency),
			Position = new Vector2(280, 90),
		};
		AddChild(currency);

		var hint = new Label
		{
			Text = I18n.T("ui.hero_select.hint"),
			Position = new Vector2(280, 112),
		};
		AddChild(hint);

		var langBtn = new Button
		{
			Text = I18n.T("ui.hero_select.lang", I18n.Instance?.LocaleDisplayName() ?? "中文"),
			Position = new Vector2(980, 60),
			Size = new Vector2(200, 36),
		};
		langBtn.Pressed += () => I18n.Instance?.ToggleLocale();
		AddChild(langBtn);

		var row = new HBoxContainer { Position = new Vector2(120, 160) };
		AddChild(row);

		foreach (HeroId id in System.Enum.GetValues(typeof(HeroId)))
			row.AddChild(MakeCard(id));
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
