# OTTO v1.0.4 通用发行包

同一份包支持已验证的正式版 v0.107.1 和 public-beta v0.113.0（16ac0fe3）。两边测试均使用 BaseLib 3.4.8 和 BaseLib Compatibility Helper。本次平台为 macOS；未在 Windows 实机验证，亦不承诺未来游戏版本自动兼容。

## 加载方式

- 根目录 OTTO.dll 是轻量入口，不包含角色类型，也不静态引用两个实现。
- 启动时检查 AttackCommand.FromCard 的真实签名，只加载 assemblies/stable 或 assemblies/beta 下的 OTTO.Content.dll。
- beta 使用游戏的 AssociateAssemblyWithMod；正式版使用 OnModDetected 加载完成事件，将 OTTO 的 assembly 记录指向实现。登记发生在模型扫描之前，不改变其他 Mod。
- 两套实现来自同一份源码，角色/卡牌/遗物 ID 保持原样。游戏只看见一套 87 张 OTTO 卡牌；已加载程序集检查排除了同时加载两套实现的情况。
- OTTO.pck 共用，不随版本重复存放。清单 ID 仍为 OTTO。

## 验证

在游戏隔离副本和 OTTOTestCards 测试存档中运行，未访问正式存档。

- 正式版与 beta 分别运行 600 项断言：所有87张卡牌注册、标题、卡面、普通/升级描述、卡牌序列化恢复；高松灯、丰川翔子、古神时序及伤害、伙伴挡伤与入狱、意图复制、易拉罐重放、死亡钩子、商人模型等。
- 两边均关闭 TestMode 运行完整战斗场景，手动打击支付1费、造成6点伤害并弃牌；升级V5回合末自动打出3张升级格挡牌；敌人行动后进入下一己方回合。
- 检查加载器分支选择、只加载一个 OTTO.Content 程序集和87张卡池注册。
- 运行使用 headless 渲染；不是人工画面验收、完整通关、多人测试或跨分支冒险存档迁移测试。卡牌数据恢复验证不代表游戏自身的跨版本存档保证。
- 构建、ZIP内容和五个发行文件 SHA-256 均验证。测试退出时可能出现 dummy renderer / 资源释放提示。

## Workshop 上传

使用 `OTTO/dist-universal/OTTO` 整个目录更新原项目，不使用 dist 或 dist-beta 的旧单分支包。必须保留以下五个文件及目录结构：

```
OTTO.dll
OTTO.json
OTTO.pck
assemblies/stable/OTTO.Content.dll
assemblies/beta/OTTO.Content.dll
```

Workshop 项目的必需依赖仍是 BaseLib，说明中注明至少3.4.8。玩家仅订阅一个 OTTO，切换分支后重启游戏；不要同时安装旧单分支版与通用版。此任务仅准备发行包并安装到本机，未发布 Workshop。

## 开发

`OTTO/build-universal.sh` 编译两套实现和入口，再导出共用资源与ZIP。自定义引用路径通过 OTTO_STABLE_DATA_DIR、OTTO_BETA_DATA_DIR、OTTO_BETA_BASELIB_PATH 配置。测试源码以 .cs.txt 归档，避免混入 Mod 编译；测试驱动位于工作区 .work/universal。
