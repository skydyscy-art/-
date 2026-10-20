"""AI 视频 -> Godot 帧动画资源 的一键管道（路线 B）。

流程：
    1. 从 mp4 / mov / webm / gif 或「png 序列文件夹」抽帧（ffmpeg）
    2. 去背：帧自带 alpha 就直接用；否则按四角颜色做「边界洪水键」（适合纯色背景的 AI 视频）
    3. 归一化：按整段 clip 的统计值做一次全局缩放 + 平移（帧间相对运动会保留，不会被抹平）
       对齐基准直接从 hu_tao_body.png 量出来，所以新动画和现有立绘的脚底/重心必然一致
    4. 输出到 HuTaoMod/images/character/anim/<state>/<state>_000.png ...
    5. 生成 SpriteFrames：HuTaoMod/scenes/hu_tao_frames.tres

用法：
    # 1) 放素材：tools/anim_in/idle.mp4、tools/anim_in/attack.mp4 ...
    python tools/ai_anim_to_godot.py --state idle --input tools/anim_in/idle.mp4
    python tools/ai_anim_to_godot.py --state attack --input tools/anim_in/attack.mp4 --fps 30 --trim 0 0.4
    python tools/ai_anim_to_godot.py --build-tres
    python tools/ai_anim_to_godot.py --info          # 看对齐基准与推荐缩放

输出分辨率说明：默认 420x526（= 立绘画布 1680x2105 的 1/4）。
屏幕显示只有 227x284 px，1/4 画布已经接近 2 倍超采样，显存/包体都省。
配套节点：AnimatedSprite2D，scale = 0.54，position = (0, -142)。
"""

import argparse
import os
import shutil
import subprocess
import sys

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "HuTaoMod", "images", "character")
ANIM_ROOT = os.path.join(ART, "anim")
BODY = os.path.join(ART, "hu_tao_body.png")
TRES = os.path.join(ROOT, "HuTaoMod", "scenes", "hu_tao_frames.tres")
TMP = os.path.join(ROOT, "tools", ".tmp_anim")

BODY_CANVAS = (1680, 2105)      # 现有立绘画布
OUT_CANVAS = (420, 526)         # 默认输出画布（1/4）
VIDEO_EXT = (".mp4", ".mov", ".webm", ".mkv", ".gif", ".avi")
LOOP_STATES = {"idle", "relaxed"}


# --------------------------------------------------------------------------- utils

def body_anchor():
    """从现有立绘量出「脚底 + 重心」基准，返回 (x, y) 于立绘画布坐标。"""
    im = Image.open(BODY).convert("RGBA")
    a = np.array(im)[..., 3]
    ys, xs = np.nonzero(a > 40)
    if len(xs) == 0:
        return BODY_CANVAS[0] / 2.0, BODY_CANVAS[1]
    return (xs.min() + xs.max()) / 2.0, ys.max() + 1


def alpha_bbox(im):
    a = np.array(im.convert("RGBA"))[..., 3]
    ys, xs = np.nonzero(a > 40)
    if len(xs) == 0:
        return None
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


# ------------------------------------------------------------------- 1. extract

def extract_frames(src, workdir, fps, trim):
    if os.path.isdir(src):
        files = sorted(f for f in os.listdir(src) if f.lower().endswith(".png"))
        if not files:
            raise SystemExit("no png in " + src)
        return [os.path.join(src, f) for f in files]

    if not os.path.isfile(src):
        raise SystemExit("input not found: " + src)

    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        raise SystemExit("ffmpeg not found in PATH")

    os.makedirs(workdir, exist_ok=True)
    for f in os.listdir(workdir):
        if f.endswith(".png"):
            os.remove(os.path.join(workdir, f))

    cmd = [ffmpeg, "-y", "-loglevel", "error", "-i", src]
    if trim:
        cmd += ["-ss", str(trim[0]), "-to", str(trim[1])]
    if src.lower().endswith(".gif"):
        cmd += ["-vsync", "0"]
    cmd += ["-vf", "fps=%d" % fps, os.path.join(workdir, "f_%04d.png")]
    subprocess.run(cmd, check=True)

    frames = sorted(f for f in os.listdir(workdir) if f.endswith(".png"))
    return [os.path.join(workdir, f) for f in frames]


# --------------------------------------------------------------------- 2. keying

def key_background(im):
    """没有 alpha 的帧：把「和四角同色且连通到边界」的像素抠掉。"""
    im = im.convert("RGBA")
    arr = np.array(im)
    alpha = arr[..., 3]
    if (alpha < 200).mean() > 0.02:
        return im          # 已经是带 alpha 的素材（webm/ProRes 等）

    h, w = alpha.shape
    corners = [arr[2, 2, :3], arr[2, w - 3, :3], arr[h - 3, 2, :3], arr[h - 3, w - 3, :3]]
    key = np.median(np.stack(corners), axis=0)

    dist = np.abs(arr[..., :3].astype(np.int16) - key.astype(np.int16)).max(axis=2)
    similar = dist < 44

    # 从边界做洪水填充，只吃掉与外部连通的背景
    outside = np.zeros((h, w), dtype=bool)
    stack = []
    stack.extend([(0, x) for x in range(w)])
    stack.extend([(h - 1, x) for x in range(w)])
    stack.extend([(y, 0) for y in range(h)])
    stack.extend([(y, w - 1) for y in range(h)])

    while stack:
        y, x = stack.pop()
        if y < 0 or x < 0 or y >= h or x >= w:
            continue
        if outside[y, x] or not similar[y, x]:
            continue
        outside[y, x] = True
        stack.append((y - 1, x))
        stack.append((y + 1, x))
        stack.append((y, x - 1))
        stack.append((y, x + 1))

    arr[..., 3] = np.where(outside, 0, alpha)
    out = Image.fromarray(arr)
    # 轻微收边 + 羽化，压掉 AI 抠像常见的白边
    out.putalpha(out.getchannel("A").filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.6)))
    return out


# ------------------------------------------------------------------ 3. normalize

def normalize(frames, out_canvas, anchor_src):
    """整段 clip 用同一组缩放/平移，帧间运动保留。"""
    boxes = [b for b in (alpha_bbox(f) for f in frames) if b]
    if not boxes:
        raise SystemExit("all frames are empty after keying")

    # 用中位数抵抗个别异常帧（比如武器甩出画面的那一帧）
    cx = float(np.median([(b[0] + b[2]) / 2.0 for b in boxes]))
    y0 = float(np.median([b[1] for b in boxes]))
    y1 = float(np.median([b[3] for b in boxes]))
    clip_h = max(y1 - y0, 1.0)

    out_w, out_h = out_canvas
    k = out_w / float(BODY_CANVAS[0])          # 输出画布相对立绘画布的比例

    # 角色高度对齐到「现有立绘的角色高度 * k」，也就是输出画布上的目标高度
    body_im = Image.open(BODY)
    bb = alpha_bbox(body_im)
    body_h = float(bb[3] - bb[1])
    scale = body_h * k / clip_h

    target_x = anchor_src[0] * k
    target_y = anchor_src[1] * k

    placed = []
    for f in frames:
        w = max(1, int(round(f.width * scale)))
        h = max(1, int(round(f.height * scale)))
        r = f.resize((w, h), Image.LANCZOS)
        canvas = Image.new("RGBA", (out_w, out_h), (0, 0, 0, 0))
        dx = int(round(target_x - cx * scale))
        dy = int(round(target_y - y1 * scale))
        canvas.alpha_composite(r, (dx, dy))
        placed.append(canvas)
    return placed, scale


# ---------------------------------------------------------------------- 4/5. io

def write_frames(state, frames):
    dst = os.path.join(ANIM_ROOT, state)
    if os.path.isdir(dst):
        for f in os.listdir(dst):
            if f.endswith(".png"):
                os.remove(os.path.join(dst, f))
    os.makedirs(dst, exist_ok=True)
    names = []
    for i, im in enumerate(frames):
        name = "%s_%03d.png" % (state, i)
        im.save(os.path.join(dst, name))
        names.append(name)
    return dst, names


def build_tres(fps_map):
    if not os.path.isdir(ANIM_ROOT):
        raise SystemExit(
            "no frames yet: %s does not exist.\n"
            "run:  python tools/ai_anim_to_godot.py --state idle --input <your clip>\n"
            % ANIM_ROOT)
    states = sorted(d for d in os.listdir(ANIM_ROOT) if os.path.isdir(os.path.join(ANIM_ROOT, d)))
    states = [s for s in states if any(f.endswith(".png") for f in os.listdir(os.path.join(ANIM_ROOT, s)))]
    if not states:
        raise SystemExit("no frames under " + ANIM_ROOT)

    ext_id = 0
    ext_lines = []
    anim_blocks = []
    for state in states:
        files = sorted(f for f in os.listdir(os.path.join(ANIM_ROOT, state)) if f.endswith(".png"))
        frame_lines = []
        for f in files:
            ext_id += 1
            rid = "%d_%s" % (ext_id, os.path.splitext(f)[0])
            rel = "res://HuTaoMod/images/character/anim/%s/%s" % (state, f)
            ext_lines.append('[ext_resource type="Texture2D" path="%s" id="%s"]' % (rel, rid))
            frame_lines.append('{\n"duration": 1.0,\n"texture": ExtResource("%s")\n}' % rid)
        loop = "true" if state in LOOP_STATES else "false"
        fps = fps_map.get(state, 24.0)
        anim_blocks.append(
            '{\n"frames": [%s],\n"loop": %s,\n"name": &"%s",\n"speed": %s\n}'
            % (", ".join(frame_lines), loop, state, fps))

    text = "[gd_resource type=\"SpriteFrames\" load_steps=%d format=3]\n\n" % (ext_id + 1)
    text += "\n".join(ext_lines) + "\n\n[resource]\nanimations = [%s]\n" % ", ".join(anim_blocks)
    os.makedirs(os.path.dirname(TRES), exist_ok=True)
    with open(TRES, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)
    print("wrote %s (%d animations, %d frames)" % (TRES, len(states), ext_id))


# ------------------------------------------------------------------------- main

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--state")
    ap.add_argument("--input")
    ap.add_argument("--fps", type=int, default=24)
    ap.add_argument("--trim", nargs=2, type=float)
    ap.add_argument("--canvas", default="%dx%d" % OUT_CANVAS)
    ap.add_argument("--build-tres", action="store_true")
    ap.add_argument("--info", action="store_true")
    args = ap.parse_args()

    ax, ay = body_anchor()
    cw, ch = (int(v) for v in args.canvas.lower().split("x"))
    k = cw / float(BODY_CANVAS[0])

    if args.info or (not args.state and not args.build_tres):
        print("body canvas      : %dx%d" % BODY_CANVAS)
        print("body anchor      : (%.1f, %.1f)  [center_x, feet_y]" % (ax, ay))
        print("output canvas    : %dx%d" % (cw, ch))
        print("target anchor    : (%.1f, %.1f)" % (ax * k, ay * k))
        print("frames node      : AnimatedSprite2D under Visuals/Body")
        print("                   position = (0, 0)   scale = (%.2f, %.2f)   visible = false"
              % (BODY_CANVAS[0] / float(cw), BODY_CANVAS[0] / float(cw)))
        return

    if args.build_tres:
        build_tres({})
        return

    if not args.state or not args.input:
        raise SystemExit("need --state and --input")

    work = os.path.join(TMP, args.state)
    paths = extract_frames(args.input, work, args.fps, args.trim)
    print("frames extracted : %d" % len(paths))

    keyed = [key_background(Image.open(p).convert("RGBA")) for p in paths]
    frames, scale = normalize(keyed, (cw, ch), (ax, ay))
    dst, names = write_frames(args.state, frames)
    print("write            : %s (%d png)" % (dst, len(names)))
    print("output scale     : %.4f (frame pixels -> body canvas pixels)" % scale)
    print("next             : python tools/ai_anim_to_godot.py --build-tres")
    print("then             : add AnimatedSprite2D 'Frames' under Visuals/Body")
    print("                   sprite_frames = res://HuTaoMod/scenes/hu_tao_frames.tres")
    print("                   position = (0, 0)   scale = (%.2f, %.2f)   visible = false"
          % (BODY_CANVAS[0] / float(cw), BODY_CANVAS[0] / float(cw)))


if __name__ == "__main__":
    main()
