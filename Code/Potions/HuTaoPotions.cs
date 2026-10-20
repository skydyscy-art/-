using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using HuTaoMod.Code.Extensions;
using HuTaoMod.Code.Powers;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace HuTaoMod.Code.Potions;

/// <summary>
/// 胡桃的药水基类：自动从 HuTaoMod/images/potions/ 读取图标。
/// </summary>
[Pool(typeof(Character.HuTaoPotionPool))]
public abstract class HuTaoModPotion : CustomPotionModel
{
    // PotionModel 抽象成员默认实现（具体药水可覆盖）
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AllEnemies; // 回退值；具体药水覆盖

    public override string? CustomPackedImagePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();

    public override string? CustomPackedOutlinePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}

/// <summary>
/// 蝶酿花蜜 —— 以往生堂秘方酿制的花蜜。
/// 回复 5 点生命，并对所有敌人施加 1 层血梅香。
/// </summary>
public class ButterflyNectar : HuTaoModPotion
{
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AllEnemies;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await ModActions.Heal(Owner.Creature, 5);
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        foreach (var enemy in combatState.Enemies)
            await CommonActions.Apply<BloodBlossomPower>(enemy, null, 1);
    }
}
