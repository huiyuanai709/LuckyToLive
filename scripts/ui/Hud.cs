using Godot;

public partial class Hud : CanvasLayer
{
	public Label TimeLabel;
	public Label HpLabel;
	public Label XpLabel;
	public Label SlotsLabel;
	public Label GoalLabel;
	public Label EliteProgressLabel;
	public Label MapLabel;
	public Label CharacterLabel;
	public Label MsgLabel;
	public Button AdButton;
	public Button LangButton;
	public HBoxContainer SlotBox;

	private ColorRect _eliteFill;
	private ColorRect _xpFill;
	private const float BarW = 200f;
	private const float BarH = 6f;
	private int _eliteProg;
	private int _eliteNeed = 14;

	[Signal] public delegate void AdPressedEventHandler();

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		// 精英击杀进度（上细条）+ 旁侧倒计时；经验条（下）
		var eliteTrack = MakeBarTrack(new Vector2(16, 14));
		_eliteFill = MakeBarFill(new Color(0.95f, 0.55f, 0.2f));
		eliteTrack.AddChild(_eliteFill);
		root.AddChild(eliteTrack);

		TimeLabel = new Label
		{
			Position = new Vector2(16 + BarW + 10, 8),
			Text = "5:00",
		};
		TimeLabel.AddThemeColorOverride("font_color", Colors.White);
		root.AddChild(TimeLabel);

		var xpTrack = MakeBarTrack(new Vector2(16, 28));
		_xpFill = MakeBarFill(new Color(0.35f, 0.65f, 1f));
		xpTrack.AddChild(_xpFill);
		root.AddChild(xpTrack);

		// 兼容旧引用：生命在头顶；精英进度改用细条
		HpLabel = new Label { Visible = false };
		root.AddChild(HpLabel);
		XpLabel = new Label { Visible = false };
		root.AddChild(XpLabel);
		EliteProgressLabel = new Label { Visible = false };
		root.AddChild(EliteProgressLabel);

		SlotsLabel = new Label { Position = new Vector2(16, 48) };
		root.AddChild(SlotsLabel);

		GoalLabel = new Label { Position = new Vector2(16, 72), Text = I18n.T("ui.hud.goal") };
		root.AddChild(GoalLabel);

		CharacterLabel = new Label
		{
			Position = new Vector2(16, 96),
			Text = FormatCharacterLabel(),
		};
		root.AddChild(CharacterLabel);

		MapLabel = new Label
		{
			Position = new Vector2(16, 120),
			Text = I18n.T("ui.hud.map", I18n.MapName(Game.Instance?.SelectedMap ?? MapId.Island)),
		};
		root.AddChild(MapLabel);

		MsgLabel = new Label { Position = new Vector2(16, 540) };
		root.AddChild(MsgLabel);

		SlotBox = new HBoxContainer { Position = new Vector2(280, 12) };
		root.AddChild(SlotBox);

		AdButton = new Button
		{
			Text = I18n.T("ui.hud.ad_unlock"),
			Position = new Vector2(700, 12),
			Size = new Vector2(200, 32),
		};
		AdButton.Pressed += () => EmitSignal(SignalName.AdPressed);
		root.AddChild(AdButton);

		LangButton = new Button
		{
			Text = I18n.Instance?.LocaleDisplayName() ?? "中文",
			Position = new Vector2(920, 12),
			Size = new Vector2(100, 32),
		};
		LangButton.Pressed += () => I18n.Instance?.ToggleLocale();
		root.AddChild(LangButton);

		if (I18n.Instance != null)
			I18n.Instance.LocaleChanged += OnLocaleChanged;
	}

	private static ColorRect MakeBarTrack(Vector2 pos)
	{
		return new ColorRect
		{
			Position = pos,
			Size = new Vector2(BarW, BarH),
			Color = new Color(0.15f, 0.12f, 0.12f, 0.85f),
		};
	}

	private static ColorRect MakeBarFill(Color color)
	{
		return new ColorRect
		{
			Position = Vector2.Zero,
			Size = new Vector2(0, BarH),
			Color = color,
		};
	}

	public override void _ExitTree()
	{
		if (I18n.Instance != null)
			I18n.Instance.LocaleChanged -= OnLocaleChanged;
	}

	private void OnLocaleChanged(string _)
	{
		if (LangButton != null)
			LangButton.Text = I18n.Instance?.LocaleDisplayName() ?? "";
		GoalLabel.Text = I18n.T("ui.hud.goal");
		SetEliteProgress(_eliteProg, _eliteNeed);
		if (MapLabel != null)
			MapLabel.Text = I18n.T("ui.hud.map", I18n.MapName(Game.Instance?.SelectedMap ?? MapId.Island));
		if (CharacterLabel != null)
			CharacterLabel.Text = FormatCharacterLabel();
		if (Game.Instance != null)
		{
			AdButton.Text = Game.Instance.AdSlotsUnlocked >= Game.MaxAdSlots
				? I18n.T("ui.hud.ad_full")
				: I18n.T("ui.hud.ad_progress", Game.Instance.AdSlotsUnlocked, Game.MaxAdSlots);
		}
		// 槽位名等由 Main.OnLocaleChanged 里 RefreshSlots 刷新
	}

	private static string FormatCharacterLabel()
	{
		string name = Game.Instance?.CharacterName;
		if (string.IsNullOrWhiteSpace(name))
			name = I18n.HeroName(Game.Instance?.SelectedHero ?? HeroId.Hunter);
		return I18n.T("ui.hud.character", name, I18n.HeroName(Game.Instance?.SelectedHero ?? HeroId.Hunter));
	}

	/// <summary>局内倒计时，显示在精英进度条右侧。</summary>
	public void SetTime(float remaining)
	{
		int sec = Mathf.Max(0, Mathf.CeilToInt(remaining));
		TimeLabel.Text = $"{sec / 60}:{sec % 60:00}";
	}

	public void SetXp(int level, float xp, float need)
	{
		float t = need > 0.01f ? Mathf.Clamp(xp / need, 0f, 1f) : 0f;
		if (_xpFill != null)
			_xpFill.Size = new Vector2(BarW * t, BarH);
		if (XpLabel != null)
			XpLabel.Text = I18n.T("ui.hud.xp", level, $"{xp:0}", $"{need:0}");
	}

	public void SetEliteProgress(int progress, int threshold)
	{
		_eliteProg = Mathf.Max(0, progress);
		_eliteNeed = Mathf.Max(1, threshold);
		float t = Mathf.Clamp(_eliteProg / (float)_eliteNeed, 0f, 1f);
		if (_eliteFill != null)
			_eliteFill.Size = new Vector2(BarW * t, BarH);
		if (EliteProgressLabel != null)
			EliteProgressLabel.Text = I18n.T("ui.hud.elite_progress", _eliteProg, _eliteNeed);
	}

	public void RefreshSlots(Loadout loadout)
	{
		foreach (var c in SlotBox.GetChildren()) ((Node)c).QueueFree();
		int cap = Game.Instance.AvailableSlotsThisRun;
		SlotsLabel.Text = I18n.T("ui.hud.slots", loadout.Count, cap);
		for (int i = 0; i < cap; i++)
		{
			var panel = new PanelContainer();
			panel.CustomMinimumSize = new Vector2(72, 40);
			var label = new Label();
			if (i < loadout.Slots.Count)
			{
				var item = loadout.Slots[i];
				string nameId = !string.IsNullOrEmpty(item.EvolveCardId) ? item.EvolveCardId : item.ItemId;
				label.Text = $"{I18n.CardName(nameId)} Lv{item.Level}";
			}
			else
			{
				label.Text = I18n.T("ui.hud.empty");
			}
			panel.AddChild(label);
			SlotBox.AddChild(panel);
		}
		AdButton.Disabled = Game.Instance.AdSlotsUnlocked >= Game.MaxAdSlots;
		AdButton.Text = Game.Instance.AdSlotsUnlocked >= Game.MaxAdSlots
			? I18n.T("ui.hud.ad_full")
			: I18n.T("ui.hud.ad_progress", Game.Instance.AdSlotsUnlocked, Game.MaxAdSlots);
		if (LangButton != null)
			LangButton.Text = I18n.Instance?.LocaleDisplayName() ?? "";
	}
}
