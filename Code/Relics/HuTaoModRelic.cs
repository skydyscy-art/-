using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using HuTaoMod.Code.Extensions;

namespace HuTaoMod.Code.Relics;

/// <summary>
/// 胡桃的遗物基类：自动从 HuTaoMod/images/relics/ 读取图标。
/// 命名：类名转小写下划线，如 SoulButterflyLamp → soul_butterfly_lamp.png
/// （普通 96x96 左右；big/ 目录下大图；_outline 为轮廓图）。
/// </summary>
[Pool(typeof(Character.HuTaoRelicPool))]
public abstract class HuTaoModRelic : CustomRelicModel
{
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
}
