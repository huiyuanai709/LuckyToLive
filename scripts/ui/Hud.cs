using Godot;

public partial class Hud : CanvasLayer
{
	public Label TimeLabel;
	public Label HpLabel;
	public Label XpLabel;
	public Label SlotsLabel;
	public Label GoalLabel;
	public Label MsgLabel;
	public Button AdButton;
	public Button LangButton;
	public HBoxContainer SlotBox;

	[Signal] public delegate void AdPressedEventHandler();

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		TimeLabel = new Label { Position = new Vector2(16, 12), Text = "5:00" };
		TimeLabel.AddThemeColorOverride("font_color", Colors.White);
		root.AddChild(TimeLabel);

		HpLabel = new Label { Position = new Vector2(16, 36) };
		root.AddChild(HpLabel);

		XpLabel = new Label { Position = new Vector2(16, 60) };
		root.AddChild(XpLabel);

		SlotsLabel = new Label { Position = new Vector2(16, 84) };
		root.AddChild(SlotsLabel);

		GoalLabel = new Label { Position = new Vector2(16, 108), Text = I18n.T("ui.hud.goal") };
		root.AddChild(GoalLabel);

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
		if (Game.Instance != null)
		{
			AdButton.Text = Game.Instance.AdSlotsUnlocked >= Game.MaxAdSlots
				? I18n.T("ui.hud.ad_full")
				: I18n.T("ui.hud.ad_progress", Game.Instance.AdSlotsUnlocked, Game.MaxAdSlots);
		}
		// 槽位名等由 Main.OnLocaleChanged 里 RefreshSlots 刷新
	}

	public void SetTime(float remaining)
	{
		int sec = Mathf.Max(0, Mathf.CeilToInt(remaining));
		TimeLabel.Text = I18n.T("ui.hud.countdown", sec / 60, sec % 60);
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
				label.Text = $"{I18n.CardName(item.ItemId)} Lv{item.Level}";
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
