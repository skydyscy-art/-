using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HuTaoMod.Code.Powers;

/// <summary>
/// 轮回 —— 胡桃的防御留存资源（无上限）。
///
/// 获得：还阳 1 张 → +1 层；部分卡牌直接给 X 层。
/// 收益：
///   A. 守 —— 玩家回合开始时，获得 2×层数 的格挡（相当于把这些格挡"留到"了下一回合）
///   B. 攻 —— 部分卡牌「消耗 X 层轮回」，每层造成 3 伤害
/// 衰减：回合开始时层数减半（向下取整），即无硬上限但有软顶。
///
/// 注：之所以放在"回合开始"而不是"回合结束"结算，是为了避开原版格挡清空时机的不确定性，
/// 效果等价（保留 2X 点格挡），实现更稳。
/// </summary>
public class SamsaraPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player) return;

        HuTaoMod.Code.Utils.SoulReturn.ResetTurnCounter();

        if (Owner == null || Amount <= 0) return;

        // A. 留存：把上一回合攒下的轮回兑现成格挡
        await CreatureCmd.GainBlock(Owner, 2 * Amount, ValueProp.Unpowered, null);

        // 衰减：层数减半
        int next = Amount / 2;
        if (next < Amount)
            await CommonActions.Apply<SamsaraPower>(Owner, null, next - Amount);
    }
}
