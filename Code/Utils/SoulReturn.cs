using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using HuTaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace HuTaoMod.Code.Utils;

/// <summary>「灵」的三种状态。</summary>
public enum SpiritState
{
    /// <summary>没有灵。</summary>
    None = 0,

    /// <summary>带灵，尚未打出 —— 金色「灵」，可强化一次。</summary>
    Active = 1,

    /// <summary>灵已被打出消耗 —— 显示为「灵·已消耗」，不再强化，但仍算「带灵牌」。</summary>
    Spent = 2,
}

/// <summary>
/// 还阳 / 灵 的实现（v11.1 修订）。
///
/// 还阳 X：把弃牌堆顶最多 X 张牌放回抽牌堆顶，并给这些牌打上「灵」。
/// 灵：带灵的牌打出时强化一次（攻击 +50% 伤害 / 技能 +1 层 / 费用 -1），打出后转为「灵·已消耗」。
///   - 攻击 +50%：BlossomSupport.BuildAttack（每张牌只结算一次）
///   - 技能 +1 层：HuTaoModCard.ModifyPowerAmountGivenAdditive
///   - 费用 -1：HuTaoModCard.TryModifyEnergyCostInCombat
///   - 消耗时机：HuTaoModCard.AfterCardPlayed（香如故会让每回合第一张带灵牌保留灵）
///
/// 这套机制是 v9 设计的核心循环：
///   还阳 → 灵 → 打出带灵牌 → 生人勿近(+轮回) → 万夫莫敌(抽牌) → 更多还阳 …
/// </summary>
public static class SoulReturn
{
    /// <summary>本回合还阳过的张数（遗物 / 能力读取），玩家回合开始时重置。</summary>
    public static int ReturnedThisTurn { get; private set; }

    public static void ResetTurnCounter() => ReturnedThisTurn = 0;

    /// <summary>卡牌当前的「灵」状态。</summary>
    public static readonly SpireField<CardModel, SpiritState> Spirit = new(() => SpiritState.None);

    /// <summary>「攻击 +50%」本张牌是否已经吃过（多段攻击只算一次）。</summary>
    public static readonly SpireField<CardModel, bool> SpiritDamageUsed = new(() => false);

    public static SpiritState StateOf(CardModel? card) => card == null ? SpiritState.None : Spirit[card];

    /// <summary>是否「带灵」且尚未消耗（可以被强化）。</summary>
    public static bool HasSpirit(CardModel? card) => StateOf(card) == SpiritState.Active;

    /// <summary>是否「带灵牌」—— 包含已消耗的暗色灵牌（命途逆旅等判定用）。</summary>
    public static bool HasSpiritMark(CardModel? card) => StateOf(card) != SpiritState.None;

    /// <summary>
    /// 设置/移除「灵」。只改状态，卡面关键词交给 <see cref="ApplyKeyword"/>（见那里的时序说明）。
    /// </summary>
    public static void SetSpirit(CardModel? card, bool on)
    {
        if (card == null) return;
        Spirit[card] = on ? SpiritState.Active : SpiritState.Spent;
        SpiritDamageUsed[card] = false;
        ApplyKeyword(card);
    }

    /// <summary>
    /// 按当前状态同步「灵 / 灵·已消耗」关键词。
    ///
    /// ⚠ 时机很关键：卡牌在 **Play** 堆（打出动画进行中）时不能动关键词 ——
    /// 动画中途改卡面会让卡牌节点重建，结果那张牌会悬停在空中不回弃牌堆。
    /// 所以这里跳过，等它落到弃牌堆（卡牌侧 AfterCardChangedPiles）再补上。
    /// </summary>
    public static void ApplyKeyword(CardModel? card)
    {
        if (card == null) return;

        var pile = card.Pile;
        if (pile != null && pile.Type == PileType.Play) return;

        var state = StateOf(card);
        try
        {
            if (state == SpiritState.Active)
            {
                card.RemoveKeyword(HuTaoKeywords.SpiritSpent);
                card.AddKeyword(HuTaoKeywords.Spirit);
            }
            else if (state == SpiritState.Spent)
            {
                card.RemoveKeyword(HuTaoKeywords.Spirit);
                card.AddKeyword(HuTaoKeywords.SpiritSpent);
            }
        }
        catch (System.Exception e)
        {
            // 卡牌尚未进入战斗状态时可能不允许改关键词，忽略即可
            global::HuTaoMod.MainFile.Logger.Info($"[SP] keyword failed {card.Id.Entry} -> {state}: {e.GetType().Name}");
        }
    }

    /// <summary>
    /// 打出后消耗「灵」。
    /// 香如故：每回合第一张带灵牌打出后，灵不消失（并触发抽牌，由卡牌侧处理）。
    /// 返回 true = 已消耗，false = 被香如故保留。
    /// </summary>
    public static bool TryConsumeSpirit(CardModel? card)
    {
        if (!HasSpirit(card)) return true;

        var owner = card.Owner?.Creature;
        var fragrance = owner?.Powers.OfType<FragranceEverlastingPower>().FirstOrDefault();
        if (fragrance != null && !fragrance.UsedThisTurn)
        {
            fragrance.UsedThisTurn = true;
            return false;
        }

        SetSpirit(card, false);
        return true;
    }

    /// <summary>
    /// 还阳：从弃牌堆取出最多 count 张（从堆顶开始），放回抽牌堆顶并赋予灵。
    /// 返回实际还阳的张数。
    /// </summary>
    public static async Task<int> ReturnFromDiscard(Player? owner, int count)
    {
        if (count <= 0 || owner?.PlayerCombatState == null) return 0;

        var discard = owner.PlayerCombatState.DiscardPile.Cards;
        if (discard.Count == 0) return 0;

        // 弃牌堆"顶部"= 列表末尾
        int take = System.Math.Min(count, discard.Count);
        var picked = new List<CardModel>(take);
        for (int i = 0; i < take; i++)
            picked.Add(discard[discard.Count - 1 - i]);

        foreach (var card in picked)
        {
            SetSpirit(card, true);
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        }

        ReturnedThisTurn += picked.Count;

        // 彼岸蝶护 / 蝶群护卫：本回合每还阳 1 张牌额外获得格挡
        await GrantHuanYangBlock(owner, picked.Count);

        return picked.Count;
    }

    /// <summary>「每还阳 1 张牌额外获得 X 格挡」的结算（由 HuanYangGuardPower 提供数值）。</summary>
    private static async Task GrantHuanYangBlock(Player owner, int returned)
    {
        var creature = owner.Creature;
        if (creature == null || returned <= 0) return;
        int perCard = creature.Powers.OfType<HuanYangGuardPower>().Sum(p => p.Amount);
        if (perCard <= 0) return;
        await ModActions.GainBlock(creature, perCard * returned);
    }

    /// <summary>把指定的一张牌（例如"本卡"）还阳回抽牌堆顶，并赋予灵。</summary>
    public static async Task ReturnCard(CardModel? card)
    {
        if (card == null) return;
        SetSpirit(card, true);
        await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
    }
}
