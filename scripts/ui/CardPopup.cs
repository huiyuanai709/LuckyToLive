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
		panel.Position = new Vector2(180, 120);
		panel.CustomMinimumSize = new Vector2(600, 320);
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
			var btn = new Button();
			btn.CustomMinimumSize = new Vector2(180, 220);
			btn.Text = $"{card.Name}\n\n{card.Desc}\n\n[{card.Kind}]";
			string id = card.Id;
			btn.Pressed += () => EmitSignal(SignalName.Chosen, id);
			row.AddChild(btn);
		}
	}
}
