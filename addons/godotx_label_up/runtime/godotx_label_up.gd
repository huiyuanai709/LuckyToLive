extends Node

signal label_spawned(id: int)
signal label_finished(id: int)

var _manager: GodotxLabelUpManager = null
var _initialized: bool = false

func _ready() -> void:
	_initialize.call_deferred()

func _initialize() -> void:
	if _initialized:
		return
	_manager = GodotxLabelUpManager.new()
	_manager.name = "GodotxLabelUpManager"
	add_child(_manager)
	_manager.label_spawned.connect(_on_label_spawned)
	_manager.label_finished.connect(_on_label_finished)
	_initialized = true

func _on_label_spawned(id: int) -> void:
	label_spawned.emit(id)

func _on_label_finished(id: int) -> void:
	label_finished.emit(id)

func show(position: Vector2, text: String, style: GodotxLabelUpStyle) -> int:
	if not _initialized:
		_initialize()
	if text.is_empty():
		push_error("GodotX Label Up: text cannot be empty")
		return -1
	if style == null:
		push_error("GodotX Label Up: style cannot be null")
		return -1
	return _manager.show_label(position, text, style)

## Convenience for C# / gameplay: damage number at world position.
func show_damage(position: Vector2, amount: float) -> int:
	if amount <= 0.0:
		return -1
	var style: GodotxLabelUpStyle = GodotxLabelUpStyles.get_instance().get_style(GodotxLabelUpStyles.DAMAGE)
	return show(position, str(roundi(amount)), style)

## Convenience for C# / gameplay: XP gain at world position.
func show_xp(position: Vector2, amount: float) -> int:
	if amount <= 0.0:
		return -1
	var style: GodotxLabelUpStyle = GodotxLabelUpStyles.get_instance().get_style(GodotxLabelUpStyles.XP)
	return show(position, "+%d XP" % roundi(amount), style)

## Convenience for C# / gameplay: heal / full restore at world position.
func show_heal(position: Vector2, amount: float) -> int:
	if amount <= 0.0:
		return -1
	var style: GodotxLabelUpStyle = GodotxLabelUpStyles.get_instance().get_style(GodotxLabelUpStyles.HEAL)
	return show(position, "+%d" % roundi(amount), style)

func show_xy(x: float, y: float, text: String, style: GodotxLabelUpStyle) -> int:
	return show(Vector2(x, y), text, style)

func dismiss(id: int) -> bool:
	if not _initialized:
		return false
	return _manager.dismiss(id)

func clear_all() -> void:
	if _initialized:
		_manager.clear_all()

func prewarm(amount: int) -> void:
	if not _initialized:
		_initialize()
	_manager.prewarm(amount)

func get_active_count() -> int:
	if not _initialized:
		return 0
	return _manager.get_active_count()

func get_pool_size() -> int:
	if not _initialized:
		return 0
	return _manager.get_pool_size()
