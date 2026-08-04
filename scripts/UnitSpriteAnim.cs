using Godot;

/// <summary>
/// 单体贴图的待机 / 行走 / 攻击动效。无多帧图集时用位移、挤压拉伸、摇摆与翻转模拟。
/// </summary>
public sealed class UnitSpriteAnim
{
	private readonly Sprite2D _sprite;
	private readonly Vector2 _baseScale;
	private readonly float _hopAmp;
	private readonly float _swayAmp;
	private readonly float _phaseOffset;

	private float _time;
	private float _attackT;
	private float _attackDur = 0.22f;
	private float _hitT;
	private bool _moving;
	private float _facingX = 1f;
	private float _walkHz = 9f;

	/// <summary>供无贴图回退绘制使用的当前视觉状态。</summary>
	public Vector2 Offset { get; private set; }
	public Vector2 Squash { get; private set; } = Vector2.One;
	public float Rot { get; private set; }
	public float FacingX => _facingX;
	public bool HitFlash => _hitT > 0f;

	public UnitSpriteAnim(Sprite2D sprite, Vector2 baseScale, float hopAmp = 4.8f, float swayAmp = 0.14f, float phaseOffset = 0f)
	{
		_sprite = sprite;
		_baseScale = baseScale;
		_hopAmp = hopAmp;
		_swayAmp = swayAmp;
		_phaseOffset = phaseOffset;
	}

	public void SetMoving(bool moving) => _moving = moving;

	public void SetFacingX(float x)
	{
		if (Mathf.Abs(x) > 0.05f)
			_facingX = Mathf.Sign(x);
	}

	/// <summary>行走节拍（Hz），随移速略调。</summary>
	public void SetWalkHz(float hz) => _walkHz = Mathf.Clamp(hz, 5f, 14f);

	public void PlayAttack(float duration = 0.22f)
	{
		_attackDur = Mathf.Max(0.08f, duration);
		_attackT = _attackDur;
	}

	public void PlayHit(float duration = 0.14f) => _hitT = duration;

	public void Update(float dt)
	{
		_time += dt;
		if (_attackT > 0f) _attackT = Mathf.Max(0f, _attackT - dt);
		if (_hitT > 0f) _hitT = Mathf.Max(0f, _hitT - dt);

		float offsetX = 0f;
		float offsetY = 0f;
		float squashX = 1f;
		float squashY = 1f;
		float rot = 0f;

		if (_attackT > 0f)
		{
			float t = 1f - _attackT / _attackDur;
			float punch = Mathf.Sin(t * Mathf.Pi);
			offsetX = _facingX * punch * 10f;
			offsetY = -punch * 3f;
			squashX = 1f + punch * 0.22f;
			squashY = 1f - punch * 0.16f;
			rot = _facingX * punch * 0.18f;
		}
		else if (_moving)
		{
			float phase = (_time + _phaseOffset) * _walkHz;
			float hop = Mathf.Abs(Mathf.Sin(phase));
			offsetY = -hop * _hopAmp;
			// 落地压扁、腾空拉长
			float land = Mathf.Cos(phase * 2f);
			squashX = 1f + land * 0.14f;
			squashY = 1f - land * 0.16f;
			rot = Mathf.Sin(phase) * _swayAmp;
			offsetX = Mathf.Sin(phase) * 1.8f;
		}
		else
		{
			float breathe = Mathf.Sin((_time + _phaseOffset) * 2.6f);
			offsetY = breathe * 2.0f;
			squashX = 1f + breathe * 0.05f;
			squashY = 1f - breathe * 0.07f;
			rot = breathe * 0.045f;
		}

		Offset = new Vector2(offsetX, offsetY);
		Squash = new Vector2(squashX, squashY);
		Rot = rot;

		if (_sprite != null && GodotObject.IsInstanceValid(_sprite))
		{
			_sprite.FlipH = _facingX < 0f;
			_sprite.Position = Offset;
			_sprite.Rotation = Rot;
			_sprite.Scale = new Vector2(_baseScale.X * squashX, _baseScale.Y * squashY);
			_sprite.Modulate = HitFlash
				? new Color(1f, 0.4f, 0.4f)
				: Colors.White;
		}
	}
}
