using System.Collections.Generic;
using BaseLib.Extensions;
using BaseLib.Utils;
using HuTaoMod.Code.Cards.Support;
using HuTaoMod.Code.Powers;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HuTaoMod.Code.Cards;

// ============ 攻击 / 能量 · 普通（Common）============

/// <summary>以血为引 —— 0 费：失去 2 生命，造成 8 伤（升级 11）。</summary>
public class BloodAsGuide() : HuTaoModCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await ModActions.LoseHp(Owner.Creature, 2);
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
    }
}

/// <summary>蝶舞 —— 全场 3 伤 ×2（升级 4 ×2）+ 还阳 1。</summary>
public class ButterflyWaltz() : HuTaoModCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrikeAll(this, choiceContext, play, hitCount: 2);
        await SoulReturn.ReturnFromDiscard(Owner, 1);
    }

    // 文档：3 → 4
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

/// <summary>幽灵来访 —— 7 伤（升级 10）+ 抽 1。</summary>
public class GhostlyVisit() : HuTaoModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}

/// <summary>夜行 —— 6 伤（升级 9）；若你本回合未失去生命，还阳 1。</summary>
public class NightProwl() : HuTaoModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        bool unhurt = !CombatTracker.LostHpToEnemiesThisTurn;
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (unhurt)
            await SoulReturn.ReturnFromDiscard(Owner, 1);
    }
}

/// <summary>烛火摇曳 —— 0 费：抽 2（升级 3），弃 1。</summary>
/// <summary>烛火摇曳 —— 0 费：抽 2 张牌，然后弃 2 张牌（升级 抽 3 弃 2）。换手，不增手牌。</summary>
public class FlickeringCandle() : HuTaoModCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await ModActions.DiscardMany(this, choiceContext, 2);
    }
}

/// <summary>献祭薄礼 —— 失去 2 生命（升级 1）+ 3 层血梅香 + 1 能量。</summary>
public class OfferingTribute() : HuTaoModCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BloodBlossomPower>(3),
        new HpLossVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await ModActions.LoseHp(Owner.Creature, DynamicVars.HpLoss.IntValue);
        if (play.Target != null)
            await CommonActions.Apply<BloodBlossomPower>(play.Target, this, DynamicVars.Power<BloodBlossomPower>().IntValue);
        await ModActions.GainEnergy(1, Owner);
    }
}

/// <summary>
/// 彼岸蝶护 —— 6 格挡（升级 9）；本回合每还阳 1 张牌，额外获得 2 点格挡。
/// </summary>
public class ParamitaGuard() : HuTaoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.Apply<HuanYangGuardPower>(Owner.Creature, this, 2);
    }
}

/// <summary>血梅前调 —— 1 费：2 层铺香（升级 3）；抽 1 张牌。开场铺香兼过牌。</summary>
public class PlumPrelude() : HuTaoModCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BloodBlossomPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target != null)
            await CommonActions.Apply<BloodBlossomPower>(play.Target, this, DynamicVars.Power<BloodBlossomPower>().IntValue);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}
