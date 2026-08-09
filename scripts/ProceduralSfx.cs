using Godot;
using System.Collections.Generic;

/// <summary>
/// 运行时合成打击音效：项目没有任何音频资源文件，这里用简单包络的噪声/正弦
/// 现算出 16-bit PCM 波形，避免引入外部素材。调用方式类比 <see cref="FloatingText"/>
/// 的「薄静态外观 + 懒加载资源池」写法。
/// </summary>
public static class ProceduralSfx
{
	private const int MixRate = 22050;
	private const int PoolSize = 12;

	private static readonly Dictionary<string, AudioStreamWav> _clips = new();
	private static readonly List<AudioStreamPlayer2D> _pool = new();
	private static int _nextPlayer = -1;
	private static Node _root;
	private static readonly RandomNumberGenerator _rng = new();

	public static void Play(string id, Vector2? globalPos = null, float pitchJitter = 0.08f, float volumeDb = 0f)
	{
		if (!EnsureRoot()) return;
		var clip = GetOrBuildClip(id);
		if (clip == null) return;

		var player = NextPlayer();
		if (player == null) return;
		player.Stream = clip;
		player.PitchScale = 1f + (_rng.Randf() * 2f - 1f) * pitchJitter;
		player.VolumeDb = volumeDb;
		if (globalPos.HasValue)
			player.GlobalPosition = globalPos.Value;
		player.Play();
	}

	private static bool EnsureRoot()
	{
		if (_root != null && GodotObject.IsInstanceValid(_root)) return true;
		if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null) return false;
		_root = tree.Root;
		_pool.Clear();
		return true;
	}

	private static AudioStreamPlayer2D NextPlayer()
	{
		if (_pool.Count < PoolSize)
		{
			var p = new AudioStreamPlayer2D { Name = $"ProceduralSfx{_pool.Count}" };
			_root.AddChild(p);
			_pool.Add(p);
			return p;
		}
		_nextPlayer = (_nextPlayer + 1) % _pool.Count;
		var player = _pool[_nextPlayer];
		return GodotObject.IsInstanceValid(player) ? player : null;
	}

	private static AudioStreamWav GetOrBuildClip(string id)
	{
		if (_clips.TryGetValue(id, out var cached)) return cached;
		AudioStreamWav wav = id switch
		{
			"hit_light" => BuildNoiseHit(0.05f, 620f, 0.55f),
			"hit_heavy" => BuildNoiseHit(0.11f, 220f, 1f),
			"enemy_death" => BuildSweep(0.16f, 480f, 90f, useNoise: false),
			"hero_hurt" => BuildNoiseHit(0.14f, 160f, 0.9f),
			"dash" => BuildSweep(0.12f, 1600f, 400f, useNoise: true),
			"swing_light" => BuildSweep(0.08f, 2400f, 900f, useNoise: true),
			"swing_heavy" => BuildSweep(0.14f, 1400f, 300f, useNoise: true),
			"crit" => BuildSweep(0.12f, 500f, 1100f, useNoise: false),
			"levelup" => BuildSweep(0.28f, 400f, 1400f, useNoise: false),
			_ => null,
		};
		if (wav != null) _clips[id] = wav;
		return wav;
	}

	/// <summary>短促噪声"砰"声：指数衰减包络 + 低音调打底，用于命中/受击。</summary>
	private static AudioStreamWav BuildNoiseHit(float duration, float toneHz, float amp)
	{
		int n = Mathf.Max(1, (int)(MixRate * duration));
		var data = new byte[n * 2];
		for (int i = 0; i < n; i++)
		{
			float t = i / (float)MixRate;
			float env = Mathf.Exp(-t / (duration * 0.28f));
			float tone = Mathf.Sin(Mathf.Tau * toneHz * t) * 0.4f;
			float noise = (_rng.Randf() * 2f - 1f) * 0.7f;
			WriteSample(data, i, Mathf.Clamp((tone + noise) * env * amp, -1f, 1f));
		}
		return Wrap(data);
	}

	/// <summary>音调（或噪声）扫频：从 fromHz 滑到 toHz，用于死亡/冲刺/挥砍/升级等短音效。</summary>
	private static AudioStreamWav BuildSweep(float duration, float fromHz, float toHz, bool useNoise)
	{
		int n = Mathf.Max(1, (int)(MixRate * duration));
		var data = new byte[n * 2];
		float phase = 0f;
		for (int i = 0; i < n; i++)
		{
			float t = i / (float)MixRate;
			float f = Mathf.Lerp(fromHz, toHz, Mathf.Clamp(t / duration, 0f, 1f));
			phase += Mathf.Tau * f / MixRate;
			// 起落包络：先扬后抑，避免咔哒声
			float env = Mathf.Sin(Mathf.Pi * Mathf.Clamp(t / duration, 0f, 1f));
			float sample = useNoise ? (_rng.Randf() * 2f - 1f) * 0.75f * env : Mathf.Sin(phase) * env;
			WriteSample(data, i, sample);
		}
		return Wrap(data);
	}

	private static void WriteSample(byte[] data, int i, float sample)
	{
		short v = (short)Mathf.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
		data[i * 2] = (byte)(v & 0xFF);
		data[i * 2 + 1] = (byte)((v >> 8) & 0xFF);
	}

	private static AudioStreamWav Wrap(byte[] data) => new()
	{
		Data = data,
		Format = AudioStreamWav.FormatEnum.Format16Bits,
		MixRate = MixRate,
		Stereo = false,
	};
}
