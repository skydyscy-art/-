"""从 pck 里按「内容锚点」还原本地化 JSON（应对误改导致的内容损坏）。

思路：JSON 在 pck 里是原样存储的文本。用每个文件开头的唯一一行做锚点，
向前找到最近的 '{' 作为起点，再用 JSONDecoder.raw_decode 精确吃掉整个对象，
这样不需要解析 pck 的目录表（该版本头布局与旧文档不同）。

用法: python tools/restore_loc_from_pck.py <pck路径>
"""

import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

ANCHORS = [
    ("HuTaoMod/localization/zhs/cards.json", '"hutaomod.hu_tao_strike.title":  "蝶刺"'),
    ("HuTaoMod/localization/zhs/powers.json", '"hutaomod.blood_blossom.title":  "血梅香"'),
    ("HuTaoMod/localization/eng/cards.json", '"hutaomod.hu_tao_strike.title":  "Butterfly Pierce"'),
    ("HuTaoMod/localization/eng/powers.json", '"hutaomod.blood_blossom.title":  "Blood Blossom"'),
]


def main():
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)
    pck = sys.argv[1]
    data = open(pck, "rb").read()
    text = data.decode("utf-8", "surrogateescape")

    for rel, needle in ANCHORS:
        idx = text.find(needle)
        if idx < 0:
            print("!! anchor not found: %s (%s)" % (rel, needle))
            continue
        start = text.rfind("{", max(0, idx - 400), idx)
        if start < 0:
            print("!! no '{' before anchor: " + rel)
            continue
        obj, end = json.JSONDecoder().raw_decode(text[start:])
        raw = text[start:start + end]
        dst = os.path.join(ROOT, rel.replace("/", os.sep))
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        with open(dst, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(raw)
        print("restored %-42s %6d bytes  keys=%d" % (rel, len(raw.encode("utf-8")), len(obj)))


if __name__ == "__main__":
    main()
