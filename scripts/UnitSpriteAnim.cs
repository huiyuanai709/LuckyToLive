using Godot;

/// <summary>
/// 驱动 AnimatedSprite2D 的 idle / walk / attack 帧动画，并处理朝向翻转与受击闪红。
/// 多帧图集到位后不再叠程序化弹跳，避免「抖动」盖过真正的帧动画。
/// 移动时优先播 walk，避免自动开火一直占住 attack 导致看不出走路帧。
/// FlipH 依据贴图默认朝向：朝右图集在往左时翻；朝左图集在往右时翻。
/// </summary>
public sealed class UnitSpriteAnim
{
	private readonly AnimatedSprite2D _sprite;
	private Vector2 _baseScale;
	private bool _moving;
	private float _attackT;
	private float _facingX = 1f;
	private float _hitT;
	private float _walkSpeed = 10f;
	private float _time;
	private string _current = "";
	private bool _multiFrame;
	/// <summary>贴图未翻转时是否朝右（猎人朝右；法师/多数敌人朝左）。</summary>
	private bool _artFacesRight;

	public Vector2 Offset { get; private set; }
	public Vector2 Squash => Vector2.One;
	public float Rot => 0f;
	public float FacingX => _facingX;
	public bool HitFlash => _hitT > 0f;

	public UnitSpriteAnim(AnimatedSprite2D sprite, Vector2 baseScale)
	{
		_sprite = sprite;
		_baseScale = baseScale;
		if (_sprite != null)
			_sprite.Scale = _baseScale;
		RefreshMultiFrameFlag();
	}

	public void SetBaseScale(Vector2 scale) => _baseScale = scale;

	public void SetArtFacesRight(bool facesRight) => _artFacesRight = facesRight;

	public void SetMoving(bool moving) => _moving = moving;

	public void SetFacingX(float x)
	{
		if (Mathf.Abs(x) > 0.05f)
			_facingX = Mathf.Sign(x);
	}

	public void SetWalkHz(float hz) => _walkSpeed = Mathf.Clamp(hz, 5f, 16f);

	/// <summary>贴图刚换上时调用，重新判断是否有可用多帧。</summary>
	public void RefreshMultiFrameFlag()
	{
		_multiFrame = false;
		if (_sprite?.SpriteFrames == null) return;
		var sf = _sprite.SpriteFrames;
		_multiFrame = sf.HasAnimation(CharacterArt.AnimWalk)
			&& sf.GetFrameCount(CharacterArt.AnimWalk) > 1;
	}

	public void PlayAttack(float duration = 0.28f)
	{
		if (_sprite?.SpriteFrames == null) return;
		if (!_sprite.SpriteFrames.HasAnimation(CharacterArt.AnimAttack)) return;
		int count = _sprite.SpriteFrames.GetFrameCount(CharacterArt.AnimAttack);
		if (count <= 0) return;
		// 已在播攻击则不重置，保证完整播完
		if (_attackT > 0f) return;

		float fps = (float)_sprite.SpriteFrames.GetAnimationSpeed(CharacterArt.AnimAttack);
		float sheetDur = fps > 0.01f ? count / fps : duration;
		_attackT = Mathf.Max(0.16f, Mathf.Max(duration, sheetDur * 0.92f));

		// 站立时立刻切攻击姿；移动中只记状态，由 Update 决定是否展示
		if (!_moving)
			PlayAnim(CharacterArt.AnimAttack, 1f);
	}

	public void PlayHit(float duration = 0.14f) => _hitT = duration;

	public void Update(float dt)
	{
		_time += dt;
		if (_hitT > 0f) _hitT = Mathf.Max(0f, _hitT - dt);
		if (_attackT > 0f) _attackT = Mathf.Max(0f, _attackT - dt);
		if (_sprite == null || !GodotObject.IsInstanceValid(_sprite)) return;

		// 多帧：脚底对齐后的图集自带动作，不再叠 Y 向弹跳
		// 单帧回退：保留轻微呼吸/迈步，避免完全静止
		if (_multiFrame)
		{
			Offset = Vector2.Zero;
		}
		else
		{
			float bob = _moving
				? -Mathf.Abs(Mathf.Sin(_time * _walkSpeed)) * 3.5f
				: Mathf.Sin(_time * 2.6f) * 1.2f;
			Offset = new Vector2(0f, bob);
		}

		// 朝右图集：往左翻；朝左图集：往右翻
		_sprite.FlipH = _artFacesRight ? _facingX < 0f : _facingX > 0f;
		_sprite.Position = Offset;
		_sprite.Scale = _baseScale;
		_sprite.Modulate = HitFlash ? new Color(1f, 0.4f, 0.4f) : Colors.White;
		if (_sprite.SpriteFrames == null) return;

		if (_moving)
		{
			// walk 基础 fps≈10，再按移速微调到约 0.9–1.4x
			EnsureAnim(CharacterArt.AnimWalk, Mathf.Clamp(_walkSpeed / 10f, 0.85f, 1.4f));
			return;
		}

		if (_attackT > 0f)
		{
			EnsureAnim(CharacterArt.AnimAttack, 1f);
			return;
		}

		EnsureAnim(CharacterArt.AnimIdle, 1f);
	}

	private void EnsureAnim(string anim, float speedScale)
	{
		if (!_sprite.SpriteFrames.HasAnimation(anim)) return;
		if (_sprite.SpriteFrames.GetFrameCount(anim) <= 0) return;
		if (_current != anim || !_sprite.IsPlaying())
			PlayAnim(anim, speedScale);
		else
			_sprite.SpeedScale = speedScale;
	}

	private void PlayAnim(string anim, float speedScale)
	{
		_current = anim;
		_sprite.SpeedScale = speedScale;
		_sprite.Play(anim);
	}
}
