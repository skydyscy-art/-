using BaseLib.Utils;
using BaseLib.Extensions;
using HuTaoMod.Code.Cards.Support;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using HuTaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Commands;

namespace HuTaoMod.Code.Cards;

// ============ 血梅香流 · 稀有（Rare）============
// 铺层 → 引爆 → 复利 的完整闭环：引爆核心、全场引爆、层数翻倍、自动铺层光环。

/// <summary>雪霁梅香 —— 全场 2 层（升级 3）+ 抽 2。</summary>
public class PlumsAfterSnow() : HuTaoModCard(1, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BloodBlossomPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        foreach (var enemy in CombatState!.Enemies)
            await CommonActions.Apply<BloodBlossomPower>(enemy, this, DynamicVars.Power<BloodBlossomPower>().IntValue);
        await CardPileCmd.Draw(choiceContext, 2, Owner);
    }
}

/// <summary>
/// 赤团华绽 —— 基础引爆卡：消耗目标全部层数，每层 4 伤（升级 6）。
/// </summary>
public class CrimsonBurstGrandBloom() : HuTaoModCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.Detonate(this, choiceContext, play.Target, DynamicVars.Damage.IntValue);
    }

    // 文档：每层 4 → 6
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

/// <summary>千朵赤团 —— X 费，消耗：引爆所有敌人，每层 2 伤（升级 3）；每引爆 1 个敌人获得 X 层轮回。</summary>
public class ThousandCrimsonBlossoms() : HuTaoModCard(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara, CardKeyword.Exhaust];

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        int perStack = DynamicVars.Damage.IntValue;
        int detonated = 0;
        foreach (var enemy in BlossomSupport.GetOpponents(Owner.Creature).ToList())
        {
            if (BlossomSupport.BlossomOn(enemy) <= 0) continue;
            await BlossomSupport.Detonate(this, choiceContext, enemy, perStack);
            detonated++;
        }
        if (detonated > 0 && x > 0)
            await CommonActions.Apply<SamsaraPower>(Owner.Creature, this, x * detonated);
    }

    // 文档：每层 2 → 3
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

/// <summary>梅雪聚散 —— X 费：目标 +X 层（升级 X+1）；目标 ≥6 层时还阳 X。</summary>
public class PlumSnowGatherScatter() : HuTaoModCard(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BloodBlossomPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target == null) return;
        int x = ResolveEnergyXValue();
        if (x <= 0) return;
        int amount = x + (IsUpgraded ? 1 : 0);
        await CommonActions.Apply<BloodBlossomPower>(play.Target, this, amount);
        if (BlossomSupport.BlossomOn(play.Target) >= 6)
            await SoulReturn.ReturnFromDiscard(Owner, x);
    }

    // 升级只影响层数（+X+1），变量本身不参与升级，所以整体覆盖
    protected override void OnUpgrade()
    {
    }
}

/// <summary>幽蝶灯引 —— 每回合开始对所有敌人 2 层（升级 3）+ 1 层轮回。</summary>
public class GhostButterflyLantern() : HuTaoModCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<GhostButterflyLanternPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<GhostButterflyLanternPower>(choiceContext, this, DynamicVars.Power<GhostButterflyLanternPower>().IntValue);
    }
}

/// <summary>香如故 —— 每回合第一张带灵牌打出后灵不消失；并抽 1 张（升级 2）。</summary>
public class FragranceEverlasting() : HuTaoModCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FragranceEverlastingPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<FragranceEverlastingPower>(choiceContext, this, DynamicVars.Power<FragranceEverlastingPower>().IntValue);
    }
}

/// <summary>蝶引魂归 —— 8 伤（升级 11）+ 还阳 3。</summary>
public class ButterflyGuideSoulHome() : HuTaoModCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        await SoulReturn.ReturnFromDiscard(Owner, 3);
    }
}

/// <summary>焚香祭礼 —— X 费，消耗：失去 X 生命（升级 X-1），全场 X 层，获得 X 层轮回。</summary>
public class IncenseSacrifice() : HuTaoModCard(0, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara, CardKeyword.Exhaust];

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BloodBlossomPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0) return;
        int loss = System.Math.Max(0, x - (IsUpgraded ? 1 : 0));
        if (loss > 0) await ModActions.LoseHp(Owner.Creature, loss);
        foreach (var enemy in CombatState!.Enemies)
            await CommonActions.Apply<BloodBlossomPower>(enemy, this, x);
        await CommonActions.Apply<SamsaraPower>(Owner.Creature, this, x);
    }

    // 升级只改 X 的生命代价，层数/轮回不变
    protected override void OnUpgrade()
    {
    }
}

/// <summary>落梅惊鸿 —— 6 伤 ×2（升级 9 ×2）；目标 ≥4 层时改为 ×3。</summary>
public class StartledPlumSwan() : HuTaoModCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int hits = play.Target != null && BlossomSupport.BlossomOn(play.Target) >= 4 ? 3 : 2;
        await BlossomSupport.BlossomStrike(this, choiceContext, play, hitCount: hits);
    }
}
