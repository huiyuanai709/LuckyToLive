using Godot;
using System.Collections.Generic;

public partial class CardPopup : CanvasLayer
{
	[Signal] public delegate void ChosenEventHandler(string cardId);
	[Signal] public delegate void RerollPressedEventHandler();

	private HBoxContainer _row;
	private Button _rerollBtn;
	private bool _allowReroll;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		// 高于局内虚拟摇杆，避免挡选卡
		Layer = 100;
	}

	public void Setup(string title, List<CardDef> options, bool allowReroll = false)
	{
		_allowReroll = allowReroll;

		var dim = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.55f),
		};
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(center);

		var panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(600, 420);
		center.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddThemeConstantOverride("separation", 16);
		panel.AddChild(vbox);

		var titleLbl = new Label { Text = title };
		titleLbl.HorizontalAlignment = HorizontalAlignment.Center;
		titleLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		vbox.AddChild(titleLbl);

		_row = new HBoxContainer();
		_row.Alignment = BoxContainer.AlignmentMode.Center;
		_row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_row.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_row.AddThemeConstantOverride("separation", 12);
		vbox.AddChild(_row);

		FillOptions(options);

		_rerollBtn = new Button
		{
			Text = RerollLabel(),
			Disabled = !_allowReroll || (Game.Instance?.RerollsLeft ?? 0) <= 0,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
		};
		_rerollBtn.Pressed += () => EmitSignal(SignalName.RerollPressed);
		vbox.AddChild(_rerollBtn);
		_rerollBtn.Visible = _allowReroll;
	}

	public void Rebuild(List<CardDef> options)
	{
		FillOptions(options);
		if (_rerollBtn != null)
		{
			_rerollBtn.Text = RerollLabel();
			_rerollBtn.Disabled = (Game.Instance?.RerollsLeft ?? 0) <= 0;
			_rerollBtn.Visible = _allowReroll;
		}
	}

	private static string RerollLabel()
	{
		int left = Game.Instance?.RerollsLeft ?? 0;
		return I18n.T("ui.card.reroll", left);
	}

	private void FillOptions(List<CardDef> options)
	{
		if (_row == null) return;
		foreach (var c in _row.GetChildren())
			((Node)c).QueueFree();
		if (options == null) return;
		foreach (var card in options)
			_row.AddChild(MakeOptionButton(card));
	}

	private Button MakeOptionButton(CardDef card)
	{
		var btn = new Button();
		btn.CustomMinimumSize = new Vector2(180, 260);

		var stack = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		stack.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		stack.AddThemeConstantOverride("separation", 4);
		btn.AddChild(stack);

		string iconPath = $"res://assets/cards/{card.Id}.png";
		// 进化卡可回退主材料图标
		if (!ResourceLoader.Exists(iconPath) && !string.IsNullOrEmpty(card.GrantsItemId))
			iconPath = $"res://assets/cards/{card.GrantsItemId}.png";
		if (ResourceLoader.Exists(iconPath))
		{
			var tex = new TextureRect
			{
				Texture = GD.Load<Texture2D>(iconPath),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				CustomMinimumSize = new Vector2(96, 96),
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			stack.AddChild(tex);
		}

		var text = new Label
		{
			Text = $"{card.LocalizedName}\n\n{card.LocalizedDesc}\n\n{I18n.KindLabel(card.Kind)}",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(160, 110),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		stack.AddChild(text);

		string id = card.Id;
		btn.Pressed += () => EmitSignal(SignalName.Chosen, id);
		return btn;
	}
}
