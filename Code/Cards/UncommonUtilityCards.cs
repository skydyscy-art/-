using BaseLib.Utils;
using HuTaoMod.Code.Cards.Support;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;
using HuTaoMod.Code.Powers;
using BaseLib.Extensions;

namespace HuTaoMod.Code.Cards;

// ============ 通用 · 罕见（Uncommon）============
// 防御反击流的护甲中坚 + 通用过牌。

/// <summary>
/// 蝶群护卫 —— 14 格挡（升级 18）；本回合每还阳 1 张牌，额外获得 3 点格挡。
/// </summary>
public class ButterflyGuard() : HuTaoModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(14m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.Apply<HuanYangGuardPower>(Owner.Creature, this, 3);
    }

    // 文档：14 → 18
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}

/// <summary>梅影重重 —— 7 格挡（升级 10）+ 抽 2。</summary>
/// <summary>梅影重重 —— 1 费：获得 8 点格挡（升级 11）；抽 1 张牌。对标原版「势不可挡」式的防御过牌。</summary>
public class PlumShadows() : HuTaoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}

/// <summary>晨露 —— 0 费：抽 2（升级 3）。</summary>
/// <summary>晨露 —— 0 费：抽 1 张牌；获得 3 点格挡（升级 6）。对标原版「灵巧」的 0 费格式。</summary>
public class MorningDew() : HuTaoModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new BlockVar(3m, BlockProps.card)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await CommonActions.CardBlock(this, play);
    }

    // 只升格挡 3 → 6，抽牌固定 1
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

/// <summary>鬼门开 —— 对所有敌人施加 2 层（升级 3）。</summary>
/// <summary>鬼门开 —— 冥门大开：全场 1 层血梅香，并施加 1 层虚弱（升级 2 层虚弱）。</summary>
public class GhostGateOpen() : HuTaoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int weak = DynamicVars.Power<WeakPower>().IntValue;
        foreach (var enemy in CombatState!.Enemies)
        {
            await CommonActions.Apply<BloodBlossomPower>(enemy, this, 1);
            await CommonActions.Apply<WeakPower>(enemy, this, weak);
        }
    }
}

/// <summary>棺材铺日课 —— 1 费：获得 3 层轮回（升级 4）。[gold]保留[/gold]：日课天天都要做。</summary>
public class CoffinShopRoutine() : HuTaoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.Samsara, CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SamsaraPower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Apply<SamsaraPower>(Owner.Creature, this, DynamicVars.Power<SamsaraPower>().IntValue);
    }
}

/// <summary>往生堂名录 —— 1 费：还阳 2 张（升级 3）。[gold]保留[/gold]：名册随身带着。</summary>
public class ParlorDirectory() : HuTaoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang, CardKeyword.Retain];

    // 这个变量只用来在卡面显示还阳张数（升级 +1）
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("HuanYangCount", 2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await SoulReturn.ReturnFromDiscard(Owner, IsUpgraded ? 3 : 2);
    }
}
