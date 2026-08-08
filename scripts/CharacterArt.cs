using Godot;
using System.Collections.Generic;

/// <summary>
/// 角色 / 怪物多帧图集。约定：res://assets/{characters|enemies}/{name}_sheet.png
/// 布局：4 列 × 3 行（idle / walk / attack），单元格正方形。
/// 切片时按 idle 首帧脚底与水平重心对齐，避免帧间跳动看起来像抖动。
/// 各图集默认朝向不一：见 <see cref="HeroArtFacesRight"/> / 敌人默认朝左。
/// </summary>
public static class CharacterArt
{
	public const string AnimIdle = "idle";
	public const string AnimWalk = "walk";
	public const string AnimAttack = "attack";

	private static readonly Dictionary<string, SpriteFrames> Cache = new();

	/// <summary>猎人/战士图集默认朝右；法师默认朝左。</summary>
	public static bool HeroArtFacesRight(HeroId id) =>
		id is HeroId.Hunter or HeroId.Warrior;

	/// <summary>敌人图集默认朝左。</summary>
	public static bool EnemyArtFacesRight(bool elite = false, MapId? map = null) => false;

	public static SpriteFrames ForHero(HeroId id)
	{
		string name = id switch
		{
			HeroId.Warrior => "hero_warrior",
			HeroId.Mage => "hero_mage",
			_ => "hero_hunter",
		};
		return GetOrBuild($"res://assets/characters/{name}_sheet.png",
			$"res://assets/characters/{name}.png", 128);
	}

	public static SpriteFrames ForEnemy(bool elite, MapId? map = null)
	{
		var theme = MapCatalog.Get(map ?? Game.Instance?.SelectedMap ?? MapId.Island);
		string name = elite ? theme.EliteEnemy : theme.BasicEnemy;
		int cell = elite ? 112 : 96;
		var frames = GetOrBuild($"res://assets/enemies/{name}_sheet.png",
			$"res://assets/enemies/{name}.png", cell);
		// 缺主题贴图时回退到默认岛怪
		if (frames == null && name != "enemy_basic" && name != "enemy_elite")
		{
			string fallback = elite ? "enemy_elite" : "enemy_basic";
			frames = GetOrBuild($"res://assets/enemies/{fallback}_sheet.png",
				$"res://assets/enemies/{fallback}.png", cell);
		}
		return frames;
	}

	private static SpriteFrames GetOrBuild(string sheetPath, string fallbackPath, int cell)
	{
		string key = sheetPath + "|" + cell;
		if (Cache.TryGetValue(key, out var cached) && cached != null)
			return cached;

		var frames = BuildFromSheet(sheetPath, cell, 4, 3)
			?? BuildSingleFrame(fallbackPath);
		if (frames != null)
		{
			int walkFrames = frames.HasAnimation(AnimWalk) ? frames.GetFrameCount(AnimWalk) : 0;
			GD.Print($"CharacterArt: {sheetPath} walk_frames={walkFrames}");
		}
		Cache[key] = frames;
		return frames;
	}

	private static SpriteFrames BuildFromSheet(string path, int cell, int cols, int rows)
	{
		Texture2D sheet = LoadTexture(path);
		if (sheet == null) return null;
		int w = sheet.GetWidth();
		int h = sheet.GetHeight();
		if (w < cell * cols || h < cell * rows) return null;

		Image src = sheet.GetImage();
		if (src == null)
		{
			// CompressedTexture2D 有时需要先拿不到 CPU 图，改走文件
			string abs = ProjectSettings.GlobalizePath(path);
			if (System.IO.File.Exists(abs))
				src = Image.LoadFromFile(abs);
		}
		if (src == null) return null;

		// 以 idle 第 0 帧为锚点：脚底 Y + 水平重心，后续帧平移对齐
		if (!TryOpaqueMetrics(src, 0, 0, cell, out _, out int refFeetY, out float refCx, out _))
			return null;

		var frames = new SpriteFrames();
		AddRowAnim(frames, AnimIdle, src, row: 0, cols, cell, refFeetY, refCx,
			fps: 5f, loop: true, idleTiming: true);
		AddRowAnim(frames, AnimWalk, src, row: 1, cols, cell, refFeetY, refCx,
			fps: 10f, loop: true, idleTiming: false);
		AddRowAnim(frames, AnimAttack, src, row: 2, cols, cell, refFeetY, refCx,
			fps: 12f, loop: false, idleTiming: false);
		return frames;
	}

	private static void AddRowAnim(
		SpriteFrames frames, string anim, Image src, int row, int cols, int cell,
		int refFeetY, float refCx, float fps, bool loop, bool idleTiming)
	{
		if (frames.HasAnimation(anim))
			frames.RemoveAnimation(anim);
		frames.AddAnimation(anim);
		frames.SetAnimationSpeed(anim, fps);
#pragma warning disable CS0618 // 兼容 Godot 4.7-dev 运行时
		frames.SetAnimationLoop(anim, loop);
#pragma warning restore CS0618
		for (int c = 0; c < cols; c++)
		{
			var region = new Rect2I(c * cell, row * cell, cell, cell);
			Image slice = src.GetRegion(region);
			Image aligned = AlignToAnchor(slice, cell, refFeetY, refCx);
			var tex = ImageTexture.CreateFromImage(aligned);
			// idle：多数时间停在站立帧，眨眼帧一闪而过，避免整行均速循环像抖动
			float duration = 1.0f;
			if (idleTiming)
				duration = (c == 2) ? 0.35f : 1.6f;
			frames.AddFrame(anim, tex, duration);
		}
	}

	/// <summary>把单元格内容平移到与参考帧相同的脚底高度与水平重心。</summary>
	private static Image AlignToAnchor(Image slice, int cell, int refFeetY, float refCx)
	{
		if (!TryOpaqueMetrics(slice, 0, 0, cell, out _, out int feetY, out float cx, out _))
			return slice;

		int dx = Mathf.RoundToInt(refCx - cx);
		int dy = refFeetY - feetY;
		if (dx == 0 && dy == 0)
			return slice;

		var aligned = Image.CreateEmpty(cell, cell, false, slice.GetFormat());
		aligned.Fill(new Color(0, 0, 0, 0));
		aligned.BlitRect(slice, new Rect2I(0, 0, cell, cell), new Vector2I(dx, dy));
		return aligned;
	}

	private static bool TryOpaqueMetrics(
		Image img, int ox, int oy, int cell,
		out int minY, out int feetY, out float cx, out float cy)
	{
		minY = cell;
		feetY = -1;
		cx = cy = 0f;
		long sumX = 0, sumY = 0;
		int count = 0;
		int maxY = -1;
		for (int y = 0; y < cell; y++)
		{
			for (int x = 0; x < cell; x++)
			{
				Color p = img.GetPixel(ox + x, oy + y);
				if (p.A < 0.06f) continue;
				if (y < minY) minY = y;
				if (y > maxY) maxY = y;
				sumX += x;
				sumY += y;
				count++;
			}
		}
		if (count == 0 || maxY < 0) return false;
		feetY = maxY;
		cx = sumX / (float)count;
		cy = sumY / (float)count;
		return true;
	}

	private static SpriteFrames BuildSingleFrame(string path)
	{
		Texture2D tex = LoadTexture(path);
		var frames = new SpriteFrames();
		foreach (string anim in new[] { AnimIdle, AnimWalk, AnimAttack })
		{
			frames.AddAnimation(anim);
			frames.SetAnimationSpeed(anim, anim == AnimWalk ? 8f : 5f);
#pragma warning disable CS0618
			frames.SetAnimationLoop(anim, anim != AnimAttack);
#pragma warning restore CS0618
			if (tex != null)
				frames.AddFrame(anim, tex);
		}
		return frames;
	}

	private static Texture2D LoadTexture(string path)
	{
		if (string.IsNullOrEmpty(path)) return null;
		if (ResourceLoader.Exists(path))
		{
			var tex = GD.Load<Texture2D>(path);
			if (tex != null) return tex;
		}
		string abs = ProjectSettings.GlobalizePath(path);
		if (!System.IO.File.Exists(abs)) return null;
		var img = Image.LoadFromFile(abs);
		return img != null ? ImageTexture.CreateFromImage(img) : null;
	}
}
