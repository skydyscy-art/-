using BaseLib.Patches.Content;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace HuTaoMod.Code;

/// <summary>
/// 自定义关键词（BaseLib 的 [CustomEnum] 会在运行时给 CardKeyword 分配新值）。
///
/// 注册后：
///   - 卡牌放进 CanonicalKeywords 时，悬停卡牌会出现该关键词的注释（CardModel.HoverTips 会自动收集）
///   - 加 KeywordProperties 的关键词，卡面会自动追加彩色关键词文字
///
/// 本地化键名规则：MODPREFIX-KEYWORD_NAME.title / .description
/// 所以这里是 HUTAOMOD-HUAN_YANG.* / HUTAOMOD-SPIRIT.* / HUTAOMOD-SPIRIT_SPENT.* / HUTAOMOD-SAMSARA.*
///
/// 注意（v11.1 修订）：
///   - 还阳 / 轮回 故意不加 KeywordProperties：卡面描述里已经写了「还阳2张牌」「获得2层轮回」，
///     再自动追加一次关键词文字会重复。这两个关键词只用来提供悬停注释，
///     描述里的文字由本地化文件用 [gold]...[/gold] 手动染色。
///   - 灵 / 灵·已消耗 保留 AutoKeywordPosition.After：灵是「临时状态」，
///     卡面末尾那行金色「灵」就是状态指示灯，消耗后替换为「灵·已消耗」。
/// </summary>
public static class HuTaoKeywords
{
    /// <summary>还阳：把弃牌堆顶的牌放回抽牌堆顶，并赋予「灵」。</summary>
    [CustomEnum("HUAN_YANG")]
    public static CardKeyword HuanYang;

    /// <summary>灵：这张牌被打出时效果强化一次（攻击 +50% 伤害 / 技能 +1 层 / 费用 -1）。</summary>
    [CustomEnum("SPIRIT"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Spirit;

    /// <summary>灵·已消耗：灵已经被打出消耗掉，这张牌不再获得强化。</summary>
    [CustomEnum("SPIRIT_SPENT"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword SpiritSpent;

    /// <summary>轮回：回合开始获得 2×层数 格挡，然后层数减半。</summary>
    [CustomEnum("SAMSARA")]
    public static CardKeyword Samsara;
}
