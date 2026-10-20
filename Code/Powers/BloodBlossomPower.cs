using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace HuTaoMod.Code.Powers;

/// <summary>
/// 血梅香 —— 胡桃的招牌减益。
///
/// 机制：
///  - 目标每受到一次攻击伤害，每层额外受到 1 点伤害（多段攻击每 hit 分别结算，由 BlossomSupport 实现）。
///  - 每过一个回合，层数 -2（不再永久累积，需要持续铺层维持输出）。
///
/// 加伤结算不在这里，本类只负责计数与每回合衰减。
/// </summary>
public class BloodBlossomPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>每个回合（玩家回合开始时）层数 -2（v11.2 平衡：从 -1 改为 -2）。</summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player) return;
        if (Amount <= 0) return;
        await CommonActions.Apply<BloodBlossomPower>(Owner, null, -2);
    }
}
