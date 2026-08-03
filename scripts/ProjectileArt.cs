using Godot;
using System.Collections.Generic;

/// <summary>
/// 弹道 / 射线的贴图解析。约定：res://assets/projectiles/{name}.png。
/// 默认按 WeaponStyle（或建筑的 BuildingStyle）取名；SlotItem.ProjectileTexture 可覆盖。
/// 缺图时返回 null，调用方回退到程序绘制的色块。
/// </summary>
public static class ProjectileArt
{
	private const string Dir = "res://assets/projectiles/";
	private static readonly Dictionary<string, Texture2D> Cache = new();

	/// <summary>飞行弹贴图；返回 null 表示无贴图（回退画圆）。</summary>
	public static Texture2D ForProjectile(SlotItem item)
	{
		if (item == null) return null;
		string name = !string.IsNullOrEmpty(item.ProjectileTexture)
			? item.ProjectileTexture
			: !string.IsNullOrEmpty(item.WeaponStyle) ? item.WeaponStyle : item.BuildingStyle;
		return Load(name);
	}

	/// <summary>射线光束条贴图；返回 null 表示无贴图（回退画线）。</summary>
	public static Texture2D ForBeam(SlotItem item)
	{
		string name = !string.IsNullOrEmpty(item?.ProjectileTexture) ? item.ProjectileTexture : "beam";
		return Load(name) ?? Load("beam");
	}

	private static Texture2D Load(string name)
	{
		if (string.IsNullOrEmpty(name)) return null;
		if (Cache.TryGetValue(name, out var cached)) return cached;
		string path = Dir + name + ".png";
		Texture2D tex = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		Cache[name] = tex;
		return tex;
	}

	/// <summary>该弹道是否应朝飞行方向旋转（箭类转，火球类不转）。</summary>
	public static bool RotatesWithVelocity(SlotItem item)
	{
		string style = !string.IsNullOrEmpty(item?.WeaponStyle) ? item.WeaponStyle : item?.BuildingStyle;
		return style is "pierce" or "ice_arrow" or "turret_phys";
	}
}
