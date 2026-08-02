using Godot;

public partial class HeroSelect : CanvasLayer
{
	[Signal] public delegate void HeroPickedEventHandler(int heroId);
	[Signal] public delegate void RefreshRequestedEventHandler();

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Rebuild();
	}

	public void Rebuild()
	{
		foreach (var c in GetChildren())
			((Node)c).QueueFree();

		var dim = new ColorRect { Color = new Color(0.08f, 0.1f, 0.14f, 0.95f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		var title = new Label
		{
			Text = Game.Instance.StarterHero == null ? "选择你的初始英雄（永久免费）" : "选择出战英雄",
			Position = new Vector2(280, 60),
		};
		title.AddThemeColorOverride("font_color", Colors.White);
		AddChild(title);

		var currency = new Label
		{
			Text = $"元进度货币: {Game.Instance.MetaCurrency}",
			Position = new Vector2(280, 90),
		};
		AddChild(currency);

		var hint = new Label
		{
			Text = "WASD 移动 · 自动攻击 · 撑满 5 分钟通关",
			Position = new Vector2(280, 112),
		};
		AddChild(hint);

		var row = new HBoxContainer { Position = new Vector2(120, 160) };
		AddChild(row);

		foreach (HeroId id in System.Enum.GetValues(typeof(HeroId)))
			row.AddChild(MakeCard(id));
	}

	private Control MakeCard(HeroId id)
	{
		bool unlocked = Game.Instance.IsHeroUnlocked(id);
		bool firstPick = Game.Instance.StarterHero == null;
		int cost = Game.Instance.UnlockCost(id);

		var box = new VBoxContainer();
		box.CustomMinimumSize = new Vector2(220, 280);

		string name = id switch
		{
			HeroId.Warrior => "战士",
			HeroId.Mage => "法师",
			_ => "猎人",
		};
		string desc = id switch
		{
			HeroId.Warrior => "近战裂斩 / 冲锋 / 盾墙战旗",
			HeroId.Mage => "冰系+火系可同装 / 射线法阵",
			_ => "穿透箭 / 冰箭 / 宠物占槽",
		};

		string path = id switch
		{
			HeroId.Warrior => "res://assets/characters/hero_warrior.png",
			HeroId.Mage => "res://assets/characters/hero_mage.png",
			_ => "res://assets/characters/hero_hunter.png",
		};
		if (ResourceLoader.Exists(path))
		{
			var tex = new TextureRect
			{
				Texture = GD.Load<Texture2D>(path),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				CustomMinimumSize = new Vector2(96, 96),
			};
			box.AddChild(tex);
		}

		var lbl = new Label { Text = $"{name}\n\n{desc}" };
		lbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		lbl.CustomMinimumSize = new Vector2(200, 100);
		box.AddChild(lbl);

		var btn = new Button();
		if (firstPick || unlocked)
		{
			btn.Text = firstPick ? $"选择 {name}" : $"出战 {name}";
			btn.Pressed += () => EmitSignal(SignalName.HeroPicked, (int)id);
		}
		else
		{
			btn.Text = $"解锁 ({cost} 货币)";
			btn.Pressed += () =>
			{
				if (Game.Instance.TryUnlockHero(id))
					Rebuild();
				else
					btn.Text = "货币不足";
			};
		}
		box.AddChild(btn);
		return box;
	}
}
