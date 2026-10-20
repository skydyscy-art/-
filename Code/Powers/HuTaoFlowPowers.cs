using System.Collections.Generic;
using BaseLib.Utils;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HuTaoMod.Code.Powers;

/// <summary>淬刃梅香（血梅香流接口）：攻击卡对带血梅香的敌人，每 hit 额外造成 Amount 点伤害。由 BlossomSupport 读取。</summary>
public class RazorBlossomPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>铁壁梅心：每回合开始，获得等同层数的格挡。</summary>
public class IronPlumHeartPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Amount <= 0 || Owner == null) return;
        await ModActions.GainBlock(Owner, Amount);
    }
}

/// <summary>幽蝶行灵：每回合开始，给随机敌人施加等同层数的血梅香。</summary>
public class ButterflyWispPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Amount <= 0 || Owner == null) return;
        var foe = Cards.Support.BlossomSupport.RandomEnemy(Owner);
        if (foe != null)
            await Cards.Support.BlossomSupport.ApplyBlossom(foe, null, Amount);
    }
}

/// <summary>香如故：每回合你的第一张攻击卡额外挂上等同层数的血梅香。</summary>
public class FirstStrikeBlossomPower : HuTaoModPower
{
    private bool _usedThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        _usedThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_usedThisTurn || Amount <= 0) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        _usedThisTurn = true;
        if (cardPlay.Target != null)
            await Cards.Support.BlossomSupport.ApplyBlossom(cardPlay.Target, null, Amount);
    }
}

/// <summary>吾身即薪（烧血流）：每回合开始，失去 2 点生命，获得等同层数的力量。以身为薪，以血为火。</summary>
public class AshenFuelPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Amount <= 0 || Owner == null) return;
        await ModActions.LoseHp(Owner, 2);
        await CommonActions.Apply<StrengthPower>(Owner, null, Amount);
    }
}

/// <summary>彼岸归途（血梅香流）：你的所有攻击卡每 hit 额外挂上等同层数的血梅香。由 BlossomSupport 读取。</summary>
public class ParamitaReturnPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>幽蝶灯引（血梅香流稀有）：每回合开始，给所有敌人施加等同层数的血梅香。</summary>
public class SoulLanternPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Amount <= 0 || Owner == null) return;
        foreach (var foe in Cards.Support.BlossomSupport.OpponentsSnapshot(Owner))
            await Cards.Support.BlossomSupport.ApplyBlossom(foe, null, Amount);
    }
}

/// <summary>向死而战（烧血流）：每回合开始，若生命不高于一半，获得等同层数的力量。</summary>
public class UndyingWillPower : HuTaoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Amount <= 0 || Owner == null) return;
        if (ModActions.IsBelowHalfHp(Owner))
            await CommonActions.Apply<StrengthPower>(Owner, null, Amount);
    }
}
