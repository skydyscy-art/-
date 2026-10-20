"""生成 Power 图标（64x64 + big 256x256）。

当前生成：
  samsara_power.png       轮回 —— 横过来的「8」(无限符号)，往生堂红金配色
  blood_blossom_power.png 血梅香 —— 五瓣梅花，深红

配色取自 mod 主色 #d64550；图标中心留空、笔画偏粗，保证 64px 下也能看清。
游戏会在图标右下角自动叠加层数，所以图形本体不要画满。

用法: python tools/make_power_icons.py
"""

import math
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "HuTaoMod", "images", "powers")
SIZES = ((64, ""), (256, "big"))

RED = (214, 69, 80)
RED_DARK = (120, 26, 36)
GOLD = (238, 200, 120)
INK = (26, 18, 22)


def lemniscate(t, size):
    """伯努利双纽线（横着的 8）。"""
    s = math.sin(t)
    c = math.cos(t)
    d = 1.0 + s * s
    x = c / d
    y = (s * c) / d
    r = size * 0.40
    return (size / 2 + x * r, size / 2 + y * r * 1.05)


def draw_samsara(size):
    ss = 4  # 超采样，画完再缩小，边缘更干净
    img = Image.new("RGBA", (size * ss, size * ss), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    n = 720
    pts = [lemniscate(2 * math.pi * i / n, size * ss) for i in range(n + 1)]

    # 深色描边
    d.line(pts, fill=RED_DARK + (255,), width=int(size * ss * 0.115), joint="curve")
    # 主体红
    d.line(pts, fill=RED + (255,), width=int(size * ss * 0.078), joint="curve")
    # 高光细线（偏上侧，做出体积感）
    d.line([(x, y - size * ss * 0.012) for x, y in pts], fill=GOLD + (200,),
           width=int(size * ss * 0.024), joint="curve")

    img = img.resize((size, size), Image.LANCZOS)
    return img


def draw_blossom(size):
    ss = 4
    img = Image.new("RGBA", (size * ss, size * ss), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = cy = size * ss / 2
    petal = size * ss * 0.23

    for i in range(5):
        a = -math.pi / 2 + i * 2 * math.pi / 5
        px = cx + math.cos(a) * petal * 0.92
        py = cy + math.sin(a) * petal * 0.92
        r = petal * 0.78
        d.ellipse([px - r, py - r, px + r, py + r], fill=RED + (255,), outline=RED_DARK + (255,),
                  width=int(size * ss * 0.02))

    core = petal * 0.34
    d.ellipse([cx - core, cy - core, cx + core, cy + core], fill=INK + (255,))
    d.ellipse([cx - core * 0.45, cy - core * 0.45, cx + core * 0.45, cy + core * 0.45],
              fill=GOLD + (255,))

    return img.resize((size, size), Image.LANCZOS)


def main():
    os.makedirs(os.path.join(OUT, "big"), exist_ok=True)
    for size, sub in SIZES:
        folder = os.path.join(OUT, sub) if sub else OUT
        draw_samsara(size).save(os.path.join(folder, "samsara_power.png"))
        draw_blossom(size).save(os.path.join(folder, "blood_blossom_power.png"))
        print("wrote %s/samsara_power.png + blood_blossom_power.png (%dpx)" % (folder, size))


if __name__ == "__main__":
    main()
