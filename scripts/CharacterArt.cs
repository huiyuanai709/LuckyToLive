using Godot;
using System.Collections.Generic;

/// <summary>
/// 角色 / 怪物多帧图集。约定：res://assets/{characters|enemies}/{name}_sheet.png
/// 布局：4 列 × 3 行（idle / walk / attack），单元格正方形。
/// </summary>
public static class CharacterArt
{
	public const string AnimIdle = "idle";
	public const string AnimWalk = "walk";
	public const string AnimAttack = "attack";

	private static readonly Dictionary<string, SpriteFrames> Cache = new();

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

	public static SpriteFrames ForEnemy(bool elite)
	{
		string name = elite ? "enemy_elite" : "enemy_basic";
		int cell = elite ? 112 : 96;
		return GetOrBuild($"res://assets/enemies/{name}_sheet.png",
			$"res://assets/enemies/{name}.png", cell);
	}

	private static SpriteFrames GetOrBuild(string sheetPath, string fallbackPath, int cell)
	{
		string key = sheetPath + "|" + cell;
		if (Cache.TryGetValue(key, out var cached) && cached != null)
			return cached;

		var frames = BuildFromSheet(sheetPath, cell, 4, 3)
			?? BuildSingleFrame(fallbackPath);
		Cache[key] = frames;
		return frames;
	}

	private static SpriteFrames BuildFromSheet(string path, int cell, int cols, int rows)
	{
		Texture2D sheet = LoadTexture(path);
		if (sheet == null) return null;
		if (sheet.GetWidth() < cell * cols || sheet.GetHeight() < cell * rows)
			return null;

		var frames = new SpriteFrames();
		AddRowAnim(frames, AnimIdle, sheet, row: 0, cols, cell, fps: 5f, loop: true);
		AddRowAnim(frames, AnimWalk, sheet, row: 1, cols, cell, fps: 10f, loop: true);
		AddRowAnim(frames, AnimAttack, sheet, row: 2, cols, cell, fps: 14f, loop: false);
		return frames;
	}

	private static void AddRowAnim(SpriteFrames frames, string anim, Texture2D sheet, int row, int cols, int cell, float fps, bool loop)
	{
		if (frames.HasAnimation(anim))
			frames.RemoveAnimation(anim);
		frames.AddAnimation(anim);
		frames.SetAnimationSpeed(anim, fps);
		frames.SetAnimationLoopMode(anim, loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
		for (int c = 0; c < cols; c++)
		{
			var atlas = new AtlasTexture
			{
				Atlas = sheet,
				Region = new Rect2(c * cell, row * cell, cell, cell),
			};
			frames.AddFrame(anim, atlas);
		}
	}

	private static SpriteFrames BuildSingleFrame(string path)
	{
		Texture2D tex = LoadTexture(path);
		var frames = new SpriteFrames();
		foreach (string anim in new[] { AnimIdle, AnimWalk, AnimAttack })
		{
			frames.AddAnimation(anim);
			frames.SetAnimationSpeed(anim, anim == AnimWalk ? 8f : 5f);
			frames.SetAnimationLoopMode(anim, anim != AnimAttack ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
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
