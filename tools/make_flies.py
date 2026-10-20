"""Split hu_tao_butterflies.png into individual butterflies, emitting tscn snippets.

Why: the butterfly layer is one Sprite2D holding every butterfly, so it can only
float as a single blob. Splitting lets each butterfly have its own phase,
amplitude and speed, which is what actually reads as a swarm.

No new textures are produced: each butterfly is an AtlasTexture region of the
original PNG, so nothing has to be re-imported.

Usage: python tools/make_flies.py
"""

import os

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TEX = os.path.join(ROOT, "HuTaoMod", "images", "character", "hu_tao_butterflies.png")

# Keep in sync with the original "Butterflies" node in hu_tao_visuals.tscn
BASE_POS = (23.6, -186.8)
SCALE = 0.135
PAD = 10          # region padding in px, keeps soft edges
MIN_AREA = 300    # minimum connected area at full resolution
DOWNSCALE = 4


def label(binary):
    """4-connected component labelling with union-find. Returns (labels, count)."""
    h, w = binary.shape
    parent = {}

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a

    def union(a, b):
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[rb] = ra

    labels = np.zeros((h, w), dtype=np.int32)
    nxt = 0
    for y in range(h):
        for x in np.nonzero(binary[y])[0]:
            up = labels[y - 1, x] if y > 0 else 0
            left = labels[y, x - 1] if x > 0 else 0
            if up and left:
                union(up, left)
                labels[y, x] = find(up)
            elif up or left:
                labels[y, x] = find(up or left)
            else:
                nxt += 1
                parent[nxt] = nxt
                labels[y, x] = nxt

    for v in range(1, nxt + 1):
        find(v)

    remap = {}
    for y in range(h):
        for x in range(w):
            if labels[y, x]:
                labels[y, x] = remap.setdefault(find(labels[y, x]), len(remap) + 1)
    return labels, len(remap)


def main():
    im = Image.open(TEX).convert("RGBA")
    width, height = im.size
    alpha = np.array(im)[..., 3]
    solid = alpha > 40

    h, w = solid.shape
    hh, ww = h // DOWNSCALE, w // DOWNSCALE
    small = solid[: hh * DOWNSCALE, : ww * DOWNSCALE].reshape(
        hh, DOWNSCALE, ww, DOWNSCALE).max(axis=(1, 3))

    labels, count = label(small)
    print("# connected components (downscaled): %d" % count)

    boxes = []
    for lid in range(1, count + 1):
        ys, xs = np.nonzero(labels == lid)
        if len(xs) == 0:
            continue
        area = len(xs) * DOWNSCALE * DOWNSCALE
        if area < MIN_AREA:
            continue
        x0, x1 = xs.min() * DOWNSCALE, (xs.max() + 1) * DOWNSCALE
        y0, y1 = ys.min() * DOWNSCALE, (ys.max() + 1) * DOWNSCALE
        sy, sx = np.nonzero(solid[y0:y1, x0:x1])
        if len(sx) == 0:
            continue
        x0, x1 = x0 + sx.min(), x0 + sx.max() + 1
        y0, y1 = y0 + sy.min(), y0 + sy.max() + 1
        x0 = max(0, x0 - PAD)
        y0 = max(0, y0 - PAD)
        x1 = min(width, x1 + PAD)
        y1 = min(height, y1 + PAD)
        boxes.append((area, x0, y0, x1 - x0, y1 - y0))

    boxes.sort(reverse=True)
    print("# usable butterflies: %d" % len(boxes))

    cw, ch = width / 2.0, height / 2.0
    for i, (area, x, y, bw, bh) in enumerate(boxes, start=1):
        px = BASE_POS[0] + (x + bw / 2.0 - cw) * SCALE
        py = BASE_POS[1] + (y + bh / 2.0 - ch) * SCALE
        print()
        print("# ---- butterfly %d: area=%d region=(%d,%d,%d,%d)" % (i, area, x, y, bw, bh))
        print('[sub_resource type="AtlasTexture" id="AtlasTexture_fly%d"]' % i)
        print('atlas = ExtResource("3_fly")')
        print("region = Rect2(%d, %d, %d, %d)" % (x, y, bw, bh))
        print()
        print('[node name="Fly%d" type="Sprite2D" parent="Visuals/Flies"]' % i)
        print('texture = SubResource("AtlasTexture_fly%d")' % i)
        print("position = Vector2(%.1f, %.1f)" % (px, py))
        print("scale = Vector2(%s, %s)" % (SCALE, SCALE))


if __name__ == "__main__":
    main()
