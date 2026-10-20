using System.Collections.Generic;
using BaseLib.Extensions;
using BaseLib.Utils;
using HuTaoMod.Code.Cards.Support;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using HuTaoMod.Code.Powers;

namespace HuTaoMod.Code.Cards;

// ============ 通用 · 普通（Common）============

/// <summary>开张大吉 —— 0 费 5 格挡（升级 8）。</summary>
public class GrandOpening() : HuTaoModCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}

/// <summary>蝶眠 —— 1 费：获得 8 点格挡（升级 12），然后结束你的回合。眠下去，把回合交出去换护身。</summary>
public class ButterflySlumber() : HuTaoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);

        // 和原版 VoidForm 一样：先结算完自身效果，再把回合交出去
        if (Owner != null)
            PlayerCmd.EndTurn(Owner, false, null);
    }

    // 文档：8 → 12
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}

/// <summary>安魂曲 —— 回复 4 生命（升级 7）；生命不高于一半时还阳 1。</summary>
public class SoulSettling() : HuTaoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [HuTaoKeywords.HuanYang];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await ModActions.Heal(Owner.Creature, DynamicVars.Heal.IntValue);
        if (ModActions.IsBelowHalfHp(Owner.Creature))
            await SoulReturn.ReturnFromDiscard(Owner, 1);
    }
}
