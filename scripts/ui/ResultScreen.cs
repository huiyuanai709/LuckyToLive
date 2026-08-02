using Godot;

public partial class ResultScreen : CanvasLayer
{
	[Signal] public delegate void ContinuePressedEventHandler();

	public void Setup(bool victory, int score, string rank, int currencyGain, int kills, int elites, int goals)
	{
		ProcessMode = ProcessModeEnum.Always;
		var dim = new ColorRect { Color = new Color(0, 0, 0, 0.7f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		var panel = new PanelContainer { Position = new Vector2(280, 140) };
		panel.CustomMinimumSize = new Vector2(400, 300);
		AddChild(panel);

		var v = new VBoxContainer();
		panel.AddChild(v);
		v.AddChild(new Label { Text = victory ? "生存成功！" : "英雄倒下了" });
		v.AddChild(new Label { Text = $"评级 {rank}  分数 {score}" });
		v.AddChild(new Label { Text = $"击杀 {kills}  精英 {elites}  分钟目标 {goals}" });
		v.AddChild(new Label { Text = $"获得货币 +{currencyGain}  (总 {Game.Instance.MetaCurrency})" });

		var btn = new Button { Text = "返回选英雄" };
		btn.Pressed += () => EmitSignal(SignalName.ContinuePressed);
		v.AddChild(btn);
	}
}
