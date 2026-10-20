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

// ============ 血梅香流 · 罕见（Uncommon）============

/// <summary>散魂蝶 —— 10 伤（升级 13），消耗目标全部层数，每层再造成 2 伤。</summary>
public class SpiritScatter() : HuTaoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (play.Target == null) return;
        int stacks = BlossomSupport.BlossomOn(play.Target);
        if (stacks <= 0) return;
        int consumed = await BlossomSupport.ConsumeBlossom(play.Target, this, stacks);
        if (consumed > 0) await BlossomSupport.FlatDamage(this, choiceContext, play.Target, consumed * 2);
    }
}

/// <summary>赤团开时 —— 9 伤（升级 12）；目标 ≥6 层时额外获得 2 层轮回。</summary>
public class CrimsonBloom() : HuTaoModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (play.Target != null && BlossomSupport.BlossomOn(play.Target) >= 6)
            await CommonActions.Apply<SamsaraPower>(Owner.Creature, this, 2);
    }
}

/// <summary>凝香成焰 —— 5 伤（升级 7）；消耗目标 2 层，每层 +3 伤。</summary>
public class CondensedFlameBlossom() : HuTaoModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (play.Target == null) return;
        int burned = await BlossomSupport.ConsumeBlossom(play.Target, this, 2);
        if (burned > 0) await BlossomSupport.FlatDamage(this, choiceContext, play.Target, burned * 3);
    }

    // 文档：5 → 7
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

/// <summary>赤团争春 —— 12 伤（升级 15）；目标每 2 层 +1 伤。</summary>
public class CrimsonPlumSpring() : HuTaoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (play.Target == null) return;
        int bonus = BlossomSupport.BlossomOn(play.Target) / 2;
        if (bonus > 0) await BlossomSupport.FlatDamage(this, choiceContext, play.Target, bonus);
    }
}

/// <summary>千蝶振翅 —— 全场 2 伤 ×3（升级 3 ×3）+ 还阳 2。</summary>
public class ThousandButterflyFlap() : HuTaoModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrikeAll(this, choiceContext, play, hitCount: 3);
        await SoulReturn.ReturnFromDiscard(Owner, 2);
    }

    // 文档：2 → 3
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

/// <summary>梅香入骨 —— 0 费：2 层（升级 3）；目标 ≥6 层时还阳 1。</summary>
public class PlumScentInBone() : HuTaoModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BloodBlossomPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target == null) return;
        await CommonActions.Apply<BloodBlossomPower>(play.Target, this, DynamicVars.Power<BloodBlossomPower>().IntValue);
        if (BlossomSupport.BlossomOn(play.Target) >= 6)
            await SoulReturn.ReturnFromDiscard(Owner, 1);
    }
}

/// <summary>蝶引千夜 —— 全场 3 层（升级 4）+ 2 层轮回。</summary>
public class ButterflyThousandNights() : HuTaoModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BloodBlossomPower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        foreach (var enemy in CombatState!.Enemies)
            await CommonActions.Apply<BloodBlossomPower>(enemy, this, DynamicVars.Power<BloodBlossomPower>().IntValue);
        await CommonActions.Apply<SamsaraPower>(Owner.Creature, this, 2);
    }
}

/// <summary>幽蝶行灵 —— 回合开始自动给随机敌人铺 2 层（升级 3）。</summary>
public class GhostButterflySpirit() : HuTaoModCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ButterflyWispPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<ButterflyWispPower>(choiceContext, this, DynamicVars.Power<ButterflyWispPower>().IntValue);
    }
}

/// <summary>彼岸归途 —— 所有攻击每段额外挂 1 层（升级 2）。</summary>
public class ParamitaReturn() : HuTaoModCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ParamitaReturnPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<ParamitaReturnPower>(choiceContext, this, DynamicVars.Power<ParamitaReturnPower>().IntValue);
    }
}

/// <summary>花开彼岸 —— 3 层（升级 4）+ 抽 1。</summary>
public class FlowerOpensParamita() : HuTaoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        foreach (var enemy in CombatState!.Enemies)
            await CommonActions.Apply<BloodBlossomPower>(enemy, this, 1);
    }
}

/// <summary>殓送之仪 —— 移除目标 2 层，每层 3 伤（升级 4）；清空则还阳 2。</summary>
public class FuneralRite() : HuTaoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target == null) return;
        int consumed = await BlossomSupport.ConsumeBlossom(play.Target, this, 2);
        if (consumed > 0)
            await BlossomSupport.FlatDamage(this, choiceContext, play.Target, consumed * DynamicVars.Damage.IntValue);
        // 清空目标身上的香 → 额外还阳 2 张
        if (BlossomSupport.BlossomOn(play.Target) <= 0)
            await SoulReturn.ReturnFromDiscard(Owner, 2);
    }

    // 文档：每层 3 → 4
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

/// <summary>蝶戏残梅 —— 0 费 5 伤（升级 7）；目标 ≥4 层时还阳 1。</summary>
public class ButterflyTeasesWitheredPlum() : HuTaoModCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        if (play.Target != null && BlossomSupport.BlossomOn(play.Target) >= 4)
            await SoulReturn.ReturnFromDiscard(Owner, 1);
    }

    // 文档：5 → 7
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

/// <summary>梅香疾冲 —— 全场 5 伤（升级 7），抽 1，弃 1。</summary>
public class PlumScentDash() : HuTaoModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrikeAll(this, choiceContext, play);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
        await ModActions.DiscardOne(this, choiceContext);
    }

    // 文档：5 → 7
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
