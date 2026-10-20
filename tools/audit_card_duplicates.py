"""审计：找出「同费、同类型、同效果」的重复卡。

判定方式：把每张卡的
  - 费用 / 类型 / 目标 / 稀有度
  - CanonicalVars（变量名 + 初值）
  - CanonicalKeywords（含灵/还阳/轮回/消耗）
  - OnPlay 方法体（去掉空白后的代码文本）
拼成签名，签名相同即视为效果重复。

用法: python tools/audit_card_duplicates.py
"""

import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARDS = os.path.join(ROOT, "Code", "Cards")

CLASS_RE = re.compile(r"public class (\w+)\s*\(\s*\)\s*:\s*HuTaoModCard\(([^)]*)\)")
VAR_RE = re.compile(r"new (\w+(?:<[^>]+>)?)\s*\(")
KEY_RE = re.compile(r"CanonicalKeywords\s*=>\s*\[([^\]]*)\]")


def norm(text):
    return re.sub(r"\s+", " ", text).strip()


def signature(body):
    ctor = CLASS_RE.search(body)
    if not ctor:
        return None
    name, args = ctor.group(1), norm(ctor.group(2))

    vars_blk = re.search(r"CanonicalVars\s*=>?\s*\[(.*?)\]", body, re.S)
    varsig = norm(vars_blk.group(1)) if vars_blk else ""

    key = KEY_RE.search(body)
    keysig = norm(key.group(1)) if key else ""

    play = re.search(r"protected override async Task OnPlay\(.*?\n    \}", body, re.S)
    playsig = norm(play.group(0)) if play else ""

    return (args, varsig, keysig, playsig), name


def main():
    groups = {}
    for fn in sorted(os.listdir(CARDS)):
        if not fn.endswith(".cs"):
            continue
        text = open(os.path.join(CARDS, fn), encoding="utf-8").read()
        matches = list(CLASS_RE.finditer(text))
        for i, _ in enumerate(matches):
            start = matches[i].start()
            end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
            body = text[start:end]
            sig = signature(body)
            if not sig:
                continue
            key, name = sig
            if not key[3]:            # 没 OnPlay（比如纯 Power）就先跳过
                continue
            groups.setdefault(key, []).append((name, fn))

    dupes = {k: v for k, v in groups.items() if len(v) > 1}
    if not dupes:
        print("没有发现完全重复的卡。")
        return

    print("发现 %d 组重复卡：\n" % len(dupes))
    for key, members in dupes.items():
        print("  [%s]" % key[0])
        for name, fn in members:
            print("    - %-26s %s" % (name, fn))
        print()


if __name__ == "__main__":
    main()
