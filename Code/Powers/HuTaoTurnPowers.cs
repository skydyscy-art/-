using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace HuTaoMod.Code.Powers;

// ============ 「本回合 / 下回合」类临时能力 ============
// 这些能力不需要常驻：用完立刻清零（Apply 负数），玩家回合结束自动失效。
// 之所以用能力而不是静态计数，是因为能力的回合钩子（AfterSideTurnStart / AfterSideTurnEnd）
// 是唯一可靠的「回合边界」回调。

/// <summary>
/// 彼岸蝶护 / 蝶群护卫：本回合每还阳 1 张牌，额外获得等同层数的格挡。
/// 结算入口在 SoulReturn.ReturnFromDiscard。
/// </summary>
public class HuanYangGuardPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner == null) return;
        if (Amount > 0) await CommonActions.Apply<HuanYangGuardPower>(Owner, null, -Amount);
    }
}

/// <summary>
/// 堂规森严：本回合每打出 1 张攻击牌，额外获得等同层数的格挡。
/// 结算入口在 BlossomSupport（胡桃所有攻击牌都经过这里）。
/// </summary>
public class StrictPlumRulePower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner == null) return;
        if (Amount > 0) await CommonActions.Apply<StrictPlumRulePower>(Owner, null, -Amount);
    }
}

/// <summary>
/// 烛泪成行：下个回合开始时，额外获得等同层数的格挡（＝把这一份格挡保留到下一回合）。
/// 格挡在「回合开始清空」之后结算，所以不会被立刻抹掉。
/// </summary>
public class CandleTearsPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner == null) return;
        if (Amount <= 0) return;
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        await CommonActions.Apply<CandleTearsPower>(Owner, null, -Amount);
    }
}

/// <summary>
/// 七星棺椁：本回合结束时，格挡不会被清空（把这一份格挡完整保留到下一回合）。
/// 生效发生在下个回合开始的「清空格挡」步骤，随后自己清零。
/// </summary>
public class BlockRetentionPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldClearBlock(Creature creature)
    {
        if (Owner == null) return true;
        if (creature != Owner) return true;
        return Amount <= 0;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner == null) return;
        if (Amount > 0) await CommonActions.Apply<BlockRetentionPower>(Owner, null, -Amount);
    }
}
