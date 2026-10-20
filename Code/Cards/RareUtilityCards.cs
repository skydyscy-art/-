using BaseLib.Utils;
using BaseLib.Extensions;
using HuTaoMod.Code.Cards.Support;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using HuTaoMod.Code.Powers;

namespace HuTaoMod.Code.Cards;

// ============ 通用 · 稀有（Rare）============
// 高资源循环工具：爆发过牌、大额护甲、能量引擎。

/// <summary>大红大紫 —— X 费：对所有敌人造成 5 伤害 X 次（升级 6）。满堂红，炸给全场看。</summary>
public class BigRedBigPurple() : HuTaoModCard(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0) return;

        // 和原版旋风斩同形：固定伤害 × X 次，攻击全场
        await BlossomSupport.BlossomStrikeAll(this, choiceContext, play, hitCount: x);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

/// <summary>七星棺椁 —— X 费：4X 格挡（升级 5X）；X≥3 时本回合结束时格挡不会被清空。</summary>
public class SevenStarCoffin() : HuTaoModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0) return;
        await ModActions.GainBlock(Owner.Creature, DynamicVars.Block.IntValue * x);
        if (x >= 3)
            await CommonActions.Apply<BlockRetentionPower>(Owner.Creature, this, 1);
    }

    // 文档：4X → 5X
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

/// <summary>阴阳两界 —— 失去 3 生命（升级 2）；4 层 + 1 能量（升级 2）。</summary>
public class YinYangTwoRealms() : HuTaoModCard(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BloodBlossomPower>(4),
        new HpLossVar(3),
        new EnergyVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await ModActions.LoseHp(Owner.Creature, DynamicVars.HpLoss.IntValue);
        if (play.Target != null)
            await CommonActions.Apply<BloodBlossomPower>(play.Target, this, DynamicVars.Power<BloodBlossomPower>().IntValue);
        await ModActions.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    // 文档：失去 3 → 2，能量 1 → 2（层数不变）
    protected override void OnUpgrade()
    {
        DynamicVars.HpLoss.UpgradeValueBy(-1m);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
