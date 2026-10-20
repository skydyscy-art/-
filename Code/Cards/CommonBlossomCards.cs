using System.Collections.Generic;
using BaseLib.Extensions;
using BaseLib.Utils;
using HuTaoMod.Code.Cards.Support;
using HuTaoMod.Code.Powers;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;

namespace HuTaoMod.Code.Cards;

// ============ 血梅香流 · 普通（Common）============
// v11.2：铺层 → 6 层落梅（攻击后抽 1）→ 引爆

/// <summary>血梅香径 —— 0 费，对所有敌人铺 2 层（升级 3 层）。</summary>
public class BloodBlossomRite() : HuTaoModCard(0, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BloodBlossomPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        foreach (var enemy in CombatState!.Enemies)
            await CommonActions.Apply<BloodBlossomPower>(enemy, this, DynamicVars.Power<BloodBlossomPower>().IntValue);
    }
}

/// <summary>花见蝶舞 —— 8 伤；目标 ≥4 层时额外获得 2 层轮回。</summary>
public class FlowerButterflyDance() : HuTaoModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (play.Target != null && BlossomSupport.BlossomOn(play.Target) >= 4)
            await CommonActions.Apply<SamsaraPower>(Owner.Creature, this, 2);
    }
}

/// <summary>暗香浮动 —— 铺层 3 + 抽 1 + 还阳 1（升级：+4 层）。</summary>
public class FloatingScent() : HuTaoModCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BloodBlossomPower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target != null)
            await CommonActions.Apply<BloodBlossomPower>(play.Target, this, DynamicVars.Power<BloodBlossomPower>().IntValue);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
        await SoulReturn.ReturnFromDiscard(Owner, 1);
    }
}

/// <summary>梅影摇红 —— 两段 4 伤（升级 5），每段挂 1 层；还阳 1。</summary>
public class PlumShadowRed() : HuTaoModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play, hitCount: 2, markPerHit: 1);
        await SoulReturn.ReturnFromDiscard(Owner, 1);
    }

    // 文档：4 → 5（每段），不是通用的 +3
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

/// <summary>蝶群戏梅 —— 全场铺 2 层 + 1 层轮回。</summary>
public class ButterfliesPlayPlum() : HuTaoModCard(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BloodBlossomPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        foreach (var enemy in CombatState!.Enemies)
            await CommonActions.Apply<BloodBlossomPower>(enemy, this, DynamicVars.Power<BloodBlossomPower>().IntValue);
        await CommonActions.Apply<SamsaraPower>(Owner.Creature, this, 1);
    }
}

/// <summary>午夜幽蝶 —— 0 费 6 伤（升级 8）；若目标有血梅香则还阳 1。</summary>
public class MidnightGhostButterfly() : HuTaoModCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        bool hadBlossom = play.Target != null && BlossomSupport.BlossomOn(play.Target) > 0;
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (hadBlossom)
            await SoulReturn.ReturnFromDiscard(Owner, 1);
    }

    // 文档：6 → 8
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

/// <summary>香满襟 —— 1 费：3 层铺香（升级 4）+ 3 格挡（升级 6）。不抽牌，专注铺香兼护身。</summary>
public class ScentFullBosom() : HuTaoModCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BloodBlossomPower>(3),
        new BlockVar(3m, BlockProps.card)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target != null)
            await CommonActions.Apply<BloodBlossomPower>(play.Target, this, DynamicVars.Power<BloodBlossomPower>().IntValue);
        await CommonActions.CardBlock(this, play);
    }

    // 层数 3 → 4（+1）、格挡 3 → 6（+3）
    protected override void OnUpgrade()
    {
        DynamicVars.Power<BloodBlossomPower>().UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
