using BaseLib.Abstracts;
using HuTaoMod.Code.Extensions;
using Godot;

namespace HuTaoMod.Code.Character;

public class HuTaoPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => HuTao.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
