using System.Reflection;
using System.Runtime.Loader;
using MegaCrit.Sts2.Core.Modding;

namespace OttoMod.Loader;

/// <summary>唯一由游戏直接加载的入口；不引用角色实现，避免加载错误版本的类型。</summary>
[ModInitializer(nameof(Initialize))]
public static class Loader
{
    private static Assembly? content;
    private static readonly object gate = new();

    public static void Initialize()
    {
        lock (gate)
        {
            if (content != null) return;
            var game = typeof(ModManager).Assembly;
            // 按实际接口而非 Steam 分支名称判断。跨平台共用，不依赖本机路径。
            var attack = game.GetType("MegaCrit.Sts2.Core.Commands.Builders.AttackCommand", true)!;
            var signatures = attack.GetMethods().Where(m => m.Name == "FromCard").ToArray();
            bool beta = signatures.Any(m => m.GetParameters() is var p && p.Length == 2 &&
                p[1].ParameterType.FullName == "MegaCrit.Sts2.Core.Entities.Cards.CardPlay");
            bool stable = signatures.Any(m => m.GetParameters().Length == 1);
            if (!beta && !stable) throw new NotSupportedException("OTTO: unsupported AttackCommand API; update OTTO.");
            string branch = beta ? "beta" : "stable";
            var self = typeof(Loader).Assembly;
            var path = Path.Combine(Path.GetDirectoryName(self.Location)!, "assemblies", branch, "OTTO.Content.dll");
            if (!File.Exists(path)) throw new FileNotFoundException("OTTO 安装不完整，请重新安装整个文件夹。", path);
            var context = AssemblyLoadContext.GetLoadContext(self)!;
            var implementation = context.LoadFromAssemblyPath(path);
            // beta 原生支持多程序集关联；在模型扫描和网络类型表建立前登记实现。
            var associate = typeof(ModManager).GetMethod("AssociateAssemblyWithMod", BindingFlags.Public | BindingFlags.Static);
            if (associate != null) associate.Invoke(null, ["OTTO", implementation]);
            else
            {
                // 正式版会在初始化结束后写回入口程序集，必须在该步骤完成后替换为角色实现。
                // 只修改 OTTO 的记录，不干预其他 Mod 的加载。
                var loadedEvent = typeof(ModManager).GetEvent("OnModDetected")
                    ?? throw new MissingMemberException("ModManager.OnModDetected");
                var callback = typeof(Loader).GetMethod(nameof(AssociateStable), BindingFlags.NonPublic | BindingFlags.Static)!;
                loadedEvent.AddEventHandler(null, Delegate.CreateDelegate(loadedEvent.EventHandlerType!, callback));
            }
            content = implementation;
            implementation.GetType("OttoMod.MainFile", true)!.GetMethod("Initialize")!.Invoke(null, null);
            var version = self.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            Console.WriteLine($"[OTTO Loader] selected={branch}; content={implementation.GetName().Name}; version={version}");
        }
    }

    private static void AssociateStable(object __0)
    {
        var field = __0.GetType().GetField("assembly");
        if (content != null && field?.GetValue(__0) is Assembly loaded && loaded == typeof(Loader).Assembly)
        {
            field.SetValue(__0, content);
            Console.WriteLine("[OTTO Loader] stable content registered for model discovery");
        }
    }
}
