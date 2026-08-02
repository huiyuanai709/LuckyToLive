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

		GoalLabel = new Label { Position = new Vector2(16, 108), Text = "分钟目标: 击杀 1 精英" };
		root.AddChild(GoalLabel);

		MsgLabel = new Label { Position = new Vector2(16, 540) };
		root.AddChild(MsgLabel);

		SlotBox = new HBoxContainer { Position = new Vector2(280, 12) };
		root.AddChild(SlotBox);

		AdButton = new Button
		{
			Text = "广告解锁槽位 (+1)",
			Position = new Vector2(700, 12),
			Size = new Vector2(180, 32),
		};
		AdButton.Pressed += () => EmitSignal(SignalName.AdPressed);
		root.AddChild(AdButton);
	}

	public void SetTime(float remaining)
	{
		int sec = Mathf.Max(0, Mathf.CeilToInt(remaining));
		TimeLabel.Text = $"倒计时 {sec / 60}:{sec % 60:00}";
	}

	public void RefreshSlots(Loadout loadout)
	{
		foreach (var c in SlotBox.GetChildren()) ((Node)c).QueueFree();
		int cap = Game.Instance.AvailableSlotsThisRun;
		SlotsLabel.Text = $"槽位 {loadout.Count}/{cap}";
		for (int i = 0; i < cap; i++)
		{
			var panel = new PanelContainer();
			panel.CustomMinimumSize = new Vector2(72, 40);
			var label = new Label();
			label.Text = i < loadout.Slots.Count ? $"{loadout.Slots[i].Name} Lv{loadout.Slots[i].Level}" : "空";
			panel.AddChild(label);
			SlotBox.AddChild(panel);
		}
		AdButton.Disabled = Game.Instance.AdSlotsUnlocked >= Game.MaxAdSlots;
		AdButton.Text = Game.Instance.AdSlotsUnlocked >= Game.MaxAdSlots
			? "广告槽已满"
			: $"广告解锁槽位 ({Game.Instance.AdSlotsUnlocked}/{Game.MaxAdSlots})";
	}
}
