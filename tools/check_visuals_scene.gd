extends SceneTree

# 立绘场景回归校验：不启动游戏、不编译 C#，直接让 Godot 解析
# HuTaoMod/scenes/hu_tao_visuals.tscn，检查节点树、贴图、动画名/时长，
# 以及每条 AnimationPlayer 轨道引用的节点与属性是否真实存在
#（手写 .tscn 最容易错的就是这里，比如写成 :skew 但没有这个属性）。
#
# 用法:
#   godot --headless --path . --script res://tools/check_visuals_scene.gd
#
# 期望输出最后一行是 SCENE_OK。
# 注意: 没有 dotnet SDK 时会报 "Cannot instantiate C# script"，这是正常的，
#       场景本身仍然会被完整解析和校验。

const SCENE_PATH := "res://HuTaoMod/scenes/hu_tao_visuals.tscn"
const REQUIRED_ANIMS := ["idle", "attack", "cast", "hurt", "die", "powerup", "revive", "relaxed"]


func _init() -> void:
	var packed = load(SCENE_PATH)
	if packed == null:
		_fail("scene failed to load/parse")
		return

	var root = packed.instantiate()
	print("ROOT ", root.name, " class=", root.get_class())

	for required in ["Visuals", "PhobiaModeVisuals", "Bounds", "CenterPos", "FormVfx", "IntentPos", "OrbPos", "TalkPos", "AnimationPlayer"]:
		if root.get_node_or_null(required) == null:
			_fail("missing node: " + required)
			return

	var vis = root.get_node_or_null("Visuals")
	for c in vis.get_children():
		print("  Visuals/", c.name, " class=", c.get_class())

	var body = vis.get_node_or_null("Body")
	if body == null or body.texture == null:
		_fail("Visuals/Body missing texture")
		return
	print("Body scale=", body.scale, " pos=", body.position, " tex=", body.texture.get_size())

	for ghost in ["Ghost1", "Ghost2"]:
		var g = vis.get_node_or_null(ghost)
		if g == null or g.texture == null:
			_fail("Visuals/" + ghost + " missing")
			return
		print(ghost, " modulate=", g.modulate)

	var flies = vis.get_node_or_null("Flies")
	if flies == null or flies.get_child_count() == 0:
		_fail("Visuals/Flies missing or empty")
		return
	for f in flies.get_children():
		print("  fly ", f.name, " tex=", f.texture.get_size() if f.texture else "null", " pos=", f.position)

	var ap = root.get_node_or_null("AnimationPlayer")
	var names = ap.get_animation_list()
	print("animations=", names)
	for required in REQUIRED_ANIMS:
		if not names.has(required):
			_fail("missing animation: " + required)
			return
		print("  anim ", required, " len=", ap.get_animation(required).length,
			" tracks=", ap.get_animation(required).get_track_count())

	var bad := 0
	for a in names:
		var anim = ap.get_animation(a)
		for i in anim.get_track_count():
			var np = anim.track_get_path(i)
			var target = root.get_node_or_null(np)
			if target == null:
				print("  BAD target ", a, "#", i, " ", np)
				bad += 1
				continue
			if not (np.get_concatenated_subnames() in target):
				print("  BAD property ", a, "#", i, " ", np, " on ", target.get_class())
				bad += 1

	if bad > 0:
		_fail("broken animation tracks: " + str(bad))
		return

	print("SCENE_OK")
	quit(0)


func _fail(msg: String) -> void:
	print("SCENE_FAILED: ", msg)
	quit(1)
