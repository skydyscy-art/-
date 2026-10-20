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

// ============ 烧血流 · 稀有（Rare）============
// 控血循环的收口与顶点：大额烧血爆发、低血引擎、回血续航。

/// <summary>安神秘法 —— 回复 10 生命（升级 14）；生命高于一半时改为获得等量格挡。</summary>
public class SpiritSoother() : HuTaoModCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(10)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int value = DynamicVars.Heal.IntValue;
        if (ModActions.IsBelowHalfHp(Owner.Creature))
            await ModActions.Heal(Owner.Creature, value);
        else
            await ModActions.GainBlock(Owner.Creature, value);
    }

    // 文档：10 → 14
    protected override void OnUpgrade() => DynamicVars.Heal.UpgradeValueBy(4m);
}

/// <summary>吾身即薪 —— 回合开始失去 1 生命，获得 2 层轮回（升级 3）。</summary>
public class MyBodyAsFirewood() : HuTaoModCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MyBodyAsFirewoodPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<MyBodyAsFirewoodPower>(choiceContext, this, DynamicVars.Power<MyBodyAsFirewoodPower>().IntValue);
    }
}

/// <summary>血宴终章 —— 失去 4 生命；18 伤（升级 22）；击杀则还阳 3。</summary>
public class BloodFeastFinale() : HuTaoModCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(18m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await ModActions.LoseHp(Owner.Creature, 4);
        var target = play.Target;
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (target != null && target.IsDead)
            await SoulReturn.ReturnFromDiscard(Owner, 3);
    }

    // 文档：18 → 22
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}

/// <summary>向死而战 —— 生命不高于一半时，你的攻击每段额外 4 伤（升级 5）。</summary>
public class FightUntoDeath() : HuTaoModCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FightUntoDeathPower>(4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<FightUntoDeathPower>(choiceContext, this, DynamicVars.Power<FightUntoDeathPower>().IntValue);
    }
}

/// <summary>蝶火燎原 —— 2 费，消耗：失去 3 生命，全场 14 伤（升级 17）并各挂 2 层。</summary>
public class ButterflyFirePrairie() : HuTaoModCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(14m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await ModActions.LoseHp(Owner.Creature, 3);
        await BlossomSupport.BlossomStrikeAll(this, choiceContext, play, markPerHit: 2);
    }
}

/// <summary>生人勿近 —— 每打出 1 张带灵牌，获得 1 层轮回（升级 2）。</summary>
public class StrangerStayAway() : HuTaoModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrangerStayAwayPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<StrangerStayAwayPower>(choiceContext, this, DynamicVars.Power<StrangerStayAwayPower>().IntValue);
    }
}

/// <summary>天命有归 —— 回复 5 生命（升级 8）；每个带香敌人额外 +2。</summary>
public class DestinyFulfilled() : HuTaoModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(5)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int heal = DynamicVars.Heal.IntValue;
        int bonus = BlossomSupport.BlossomEnemyCount(Owner.Creature) * 2;
        await ModActions.Heal(Owner.Creature, heal + bonus);
    }
}
