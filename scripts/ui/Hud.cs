using Godot;

public partial class Hud : CanvasLayer
{
	public Label TimeLabel;
	public Label HpLabel;
	public Label XpLabel;
	public Label SlotsLabel;
	public Label GoalLabel;
	public Label MapLabel;
	public Label MsgLabel;
	public Button AdButton;
	public Button LangButton;
	public HBoxContainer SlotBox;

	private ColorRect _progressFill;
	private ColorRect _xpFill;
	private const float BarW = 200f;
	private const float BarH = 6f;

	[Signal] public delegate void AdPressedEventHandler();

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		// 进度条（上）+ 右侧生存时间；经验条（下）— 细条样式贴近头顶血条
		var progressTrack = MakeBarTrack(new Vector2(16, 14));
		_progressFill = MakeBarFill(new Color(0.95f, 0.75f, 0.25f));
		progressTrack.AddChild(_progressFill);
		root.AddChild(progressTrack);

		TimeLabel = new Label
		{
			Position = new Vector2(16 + BarW + 10, 8),
			Text = "0:00",
		};
		TimeLabel.AddThemeColorOverride("font_color", Colors.White);
		root.AddChild(TimeLabel);

		var xpTrack = MakeBarTrack(new Vector2(16, 28));
		_xpFill = MakeBarFill(new Color(0.35f, 0.65f, 1f));
		xpTrack.AddChild(_xpFill);
		root.AddChild(xpTrack);

		// 兼容旧引用：生命已在角色头顶显示，HUD 不再叠文字
		HpLabel = new Label { Visible = false };
		root.AddChild(HpLabel);
		XpLabel = new Label { Visible = false };
		root.AddChild(XpLabel);

		SlotsLabel = new Label { Position = new Vector2(16, 48) };
		root.AddChild(SlotsLabel);

		GoalLabel = new Label { Position = new Vector2(16, 72), Text = I18n.T("ui.hud.goal") };
		root.AddChild(GoalLabel);

		MapLabel = new Label
		{
			Position = new Vector2(16, 96),
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
		if (MapLabel != null)
			MapLabel.Text = I18n.T("ui.hud.map", I18n.MapName(Game.Instance?.SelectedMap ?? MapId.Island));
		if (Game.Instance != null)
		{
			AdButton.Text = Game.Instance.AdSlotsUnlocked >= Game.MaxAdSlots
				? I18n.T("ui.hud.ad_full")
				: I18n.T("ui.hud.ad_progress", Game.Instance.AdSlotsUnlocked, Game.MaxAdSlots);
		}
		// 槽位名等由 Main.OnLocaleChanged 里 RefreshSlots 刷新
	}

	/// <summary>剩余时间 → 进度条填充 + 右侧生存时长。</summary>
	public void SetTime(float remaining)
	{
		float duration = Game.RunDuration;
		float elapsed = Mathf.Clamp(duration - remaining, 0f, duration);
		float t = duration > 0.01f ? elapsed / duration : 0f;
		if (_progressFill != null)
			_progressFill.Size = new Vector2(BarW * t, BarH);

		int sec = Mathf.Max(0, Mathf.FloorToInt(elapsed));
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
