"""审计：卡面描述里的 {占位符} 是否真的有对应的 DynamicVar。

原版 DynamicVarSet 是字典硬索引，`{X}` 找不到 X 就会把花括号原样显示在卡面上
（往生堂训写了 {BloodBlossomPower} 但只声明了 PowerVar<ButterflyWispPower> 就是这种bug）。
本脚本把每个卡牌类声明的变量名抽出来，和 zhs 卡面描述里的占位符对比。

用法: python tools/audit_card_placeholders.py
"""

import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARDS_DIR = os.path.join(ROOT, "Code", "Cards")
LOC = os.path.join(ROOT, "HuTaoMod", "localization", "zhs", "cards.json")

# 非变量占位符（rich text / 其它系统注入的），忽略
IGNORE = {"gold", "purple", "red", "green", "blue", "aqua", "orange", "pink"}

CLASS_RE = re.compile(r"public class (\w+)\s*\(\s*\)\s*:\s*HuTaoModCard")
VAR_PATTERNS = [
    (re.compile(r"new PowerVar<(\w+)>"), lambda m: m.group(1)),
    (re.compile(r"new DamageVar"), lambda m: "Damage"),
    (re.compile(r"new BlockVar"), lambda m: "Block"),
    (re.compile(r"new HealVar"), lambda m: "Heal"),
    (re.compile(r"new HpLossVar"), lambda m: "HpLoss"),
    (re.compile(r"new EnergyVar"), lambda m: "Energy"),
    (re.compile(r'new CardsVar\("(\w+)"'), lambda m: m.group(1)),
    (re.compile(r"new CardsVar\b(?!\()"), lambda m: "Cards"),
    (re.compile(r"new CardsVar\("), lambda m: "Cards"),
]


def snake(name):
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", name).lower()


def parse_classes():
    out = {}
    for fn in sorted(os.listdir(CARDS_DIR)):
        if not fn.endswith(".cs"):
            continue
        text = open(os.path.join(CARDS_DIR, fn), encoding="utf-8").read()
        matches = list(CLASS_RE.finditer(text))
        for i, m in enumerate(matches):
            name = m.group(1)
            end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
            body = text[m.start():end]
            # 只看 CanonicalVars 那一段，避免把 OnPlay 里的引用算进来
            blk = re.search(r"CanonicalVars\s*=>?\s*\[(.*?)\]", body, re.S)
            scope = blk.group(1) if blk else ""
            names = set()
            for pat, get in VAR_PATTERNS:
                for pm in pat.finditer(scope):
                    names.add(get(pm))
            out[name] = names
    return out


def main():
    classes = parse_classes()
    loc = json.load(open(LOC, encoding="utf-8"))
    problems = 0
    checked = 0

    for cls, names in sorted(classes.items()):
        key = "hutaomod.%s.description" % snake(cls)
        desc = loc.get(key)
        if desc is None:
            continue
        checked += 1
        used = set(re.findall(r"\{(\w+)\}", desc)) - IGNORE
        missing = used - names
        if missing:
            problems += 1
            print("[MISSING] %-26s 卡面用了 %-24s 实际声明 %s" %
                  (cls, ",".join(sorted(missing)), ",".join(sorted(names)) or "(无)"))
        unused = names - used
        if unused:
            print("[UNUSED ] %-26s 声明了但卡面没引用: %s" % (cls, ",".join(sorted(unused))))

    print("\n检查 %d 张卡，占位符不匹配 %d 张" % (checked, problems))


if __name__ == "__main__":
    main()
