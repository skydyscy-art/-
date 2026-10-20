using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HuTaoMod.Code.Utils;

/// <summary>
/// 把「与当前游戏版本相关性最高的几行 API」集中在这里。
/// 如果游戏更新导致方法名变化，只需要改这一个文件。
///
/// 已按 sts2 v0.111.0 的真实签名校对：
///  - 失去生命 = CreatureCmd.Damage(..., ValueProp.Unblockable | Unpowered | Move, ...)
///    （对应原版 Bloodletting / Offering 的实现）
///  - 获得护甲 = CreatureCmd.GainBlock(creature, amount, props, cardPlay)
///  - 获得能量 = PlayerCmd.GainEnergy(amount, player)
///  - 获得金币 = PlayerCmd.GainGold(amount, player)
/// </summary>
public static class ModActions
{
    /// <summary>
    /// 无玩家选择场景下使用的占位上下文。
    /// 游戏自身在 Power 触发链里也用 <see cref="ThrowingPlayerChoiceContext"/>，
    /// 前提是这条调用链不会真的弹出选择界面。
    /// </summary>
    private static readonly PlayerChoiceContext NoChoice = new ThrowingPlayerChoiceContext();

    /// <summary>失去生命（不经过护甲）。胡桃的核心机制：以血换伤。</summary>
    public static async Task LoseHp(Creature creature, decimal amount)
    {
        await CreatureCmd.Damage(NoChoice, creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, null);
        // 自己烧掉的血不算「受伤」，夜行等卡牌要能把它从总掉血里扣掉
        CombatTracker.SelfHpLostThisTurn += amount;
    }

    /// <summary>回复生命（安神秘法的治疗）。</summary>
    public static Task Heal(Creature creature, decimal amount)
        => CreatureCmd.Heal(creature, amount);

    /// <summary>获得护甲（非卡牌来源：遗物 / 能力触发）。</summary>
    public static Task GainBlock(Creature creature, decimal amount)
        => CreatureCmd.GainBlock(creature, amount, ValueProp.Unpowered, null);

    /// <summary>生命是否不高于一半（安神秘法的治疗条件）。</summary>
    public static bool IsBelowHalfHp(Creature creature)
    {
        return creature.CurrentHp * 2 <= creature.MaxHp;
    }

    /// <summary>获得能量（血偿契约 / 阴阳两界）。</summary>
    public static Task GainEnergy(decimal amount, Player player)
        => PlayerCmd.GainEnergy(amount, player);

    /// <summary>获得金币（开张大吉 / 棺材铺日课）。</summary>
    public static Task GainGold(decimal amount, Player player)
        => PlayerCmd.GainGold(amount, player);

    /// <summary>
    /// 弃 1 张手牌（烛火摇曳 / 梅香疾冲）。
    /// 由玩家自己挑一张，和原版「抽牌后弃一张」一致。
    /// </summary>
    public static async Task DiscardOne(CardModel card, PlayerChoiceContext context)
    {
        var player = card?.Owner;
        if (player?.PlayerCombatState == null) return;
        int handCount = player.PlayerCombatState.Hand.Cards.Count;
        Diag($"discard.begin hand={handCount}");
        if (handCount == 0) return;

        var prompt = new LocString("static_hover_tips", "HUTAOMOD-SELECT_DISCARD");
        IEnumerable<CardModel> picked = await CommonActions.SelectCards(card, prompt, context, PileType.Hand, 1);
        int n = picked == null ? 0 : picked.Count();
        Diag($"discard.selected {n}");
        if (n > 0)
        {
            await CardCmd.Discard(context, picked);
            Diag("discard.done");
        }
    }

    /// <summary>
    /// 弃 N 张手牌（烛火摇曳的「抽 2 弃 2」）。
    /// 由玩家自己挑，手牌不足时自动按手牌数结算。
    /// </summary>
    public static async Task DiscardMany(CardModel card, PlayerChoiceContext context, int count)
    {
        var player = card?.Owner;
        if (player?.PlayerCombatState == null || count <= 0) return;

        int handCount = player.PlayerCombatState.Hand.Cards.Count;
        Diag($"discard.many.begin hand={handCount} want={count}");
        int take = System.Math.Min(count, handCount);
        if (take <= 0) return;

        var prompt = new LocString("static_hover_tips", "HUTAOMOD-SELECT_DISCARD");
        IEnumerable<CardModel> picked = await CommonActions.SelectCards(card, prompt, context, PileType.Hand, take);
        int n = picked == null ? 0 : picked.Count();
        Diag($"discard.many.selected {n}");
        if (n > 0)
            await CardCmd.Discard(context, picked);
    }

    /// <summary>诊断日志（临时）：和卡牌出牌日志同一前缀，方便对齐时间线。</summary>
    private static void Diag(string msg)
        => global::HuTaoMod.MainFile.Logger.Info("[OP] " + msg);
}
