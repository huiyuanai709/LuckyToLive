using Godot;

/// <summary>
/// 驱动 AnimatedSprite2D 的 idle / walk / attack 帧动画，并处理朝向翻转与受击闪红。
/// </summary>
public sealed class UnitSpriteAnim
{
	private readonly AnimatedSprite2D _sprite;
	private Vector2 _baseScale;
	private bool _moving;
	private bool _attacking;
	private float _facingX = 1f;
	private float _hitT;
	private float _walkSpeed = 10f;

	public Vector2 Offset => Vector2.Zero;
	public Vector2 Squash => Vector2.One;
	public float Rot => 0f;
	public float FacingX => _facingX;
	public bool HitFlash => _hitT > 0f;

	public UnitSpriteAnim(AnimatedSprite2D sprite, Vector2 baseScale)
	{
		_sprite = sprite;
		_baseScale = baseScale;
		if (_sprite != null)
		{
			_sprite.Scale = _baseScale;
			_sprite.AnimationFinished += OnAnimationFinished;
		}
	}

	public void SetBaseScale(Vector2 scale) => _baseScale = scale;

	public void SetMoving(bool moving) => _moving = moving;

	public void SetFacingX(float x)
	{
		if (Mathf.Abs(x) > 0.05f)
			_facingX = Mathf.Sign(x);
	}

	public void SetWalkHz(float hz) => _walkSpeed = Mathf.Clamp(hz, 5f, 16f);

	public void PlayAttack(float _duration = 0.22f)
	{
		if (_sprite?.SpriteFrames == null) return;
		if (!_sprite.SpriteFrames.HasAnimation(CharacterArt.AnimAttack)) return;
		_attacking = true;
		_sprite.SpeedScale = 1f;
		_sprite.Play(CharacterArt.AnimAttack);
	}

	public void PlayHit(float duration = 0.14f) => _hitT = duration;

	public void Update(float dt)
	{
		if (_hitT > 0f) _hitT = Mathf.Max(0f, _hitT - dt);
		if (_sprite == null || !GodotObject.IsInstanceValid(_sprite)) return;

		_sprite.FlipH = _facingX < 0f;
		_sprite.Scale = _baseScale;
		_sprite.Modulate = HitFlash ? new Color(1f, 0.4f, 0.4f) : Colors.White;

		if (_attacking) return;
		if (_sprite.SpriteFrames == null) return;

		string want = _moving ? CharacterArt.AnimWalk : CharacterArt.AnimIdle;
		if (!_sprite.SpriteFrames.HasAnimation(want)) return;

		if (_sprite.Animation != want || !_sprite.IsPlaying())
		{
			_sprite.SpeedScale = _moving ? _walkSpeed / 10f : 1f;
			_sprite.Play(want);
		}
		else if (_moving)
		{
			_sprite.SpeedScale = _walkSpeed / 10f;
		}
	}

	private void OnAnimationFinished()
	{
		if (_sprite == null || !GodotObject.IsInstanceValid(_sprite)) return;
		if (_sprite.Animation != CharacterArt.AnimAttack) return;
		_attacking = false;
		string next = _moving ? CharacterArt.AnimWalk : CharacterArt.AnimIdle;
		if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation(next))
			_sprite.Play(next);
	}
}
