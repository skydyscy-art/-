# 立绘与动画

两条路线：**A 程序化演出（已落地）**、**B AI 帧动画（管道已就绪，等你出片）**。
两条可以共存、逐段替换。

---

## 路线 A：程序化演出（当前生效）

场景：[HuTaoMod/scenes/hu_tao_visuals.tscn](../HuTaoMod/scenes/hu_tao_visuals.tscn)
代码：[Code/Character/HuTaoVisuals.cs](../Code/Character/HuTaoVisuals.cs)

### 节点树

```
HuTaoVisuals (Node2D, script)
├── Visuals
│   ├── Ghost2      Sprite2D  冲刺残影（青色灵体，默认 alpha=0，只在 attack 里亮起）
│   ├── Ghost1      Sprite2D  冲刺残影（近一层）
│   ├── Body        Sprite2D  本体 hu_tao_body.png / scale 0.135 / pos (0,-142)
│   └── Flies       Node2D    蝴蝶容器（代码驱动，不受动画影响）
│       ├── Fly1    Sprite2D  ← AtlasTexture 裁切 hu_tao_butterflies.png
│       ├── Fly2    Sprite2D
│       └── Fly3    Sprite2D
├── PhobiaModeVisuals / Bounds / CenterPos / FormVfx / IntentPos / OrbPos / TalkPos
└── AnimationPlayer
```

`Visuals` 下的绘制顺序即层级：残影在身后 → 本体 → 蝴蝶在最前。

### 8 个动画（名字不能改，游戏按这套状态机切）

| 动画 | 时长 | 内容 | loop |
|---|---|---|---|
| `idle` | 3.0s | 呼吸（纵向 scale + 上下 position + 微小 rotation/skew） | 是 |
| `attack` | 0.36s | 后仰蓄力 → 突进（位移 + 倾斜 + skew）→ 回收，同时两层青色残影亮起再淡出 | 否 |
| `cast` | 0.55s | 下沉蓄力 → 上浮发光（提亮偏粉）→ 回位 | 否 |
| `hurt` | 0.3s | 快速抖动 + 红闪 + 轻微后滑 | 否 |
| `die` | 1.6s | 后仰倒地 + 下沉 + 淡出 | 否 |
| `powerup` | 0.7s | 上浮放大 + 提亮 | 否 |
| `revive` | 0.9s | 从倒地姿态回升 | 否 |
| `relaxed` | 4.0s | 非战斗站姿，更慢的呼吸 | 是 |

**时长必须和 [HuTao.cs](../Code/Character/HuTao.cs) 的 `AttackAnimDelay` / `CastAnimDelay` / `DeathAnimTime` 对齐**，
否则游戏会提前打断动画（或等太久）。改动画时长时记得同步这三个常量。

### 蝴蝶

每只蝴蝶在 `HuTaoVisuals._Process` 里独立飘动，参数随机化（相位 / 振幅 / 速度 / 透明度呼吸 / 翅膀缩放脉冲），
所以放技能、受击、死亡时蝶群照样在飞。攻击/施法/受击时会短暂「躁动」(`_excite`)，速度和振幅一起上去。

想调手感直接改 `MakeFlutter()` 里的随机区间：

| 字段 | 作用 | 当前区间 |
|---|---|---|
| `AmpX` / `AmpY` | 绕圈幅度（屏幕像素） | 5~9 / 3.5~7 |
| `Speed` | 绕圈速度 | 0.75~1.15 |
| `BaseAlpha` | 基准不透明度 | 0.72~1.0 |
| `Spin` | 自转幅度（弧度） | 0.04~0.1 |
| `Pulse` | 翅膀缩放脉冲强度 | 0.5~1.0 |

想加蝴蝶：往 `Flies` 下面多放一个 Sprite2D（`AtlasTexture` 裁切原图即可，不需要新贴图），代码会自动接管。
裁切区域可以用 `python tools/make_flies.py` 算出来。

### 校验（改完场景先跑这个，不用进游戏）

```bash
godot --headless --path . --script res://tools/check_visuals_scene.gd
```

它会解析场景、检查节点/贴图/动画名/时长，并逐条验证 AnimationPlayer 轨道引用的节点与属性是否存在
（手写 tscn 最容易在这里写错，比如 `:skew` 之类）。最后一行 `SCENE_OK` 表示通过。
没有 dotnet SDK 时会顺带报一句「Cannot instantiate C# script」，那是正常的，不影响校验。

---

## 路线 B：AI 帧动画

管道：[tools/ai_anim_to_godot.py](../tools/ai_anim_to_godot.py)（ffmpeg 抽帧 → 去背 → 归一化 → SpriteFrames）

### 1. 出片要求（这几条决定成败）

- **纯色背景**（绿幕/洋红/纯白都行，脚本用「四角取色 + 边界洪水填充」去背），不要复杂背景、不要地面阴影
- **固定机位、全身入画**（头顶和脚底都留白，别切到武器）
- **一段一个动作**，时长对准上表（attack 0.36s / cast 0.55s / die 1.6s …）
- **idle 要首尾帧一致**，否则循环会跳帧；用「首帧 + 尾帧同图」或让 AI 生成 2~4 秒后自己裁掉半帧过渡
- 提示词里锁死：同一角色 / 同一套衣服 / 同一镜头 / 不要字幕 / 不要手部特写
- 24 fps 就够（脚本可指定，帧多了只会涨显存）

### 2. 三条命令

```bash
# 把 AI 出的视频放进 tools/anim_in/
python tools/ai_anim_to_godot.py --state idle   --input tools/anim_in/idle.mp4
python tools/ai_anim_to_godot.py --state attack --input tools/anim_in/attack.mp4 --fps 30 --trim 0 0.36
python tools/ai_anim_to_godot.py --build-tres
```

先看对齐基准（脚底/重心/推荐缩放）：

```bash
python tools/ai_anim_to_godot.py --info
```

### 3. 输出规格

- 帧输出到 `HuTaoMod/images/character/anim/<state>/<state>_000.png`
- 画布 **420×526**（= 立绘 1680×2105 的 1/4，屏幕只显示 227×284，1/4 已经接近 2 倍超采样）
- 对齐方式：整段 clip 用**同一组**缩放+平移，把「脚底中心」对齐到现有立绘的脚底中心
  → 帧间相对运动（突进、倒地）保留，只是保证换新素材后站位不跑偏
- SpriteFrames 生成到 `HuTaoMod/scenes/hu_tao_frames.tres`

**体量预算**：一帧 420×526 RGBA ≈ 0.88 MB 显存，24 帧 ≈ 21 MB/状态，PNG 约 120~200 KB/帧。
四段（idle/attack/cast/die）合计约 85 MB 显存、磁盘十几 MB，可以接受；别再往上加画布分辨率。

### 4. 挂进场景（代码已支持，放上去就生效）

在 `Visuals/Body` **下面**加一个节点：

```
Visuals/Body
└── Frames   AnimatedSprite2D
    sprite_frames = res://HuTaoMod/scenes/hu_tao_frames.tres
    position = Vector2(0, 0)
    scale = Vector2(4, 4)      # 420 * 4 = 1680，和 Body 贴图同尺寸
    visible = false
```

- 放在 `Body` 里是为了**自动继承 Body 的关键帧变换**（突进、倾斜、倒地都在 Body 上），
  帧动画自己不用再重复一遍位移
- `HuTaoVisuals.ApplyFrames()` 会按当前 AnimationPlayer 的状态切同名动画：
  SpriteFrames 里**有**该动画就显示帧动画并隐藏 Body，**没有**就退回关键帧表演
  → 可以只做 `idle` + `attack` 两段先上，其余保持现状，逐段替换
- 没有放 `Frames` 节点时行为与现在完全一致（零影响）

---

## 已知限制

1. **`images/character/masks/` 那三张 mask 不能用来拆层**。
   实测 `mask_hair` 的白色区域只覆盖角色面积的 2.6%，`mask_weapon` 8.7%，而且形状是几块随机色块，不是发丝/武器的完整选区。
   想真正分层（头发/武器独立摆动）需要先用 AI 局部重绘把「被头发/武器挡住的衣服」补全，否则一摆动就会露出透明缺口。
2. **`hu_tao_body.png` 里烘进了那 3 只静态蝴蝶**，而 `Flies` 是同一批蝴蝶的独立副本。
   静止时完全重合，蝴蝶飘动时会出现轻微「重影」。彻底解决要用 AI 局部重绘把本体上的蝴蝶抹掉（顺手也能把这块交给同一轮 inpaint）。
3. 帧动画的显存比关键帧高一个数量级，所以只建议对 `idle` / `attack` / `cast` 这种高频、需要真动作的状态使用；
   `die` / `revive` / `hurt` 用关键帧更划算。
