using BaseLib.Abstracts;
using BaseLib.BaseLibScenes;
using BaseLib.Utils.NodeFactories;
using HuTaoMod.Code.Cards;
using HuTaoMod.Code.Extensions;
using HuTaoMod.Code.Relics;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HuTaoMod.Code.Character;

/// <summary>
/// 往生堂七十七代堂主 —— 胡桃。
/// 机制：以血换伤（LossHp 换取高额输出）+ 血梅香（持续伤害）。
/// </summary>
public class HuTao : PlaceholderCharacterModel
{
    public const string CharacterId = "HuTao";

    // 胡桃主题色：往生堂的深红
    public static readonly Color Color = new("d64550");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 70;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<HuTaoStrike>(),
        ModelDb.Card<HuTaoStrike>(),
        ModelDb.Card<HuTaoStrike>(),
        ModelDb.Card<HuTaoStrike>(),
        ModelDb.Card<HuTaoDefend>(),
        ModelDb.Card<HuTaoDefend>(),
        ModelDb.Card<HuTaoDefend>(),
        ModelDb.Card<HuTaoDefend>(),
        ModelDb.Card<GuideToAfterlife>(),
        ModelDb.Card<CrimsonBurstGrandBloom>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<SoulButterflyLamp>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<HuTaoCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<HuTaoRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<HuTaoPotionPool>();

    // 战斗内形象：自建场景（根节点脚本 = HuTaoVisuals : NCreatureVisuals）。
    // BaseLib 会在 RegisterSceneConversions() 里注册这个场景路径，运行时按需转换/补全节点。
    public override string? CustomVisualPath => $"{MainFile.ResPath}/scenes/hu_tao_visuals.tscn";

    // 角色选择界面背景（替换原版铁甲战士的背景场景）
    public override string? CustomCharacterSelectBg => $"{MainFile.ResPath}/scenes/char_select_bg_hu_tao.tscn";

    // 商店（商人）处形象
    public override string? CustomMerchantAnimPath => $"{MainFile.ResPath}/scenes/hu_tao_merchant.tscn";

    // 营火（休息处）形象
    public override string? CustomRestSiteAnimPath => $"{MainFile.ResPath}/scenes/hu_tao_rest_site.tscn";

    // 动画时长：让战斗系统等待的时间与 hu_tao_visuals.tscn 里的动画长度对齐
    // （默认值 0.15 / 0.25 是按 Spine 动画设的，比我们的关键帧动画短，会被硬切）
    public override float AttackAnimDelay => 0.2f;
    public override float CastAnimDelay => 0.35f;
    public override float DeathAnimTime => 1.6f;

    /// <summary>
    /// Spine 骨骼动画的「动画名映射」。
    /// 只有当 hu_tao_visuals.tscn 的 %Visuals 节点是 SpineSprite（真正的 Spine 场景）时才会被调用；
    /// 用关键帧 AnimationPlayer 时这个方法不会被触发，可以安全保留。
    ///
    /// 原版动作集 = Idle / Attack / Cast / Hit / Dead / PowerUp / Revive（+ Relaxed 非战斗）。
    /// 骨架里的动画如果叫别的名字，改下面这些字符串即可，不用动结构。
    /// </summary>
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        return SetupAnimationState(
            controller,
            idleName: "idle",
            deadName: "dead", deadLoop: false,
            hitName: "hit", hitLoop: false,
            attackName: "attack", attackLoop: false,
            castName: "cast", castLoop: false,
            relaxedName: "relaxed", relaxedLoop: true);
    }

    // 角色选择 / 地图标记等 UI 图标
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomIconTexturePath => "character_icon_hu_tao.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_hu_tao.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_hu_tao_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_hu_tao.png".CharacterUiPath();
}
