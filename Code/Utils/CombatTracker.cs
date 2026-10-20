namespace HuTaoMod.Code.Utils;

/// <summary>
/// 战斗内的「本回合」状态追踪（目前只服务夜行的「本回合未失去生命」判定）。
///
/// 为什么要分成两个计数：胡桃自己大量烧血（LoseHp），那属于「失去生命」的代价，
/// 不应该算作「受伤」。所以：
///   - HpLostThisTurn      ：本回合所有掉血（遗物钩子 AfterCurrentHpChanged 累计）
///   - SelfHpLostThisTurn  ：其中由胡桃自己烧掉的部分（ModActions.LoseHp 累计）
/// 只有后者小于前者，才说明本回合被敌人打过。
/// </summary>
public static class CombatTracker
{
    public static decimal HpLostThisTurn;
    public static decimal SelfHpLostThisTurn;

    public static void ResetTurn()
    {
        HpLostThisTurn = 0m;
        SelfHpLostThisTurn = 0m;
    }

    /// <summary>本回合是否因为敌人的伤害失去过生命。</summary>
    public static bool LostHpToEnemiesThisTurn => HpLostThisTurn > SelfHpLostThisTurn;
}
