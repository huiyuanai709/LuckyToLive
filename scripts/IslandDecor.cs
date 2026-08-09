using Godot;
using System.Collections.Generic;

/// <summary>
/// 岛屿装饰与障碍：主题障碍阵列（可碰撞）+ 氛围装饰。
/// 精灵挂在 Main 下以便与英雄 / 敌人一起 Y 排序遮挡。
/// </summary>
public partial class IslandDecor : Node2D
{
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
		float clearRadius = theme.ClearRadius > 0f ? theme.ClearRadius : 170f;
		var occupied = new List<Vector2>(128);

		var blockKinds = theme.BlockKinds is { Length: > 0 }
			? theme.BlockKinds
			: (theme.RockKinds is { Length: > 0 } ? theme.RockKinds : new[] { "rock" });

		// 1) 主题障碍阵列：真正阻挡走位
		if (theme.Barriers != null)
		{
			foreach (var barrier in theme.Barriers)
			{
				if (barrier == null) continue;
				var kinds = barrier.Kinds is { Length: > 0 } ? barrier.Kinds : blockKinds;
				PlaceBarrier(world, island, rng, clearCenter, clearRadius, barrier, kinds, occupied);
			}
		}

		// 2) 少量散落硬障碍（填空，不破坏主通道）
		int scatterRocks = theme.ScatterRockCount > 0 ? theme.ScatterRockCount : 6;
		PlaceScatter(world, island, rng, clearCenter, clearRadius + 30f, count: scatterRocks, minSep: 110f,
			kinds: blockKinds, scaleMin: 1.05f, scaleMax: 1.4f, edgeBias: 0.55f,
			occupied: occupied, blocking: true, margin: 60f);

		// 3) 氛围装饰（树 / 灌木可轻阻挡；草花等不挡）
		if (theme.TreeKinds is { Length: > 0 } && theme.TreeCount > 0)
		{
			bool treesBlock = theme.Id is MapId.Wilderness or MapId.Desert or MapId.Apocalypse;
			PlaceScatter(world, island, rng, clearCenter, clearRadius + 50f, count: theme.TreeCount, minSep: 78f,
				kinds: theme.TreeKinds, scaleMin: 0.55f, scaleMax: 0.9f, edgeBias: 0.75f,
				occupied: occupied, blocking: treesBlock, margin: 55f);
		}

		if (theme.BushKinds is { Length: > 0 } && theme.BushCount > 0)
		{
			PlaceScatter(world, island, rng, clearCenter, clearRadius * 0.55f, count: theme.BushCount, minSep: 52f,
				kinds: theme.BushKinds, scaleMin: 0.75f, scaleMax: 1.15f, edgeBias: 0.4f,
				occupied: occupied, blocking: true, margin: 40f);
		}

		if (theme.GrassKinds is { Length: > 0 } && theme.GrassCount > 0)
		{
			PlaceScatter(world, island, rng, clearCenter, 40f, count: theme.GrassCount, minSep: 28f,
				kinds: theme.GrassKinds, scaleMin: 0.7f, scaleMax: 1.25f, edgeBias: 0.2f,
				occupied: occupied, blocking: false, margin: 28f, zBehind: true);
		}

		if (theme.FlowerKinds is { Length: > 0 } && theme.FlowerCount > 0)
		{
			PlaceScatter(world, island, rng, clearCenter, 50f, count: theme.FlowerCount, minSep: 40f,
				kinds: theme.FlowerKinds, scaleMin: 0.85f, scaleMax: 1.2f, edgeBias: 0.15f,
				occupied: occupied, blocking: false, zBehind: true);
		}

		if (theme.ScatterExtra is { Length: > 0 } && theme.ExtraCount > 0)
		{
			PlaceScatter(world, island, rng, clearCenter, 90f, count: theme.ExtraCount, minSep: 58f,
				kinds: theme.ScatterExtra, scaleMin: 0.85f, scaleMax: 1.2f, edgeBias: 0.35f,
				occupied: occupied, blocking: false, zBehind: true);
		}

		// 4) 可踩踏减速草丛区（地形互动；与装饰草分离，保证体感）
		PlaceTerrainBrushes(world, island, rng, clearCenter, clearRadius, theme);
	}

	private void PlaceTerrainBrushes(
		Node2D world,
		Rect2 island,
		RandomNumberGenerator rng,
		Vector2 clearCenter,
		float clearRadius,
		MapTheme theme)
	{
		int count = theme.Id switch
		{
			MapId.Wilderness => 10,
			MapId.Desert => 6,
			MapId.Apocalypse => 7,
			_ => 8,
		};
		var tint = theme.Id switch
		{
			MapId.Desert => new Color(0.85f, 0.7f, 0.35f, 0.2f),
			MapId.Apocalypse => new Color(0.45f, 0.35f, 0.3f, 0.22f),
			MapId.Wilderness => new Color(0.25f, 0.55f, 0.3f, 0.26f),
			_ => new Color(0.35f, 0.7f, 0.35f, 0.22f),
		};
		var inner = island.Grow(-70f);
		for (int attempt = 0, made = 0; attempt < count * 12 && made < count; attempt++)
		{
			var p = new Vector2(
				rng.RandfRange(inner.Position.X, inner.End.X),
				rng.RandfRange(inner.Position.Y, inner.End.Y));
			if (p.DistanceTo(clearCenter) < clearRadius * 0.7f) continue;

			bool nearOther = false;
			foreach (var n in world.GetTree().GetNodesInGroup("terrain_brush"))
			{
				if (n is Node2D other && other.GlobalPosition.DistanceTo(p) < 120f)
				{
					nearOther = true;
					break;
				}
			}
			if (nearOther) continue;

			var brush = new TerrainBrush
			{
				Radius = rng.RandfRange(48f, 78f),
				Tint = tint,
				EnemySlow = theme.Id == MapId.Desert ? 0.7f : 0.6f,
				HeroSlow = 0.88f,
			};
			world.AddChild(brush);
			brush.GlobalPosition = p;
			made++;
		}
	}

	private void PlaceBarrier(
		Node2D world,
		Rect2 island,
		RandomNumberGenerator rng,
		Vector2 clearCenter,
		float clearRadius,
		BarrierSpec spec,
		string[] kinds,
		List<Vector2> occupied)
	{
		var points = SampleBarrierPoints(spec, rng);
		foreach (var raw in points)
		{
			var p = raw;
			p.X = Mathf.Clamp(p.X, island.Position.X + 36, island.End.X - 36);
			p.Y = Mathf.Clamp(p.Y, island.Position.Y + 36, island.End.Y - 36);
			if (p.DistanceTo(clearCenter) < clearRadius) continue;
			if (TooClose(occupied, p, 34f)) continue;

			string kind = kinds[rng.RandiRange(0, kinds.Length - 1)];
			float scale = rng.RandfRange(spec.ScaleMin, spec.ScaleMax);
			if (AddProp(world, kind, p, scale, feetAnchor: true, blocking: true))
				occupied.Add(p);
		}
	}

	private static List<Vector2> SampleBarrierPoints(BarrierSpec spec, RandomNumberGenerator rng)
	{
		var pts = new List<Vector2>(spec.Count);
		var c = spec.Center;
		float sx = Mathf.Max(40f, spec.SpanX);
		float sy = Mathf.Max(40f, spec.SpanY);

		switch (spec.Shape)
		{
			case BarrierShape.LineH:
				for (int i = 0; i < spec.Count; i++)
				{
					float t = spec.Count == 1 ? 0.5f : i / (float)(spec.Count - 1);
					float x = c.X - sx * 0.5f + t * sx;
					float y = c.Y + rng.RandfRange(-sy * 0.35f, sy * 0.35f);
					pts.Add(new Vector2(x, y));
				}
				break;

			case BarrierShape.LineV:
				for (int i = 0; i < spec.Count; i++)
				{
					float t = spec.Count == 1 ? 0.5f : i / (float)(spec.Count - 1);
					float y = c.Y - sy * 0.5f + t * sy;
					float x = c.X + rng.RandfRange(-sx * 0.35f, sx * 0.35f);
					pts.Add(new Vector2(x, y));
				}
				break;

			case BarrierShape.LShape:
			{
				int arm = Mathf.Max(2, spec.Count / 2);
				for (int i = 0; i < arm; i++)
				{
					float t = arm == 1 ? 0.5f : i / (float)(arm - 1);
					pts.Add(new Vector2(c.X - sx * 0.5f + t * sx, c.Y + rng.RandfRange(-10f, 10f)));
				}
				for (int i = 1; i < spec.Count - arm + 1; i++)
				{
					float t = i / (float)Mathf.Max(1, spec.Count - arm);
					pts.Add(new Vector2(c.X + rng.RandfRange(-10f, 10f), c.Y + t * sy * 0.5f));
				}
				break;
			}

			case BarrierShape.Arc:
			{
				// 开口朝向地图中心，形成半围掩体
				float start = rng.RandfRange(-0.35f, 0.35f);
				float sweep = Mathf.Pi * 0.95f;
				float rx = sx * 0.5f;
				float ry = sy * 0.5f;
				for (int i = 0; i < spec.Count; i++)
				{
					float t = spec.Count == 1 ? 0.5f : i / (float)(spec.Count - 1);
					float ang = start + t * sweep;
					float jx = rng.RandfRange(-8f, 8f);
					float jy = rng.RandfRange(-8f, 8f);
					pts.Add(c + new Vector2(Mathf.Cos(ang) * rx + jx, Mathf.Sin(ang) * ry + jy));
				}
				break;
			}

			default: // Clump
				pts.Add(c);
				for (int i = 1; i < spec.Count; i++)
				{
					float ang = rng.Randf() * Mathf.Tau;
					float rad = rng.RandfRange(18f, Mathf.Max(28f, Mathf.Min(sx, sy) * 0.45f));
					pts.Add(c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad);
				}
				break;
		}

		return pts;
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
		List<Vector2> occupied,
		bool blocking,
		float margin = 40f,
		bool zBehind = false)
	{
		if (kinds == null || kinds.Length == 0 || count <= 0) return;

		var inner = island.Grow(-margin);
		if (inner.Size.X < 80 || inner.Size.Y < 80) inner = island;

		for (int attempt = 0, made = 0; attempt < count * 14 && made < count; attempt++)
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
			if (TooClose(occupied, p, minSep)) continue;

			string kind = kinds[rng.RandiRange(0, kinds.Length - 1)];
			float scale = rng.RandfRange(scaleMin, scaleMax);
			bool useBlock = blocking && IsBlockingKind(kind);
			if (AddProp(world, kind, p, scale, feetAnchor: !zBehind, blocking: useBlock, zBehind: zBehind))
			{
				occupied.Add(p);
				made++;
			}
		}
	}

	private static bool TooClose(List<Vector2> occupied, Vector2 p, float minSep)
	{
		foreach (var q in occupied)
		{
			if (q.DistanceTo(p) < minSep) return true;
		}
		return false;
	}

	private static bool IsBlockingKind(string kind)
	{
		if (string.IsNullOrEmpty(kind)) return false;
		return kind.StartsWith("rock")
			|| kind.StartsWith("tree")
			|| kind.StartsWith("bush")
			|| kind.StartsWith("cactus")
			|| kind is "stump" or "bone_pile";
	}

	private static float CollisionRadius(string kind, float scale)
	{
		float bas = kind switch
		{
			"rock" or "rock_rubble" or "rock_sand" => 26f,
			"stump" or "bone_pile" => 20f,
			"cactus" => 18f,
			"cactus_small" => 12f,
			_ when kind.StartsWith("tree") => 15f,
			_ when kind.StartsWith("bush") => 14f,
			_ => 0f,
		};
		return bas * Mathf.Clamp(scale, 0.7f, 2.2f);
	}

	private static bool IsDestructibleKind(string kind)
	{
		if (string.IsNullOrEmpty(kind)) return false;
		return kind.StartsWith("rock")
			|| kind is "stump" or "bone_pile"
			|| kind.StartsWith("cactus");
	}

	private static bool AddProp(
		Node2D world,
		string kind,
		Vector2 pos,
		float scale,
		bool feetAnchor,
		bool blocking = false,
		bool zBehind = false)
	{
		var tex = EnvironmentArt.Load(kind);
		if (tex == null) return false;

		float radius = blocking ? CollisionRadius(kind, scale) : 0f;
		Node2D root;

		if (radius > 0.5f && IsDestructibleKind(kind))
		{
			// 岩石 / 树桩等：可破坏掩体
			root = DestructibleCover.Create(kind, pos, scale, tex, feetAnchor);
		}
		else if (radius > 0.5f)
		{
			var body = new StaticBody2D
			{
				Position = pos,
				CollisionLayer = 1,
				CollisionMask = 0,
			};
			body.AddToGroup("island_decor");
			body.AddToGroup("island_obstacles");

			var sprite = new Sprite2D
			{
				Texture = tex,
				TextureFilter = TextureFilterEnum.Nearest,
				Scale = new Vector2(scale, scale),
				Centered = true,
			};
			if (feetAnchor)
				sprite.Offset = new Vector2(0, -tex.GetHeight() * 0.5f + 6f);
			body.AddChild(sprite);

			var col = new CollisionShape2D
			{
				Shape = new CircleShape2D { Radius = radius },
				Position = new Vector2(0, -2f),
			};
			body.AddChild(col);
			root = body;
		}
		else
		{
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
			root = sprite;
		}

		if (zBehind)
			root.ZIndex = -1;

		world.AddChild(root);
		return true;
	}
}
