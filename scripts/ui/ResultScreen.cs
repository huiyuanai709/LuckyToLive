using Godot;

public partial class ResultScreen : CanvasLayer
{
	[Signal] public delegate void ContinuePressedEventHandler();

	public void Setup(
		bool victory, int score, string rank, int currencyGain,
		int kills, int elites, int goals,
		int synergies = 0, int bossKills = 0)
	{
		ProcessMode = ProcessModeEnum.Always;
		Layer = 100;
		var dim = new ColorRect { Color = new Color(0, 0, 0, 0.7f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(center);

		var panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(420, 360);
		center.AddChild(panel);

		var v = new VBoxContainer();
		v.Alignment = BoxContainer.AlignmentMode.Center;
		v.AddThemeConstantOverride("separation", 10);
		panel.AddChild(v);

		var title = MakeCenteredLabel(
			victory ? I18n.T("ui.result.victory") : I18n.T("ui.result.defeat"),
			fontSize: 26,
			color: victory ? new Color(0.55f, 0.95f, 0.65f) : new Color(1f, 0.55f, 0.5f));
		v.AddChild(title);

		string charName = string.IsNullOrWhiteSpace(Game.Instance.CharacterName)
			? I18n.HeroName(Game.Instance.SelectedHero)
			: Game.Instance.CharacterName;
		v.AddChild(MakeCenteredLabel(
			I18n.T("ui.result.character", charName, I18n.HeroName(Game.Instance.SelectedHero)),
			color: new Color(0.9f, 0.85f, 0.55f)));
		v.AddChild(MakeCenteredLabel(I18n.T("ui.result.rank", rank, score)));
		v.AddChild(MakeCenteredLabel(I18n.T("ui.result.stats", kills, elites, goals)));
		v.AddChild(MakeCenteredLabel(I18n.T("ui.result.extra", synergies, bossKills)));
		v.AddChild(MakeCenteredLabel(I18n.T("ui.result.currency", currencyGain, Game.Instance.MetaCurrency)));

		var btn = new Button
		{
			Text = I18n.T("ui.result.continue"),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			CustomMinimumSize = new Vector2(220, 36),
		};
		btn.Pressed += () => EmitSignal(SignalName.ContinuePressed);
		v.AddChild(btn);
	}

	private static Label MakeCenteredLabel(string text, int fontSize = 0, Color? color = null)
	{
		var label = new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		if (fontSize > 0)
			label.AddThemeFontSizeOverride("font_size", fontSize);
		if (color != null)
			label.AddThemeColorOverride("font_color", color.Value);
		return label;
	}
}
