using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using HuTaoMod.Code.Cards.Support;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace HuTaoMod.Code.Powers;

// v11 新增能力：全部围绕「轮回 / 还阳 / 低血」三条线

/// <summary>读取玩家当前轮回层数。</summary>
internal static class Samsara
{
    public static int Of(Creature? c) => c == null ? 0 : c.Powers.OfType<SamsaraPower>().Sum(p => p.Amount);

    public static Task Gain(Creature? c, CardModel? card, int amount)
        => c == null || amount <= 0 ? Task.CompletedTask : CommonActions.Apply<SamsaraPower>(c, card, amount);
}

/// <summary>生死边缘：生命不高于一半时，回合开始获得 Amount 层轮回。</summary>
public class LifeDeathEdgePower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner == null) return;
        if (!ModActions.IsBelowHalfHp(Owner)) return;
        await Samsara.Gain(Owner, null, Amount);
    }
}

/// <summary>向死而生：生命不高于一半时，回合开始给所有敌人挂 Amount 层血梅香。</summary>
public class UntoDeathPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner == null) return;
        if (!ModActions.IsBelowHalfHp(Owner)) return;
        foreach (var enemy in BlossomSupport.GetOpponents(Owner))
            await BlossomSupport.ApplyBlossom(enemy, null, Amount);
    }
}

/// <summary>
/// 焚身以火：回合开始失去 HpLossPerTurn 点生命，获得 Amount 层轮回。
/// 生命损失因为随升级变化（2 → 1），由卡牌在打出时写进 HpLossPerTurn。
/// </summary>
public class BurningFleshPower : HuTaoModPower
{
    /// <summary>每回合开始失去的生命（基础 2，升级后 1）。</summary>
    public int HpLossPerTurn = 2;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner == null) return;
        await ModActions.LoseHp(Owner, HpLossPerTurn);
        await Samsara.Gain(Owner, null, Amount);
    }
}

/// <summary>威仪天下：回合结束时，若轮回 ≥3 则抽 Amount 张牌。</summary>
public class MajesticRulePower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner == null) return;
        if (Samsara.Of(Owner) < 3) return;
        if (Owner.Player != null)
            await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
    }
}

/// <summary>镇魂安神：回合开始，生命不高于一半时回复 3 点生命，否则获得 Amount 层轮回。</summary>
public class SoulComfortPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner == null) return;
        if (ModActions.IsBelowHalfHp(Owner))
            await ModActions.Heal(Owner, 3);
        else
            await Samsara.Gain(Owner, null, Amount);
    }
}

/// <summary>吾身即薪：回合开始失去 1 点生命，获得 Amount 层轮回。</summary>
public class MyBodyAsFirewoodPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner == null) return;
        await ModActions.LoseHp(Owner, 1);
        await Samsara.Gain(Owner, null, Amount);
    }
}

/// <summary>
/// 生人勿近：每当你打出一张带「灵」的牌，获得 Amount 层轮回。
/// 判定与结算入口在 HuTaoModCard.AfterCardPlayed（灵在那一瞬间才被消耗，能力读不到旧状态）。
/// </summary>
public class StrangerStayAwayPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>阴阳要诀：生命高于一半时给随机敌人挂 Amount 层；不高于一半则获得 Amount 层轮回。</summary>
public class YinYangKeyPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner == null) return;
        if (ModActions.IsBelowHalfHp(Owner))
        {
            await Samsara.Gain(Owner, null, Amount);
        }
        else
        {
            var foe = BlossomSupport.RandomEnemy(Owner);
            if (foe != null) await BlossomSupport.ApplyBlossom(foe, null, Amount);
        }
    }
}

/// <summary>向死而战：生命不高于一半时，攻击每段额外造成 Amount 点伤害（由 BlossomSupport 读取）。</summary>
public class FightUntoDeathPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>幽蝶灯引：回合开始给所有敌人挂 Amount 层血梅香，并获得 1 层轮回。</summary>
public class GhostButterflyLanternPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner == null) return;
        foreach (var enemy in BlossomSupport.GetOpponents(Owner))
            await BlossomSupport.ApplyBlossom(enemy, null, Amount);
        await Samsara.Gain(Owner, null, 1);
    }
}

/// <summary>万夫莫敌：回合结束时，若轮回 ≥3 则抽 DrawCount 张并获得 Amount 层轮回。</summary>
public class UnbeatablePower : HuTaoModPower
{
    /// <summary>每次触发的抽牌数（基础 1，升级后 2）。</summary>
    public int DrawCount = 1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner == null) return;
        if (Samsara.Of(Owner) < 3) return;
        if (Owner.Player != null && DrawCount > 0)
            await CardPileCmd.Draw(choiceContext, DrawCount, Owner.Player);
        await Samsara.Gain(Owner, null, Amount);
    }
}

/// <summary>
/// 香如故：每回合你的第一张带「灵」的牌打出后，灵不消失，并抽 Amount 张牌。
/// 「第一张」由 UsedThisTurn 记录，灵是否保留由 SoulReturn.TryConsumeSpirit 判定。
/// </summary>
public class FragranceEverlastingPower : HuTaoModPower
{
    /// <summary>本回合的「第一张带灵牌保留灵」是否已经用掉。</summary>
    public bool UsedThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player) UsedThisTurn = false;
        return Task.CompletedTask;
    }
}
