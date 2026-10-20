using System.Linq;
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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Commands;

namespace HuTaoMod.Code.Cards;

// ============ 轮回 · 稀有（Rare）============

/// <summary>狂杀 —— 5 伤（升级 7），重复「目标血梅香层数」次（上限 4）。</summary>
public class WildSlaughter() : HuTaoModCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int hits = 1;
        if (play.Target != null)
            hits = System.Math.Clamp(BlossomSupport.BlossomOn(play.Target), 1, 4);
        await BlossomSupport.BlossomStrike(this, choiceContext, play, hitCount: hits);
    }

    // 文档：5 → 7
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

/// <summary>阴阳要诀 —— 生命高于一半：随机敌人 +2 层（升级 3）；≤50%：+2 层轮回。</summary>
public class YinYangKeyDoctrine() : HuTaoModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<YinYangKeyPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<YinYangKeyPower>(choiceContext, this, DynamicVars.Power<YinYangKeyPower>().IntValue);
    }
}

/// <summary>万夫莫敌 —— 回合结束若轮回 ≥3：抽 1（升级 2）并获得 2 层轮回（升级 3）。</summary>
public class UnbeatableTenThousand() : HuTaoModCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<UnbeatablePower>(2),
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<UnbeatablePower>(choiceContext, this, DynamicVars.Power<UnbeatablePower>().IntValue);
        var power = Owner.Creature.Powers.OfType<UnbeatablePower>().FirstOrDefault();
        if (power != null) power.DrawCount = DynamicVars.Cards.IntValue;
    }
}

/// <summary>葬仪对策 —— 1 费：造成 8 伤害（升级 11），并施加 2 层易伤（升级 3）。
/// 「对策」= 看穿亡者的破绽：易伤放大后续所有伤害；目标身上有血梅香再抽 1。</summary>
public class FuneralCountermeasure() : HuTaoModCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, DamageProps.card),
        new PowerVar<VulnerablePower>(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (play.Target == null) return;

        await CommonActions.Apply<VulnerablePower>(play.Target, this, DynamicVars.Power<VulnerablePower>().IntValue);
        if (BlossomSupport.BlossomOn(play.Target) > 0)
            await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}

/// <summary>往生诀 —— 10 伤（升级 13）；击杀则还阳 4、抽 2、+1 能量。</summary>
public class RebirthArt() : HuTaoModCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var target = play.Target;
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (target != null && target.IsDead)
        {
            await SoulReturn.ReturnFromDiscard(Owner, 4);
            await CardPileCmd.Draw(choiceContext, 2, Owner);
            await ModActions.GainEnergy(1, Owner);
        }
    }
}
