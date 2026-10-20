using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using BaseLib.Extensions;
using HuTaoMod.Code.Cards.Support;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using HuTaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;

namespace HuTaoMod.Code.Cards;

// ============ 烧血流 · 罕见（Uncommon）============
// 控血主动循环的中坚：烧血 → 资源（抽牌/能量/格挡）。

/// <summary>焚身以火 —— 3 费：回合开始失去 2 生命（升级 1）、获得 3 层轮回（升级 4）。</summary>
public class BurningFlesh() : HuTaoModCard(3, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BurningFleshPower>(3),
        new HpLossVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<BurningFleshPower>(choiceContext, this, DynamicVars.Power<BurningFleshPower>().IntValue);
        // 生命代价随升级变化，写进能力里让回合开始钩子读到
        var power = Owner.Creature.Powers.OfType<BurningFleshPower>().FirstOrDefault();
        if (power != null) power.HpLossPerTurn = DynamicVars.HpLoss.IntValue;
    }
}

/// <summary>
/// 命途逆旅 —— 1 费：抽 2（升级 3）；每抽到 1 张带「灵」的牌（含已消耗的暗色灵牌）获得 1 点能量。
/// </summary>
public class FateJourney() : HuTaoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 逆旅短暂：本回合不打出就会被消耗（原版「虚无」）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        IEnumerable<CardModel> drawn =
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

        int spirits = drawn == null ? 0 : drawn.Count(SoulReturn.HasSpiritMark);
        if (spirits > 0)
            await ModActions.GainEnergy(spirits, Owner);
    }
}

/// <summary>生死边缘 —— 生命不高于一半时，回合开始获得 3 层轮回（升级 4）。</summary>
public class LifeDeathEdge() : HuTaoModCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<LifeDeathEdgePower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<LifeDeathEdgePower>(choiceContext, this, DynamicVars.Power<LifeDeathEdgePower>().IntValue);
    }
}

/// <summary>宴罢人散 —— 1 费：弃掉所有手牌；每弃 1 张获得 3 点格挡（升级 4）。
/// 「宴席散了，宾客各自离席」→ 手牌全部离手，换一层护身。</summary>
public class FeastEnds() : HuTaoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = Owner?.PlayerCombatState?.Hand.Cards.ToList();
        if (hand == null || hand.Count == 0) return;

        int n = hand.Count;
        await CardCmd.Discard(choiceContext, hand);

        // 每弃 1 张结算一次格挡，数字随升级变化
        for (int i = 0; i < n; i++)
            await CommonActions.CardBlock(this, play);
    }

    // 文档：每张 3 → 4
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

/// <summary>血偿契约 —— 0 费：失去 3 生命（升级 2），获得 2 能量。</summary>
public class BloodContract() : HuTaoModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(3),
        new EnergyVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await ModActions.LoseHp(Owner.Creature, DynamicVars.HpLoss.IntValue);
        await ModActions.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}

/// <summary>薪尽火传 —— 2 费：失去 4 生命（升级 3），抽 3，获得 2 能量。</summary>
public class FirewoodPassesOn() : HuTaoModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(4),
        new EnergyVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await ModActions.LoseHp(Owner.Creature, DynamicVars.HpLoss.IntValue);
        await CardPileCmd.Draw(choiceContext, 3, Owner);
        await ModActions.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}

/// <summary>向死而生 —— 生命不高于一半时，回合开始给所有敌人挂 2 层（升级 3）。</summary>
public class UntoDeath() : HuTaoModCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<UntoDeathPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<UntoDeathPower>(choiceContext, this, DynamicVars.Power<UntoDeathPower>().IntValue);
    }
}

/// <summary>烛泪成行 —— 0 费：4 格挡（升级 7），并把等量格挡保留到下一回合。</summary>
public class CandleTears() : HuTaoModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.Apply<CandleTearsPower>(Owner.Creature, this, DynamicVars.Block.IntValue);
    }
}

/// <summary>殓骨还乡 —— 16 伤（升级 20）；若击杀，本卡还阳回抽牌堆顶（带灵）。</summary>
public class BoneReturnHome() : HuTaoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(16m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var target = play.Target;
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
        // 击杀则本卡还阳回抽牌堆顶（带灵）
        if (target != null && target.IsDead)
            await SoulReturn.ReturnCard(this);
    }

    // 文档：16 → 20
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}

/// <summary>灯枯之际 —— 1 费：失去 3 生命（升级 2），抽 3 张牌。燃尽自己换最后一波手牌。</summary>
public class LampDryingOut() : HuTaoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HpLossVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await ModActions.LoseHp(Owner.Creature, DynamicVars.HpLoss.IntValue);
        await CardPileCmd.Draw(choiceContext, 3, Owner);
    }
}

/// <summary>焰蝶焚野 —— 2 费，消耗：对所有敌人造成 10 伤（升级 13）并各挂 1 层。</summary>
public class FlameButterflyWild() : HuTaoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrikeAll(this, choiceContext, play, markPerHit: 1);
    }
}
