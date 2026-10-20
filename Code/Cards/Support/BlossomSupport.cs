using System.Collections.Generic;
using System.Linq;
using BaseLib.Extensions;
using BaseLib.Utils;
using HuTaoMod.Code.Powers;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HuTaoMod.Code.Cards.Support;

/// <summary>
/// 血梅香核心工具 —— 所有胡桃攻击卡的统一入口。
///
/// 机制：血梅香不再是回合结束 DoT，而是「目标每受到一次攻击伤害，
/// 每层额外受到 1 点伤害」——多段攻击每 hit 各结算一次。
///
/// 实现路线：基础伤害走 CommonActions.CardAttack（原版伤害管道，正确吃力量/易伤/虚弱），
/// 血梅香的平坦加伤用 CreatureCmd.Damage 直接补刀（不进攻击管道，不吃力量）。
/// </summary>
public static class BlossomSupport
{
    private static readonly Random Rng = new();

    /// <summary>目标身上的血梅香层数（0 = 无）。</summary>
    public static int BlossomOn(Creature? target)
        => target == null ? 0 : target.Powers.OfType<BloodBlossomPower>().Sum(p => p.Amount);

    /// <summary>是否存在至少一个带血梅香的敌人。</summary>
    public static bool AnyBlossomEnemy(Creature playerCreature)
        => GetOpponents(playerCreature).Any(e => BlossomOn(e) > 0);

    /// <summary>带血梅香的敌人数（天命有归的回血加成用）。</summary>
    public static int BlossomEnemyCount(Creature playerCreature)
        => GetOpponents(playerCreature).Count(e => BlossomOn(e) > 0);

    /// <summary>读取生物身上任意 Power 的总层数（泛化版）。</summary>
    public static int PowerLevelOf<TPower>(Creature? creature) where TPower : PowerModel
        => creature == null ? 0 : creature.Powers.OfType<TPower>().Sum(p => p.Amount);

    /// <summary>施加血梅香（正数加层）。</summary>
    public static Task ApplyBlossom(Creature target, CardModel? source, int amount)
        => CommonActions.Apply<BloodBlossomPower>(target, source, amount);

    /// <summary>消耗（移除）目标 n 层血梅香，返回实际消耗的层数。</summary>
    public static async Task<int> ConsumeBlossom(Creature target, CardModel? source, int amount)
    {
        amount = Math.Min(amount, BlossomOn(target));
        if (amount <= 0) return 0;
        await CommonActions.Apply<BloodBlossomPower>(target, source, -amount);
        return amount;
    }

    /// <summary>获取对手列表（安全封装）。</summary>
    public static IEnumerable<Creature> GetOpponents(Creature playerCreature)
    {
        var state = CombatManager.Instance.DebugOnlyGetState();
        if (state == null) return [];
        return state.GetOpponentsOf(playerCreature);
    }

    /// <summary>血梅香加伤补刀：对目标结算「层数 × perLayer」点直接伤害。</summary>
    public static async Task BlossomBonusDamage(HuTaoModCard card, PlayerChoiceContext ctx, Creature? target, int perLayer)
    {
        if (target == null || perLayer <= 0) return;
        int stacks = BlossomOn(target);
        if (stacks <= 0) return;
        await FlatDamage(card, ctx, target, stacks * perLayer);
    }

    /// <summary>
    /// 平坦直接伤害（血梅香的补刀），**不吃任何伤害修正**：
    /// ValueProp.Unpowered 让缩小/虚弱/易伤/力量都不作用于它，符合设计意图。
    /// </summary>
    public static async Task FlatDamage(HuTaoModCard card, PlayerChoiceContext ctx, Creature? target, int amount)
    {
        if (target == null || amount <= 0) return;
        await CreatureCmd.Damage(ctx, target, amount, ValueProp.Unpowered, card, null);
    }

    /// <summary>
    /// 用游戏原生 AttackCommand 构造攻击。
    /// 关键在 <c>FromCard</c>：它会把 Attacker（= dealer）设成卡主，
    /// 这样所有「按 dealer 生效」的修正（缩小 ShrinkPower、力量、虚弱…）才会正确应用。
    /// BaseLib 的 CommonActions.CardAttack 走的是 FromCardCompatibility 兼容层，
    /// 这条链路里 dealer 可能没设上，于是缩小对普通攻击无效。
    /// </summary>
    private static AttackCommand BuildAttack(HuTaoModCard card, CardPlay? play, string? vfx)
    {
        DamageVar damageVar = card.DynamicVars.Damage;

        // 灵：带灵的牌打出时伤害 +50%（同一张牌的多段攻击只结算第一段；
        // 灵的「消耗」统一在 HuTaoModCard.AfterCardPlayed 里做，这样生人勿近 / 香如故也能读到）
        decimal baseDamage = damageVar.BaseValue;
        if (SoulReturn.HasSpirit(card) && !SoulReturn.SpiritDamageUsed[card])
        {
            baseDamage = System.Math.Round(baseDamage * 1.5m, System.MidpointRounding.AwayFromZero);
            SoulReturn.SpiritDamageUsed[card] = true;
        }

        AttackCommand cmd = DamageCmd.Attack(baseDamage)
            .WithHitCount(1)
            .WithValueProp(damageVar.Props)
            .FromCard(card, play);

        // FromCard 不负责选目标，必须显式指定，否则 Execute 会抛 "No targets set"
        if (card.TargetType == TargetType.AllEnemies)
        {
            if (card.CombatState != null)
                cmd = cmd.TargetingAllOpponents(card.CombatState);
        }
        else if (play?.Target != null)
        {
            cmd = cmd.Targeting(play.Target);
        }
        else if (play?.Target == null)
        {
            // 兜底：没有目标时退回 BaseLib 的通用实现，避免卡牌直接失效
            return CommonActions.CardAttack(card, play, vfx: vfx);
        }

        if (!string.IsNullOrEmpty(vfx))
            cmd = cmd.WithAttackerFx(vfx);
        return cmd;
    }

    /// <summary>
    /// 胡桃单体攻击卡统一入口：逐 hit 打基础伤害 + 每 hit 结算血梅香加伤 + 可选每 hit 挂香。
    /// </summary>
    public static async Task BlossomStrike(HuTaoModCard card, PlayerChoiceContext ctx, CardPlay play,
        int hitCount = 1, int perLayer = 1, int markPerHit = 0, string vfx = "vfx/vfx_attack_slash")
    {
        // 彼岸归途：你的所有攻击卡每 hit 额外挂层（能力光环）
        var playerC = card.Owner?.Creature;
        if (playerC == null) return;
        markPerHit += PowerLevelOf<ParamitaReturnPower>(playerC);
        for (int i = 0; i < hitCount; i++)
        {
            if (play.Target == null) break;
            await BuildAttack(card, play, vfx).Execute(ctx);
            // 淬刃梅香：对带香目标的每 hit 额外增伤（层数即增伤值）
            int razor = PowerLevelOf<RazorBlossomPower>(playerC);
            if (razor > 0 && BlossomOn(play.Target) > 0)
                await FlatDamage(card, ctx, play.Target, razor);
            // 向死而战：低血时每段额外伤害
            int lowBonus = PowerLevelOf<FightUntoDeathPower>(playerC);
            if (lowBonus > 0 && ModActions.IsBelowHalfHp(playerC))
                await FlatDamage(card, ctx, play.Target, lowBonus);
            await BlossomBonusDamage(card, ctx, play.Target, perLayer);
            if (markPerHit > 0)
                await ApplyBlossom(play.Target, card, markPerHit);
        }

        // 阈值奖励：≥6 层「落梅」抽 1（每回合最多 2 次），每张攻击卡结算一次
        if (play.Target != null)
            await ThresholdReward(card, ctx, play.Target);

        // 堂规森严：本回合每打出 1 张攻击牌，额外获得格挡
        await GrantStrictPlumRuleBlock(playerC);
    }

    /// <summary>堂规森严的结算：每张攻击牌一次（不是每段一次）。</summary>
    private static async Task GrantStrictPlumRuleBlock(Creature playerCreature)
    {
        int perCard = PowerLevelOf<StrictPlumRulePower>(playerCreature);
        if (perCard > 0)
            await ModActions.GainBlock(playerCreature, perCard);
    }

    /// <summary>
    /// 血梅香阈值（每张攻击卡结算一次）：
    ///   ≥6 层「落梅」→ 攻击后抽 1 张（每回合最多 2 次）
    ///
    /// v11.2 平衡调整：删掉了旧的「≥4 层落梅加伤」与「≥10 层绽裂引爆翻倍」，
    /// 阈值只保留这一条，血梅香也改成每回合 -2 层，整体节奏更快、爆发更低。
    ///
    /// ⚠ 抽牌必须显式给数量：CommonActions.Draw 读的是 DynamicVars.Cards，
    /// 而绝大多数攻击卡没声明 CardsVar（字典硬索引会抛 KeyNotFoundException，
    /// 导致出牌中断、卡牌悬停在空中）。
    /// </summary>
    private static async Task ThresholdReward(HuTaoModCard card, PlayerChoiceContext ctx, Creature target)
    {
        int stacks = BlossomOn(target);
        if (stacks < 6) return;
        if (!TryConsumeHarvest(card)) return;
        if (card.Owner == null) return;
        await CardPileCmd.Draw(ctx, 1, card.Owner);
    }

    private static int _harvestRound = -1;
    private static int _harvestCount;

    /// <summary>落梅抽牌每回合最多触发 2 次。</summary>
    private static bool TryConsumeHarvest(HuTaoModCard card)
    {
        int round = card.CombatState?.RoundNumber ?? -1;
        if (round != _harvestRound)
        {
            _harvestRound = round;
            _harvestCount = 0;
        }
        if (_harvestCount >= 2) return false;
        _harvestCount++;
        return true;
    }

    /// <summary>
    /// AOE 攻击卡：先全场攻击（可多段），再对每个存活敌人逐个结算
    /// 血梅香加伤 / 每段挂层 / 落梅阈值抽牌。
    /// </summary>
    public static async Task BlossomStrikeAll(HuTaoModCard card, PlayerChoiceContext ctx, CardPlay play,
        int perLayer = 1, int hitCount = 1, int markPerHit = 0, string vfx = "vfx/vfx_attack_slash")
    {
        for (int i = 0; i < hitCount; i++)
            await BuildAttack(card, play, vfx).Execute(ctx);

        var playerC = card.Owner?.Creature;
        if (playerC == null) return;

        markPerHit += PowerLevelOf<ParamitaReturnPower>(playerC);
        foreach (var enemy in GetOpponents(playerC))
        {
            if (enemy.IsDead) continue;
            await BlossomBonusDamage(card, ctx, enemy, perLayer);
            if (markPerHit > 0)
                await ApplyBlossom(enemy, card, markPerHit);
            await ThresholdReward(card, ctx, enemy);
        }
        await GrantStrictPlumRuleBlock(playerC);
    }

    /// <summary>
    /// 单体引爆：先把血梅香「烧一遍」造成伤害，再清除层数。
    ///
    /// 顺序很关键：如果先清层数再结算伤害，敌人死亡时身上是 0 层，
    /// 遗物「引魂蝶灯：敌人死亡时按剩余血梅香层数回复生命」就永远回不到血。
    /// 现在的顺序保证伤害与死亡判定时层数还在，遗物能读到并回血。
    /// </summary>
    public static async Task Detonate(HuTaoModCard card, PlayerChoiceContext ctx, Creature? target, int perStack)
    {
        if (target == null) return;

        int stacks = BlossomOn(target);
        if (stacks <= 0) return;

        int damage = stacks * perStack;                                  // v11.2：移除绽裂翻倍

        await FlatDamage(card, ctx, target, damage);

        // 目标没死才清除层数；死亡时层数留在身上，让遗物按「层数」回血
        if (!target.IsDead)
            await ConsumeBlossom(target, card, stacks);
    }

    /// <summary>全场引爆：每个敌人的血梅香各自结算。</summary>
    public static async Task DetonateAll(HuTaoModCard card, PlayerChoiceContext ctx, int perStack)
    {
        var playerC = card.Owner?.Creature;
        if (playerC == null) return;
        foreach (var foe in GetOpponents(playerC).Where(e => BlossomOn(e) > 0).ToList())
            await Detonate(card, ctx, foe, perStack);
    }

    /// <summary>随机存活敌人（幽蝶行灵的回合开始铺层用）。</summary>
    public static Creature? RandomEnemy(Creature playerCreature)
    {
        var foes = GetOpponents(playerCreature).ToList();
        return foes.Count == 0 ? null : foes[Rng.Next(foes.Count)];
    }

    /// <summary>当前全部存活敌人的快照（供 Power 的回合钩子遍历用）。</summary>
    public static List<Creature> OpponentsSnapshot(Creature playerCreature)
        => GetOpponents(playerCreature).ToList();
}
