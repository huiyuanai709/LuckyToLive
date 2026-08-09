using Godot;

/// <summary>
/// 打击反馈统一入口：震屏 / 命中火花。全部代码生成，不依赖预制体或美术资源；
/// 调用方无需关心具体实现，风格参照 <see cref="FloatingText"/> 的「薄静态外观」写法。
/// 注：命中硬直没有走全局 Engine.TimeScale——那会连带拖慢第三方插件（GodotxLabelUp）
/// 的内部 Tween，在极小 delta 下触发其 GDScript 报错；改为在 Hero.MeleeHit 里对
/// 单个受击目标施加短暂的 ApplySlow，效果类似但只影响被打中的敌人。
/// </summary>
public static class CombatFx
{
	/// <summary>转发给 Main（持有 Camera2D），无当前对局时静默忽略。</summary>
	public static void Shake(float strength, float duration)
	{
		Main.Instance?.Shake(strength, duration);
	}

	/// <summary>
	/// 一次性命中火花：生成 <see cref="ImpactSpark"/>（纯 _Draw 自淡出），不依赖粒子
	/// 材质/贴图资源，与项目里其余程序化视觉（MeleeSwing、Enemy._Draw 等）风格一致。
	/// </summary>
	public static void ImpactBurst(Node parent, Vector2 globalPos, Color color, float scale = 1f)
	{
		if (parent == null || !GodotObject.IsInstanceValid(parent)) return;
		var spark = new ImpactSpark();
		parent.AddChild(spark);
		spark.GlobalPosition = globalPos;
		spark.Setup(color, scale);
	}
}
