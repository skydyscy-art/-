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

// ============ 基础（Basic）============

/// <summary>
/// 蝶刺 —— 基础攻击（v11：纯伤害 6，升级 9）。
/// 血梅香的加伤由 BlossomSupport 统一结算，本卡自身不额外挂层。
/// </summary>
public class HuTaoStrike() : HuTaoModCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    // 打上「打击」标签：原版「涅奥的护符」等遗物是靠 CardTag.Strike / Defend + Basic 稀有度
    // 来挑基础牌升级的；不加标签的话，这类遗物对胡桃完全无效。
    public override IEnumerable<CardTag> Tags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, DamageProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await BlossomSupport.BlossomStrike(this, choiceContext, play);
    }
}

/// <summary>梅盾 —— 基础防御（5 格挡，升级 8）。</summary>
public class HuTaoDefend() : HuTaoModCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override IEnumerable<CardTag> Tags => [CardTag.Defend];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, BlockProps.card)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}

/// <summary>
/// 蝶引来生 —— 胡桃的灵魂卡，初始卡组自带一张。
/// v11：失去 2 生命 + 3 层血梅香（升级：失去 1 生命 / 4 层），不再抽牌。
/// v11.5：结算顺序改为「失去生命 → 铺层 → 最后造成 3 点伤害」，
/// 伤害放在最后，卡面文字的顺序与实际结算完全一致。
/// </summary>
public class GuideToAfterlife() : HuTaoModCard(1, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BloodBlossomPower>(3),
        new HpLossVar(2),
        new DamageVar(3m, DamageProps.card)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await ModActions.LoseHp(Owner.Creature, DynamicVars.HpLoss.IntValue);
        if (play.Target == null) return;

        await CommonActions.Apply<BloodBlossomPower>(play.Target, this, DynamicVars.Power<BloodBlossomPower>().IntValue);

        // 最后一步：铺完层再补 3 点伤害（固定值，不吃血梅香加伤）
        await BlossomSupport.FlatDamage(this, choiceContext, play.Target, DynamicVars.Damage.IntValue);
    }
}
