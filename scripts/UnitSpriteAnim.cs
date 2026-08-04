using Godot;

/// <summary>
/// 驱动 AnimatedSprite2D 的 idle / walk / attack 帧动画，并处理朝向翻转与受击闪红。
/// 移动时优先播 walk，避免自动开火一直占住 attack 导致看不出走路帧。
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
	}

	public void SetBaseScale(Vector2 scale) => _baseScale = scale;

	public void SetMoving(bool moving) => _moving = moving;

	public void SetFacingX(float x)
	{
		if (Mathf.Abs(x) > 0.05f)
			_facingX = Mathf.Sign(x);
	}

	public void SetWalkHz(float hz) => _walkSpeed = Mathf.Clamp(hz, 5f, 16f);

	public void PlayAttack(float duration = 0.28f)
	{
		if (_sprite?.SpriteFrames == null) return;
		if (!_sprite.SpriteFrames.HasAnimation(CharacterArt.AnimAttack)) return;
		if (_sprite.SpriteFrames.GetFrameCount(CharacterArt.AnimAttack) <= 0) return;
		// 已在播攻击则不重置，保证完整播完
		if (_attackT > 0f) return;
		_attackT = Mathf.Max(0.16f, duration);
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

		// 帧动画为主；行走时再叠一点弹跳，远距离也更容易看出在动
		float bob = _moving ? -Mathf.Abs(Mathf.Sin(_time * _walkSpeed)) * 3.5f
			: Mathf.Sin(_time * 2.6f) * 1.2f;
		Offset = new Vector2(0f, bob);

		_sprite.FlipH = _facingX < 0f;
		_sprite.Position = Offset;
		_sprite.Scale = _baseScale;
		_sprite.Modulate = HitFlash ? new Color(1f, 0.4f, 0.4f) : Colors.White;
		if (_sprite.SpriteFrames == null) return;

		if (_moving)
		{
			EnsureAnim(CharacterArt.AnimWalk, _walkSpeed / 8f);
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
