"""从 sts2.dll 提取原版卡牌的数值结构（费用 / 伤害 / 格挡 / 抽牌），用于给 mod 定基准。

输出：
  1. 攻击牌：按费用分组的伤害（次数、最小/中位/最大）
  2. 技能牌：按费用分组的格挡
  3. 所有带 CardsVar 的牌（抽牌数值 + 费用）
  4. 0 费牌的手牌份量参考

用法: python tools/vanilla_stat_curve.py <sts2.dll 路径>
（依赖 pythonnet 太重，这里改用 dotnet 脚本不方便，故用 Cecil 的 dump 文本作为输入）
"""

import io
import os
import re
import statistics
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DUMP = os.path.join(ROOT, "tools", "_vanilla_cards.txt")

LINE_RE = re.compile(r"^(\S+)\s*\|\s*cost=(-?\d+)\s*\|\s*type=(\w+)\s*\|\s*rarity=(\w+)\s*\|\s*vars=(.*)$")


def load():
    rows = []
    if not os.path.exists(DUMP):
        raise SystemExit("先运行 Cecil dump 生成 " + DUMP)
    for line in io.open(DUMP, encoding="utf-8"):
        m = LINE_RE.match(line.strip())
        if not m:
            continue
        name, cost, ctype, rarity, varsig = m.groups()
        vars_ = {}
        for part in varsig.split(","):
            part = part.strip()
            if "=" in part:
                k, v = part.split("=", 1)
                try:
                    vars_[k.strip()] = float(v)
                except ValueError:
                    pass
        rows.append({"name": name, "cost": int(cost), "type": ctype, "rarity": rarity, "vars": vars_})
    return rows


def stats(values):
    if not values:
        return "-"
    return "%d 张  中位 %.1f  区间 %.0f~%.0f" % (
        len(values), statistics.median(values), min(values), max(values))


def main():
    rows = load()
    print("原版卡牌数：%d\n" % len(rows))

    print("=== 攻击牌伤害（按费用）===")
    for cost in sorted({r["cost"] for r in rows if r["type"] == "Attack"}):
        vals = [r["vars"]["Damage"] for r in rows if r["type"] == "Attack" and r["cost"] == cost and "Damage" in r["vars"]]
        print("  %2d 费: %s" % (cost, stats(vals)))

    print("\n=== 技能/能力牌格挡（按费用）===")
    for cost in sorted({r["cost"] for r in rows if "Block" in r["vars"]}):
        vals = [r["vars"]["Block"] for r in rows if r["cost"] == cost and "Block" in r["vars"]]
        print("  %2d 费: %s" % (cost, stats(vals)))

    print("\n=== 带抽牌（Cards）的卡：费用 / 抽数 / 同卡其它数值 ===")
    draws = sorted([r for r in rows if "Cards" in r["vars"]], key=lambda r: (r["cost"], r["vars"]["Cards"]))
    for r in draws:
        extra = ", ".join("%s=%g" % (k, v) for k, v in r["vars"].items() if k != "Cards")
        print("  %-28s %2d 费 抽 %-3g %s %s" % (r["name"], r["cost"], r["vars"]["Cards"], r["type"], ("| " + extra) if extra else ""))

    print("\n=== 0 费牌的全部数值 ===")
    for r in sorted([r for r in rows if r["cost"] == 0], key=lambda r: r["name"]):
        vs = ", ".join("%s=%g" % (k, v) for k, v in r["vars"].items())
        print("  %-30s %-6s %-8s %s" % (r["name"], r["type"], r["rarity"], vs))


if __name__ == "__main__":
    main()
