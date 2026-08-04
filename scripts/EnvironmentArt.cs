using Godot;
using System.Collections.Generic;

/// <summary>
/// 岛屿环境贴图解析。约定：res://assets/environment/{name}.png。
/// 缺图时返回 null，调用方回退到纯色绘制。
/// </summary>
public static class EnvironmentArt
{
	private const string Dir = "res://assets/environment/";
	private static readonly Dictionary<string, Texture2D> Cache = new();

	public static Texture2D Load(string name)
	{
		if (string.IsNullOrEmpty(name)) return null;
		if (Cache.TryGetValue(name, out var cached)) return cached;
		string path = Dir + name + ".png";
		Texture2D tex = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		Cache[name] = tex;
		return tex;
	}

	public static Texture2D GroundGrass => Load("ground_grass");
	public static Texture2D GroundDirt => Load("ground_dirt");
	public static Texture2D GroundSand => Load("ground_sand");
	public static Texture2D GroundAsh => Load("ground_ash");
	public static Texture2D GroundRubble => Load("ground_rubble");
	public static Texture2D GroundWild => Load("ground_wild");
	public static Texture2D GroundDune => Load("ground_dune");
	public static Texture2D GroundCracked => Load("ground_cracked");
}
