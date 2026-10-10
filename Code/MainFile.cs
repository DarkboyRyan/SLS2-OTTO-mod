using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace OttoMod;

/// <summary>
/// Mod 的入口。游戏读取 OTTO.json 后，会调用这个类的 Initialize 方法。
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    // 必须与清单中的 id、资源文件夹名称保持一致。
    public const string ModId = "OTTO";

    // 游戏日志中会带上 OTTO 标签，方便检查是否加载成功。
    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // 注册本程序集的 Harmony 补丁：在视觉重建后恢复伙伴等运行时功能。
        // 角色本身由游戏扫描 CharacterModel 子类，并由 BaseLib 加入选择界面；
        // 这里不要手动 new 角色或再次注册，否则容易产生重复内容。
        new Harmony(ModId).PatchAll(Assembly.GetExecutingAssembly());
        Logger.Info("OTTO initialized: 87 designed cards, escalating Ancient Form, Zoo companions, Can replays and the V5 Ancient card.");
    }
}
