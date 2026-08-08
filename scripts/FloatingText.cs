using Godot;

/// <summary>
/// Thin C# bridge to the GodotX Label Up autoload (damage / XP floating text).
/// </summary>
public static class FloatingText
{
	private static Node _api;

	public static void Prewarm(int amount = 200)
	{
		if (!Ensure()) return;
		_api.Call("prewarm", amount);
	}

	public static void ClearAll()
	{
		if (!Ensure()) return;
		_api.Call("clear_all");
	}

	public static void ShowDamage(Vector2 worldPos, float amount)
	{
		if (amount <= 0f || !Ensure()) return;
		_api.Call("show_damage", worldPos + new Vector2(0, -22), amount);
	}

	public static void ShowXp(Vector2 worldPos, float amount)
	{
		if (amount <= 0f || !Ensure()) return;
		_api.Call("show_xp", worldPos + new Vector2(0, -36), amount);
	}

	public static void ShowHeal(Vector2 worldPos, float amount)
	{
		if (amount <= 0f || !Ensure()) return;
		_api.Call("show_heal", worldPos + new Vector2(0, -28), amount);
	}

	private static bool Ensure()
	{
		if (_api != null && GodotObject.IsInstanceValid(_api))
			return true;

		if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
			return false;

		_api = tree.Root.GetNodeOrNull("GodotxLabelUp");
		return _api != null;
	}
}
