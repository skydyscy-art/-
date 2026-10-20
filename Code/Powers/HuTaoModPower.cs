using BaseLib.Abstracts;
using BaseLib.Extensions;
using HuTaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace HuTaoMod.Code.Powers;

/// <summary>
/// 胡桃的能力基类：自动从 HuTaoMod/images/powers/ 读取图标。
/// 命名：类名转小写下划线，如 BloodBlossomPower → blood_blossom.png（64x64，big/ 下 256x256）。
/// </summary>
public abstract class HuTaoModPower : CustomPowerModel
{
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();

    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();

    /// <summary>增益还是减益。</summary>
    public abstract override PowerType Type { get; }

    /// <summary>
    /// 重复施加时的叠层方式。Counter 最常见：再施加就是层数相加。
    /// </summary>
    public abstract override PowerStackType StackType { get; }
}
