using Godot;

namespace HuTaoMod.Code.Character;

/// <summary>
/// 角色选择界面背景：紫红色底 + 一张会动的胡桃立绘（上下浮动 + 轻微呼吸缩放）。
/// 场景：res://HuTaoMod/scenes/char_select_bg_hu_tao.tscn
/// </summary>
// [GlobalClass] disabled for headless export
public partial class HuTaoCharSelectBg : Control
{
    private const float BaseScale = 0.5f;

    private Sprite2D _sprite;
    private float _baseY;
    private float _time;

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite2D>("HuTao");
        if (_sprite != null) _baseY = _sprite.Position.Y;
    }

    public override void _Process(double delta)
    {
        if (_sprite == null) return;

        _time += (float)delta;

        float bob = Mathf.Sin(_time * 1.4f) * 9f;
        float breathe = 1f + Mathf.Sin(_time * 1.4f + 1.2f) * 0.006f;

        _sprite.Position = new Vector2(_sprite.Position.X, _baseY + bob);
        _sprite.Scale = new Vector2(BaseScale, BaseScale) * breathe;
    }
}
