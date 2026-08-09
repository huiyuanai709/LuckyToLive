using Godot;

/// <summary>
/// 精英掉落宝箱：靠近自动开启选卡。不用圆点占位。
/// </summary>
public partial class BigItemDrop : Node2D
{
	public CardDef Card;

	private float _bob;
	private Texture2D _icon;
	private float _pulse;

	public override void _Ready()
	{
		AddToGroup("big_drops");
		ZIndex = 2;
		TryLoadIcon();
		QueueRedraw();
	}

	private void TryLoadIcon()
	{
		if (Card == null || string.IsNullOrEmpty(Card.Id)) return;
		string path = $"res://assets/cards/{Card.Id}.png";
		if (!ResourceLoader.Exists(path) && !string.IsNullOrEmpty(Card.GrantsItemId))
			path = $"res://assets/cards/{Card.GrantsItemId}.png";
		if (ResourceLoader.Exists(path))
			_icon = GD.Load<Texture2D>(path);
	}

	public override void _Process(double delta)
	{
		_bob += (float)delta * 3.2f;
		_pulse += (float)delta * 4.5f;
		QueueRedraw();
	}

	public override void _Draw()
	{
		float bobY = Mathf.Sin(_bob) * 3.5f;
		float glow = 0.35f + 0.15f * Mathf.Sin(_pulse);

		// 地面光圈
		DrawCircle(new Vector2(0, 10), 28, new Color(1f, 0.85f, 0.25f, glow * 0.55f));
		DrawArc(new Vector2(0, 10), 30, 0f, Mathf.Tau, 32, new Color(1f, 0.9f, 0.4f, 0.65f), 2f);

		// 宝箱体
		var body = new Rect2(-18, -6 + bobY, 36, 22);
		DrawRect(body, new Color(0.55f, 0.32f, 0.14f));
		DrawRect(new Rect2(body.Position + new Vector2(2, 2), body.Size - new Vector2(4, 4)), new Color(0.72f, 0.45f, 0.2f));

		// 箱盖
		var lid = new Rect2(-20, -18 + bobY, 40, 14);
		DrawRect(lid, new Color(0.45f, 0.26f, 0.12f));
		DrawRect(new Rect2(lid.Position.X, lid.End.Y - 3f, lid.Size.X, 3f), new Color(0.35f, 0.18f, 0.08f));

		// 金属锁扣
		DrawRect(new Rect2(-5, -4 + bobY, 10, 10), new Color(0.95f, 0.78f, 0.25f));
		DrawCircle(new Vector2(0, 1 + bobY), 2.2f, new Color(0.55f, 0.35f, 0.1f));

		// 边线
		DrawRect(body, new Color(0.25f, 0.12f, 0.05f), false, 1.5f);
		DrawRect(lid, new Color(0.25f, 0.12f, 0.05f), false, 1.5f);

		// 卡面图标浮在箱顶
		if (_icon != null)
		{
			float s = 28f;
			Vector2 size = _icon.GetSize();
			float scale = s / Mathf.Max(size.X, size.Y);
			Vector2 draw = size * scale;
			DrawTextureRect(_icon, new Rect2(-draw.X * 0.5f, -42 + bobY, draw.X, draw.Y), false);
		}
		else
		{
			DrawCircle(new Vector2(0, -34 + bobY), 7, new Color(0.85f, 0.45f, 1f));
		}
	}
}
