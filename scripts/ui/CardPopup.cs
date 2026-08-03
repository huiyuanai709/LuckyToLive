using Godot;
using System.Collections.Generic;

public partial class CardPopup : CanvasLayer
{
	[Signal] public delegate void ChosenEventHandler(string cardId);

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
	}

	public void Setup(string title, List<CardDef> options)
	{
		var dim = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.55f),
		};
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		var panel = new PanelContainer();
		panel.Position = new Vector2(180, 90);
		panel.CustomMinimumSize = new Vector2(600, 380);
		AddChild(panel);

		var vbox = new VBoxContainer();
		panel.AddChild(vbox);

		var titleLbl = new Label { Text = title };
		titleLbl.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(titleLbl);

		var row = new HBoxContainer();
		row.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddChild(row);

		foreach (var card in options)
		{
			row.AddChild(MakeOptionButton(card));
		}
	}

	private Button MakeOptionButton(CardDef card)
	{
		var btn = new Button();
		btn.CustomMinimumSize = new Vector2(180, 260);

		var stack = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		stack.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		stack.AddThemeConstantOverride("separation", 4);
		btn.AddChild(stack);

		string iconPath = $"res://assets/cards/{card.Id}.png";
		if (ResourceLoader.Exists(iconPath))
		{
			var tex = new TextureRect
			{
				Texture = GD.Load<Texture2D>(iconPath),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				CustomMinimumSize = new Vector2(96, 96),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			stack.AddChild(tex);
		}

		var text = new Label
		{
			Text = $"{card.Name}\n\n{card.Desc}\n\n{KindLabel(card.Kind)}",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(160, 110),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		stack.AddChild(text);

		string id = card.Id;
		btn.Pressed += () => EmitSignal(SignalName.Chosen, id);
		return btn;
	}

	private static string KindLabel(CardKind kind) => kind switch
	{
		CardKind.Weapon => "武器",
		CardKind.Building => "建筑",
		CardKind.Upgrade => "升级",
		CardKind.Passive => "被动",
		CardKind.Pet => "宠物",
		_ => "",
	};
}
