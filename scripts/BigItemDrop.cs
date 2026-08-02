using Godot;

public partial class BigItemDrop : Node2D
{
	public CardDef Card;

	public override void _Ready()
	{
		AddToGroup("big_drops");
		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, 16, new Color(1f, 0.85f, 0.2f, 0.9f));
		DrawCircle(Vector2.Zero, 10, new Color(0.9f, 0.4f, 1f));
	}
}
