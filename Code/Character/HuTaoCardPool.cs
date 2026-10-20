using BaseLib.Abstracts;
using HuTaoMod.Code.Extensions;
using Godot;

namespace HuTaoMod.Code.Character;

public class HuTaoCardPool : CustomCardPoolModel
{
    public override string Title => HuTao.CharacterId; //这不是显示名称。

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    /* 以下 HSV 会以着色器的方式叠加在卡背底图上，决定卡框颜色。
     * 胡桃 → 往生堂深红。 */
    public override float H => 0.98f; //Hue; 色相
    public override float S => 0.62f; //Saturation; 饱和度
    public override float V => 1f;    //Brightness; 亮度

    //小卡牌图标的颜色
    public override Color DeckEntryCardColor => new("d64550");

    public override bool IsColorless => false;
}
