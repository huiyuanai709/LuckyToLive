using Godot;

/// <summary>
/// 统一读取移动 / 冲刺 / 技能：键盘 WASD、手柄、触屏虚拟摇杆。
/// </summary>
public static class PlayerInput
{
	public static Vector2 GetMoveVector()
	{
		Vector2 mapped = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Vector2 touch = VirtualControls.Instance?.MoveVector ?? Vector2.Zero;
		// 触屏按下时优先；否则用 InputMap（键盘 + 手柄摇杆/十字键）
		if (touch.LengthSquared() > 0.01f)
			return touch.LimitLength(1f);
		return mapped;
	}

	public static bool IsDashJustPressed() =>
		Input.IsActionJustPressed("dash")
		|| (VirtualControls.Instance?.ConsumeDash() ?? false);

	public static bool IsSkillJustPressed(int slot)
	{
		string action = slot == 0 ? "skill_1" : "skill_2";
		if (Input.IsActionJustPressed(action))
			return true;
		return VirtualControls.Instance?.ConsumeSkill(slot) ?? false;
	}
}
