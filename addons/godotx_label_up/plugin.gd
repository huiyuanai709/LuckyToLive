@tool
extends EditorPlugin

const AUTOLOAD_NAME = "GodotxLabelUp"
const AUTOLOAD_PATH = "res://addons/godotx_label_up/runtime/godotx_label_up.gd"

func _enter_tree() -> void:
	add_autoload_singleton(AUTOLOAD_NAME, AUTOLOAD_PATH)

func _exit_tree() -> void:
	remove_autoload_singleton(AUTOLOAD_NAME)
