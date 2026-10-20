using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using HuTaoMod.Code.Extensions;
using HuTaoMod.Code.Powers;
using HuTaoMod.Code.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace HuTaoMod.Code.Cards;

/// <summary>
/// 胡桃卡牌基类：自动从 HuTaoMod/images/card_portraits/ 读取卡面。
/// 卡面命名：类名转小写下划线，如 GuideToAfterlife → guide_to_afterlife.png
/// 普通卡面 1000x760（大图 big/ 目录），缩略图 250x190。
///
/// 升级规则统一在这里实现：所有继承本类的卡都会自动获得可用的「升级」效果，
/// 不用在 83 个卡牌类里各写一遍 OnUpgrade。需要特殊升级的卡会自己 override 覆盖。
///
/// 「灵」的三条强化也统一在这里（数值见 v11 文档）：
///   攻击 +50% 伤害  → BlossomSupport.BuildAttack
///   技能 +1 层      → ModifyPowerAmountGivenAdditive
///   费用 -1         → TryModifyEnergyCostInCombat
///   打出后消耗      → AfterCardPlayed（香如故会让每回合第一张带灵牌保留灵）
/// </summary>
[Pool(typeof(Character.HuTaoCardPool))]
public abstract class HuTaoModCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.jpg".BigCardImagePath();

    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.jpg".CardImagePath();

    /// <summary>
    /// 原版 CardModel 的 GainsBlock 默认是 false，只有护甲卡自己 override 成 true。
    /// 附魔「灵巧 Nimble」的 CanEnchant 就依赖这个属性 —— 不 override 的话，
    /// 附魔会加不上去（数值没变化），也看不到护甲提示。
    /// 这里按 DynamicVars 里有没有 BlockVar 自动判定。
    /// </summary>
    public override bool GainsBlock => CanonicalVars.Any(variable => variable is BlockVar);

    /// <summary>灵：带灵的牌费用 -1（打出前就生效，所以走费用修正钩子）。</summary>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this) return false;
        if (!SoulReturn.HasSpirit(this)) return false;
        modifiedCost = System.Math.Max(0m, originalCost - 1m);
        return true;
    }

    /// <summary>
    /// 灵：技能牌带灵时，施加的能力层数 +1。
    ///
    /// ⚠ 这个钩子返回的是「**增量**」，不是新的总量：框架会把所有模型的返回值累加到原值上
    /// （对照原版遗物 SneckoSkull 的实现：不满足条件时返回 Decimal.Zero）。
    /// v11.1 第一版误返回了总量本身，结果同一个铺层被每个模型各加一遍 → 3 × 2^16 = 196608 层。
    /// </summary>
    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount,
        Creature target, CardModel cardSource)
    {
        if (cardSource != this) return 0m;
        if (Type != CardType.Skill) return 0m;
        if (!SoulReturn.HasSpirit(this)) return 0m;
        if (amount <= 0m) return 0m;

        return 1m;
    }

    /// <summary>
    /// 卡面数字修正：让 {Damage} 显示吃满伤害修正（力量 / 易伤 / 虚弱 / 淬刃梅香 等）。
    ///
    /// 走的是和出牌时完全相同的 Hook.ModifyDamage 通道，所以卡面数字与实战伤害一致。
    /// 之所以要在这里手动刷一次：原版的自动预览只覆盖 Hand/Play 堆的刷新时机，
    /// 而卡面文本在描述构建阶段取的是 DynamicVar.PreviewValue，这里保证它是最新的。
    /// </summary>
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        try { RefreshDamagePreview(); }
        catch { /* 预览失败时保留基准值，不影响卡面显示 */ }
    }

    /// <summary>按当前战斗状态刷新所有伤害/格挡变量的预览值（含力量、敏捷等全局修正）。</summary>
    private void RefreshDamagePreview()
    {
        if (CombatState == null) return;
        var owner = Owner;
        if (owner?.Creature == null || owner.RunState == null) return;

        foreach (var v in DynamicVars.Values)
        {
            switch (v)
            {
                // 伤害：力量 / 虚弱 / 易伤 / 淬刃梅香 等都在这条 Hook 里
                case DamageVar dv:
                    dv.PreviewValue = Hook.ModifyDamage(
                        owner.RunState, CombatState,
                        null, owner.Creature,
                        dv.BaseValue, dv.Props,
                        this, null,
                        ModifyDamageHookType.All, CardPreviewMode.None, out _);
                    break;

                // 格挡：敏捷等修正
                case BlockVar bv:
                    bv.PreviewValue = Hook.ModifyBlock(
                        CombatState, owner.Creature,
                        bv.BaseValue, bv.Props,
                        this, null, out _);
                    break;
            }
        }
    }

    /// <summary>
    /// 出牌追踪（临时诊断）：配合 AfterCardPlayed 的 end 行，
    /// 只 start 没有 end 的那张牌就是「打完悬停在空中、效果没跑完」的肇事卡。
    /// </summary>
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == this)
            global::HuTaoMod.MainFile.Logger.Info($"[OP] start {Id.Entry} target={cardPlay.Target?.ToString() ?? "-"} spirit={SoulReturn.StateOf(this)} dmg={DamageTrace()}");
        return Task.CompletedTask;
    }

    /// <summary>诊断：把基准伤害与预览伤害一起打出来，方便核对卡面显示。</summary>
    private string DamageTrace()
    {
        foreach (var v in DynamicVars.Values)
            if (v is DamageVar dv)
                return $"{dv.BaseValue}/{dv.PreviewValue}";
        return "-";
    }

    /// <summary>
    /// 打出后补同步「灵」关键词：灵的消耗发生在打出的瞬间（卡牌还在 Play 堆），
    /// 那时不能改卡面，否则卡牌节点会重建并悬停在半空。等它落进弃牌堆再补。
    /// </summary>
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel clonedBy)
    {
        if (card == this)
            SoulReturn.ApplyKeyword(card);
        return Task.CompletedTask;
    }

    /// <summary>兜底：抽到手时再同步一次（正常流程上面那个钩子已经处理过了）。</summary>
    public override Task AfterCardDrawnEarly(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card == this)
            SoulReturn.ApplyKeyword(card);
        return Task.CompletedTask;
    }

    /// <summary>灵：打出后消耗（香如故保留时顺带抽牌），并结算「生人勿近」。</summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return;
        global::HuTaoMod.MainFile.Logger.Info($"[OP] end {Id.Entry}");
        if (!SoulReturn.HasSpirit(this)) return;

        var me = Owner?.Creature;
        if (me == null) return;

        bool consumed = SoulReturn.TryConsumeSpirit(this);

        // 生人勿近：每打出一张带灵牌，获得等同层数的轮回
        int samsara = me.Powers.OfType<StrangerStayAwayPower>().Sum(p => p.Amount);
        if (samsara > 0)
            await CommonActions.Apply<SamsaraPower>(me, this, samsara);

        // 香如故：本回合第一张带灵牌打出后灵不消失，并抽牌
        if (!consumed && Owner != null && Owner.PlayerCombatState != null)
        {
            int draw = me.Powers.OfType<FragranceEverlastingPower>().Sum(p => p.Amount);
            if (draw > 0)
                await CardPileCmd.Draw(choiceContext, draw, Owner);
        }
    }

    /// <summary>
    /// 统一的升级逻辑（原版卡牌是在每张卡的 OnUpgrade 里手动调 UpgradeValueBy）：
    ///  - 伤害 / 护甲 / 治疗：+3
    ///  - 能力层数（PowerVar&lt;T&gt;）：+1
    ///  - 抽牌数：+1
    ///  - 失去生命（自我伤害代价）：-1（最低 1）
    /// 注意：v11 文档里单张卡的升级增量并不都是 +3（如蝶舞 +1、殓骨还乡 +4），
    /// 这些卡自己在 OnUpgrade 里覆盖本方法。
    /// </summary>
    protected override void OnUpgrade()
    {
        foreach (var variable in DynamicVars.Values)
        {
            switch (variable)
            {
                case DamageVar damage:
                    damage.UpgradeValueBy(3m);
                    break;
                case BlockVar block:
                    block.UpgradeValueBy(3m);
                    break;
                case HealVar heal:
                    heal.UpgradeValueBy(3m);
                    break;
                case CardsVar cards:
                    cards.UpgradeValueBy(1m);
                    break;
                case HpLossVar hpLoss:
                    if (hpLoss.BaseValue > 1m) hpLoss.UpgradeValueBy(-1m);
                    break;
                default:
                    var type = variable.GetType();
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PowerVar<>))
                        variable.UpgradeValueBy(1m);
                    break;
            }
        }
    }
}
