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

// ============ 能力 / 防御 · 普通（Common）============

/// <summary>往生堂训 —— 每回合开始给随机敌人挂 1 层（升级 2 层）。</summary>
public class FuneralDoctrine() : HuTaoModCard(1, CardType.Power, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ButterflyWispPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<ButterflyWispPower>(this, DynamicVars.Power<ButterflyWispPower>().IntValue);
    }
}

/// <summary>
/// 堂规森严 —— 5 格挡（升级 8）；本回合每打出 1 张攻击牌，额外获得 2 点格挡。
/// </summary>
public class StrictPlumRule() : HuTaoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.Apply<StrictPlumRulePower>(Owner.Creature, this, 2);
    }
}
