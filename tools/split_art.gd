extends SceneTree
# 把 hu_tao.png 拆成「本体」与「漂浮蝴蝶」两张图：
#   - 用 1/4 分辨率做连通域分析（快）
#   - 最大连通域 = 人物本体
#   - 其它连通域里偏青色的 = 蝴蝶（独立元素，可以单独运动）
#   - 掩码用最近邻放大回原尺寸，再逐像素切分
# 用法： godot --headless --path <项目> --script res://tools/split_art.gd

const SRC := "res://HuTaoMod/images/character/hu_tao.png"
const OUT_BODY := "res://HuTaoMod/images/character/hu_tao_body.png"
const OUT_FLY := "res://HuTaoMod/images/character/hu_tao_butterflies.png"
const DOWN := 4

func _init() -> void:
	var img := Image.load_from_file(ProjectSettings.globalize_path(SRC))
	if img == null:
		print("ERR: 读不到 ", SRC)
		quit(1)
		return
	img.convert(Image.FORMAT_RGBA8)
	var w := img.get_width()
	var h := img.get_height()
	var sw := int(w / DOWN)
	var sh := int(h / DOWN)

	# 1/4 缩放图（用于连通域）
	var small := img.duplicate()
	small.resize(sw, sh, Image.INTERPOLATE_BILINEAR)

	var label := PackedInt32Array()
	label.resize(sw * sh)
	label.fill(-1)
	var areas := []
	var comps := 0
	var stack := []

	for y in sh:
		for x in sw:
			var idx := y * sw + x
			if label[idx] != -1:
				continue
			if small.get_pixel(x, y).a <= 0.03:
				label[idx] = -2
				continue
			var c: int = comps
			comps += 1
			var area := 0
			stack.append(idx)
			label[idx] = c
			while stack.size() > 0:
				var p: int = stack.pop_back()
				var cx := p % sw
				var cy := int(p / sw)
				area += 1
				for dy: int in [-1, 0, 1]:
					for dx: int in [-1, 0, 1]:
						if dx == 0 and dy == 0:
							continue
						var nx: int = cx + dx
						var ny: int = cy + dy
						if nx < 0 or ny < 0 or nx >= sw or ny >= sh:
							continue
						var ni: int = ny * sw + nx
						if label[ni] != -1:
							continue
						if small.get_pixel(nx, ny).a <= 0.03:
							label[ni] = -2
							continue
						label[ni] = c
						stack.append(ni)
			areas.append(area)

	var body := 0
	for i in range(1, comps):
		if areas[i] > areas[body]:
			body = i

	# 每个连通域的平均颜色
	var sr := PackedFloat64Array(); sr.resize(comps)
	var sg := PackedFloat64Array(); sg.resize(comps)
	var sb := PackedFloat64Array(); sb.resize(comps)
	var cnt := PackedInt32Array(); cnt.resize(comps)
	for y in sh:
		for x in sw:
			var c: int = label[y * sw + x]
			if c < 0:
				continue
			var col: Color = small.get_pixel(x, y)
			sr[c] += col.r * 255.0; sg[c] += col.g * 255.0; sb[c] += col.b * 255.0
			cnt[c] += 1

	var is_fly := []
	is_fly.resize(comps)
	is_fly.fill(false)
	var fly_comps := 0
	for c in comps:
		if c == body or areas[c] < 20:
			continue
		var r: float = sr[c] / float(cnt[c])
		var g: float = sg[c] / float(cnt[c])
		var b: float = sb[c] / float(cnt[c])
		var cyan: bool = b > 110.0 and b > r + 35.0 and g > r + 20.0
		is_fly[c] = cyan
		if cyan:
			fly_comps += 1
		print("comp %d area=%d rgb=(%.0f,%.0f,%.0f) butterfly=%s" % [c, areas[c], r, g, b, str(cyan)])

	# 把标签画成两张掩码小图（白=是），再最近邻放大
	var mask_body := Image.create(sw, sh, false, Image.FORMAT_RGBA8)
	var mask_fly := Image.create(sw, sh, false, Image.FORMAT_RGBA8)
	for y in sh:
		for x in sw:
			var c: int = label[y * sw + x]
			var fly: bool = c >= 0 and c != body and is_fly[c]
			mask_body.set_pixel(x, y, Color(1, 1, 1, 0 if fly else 1))
			mask_fly.set_pixel(x, y, Color(1, 1, 1, 1 if fly else 0))
	mask_body.resize(w, h, Image.INTERPOLATE_NEAREST)
	mask_fly.resize(w, h, Image.INTERPOLATE_NEAREST)

	var out_body := Image.create(w, h, false, Image.FORMAT_RGBA8)
	var out_fly := Image.create(w, h, false, Image.FORMAT_RGBA8)
	var fly_pixels := 0
	for y in h:
		for x in w:
			var col: Color = img.get_pixel(x, y)
			var mb: float = mask_body.get_pixel(x, y).a
			var mf: float = mask_fly.get_pixel(x, y).a
			out_body.set_pixel(x, y, Color(col.r, col.g, col.b, col.a * mb))
			out_fly.set_pixel(x, y, Color(col.r, col.g, col.b, col.a * mf))
			if mf > 0.5 and col.a > 0.03:
				fly_pixels += 1

	out_body.save_png(ProjectSettings.globalize_path(OUT_BODY))
	out_fly.save_png(ProjectSettings.globalize_path(OUT_FLY))
	print("DONE size=%dx%d comps=%d body=%d flyComps=%d flyPixels=%d" % [w, h, comps, body, fly_comps, fly_pixels])
	quit(0)
