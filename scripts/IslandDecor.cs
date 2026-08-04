using Godot;
using System.Collections.Generic;

/// <summary>
/// 岛屿装饰物：树木、草丛、灌木、岩石、小花等。
/// 精灵作为 Main 的直接子节点，以便与英雄 / 敌人一起 Y 排序遮挡。
/// 布局与种类随当前 <see cref="MapTheme"/> 变化。
/// </summary>
public partial class IslandDecor : Node2D
{
	/// <summary>固定障碍点（与旧版棕色方块位置对应）。</summary>
	private static readonly Vector2[] LandmarkRocks =
	{
		new(660, 440),
		new(1480, 930),
		new(1040, 670),
	};

	public static IslandDecor Spawn(Node2D world, Rect2 island, MapTheme theme = null)
	{
		theme ??= MapCatalog.Island;
		var decor = new IslandDecor();
		world.AddChild(decor);
		decor.Build(world, island, theme);
		return decor;
	}

	private void Build(Node2D world, Rect2 island, MapTheme theme)
	{
		AddToGroup("island_decor");
		var rng = new RandomNumberGenerator();
		rng.Seed = theme.DecorSeed;

		var clearCenter = island.GetCenter();
		const float clearRadius = 160f;

		var rockKinds = theme.RockKinds is { Length: > 0 } ? theme.RockKinds : new[] { "rock" };
		foreach (var p in LandmarkRocks)
		{
			if (island.HasPoint(p))
				AddProp(world, rockKinds[0], p, rng.RandfRange(0.95f, 1.15f), feetAnchor: true);
		}

		PlaceScatter(world, island, rng, clearCenter, clearRadius, count: 14, minSep: 90f,
			kinds: rockKinds, scaleMin: 0.75f, scaleMax: 1.1f, edgeBias: 0.35f);

		if (theme.TreeKinds is { Length: > 0 } && theme.TreeCount > 0)
		{
			PlaceScatter(world, island, rng, clearCenter, clearRadius + 40f, count: theme.TreeCount, minSep: 70f,
				kinds: theme.TreeKinds, scaleMin: 0.55f, scaleMax: 0.85f, edgeBias: 0.7f, margin: 50f);
		}

		if (theme.BushKinds is { Length: > 0 } && theme.BushCount > 0)
		{
			PlaceScatter(world, island, rng, clearCenter, clearRadius * 0.6f, count: theme.BushCount, minSep: 48f,
				kinds: theme.BushKinds, scaleMin: 0.7f, scaleMax: 1.1f, edgeBias: 0.45f);
		}

		if (theme.GrassKinds is { Length: > 0 } && theme.GrassCount > 0)
		{
			PlaceScatter(world, island, rng, clearCenter, 40f, count: theme.GrassCount, minSep: 28f,
				kinds: theme.GrassKinds, scaleMin: 0.7f, scaleMax: 1.25f, edgeBias: 0.2f, margin: 28f, zBehind: true);
		}

		if (theme.FlowerKinds is { Length: > 0 } && theme.FlowerCount > 0)
		{
			PlaceScatter(world, island, rng, clearCenter, 50f, count: theme.FlowerCount, minSep: 40f,
				kinds: theme.FlowerKinds, scaleMin: 0.85f, scaleMax: 1.2f, edgeBias: 0.15f, zBehind: true);
		}

		if (theme.ScatterExtra is { Length: > 0 } && theme.ExtraCount > 0)
		{
			PlaceScatter(world, island, rng, clearCenter, 80f, count: theme.ExtraCount, minSep: 55f,
				kinds: theme.ScatterExtra, scaleMin: 0.8f, scaleMax: 1.15f, edgeBias: 0.4f, zBehind: true);
		}
	}

	private void PlaceScatter(
		Node2D world,
		Rect2 island,
		RandomNumberGenerator rng,
		Vector2 clearCenter,
		float clearRadius,
		int count,
		float minSep,
		string[] kinds,
		float scaleMin,
		float scaleMax,
		float edgeBias,
		float margin = 40f,
		bool zBehind = false)
	{
		if (kinds == null || kinds.Length == 0 || count <= 0) return;

		var placed = new List<Vector2>(count);
		var inner = island.Grow(-margin);
		if (inner.Size.X < 80 || inner.Size.Y < 80) inner = island;

		for (int attempt = 0, made = 0; attempt < count * 12 && made < count; attempt++)
		{
			Vector2 p;
			if (rng.Randf() < edgeBias)
			{
				float t = rng.Randf();
				float band = 120f;
				int side = rng.RandiRange(0, 3);
				p = side switch
				{
					0 => new Vector2(inner.Position.X + t * inner.Size.X, inner.Position.Y + rng.Randf() * band),
					1 => new Vector2(inner.Position.X + t * inner.Size.X, inner.End.Y - rng.Randf() * band),
					2 => new Vector2(inner.Position.X + rng.Randf() * band, inner.Position.Y + t * inner.Size.Y),
					_ => new Vector2(inner.End.X - rng.Randf() * band, inner.Position.Y + t * inner.Size.Y),
				};
			}
			else
			{
				p = new Vector2(
					rng.RandfRange(inner.Position.X, inner.End.X),
					rng.RandfRange(inner.Position.Y, inner.End.Y));
			}

			if (p.DistanceTo(clearCenter) < clearRadius) continue;

			bool near = false;
			foreach (var q in placed)
			{
				if (q.DistanceTo(p) < minSep) { near = true; break; }
			}
			if (near) continue;

			string kind = kinds[rng.RandiRange(0, kinds.Length - 1)];
			float scale = rng.RandfRange(scaleMin, scaleMax);
			if (AddProp(world, kind, p, scale, feetAnchor: !zBehind, zBehind: zBehind))
			{
				placed.Add(p);
				made++;
			}
		}
	}

	private static bool AddProp(Node2D world, string kind, Vector2 pos, float scale, bool feetAnchor, bool zBehind = false)
	{
		var tex = EnvironmentArt.Load(kind);
		if (tex == null) return false;

		var sprite = new Sprite2D
		{
			Texture = tex,
			TextureFilter = TextureFilterEnum.Nearest,
			Scale = new Vector2(scale, scale),
			Position = pos,
			Centered = true,
		};
		sprite.AddToGroup("island_decor");

		if (feetAnchor)
			sprite.Offset = new Vector2(0, -tex.GetHeight() * 0.5f + 6f);

		// 矮植被固定压在角色脚下；树木靠 YSort + 底部锚点遮挡
		if (zBehind)
			sprite.ZIndex = -1;

		world.AddChild(sprite);
		return true;
	}
}
