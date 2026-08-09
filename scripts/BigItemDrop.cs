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
		float bobY = Mathf.Sin(_bob) * 4.5f;
		float glow = 0.4f + 0.18f * Mathf.Sin(_pulse);

		// 地面光圈（提示可拾取，本身不是掉落本体）
		DrawCircle(new Vector2(0, 16), 40, new Color(1f, 0.85f, 0.25f, glow * 0.45f));
		DrawArc(new Vector2(0, 16), 42, 0f, Mathf.Tau, 36, new Color(1f, 0.9f, 0.4f, 0.75f), 2.5f);

		// 宝箱体（放大，远镜头也能认出是箱子）
		var body = new Rect2(-28, -8 + bobY, 56, 34);
		DrawRect(body, new Color(0.48f, 0.28f, 0.12f));
		DrawRect(new Rect2(body.Position + new Vector2(3, 3), body.Size - new Vector2(6, 6)), new Color(0.72f, 0.45f, 0.2f));
		// 木纹条
		for (int i = 0; i < 3; i++)
		{
			float y = body.Position.Y + 8 + i * 8;
			DrawLine(new Vector2(body.Position.X + 4, y), new Vector2(body.End.X - 4, y),
				new Color(0.4f, 0.22f, 0.1f, 0.55f), 1.2f);
		}

		// 箱盖
		var lid = new Rect2(-32, -26 + bobY, 64, 20);
		DrawRect(lid, new Color(0.4f, 0.22f, 0.1f));
		DrawRect(new Rect2(lid.Position + new Vector2(2, 2), new Vector2(lid.Size.X - 4, 10)), new Color(0.58f, 0.34f, 0.16f));
		DrawRect(new Rect2(lid.Position.X, lid.End.Y - 4f, lid.Size.X, 4f), new Color(0.3f, 0.15f, 0.06f));

		// 金属包角
		DrawRect(new Rect2(body.Position.X, body.Position.Y, 8, 8), new Color(0.85f, 0.7f, 0.25f));
		DrawRect(new Rect2(body.End.X - 8, body.Position.Y, 8, 8), new Color(0.85f, 0.7f, 0.25f));
		DrawRect(new Rect2(body.Position.X, body.End.Y - 8, 8, 8), new Color(0.85f, 0.7f, 0.25f));
		DrawRect(new Rect2(body.End.X - 8, body.End.Y - 8, 8, 8), new Color(0.85f, 0.7f, 0.25f));

		// 锁扣
		DrawRect(new Rect2(-8, -6 + bobY, 16, 16), new Color(0.95f, 0.78f, 0.25f));
		DrawCircle(new Vector2(0, 2 + bobY), 3.5f, new Color(0.45f, 0.28f, 0.08f));

		DrawRect(body, new Color(0.2f, 0.1f, 0.04f), false, 2f);
		DrawRect(lid, new Color(0.2f, 0.1f, 0.04f), false, 2f);

		// 卡面图标浮在箱顶
		if (_icon != null)
		{
			float s = 40f;
			Vector2 size = _icon.GetSize();
			float scale = s / Mathf.Max(size.X, size.Y);
			Vector2 draw = size * scale;
			DrawTextureRect(_icon, new Rect2(-draw.X * 0.5f, -58 + bobY, draw.X, draw.Y), false);
		}
		else
		{
			DrawCircle(new Vector2(0, -48 + bobY), 9, new Color(0.85f, 0.45f, 1f));
		}
	}
}
