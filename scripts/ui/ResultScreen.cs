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
		var dim = new ColorRect { Color = new Color(0, 0, 0, 0.7f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		var panel = new PanelContainer { Position = new Vector2(280, 120) };
		panel.CustomMinimumSize = new Vector2(400, 340);
		AddChild(panel);

		var v = new VBoxContainer();
		panel.AddChild(v);
		v.AddChild(new Label { Text = victory ? I18n.T("ui.result.victory") : I18n.T("ui.result.defeat") });
		string charName = string.IsNullOrWhiteSpace(Game.Instance.CharacterName)
			? I18n.HeroName(Game.Instance.SelectedHero)
			: Game.Instance.CharacterName;
		v.AddChild(new Label
		{
			Text = I18n.T("ui.result.character", charName, I18n.HeroName(Game.Instance.SelectedHero)),
		});
		v.AddChild(new Label { Text = I18n.T("ui.result.rank", rank, score) });
		v.AddChild(new Label { Text = I18n.T("ui.result.stats", kills, elites, goals) });
		v.AddChild(new Label { Text = I18n.T("ui.result.extra", synergies, bossKills) });
		v.AddChild(new Label { Text = I18n.T("ui.result.currency", currencyGain, Game.Instance.MetaCurrency) });

		var btn = new Button { Text = I18n.T("ui.result.continue") };
		btn.Pressed += () => EmitSignal(SignalName.ContinuePressed);
		v.AddChild(btn);
	}
}
