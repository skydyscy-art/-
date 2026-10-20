extends SceneTree
# 粗分割 v2：用「几何约束 + 颜色」把头发（双马尾）和武器（长柄）圈出来。
#  头发：离身体中线较远 + 浅色（白/浅灰）
#  武器：靠近一条斜线的长条区域内 + 偏暗/灰
# 掩码：白=选中（可直接在 PS/Photopea 里刷）
# 用法： godot --headless --path <项目> --script res://tools/split_masks.gd

const SRC := "res://HuTaoMod/images/character/hu_tao_body.png"
const OUT_DIR := "res://HuTaoMod/images/character/masks/"

# 武器长柄的两端点（原图像素坐标，1680x2105 大致位置，可微调）
const SHAFT_A := Vector2(558, 1904)
const SHAFT_B := Vector2(1300, 560)
const SHAFT_HALF_WIDTH := 62.0

func _init() -> void:
	var img := Image.load_from_file(ProjectSettings.globalize_path(SRC))
	if img == null:
		print("ERR: 读不到 ", SRC)
		quit(1)
		return
	img.convert(Image.FORMAT_RGBA8)
	var w := img.get_width()
	var h := img.get_height()
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))

	var cx := w * 0.5
	var hair_zone := w * 0.20          # 距中线超过这个宽度才算头发区
	var mask_hair := Image.create(w, h, false, Image.FORMAT_RGBA8)
	var mask_weapon := Image.create(w, h, false, Image.FORMAT_RGBA8)
	var preview := Image.create(w, h, false, Image.FORMAT_RGBA8)
	var hair_n := 0
	var weapon_n := 0

	var dir := (SHAFT_B - SHAFT_A).normalized()
	for y in h:
		for x in w:
			var c: Color = img.get_pixel(x, y)
			var opaque: bool = c.a > 0.04
			var val: float = max(max(c.r, c.g), c.b)
			var sat: float = c.s

			# --- 武器：点到线段的距离 ---
			var p := Vector2(x, y)
			var ap := p - SHAFT_A
			var t: float = ap.dot(dir)
			var is_weapon := false
			if opaque and t > 0.0 and t < (SHAFT_B - SHAFT_A).length():
				var proj := SHAFT_A + dir * t
				var dist := p.distance_to(proj)
				if (dist < SHAFT_HALF_WIDTH and val < 0.62) or (p.distance_to(SHAFT_B) < 190.0 and val < 0.80):
					is_weapon = true

			# --- 头发：两侧长发 + 浅色（排除武器区域） ---
			var is_hair := false
			if opaque and not is_weapon:
				var far_from_center: bool = abs(x - cx) > hair_zone
				var pale: bool = val > 0.55 and sat < 0.55 and not (c.b > c.r + 0.12)
				if far_from_center and pale:
					is_hair = true

			mask_hair.set_pixel(x, y, Color(1, 1, 1, 1) if is_hair else Color(0, 0, 0, 1))
			mask_weapon.set_pixel(x, y, Color(1, 1, 1, 1) if is_weapon else Color(0, 0, 0, 1))

			var pv := c
			if is_hair:
				pv = Color(0.25, 1.0, 0.35, 1)
				hair_n += 1
			elif is_weapon:
				pv = Color(1.0, 0.45, 0.1, 1)
				weapon_n += 1
			preview.set_pixel(x, y, pv)

	mask_hair.save_png(ProjectSettings.globalize_path(OUT_DIR + "mask_hair.png"))
	mask_weapon.save_png(ProjectSettings.globalize_path(OUT_DIR + "mask_weapon.png"))
	preview.save_png(ProjectSettings.globalize_path(OUT_DIR + "preview_layers.png"))
	print("DONE %dx%d hair=%d weapon=%d" % [w, h, hair_n, weapon_n])
	quit(0)
