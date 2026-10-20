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

// ============ 轮回 / 防御 · 罕见（Uncommon）============

/// <summary>镇魂安神 —— 回合开始：≤50% 回复 3 生命，否则获得 2 层轮回（升级 5）。</summary>
public class SoulComfort() : HuTaoModCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SoulComfortPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<SoulComfortPower>(choiceContext, this, DynamicVars.Power<SoulComfortPower>().IntValue);
    }

    // 文档：2 → 5
    protected override void OnUpgrade() => DynamicVars.Power<SoulComfortPower>().UpgradeValueBy(3m);
}

/// <summary>威仪天下 —— 回合结束若轮回 ≥3，抽 1（升级 2）。</summary>
public class MajesticRule() : HuTaoModCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MajesticRulePower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<MajesticRulePower>(choiceContext, this, DynamicVars.Power<MajesticRulePower>().IntValue);
    }
}

/// <summary>铁壁梅心 —— 回合开始获得 4 格挡（升级 6）。</summary>
public class IronPlumHeart() : HuTaoModCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<IronPlumHeartPower>(4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<IronPlumHeartPower>(choiceContext, this, DynamicVars.Power<IronPlumHeartPower>().IntValue);
    }

    // 文档：4 → 6
    protected override void OnUpgrade() => DynamicVars.Power<IronPlumHeartPower>().UpgradeValueBy(2m);
}

/// <summary>淬刃梅香 —— 攻击带香目标每段额外 3 伤（升级 4）。</summary>
public class RazorBlossom() : HuTaoModCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<RazorBlossomPower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<RazorBlossomPower>(choiceContext, this, DynamicVars.Power<RazorBlossomPower>().IntValue);
    }
}

/// <summary>堂主亲征 —— 14 伤（升级 18）；生命不高于一半时改为 20（升级 24）。</summary>
public class DirectorMarch() : HuTaoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(14m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        bool low = ModActions.IsBelowHalfHp(Owner.Creature);
        int bonus = low ? 6 : 0;
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (bonus > 0 && play.Target != null)
            await BlossomSupport.FlatDamage(this, choiceContext, play.Target, bonus);
    }

    // 文档：14 → 18（低血时 20 → 24）
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}

/// <summary>力开金石 —— 6 伤 ×2（升级 8 ×2）。</summary>
public class StoneSplittingForce() : HuTaoModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play, hitCount: 2);
    }

    // 文档：6 → 8
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
