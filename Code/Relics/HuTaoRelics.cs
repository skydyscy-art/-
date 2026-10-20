using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using HuTaoMod.Code.Cards.Support;
using HuTaoMod.Code.Powers;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HuTaoMod.Code.Relics;

/// <summary>
/// 引魂蝶灯 —— 初始遗物。蝶灯引路，亡魂安行。
/// 敌人死亡时，按它身上剩余的血梅香层数回复等量生命（最后一击的余香化作蝶，回到堂主身上）。
///
/// 顺带承担「本回合是否失去生命」的追踪（夜行的判定），因为遗物是唯一在整场战斗里
/// 一定会存在的模型，能稳定收到回合开始 / 生命值变化的钩子。
/// </summary>
public class SoulButterflyLamp : HuTaoModRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented) return;          // 被免死效果挡下，不算击杀
        if (creature == Owner.Creature) return;   // 自己死了不触发
        if (creature.IsPlayer) return;            // 只对敌人结算

        int stacks = BlossomSupport.BlossomOn(creature);
        if (stacks > 0)
            await ModActions.Heal(Owner.Creature, stacks);
    }

    /// <summary>累计本回合的掉血总量（含自己烧血，烧血部分由 ModActions.LoseHp 单独记）。</summary>
    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner.Creature && delta < 0m)
            CombatTracker.HpLostThisTurn += -delta;
        return Task.CompletedTask;
    }

    /// <summary>玩家回合开始时重置追踪。</summary>
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == CombatSide.Player)
            CombatTracker.ResetTurn();
        return Task.CompletedTask;
    }
}

/// <summary>
/// 血梅香炉 —— 罕见遗物。炉中梅香，见者沾衣。
/// 战斗开始时，对所有敌人施加 2 层血梅香。
/// </summary>
public class PlumBrazier : HuTaoModRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeCombatStart()
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        foreach (var enemy in combatState.Enemies)
            await CommonActions.Apply<BloodBlossomPower>(enemy, null, 2);
    }
}

/// <summary>
/// 镇魂香烛 —— 稀有遗物。香火燃寿，换堂主之神力。
/// 战斗开始时，失去 3 点生命，获得 2 点力量。
/// </summary>
public class RequiemCandle : HuTaoModRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task BeforeCombatStart()
    {
        await ModActions.LoseHp(Owner.Creature, 3);
        await CommonActions.Apply<StrengthPower>(Owner.Creature, null, 2);
    }
}
