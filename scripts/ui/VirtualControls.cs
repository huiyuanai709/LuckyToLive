using Godot;

/// <summary>
/// 触屏虚拟摇杆（左下）+ 冲刺键（右下）。无触屏的桌面端不显示，仍用 WASD / 手柄。
/// </summary>
public partial class VirtualControls : CanvasLayer
{
	public static VirtualControls Instance { get; private set; }

	/// <summary>归一化移动向量，长度 ≤ 1。</summary>
	public Vector2 MoveVector { get; private set; }

	private bool _dashQueued;
	private Control _root;
	private StickPad _stick;
	private DashPad _dash;
	private bool _uiBuilt;

	public static bool ShouldShow()
	{
		// 云测 / 强制开启（桌面无触屏时也可验收 UI）
		if (!string.IsNullOrEmpty(OS.GetEnvironment("LUCKY_TOUCH_CONTROLS")))
			return true;
		if (OS.HasFeature("mobile") || OS.HasFeature("web_android") || OS.HasFeature("web_ios"))
			return true;
		return DisplayServer.IsTouchscreenAvailable();
	}

	public bool ConsumeDash()
	{
		if (!_dashQueued) return false;
		_dashQueued = false;
		return true;
	}

	public override void _Ready()
	{
		Instance = this;
		Layer = 8;
		ProcessMode = ProcessModeEnum.Always;
		if (ShouldShow())
			BuildUi();
	}

	public override void _ExitTree()
	{
		if (Instance == this) Instance = null;
	}

	public override void _Process(double delta)
	{
		bool want = ShouldShow() && !(GetTree()?.Paused ?? false);
		if (want && !_uiBuilt)
			BuildUi();
		if (_root != null)
			_root.Visible = want;
		if (!want)
		{
			MoveVector = Vector2.Zero;
			_dashQueued = false;
			_stick?.Reset();
		}
	}

	private void BuildUi()
	{
		if (_uiBuilt) return;
		_uiBuilt = true;

		_root = new Control();
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_root);

		_stick = new StickPad();
		_stick.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		_stick.Position = new Vector2(28, -236);
		_stick.Size = new Vector2(200, 200);
		_stick.VectorChanged += v => MoveVector = v;
		_root.AddChild(_stick);

		_dash = new DashPad();
		_dash.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
		_dash.Position = new Vector2(-148, -148);
		_dash.Size = new Vector2(112, 112);
		_dash.DashPressed += () => _dashQueued = true;
		_root.AddChild(_dash);
	}

	/// <summary>左下角虚拟摇杆。</summary>
	private partial class StickPad : Control
	{
		[Signal] public delegate void VectorChangedEventHandler(Vector2 vector);

		private const float Deadzone = 0.12f;
		private int _pointer = int.MinValue;
		private Vector2 _knobOffset;
		private float _radius;

		public override void _Ready()
		{
			MouseFilter = MouseFilterEnum.Stop;
			_radius = Mathf.Min(Size.X, Size.Y) * 0.5f - 10f;
			Resized += () => _radius = Mathf.Min(Size.X, Size.Y) * 0.5f - 10f;
		}

		public void Reset()
		{
			_pointer = int.MinValue;
			_knobOffset = Vector2.Zero;
			QueueRedraw();
		}

		public override void _GuiInput(InputEvent @event)
		{
			if (@event is InputEventScreenTouch touch)
			{
				if (touch.Pressed && _pointer == int.MinValue)
				{
					_pointer = touch.Index;
					ApplyLocal(touch.Position);
					AcceptEvent();
				}
				else if (!touch.Pressed && touch.Index == _pointer)
				{
					ClearPointer();
					AcceptEvent();
				}
				return;
			}

			if (@event is InputEventScreenDrag drag && drag.Index == _pointer)
			{
				ApplyLocal(drag.Position);
				AcceptEvent();
				return;
			}

			// 鼠标便于编辑器 / 强制触控模式下调试
			if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
			{
				if (mb.Pressed && _pointer == int.MinValue)
				{
					_pointer = -1;
					ApplyLocal(mb.Position);
					AcceptEvent();
				}
				else if (!mb.Pressed && _pointer == -1)
				{
					ClearPointer();
					AcceptEvent();
				}
				return;
			}

			if (@event is InputEventMouseMotion mm && _pointer == -1 && (mm.ButtonMask & MouseButtonMask.Left) != 0)
			{
				ApplyLocal(mm.Position);
				AcceptEvent();
			}
		}

		private void ApplyLocal(Vector2 local)
		{
			Vector2 center = Size * 0.5f;
			Vector2 delta = local - center;
			float len = delta.Length();
			if (len > _radius && len > 0.001f)
				delta = delta * (_radius / len);
			_knobOffset = delta;
			float norm = _radius > 0.001f ? delta.Length() / _radius : 0f;
			Vector2 v = norm < Deadzone || len < 0.001f
				? Vector2.Zero
				: (delta / _radius);
			if (v.LengthSquared() > 1f) v = v.Normalized();
			EmitSignal(SignalName.VectorChanged, v);
			QueueRedraw();
		}

		private void ClearPointer()
		{
			_pointer = int.MinValue;
			_knobOffset = Vector2.Zero;
			EmitSignal(SignalName.VectorChanged, Vector2.Zero);
			QueueRedraw();
		}

		public override void _Draw()
		{
			Vector2 center = Size * 0.5f;
			float baseR = _radius + 8f;
			DrawCircle(center, baseR, new Color(0.12f, 0.14f, 0.18f, 0.45f));
			DrawArc(center, baseR, 0f, Mathf.Tau, 48, new Color(1f, 1f, 1f, 0.28f), 2.5f, true);
			DrawCircle(center + _knobOffset, 34f, new Color(0.85f, 0.9f, 1f, 0.55f));
			DrawArc(center + _knobOffset, 34f, 0f, Mathf.Tau, 32, new Color(1f, 1f, 1f, 0.55f), 2f, true);
		}
	}

	/// <summary>右下角冲刺键。</summary>
	private partial class DashPad : Control
	{
		[Signal] public delegate void DashPressedEventHandler();

		private int _pointer = int.MinValue;
		private bool _held;
		private Label _label;

		public override void _Ready()
		{
			MouseFilter = MouseFilterEnum.Stop;
			_label = new Label
			{
				Text = I18n.T("ui.hud.dash"),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			_label.SetAnchorsPreset(LayoutPreset.FullRect);
			_label.AddThemeColorOverride("font_color", Colors.White);
			_label.AddThemeFontSizeOverride("font_size", 22);
			AddChild(_label);
			if (I18n.Instance != null)
				I18n.Instance.LocaleChanged += OnLocale;
		}

		public override void _ExitTree()
		{
			if (I18n.Instance != null)
				I18n.Instance.LocaleChanged -= OnLocale;
		}

		private void OnLocale(string _) => _label.Text = I18n.T("ui.hud.dash");

		public override void _GuiInput(InputEvent @event)
		{
			if (@event is InputEventScreenTouch touch)
			{
				if (touch.Pressed && _pointer == int.MinValue)
				{
					_pointer = touch.Index;
					Press();
					AcceptEvent();
				}
				else if (!touch.Pressed && touch.Index == _pointer)
				{
					Release();
					AcceptEvent();
				}
				return;
			}

			if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
			{
				if (mb.Pressed && _pointer == int.MinValue)
				{
					_pointer = -1;
					Press();
					AcceptEvent();
				}
				else if (!mb.Pressed && _pointer == -1)
				{
					Release();
					AcceptEvent();
				}
			}
		}

		private void Press()
		{
			_held = true;
			EmitSignal(SignalName.DashPressed);
			QueueRedraw();
		}

		private void Release()
		{
			_pointer = int.MinValue;
			_held = false;
			QueueRedraw();
		}

		public override void _Draw()
		{
			Vector2 center = Size * 0.5f;
			float r = Mathf.Min(Size.X, Size.Y) * 0.48f;
			Color fill = _held
				? new Color(0.35f, 0.75f, 1f, 0.7f)
				: new Color(0.15f, 0.2f, 0.28f, 0.55f);
			DrawCircle(center, r, fill);
			DrawArc(center, r, 0f, Mathf.Tau, 40, new Color(1f, 1f, 1f, 0.4f), 2.5f, true);
		}
	}
}
