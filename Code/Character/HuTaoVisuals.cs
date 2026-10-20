using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HuTaoMod.Code.Character;

/// <summary>
/// 胡桃的战斗内形象根节点（v2 表演版）。
///
/// 结构（见 hu_tao_visuals.tscn）：
///   Visuals/Ghost2      —— 冲刺残影（青色灵体，平时 modulate alpha = 0，只在 attack 里亮起）
///   Visuals/Ghost1      —— 冲刺残影（更近的一层）
///   Visuals/Body        —— 人物本体（hu_tao_body.png），位置由 AnimationPlayer 的 idle/attack/cast/hurt/die/... 驱动
///   Visuals/Flies/Fly*  —— 从蝶群里拆出来的单只蝴蝶，每只独立相位/振幅/呼吸
///
/// 职责划分：
///   - 「身体怎么动」全部写在 tscn 的 AnimationPlayer 里（关键帧，不改代码就能调）
///   - 「蝴蝶怎么飘」在 _Process 里算，和动画互不干扰，所以放技能、受击、死亡时蝶群依然在飞
///   - 攻击/施法时短暂提升蝶群的速度与振幅（_excite），让演出更有呼应
/// 注意：动画名必须保持 idle/attack/cast/hurt/die/powerup/revive/relaxed，
/// 并且 attack/cast/die 的时长要和 HuTao.cs 里的 AttackAnimDelay / CastAnimDelay / DeathAnimTime 对齐。
/// </summary>
public partial class HuTaoVisuals : NCreatureVisuals
{
    private sealed class Flutter
    {
        public Sprite2D Node;
        public Vector2 Home;
        public Vector2 ScaleBase;
        public float Phase;
        public float AmpX;
        public float AmpY;
        public float Speed;
        public float BaseAlpha;
        public float Spin;
        public float Pulse;
    }

    private readonly List<Flutter> _flies = new();
    private AnimationPlayer _anim;
    private Sprite2D _bodySprite;
    private AnimatedSprite2D _frames;
    private string _frameState = "";
    private float _time;
    private float _excite;

    public override void _Ready()
    {
        base._Ready();

        _anim = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _bodySprite = GetNodeOrNull<Sprite2D>("Visuals/Body");

        // 路线 B 的接入口：Visuals/Body/Frames 存在时，用 AI 生成的帧动画接管身体，
        // 不存在时完全走原来的关键帧表演（零影响）。
        // 放在 Body 下面是为了自动继承 Body 的 position/rotation/skew 关键帧，
        // 所以冲刺、倒地这些位移不用在帧动画里重复一遍。
        _frames = GetNodeOrNull<AnimatedSprite2D>("Visuals/Body/Frames");

        // 优先读新的逐只蝴蝶节点
        var flies = GetNodeOrNull<Node2D>("Visuals/Flies");
        if (flies != null)
        {
            foreach (var child in flies.GetChildren())
            {
                if (child is Sprite2D sprite)
                    _flies.Add(MakeFlutter(sprite));
            }
        }

        // 兼容旧的整张蝶群节点（如果场景回退到老版本）
        var legacy = GetNodeOrNull<Sprite2D>("Visuals/Butterflies");
        if (legacy != null)
            _flies.Add(MakeFlutter(legacy));

        _time = (float)GD.RandRange(0.0, 6.283);
    }

    private static Flutter MakeFlutter(Sprite2D node)
    {
        return new Flutter
        {
            Node = node,
            Home = node.Position,
            ScaleBase = node.Scale,
            // 每只蝴蝶的相位/振幅/速度都不同，避免整群同步摆动
            Phase = (float)GD.RandRange(0.0, 6.283),
            AmpX = (float)GD.RandRange(5.0, 9.0),
            AmpY = (float)GD.RandRange(3.5, 7.0),
            Speed = (float)GD.RandRange(0.75, 1.15),
            BaseAlpha = (float)GD.RandRange(0.72, 1.0),
            Spin = (float)GD.RandRange(0.04, 0.1),
            Pulse = (float)GD.RandRange(0.5, 1.0),
        };
    }

    /// <summary>
    /// 帧动画接管：SpriteFrames 里有同名动画就用帧动画，没有就退回关键帧表演。
    /// 这样可以先只做 idle/attack 两段 AI 帧动画，其余状态保持现状，逐段替换。
    /// </summary>
    private void ApplyFrames(string state)
    {
        if (_frames == null) return;

        bool has = !string.IsNullOrEmpty(state)
                   && _frames.SpriteFrames != null
                   && _frames.SpriteFrames.HasAnimation(state);

        _frames.Visible = has;
        if (_bodySprite != null) _bodySprite.Visible = !has;
        if (has) _frames.Play(state);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;

        // 攻击 / 施法 / 强化时蝶群躁动一下
        float target = 0f;
        string state = "";
        if (_anim != null)
        {
            state = _anim.CurrentAnimation;
            if (state == "attack" || state == "cast" || state == "powerup" || state == "hurt")
                target = 1f;
        }

        // 帧动画接管（只有放了 hu_tao_frames.tres 才生效）
        if (_frames != null && state != _frameState)
        {
            _frameState = state;
            ApplyFrames(state);
        }

        if (_flies.Count == 0) return;

        _excite = Mathf.Lerp(_excite, target, 1f - Mathf.Pow(0.001f, dt));

        foreach (var f in _flies)
        {
            f.Phase += dt * f.Speed * (1f + 0.7f * _excite);

            float ampX = f.AmpX * (1f + 0.9f * _excite);
            float ampY = f.AmpY * (1f + 0.9f * _excite);

            // 主绕圈 + 一个高频小抖动，飞行轨迹才有“扑扇”感
            float dx = Mathf.Sin(f.Phase) * ampX + Mathf.Sin(f.Phase * 2.3f) * ampX * 0.22f;
            float dy = Mathf.Cos(f.Phase * 1.27f) * ampY + Mathf.Sin(f.Phase * 0.6f) * ampY * 0.45f;

            f.Node.Position = f.Home + new Vector2(dx, dy);
            f.Node.Rotation = Mathf.Sin(f.Phase * 0.8f) * f.Spin * (1f + _excite);

            // 翅膀扑扇 → 用整体缩放近似（贴图不变形，够用且零风险）
            float s = 1f + Mathf.Sin(f.Phase * 3.4f) * 0.05f * f.Pulse;
            f.Node.Scale = f.ScaleBase * s;

            // 透明度呼吸
            float a = f.BaseAlpha * (0.86f + Mathf.Sin(f.Phase * 1.9f) * 0.14f);
            f.Node.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(a, 0f, 1f));
        }
    }
}
