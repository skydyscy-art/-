using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace HuTaoMod;

// 资源全部放在 HuTaoMod 文件夹（打包进 .pck），代码全部放在 HuTaoMod 命名空间下。
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "HuTaoMod"; //用于资源路径
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 让 Godot 能解析本模组 dll 里的脚本（.tscn 的根节点脚本 HuTaoVisuals 靠它才能生效）
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        Harmony harmony = new(ModId);
        harmony.PatchAll(assembly);

        Logger.Info("胡桃 (Hu Tao) mod 加载成功！往生堂七十七代堂主，参上！");
    }
}
