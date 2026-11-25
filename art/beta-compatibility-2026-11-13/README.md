# OTTO v1.0.3-beta 兼容性验收

日期：2026-11-13；平台：本机 macOS。

适配对象：Steam public-beta v0.113.0，commit 16ac0fe3，build 26625880；BaseLib 3.4.8（连同其 BaseLib Compatibility Helper）。本次未在 Windows 上运行，亦不代表后续所有 beta 更新均兼容。

## 修复

- beta 攻击命令要求提供 CardPlay：所有 OTTO 攻击统一传入真实出牌上下文，覆盖打击、群攻、多段攻击和其人之道。
- 古神的伤害上限和伤害倍率、山泥若挡伤回调适配新增 CardPlay 参数。
- 伙伴死亡清理适配新增来源卡参数。
- 选人音乐补丁适配 beta SelectCharacter 签名，避免 Mod 初始化失败。
- 正式版与 beta 使用条件编译及独立 manifest、分发目录。正式版原包保留，未覆盖。

## 验证方式

使用安装游戏的隔离副本，独立测试存档 OTTOTestCards，不读写正式游戏存档。逻辑探针启用游戏 TestMode；真实场景探针关闭 TestMode，通过 TryManualPlay 入队，实际运行游戏 UI 节点、动画和战斗队列。两者均使用 headless 渲染，不是人工完整通关或画面验收。

- beta 编译：0 错误、0 警告；正式版 v0.107.1 API 编译：0 错误、0 警告。
- 425 项逻辑/资源断言：87 张卡注册、普通及升级描述、卡面；高松灯弃牌回收及能量；丰川翔子每回合触发；古神持续及阈值、免伤及增伤；伙伴共存、替换、三连击挡伤及入狱；两敌人场景的其人之道；易拉罐重放；死亡回调；假商人与普通商人模型资源。
- 真实场景：OTTO 打击造成 6 点伤害，支付 1 费，进入弃牌堆，队列继续运行。
- 真实场景：手动打出升级 V5，回合末自动打出 3 张升级格挡牌（MANIFEST_AUTHORITY、IMPERVIOUS、BLUR），敌人执行行动后顺利进入己方第 2 回合。
- BaseLib 与 OTTO 初始化通过，没有 Harmony 补丁初始化异常。

日志中有 headless dummy renderer 的纹理提示和测试退出时的资源释放提示；无上述测试路径的出牌异常。未穷举全部随机格挡牌、全部怪物/事件、多人模式或完整通关。

## 构建及安装

在 OTTO 目录执行 `./build-beta.sh`。自动读取本机游戏 DLL 和创意工坊 BaseLib；不同安装位置可设置 `OTTO_BETA_BASELIB_PATH`。输出 `dist-beta/OTTO`。`./build-beta.sh --install` 额外安装到游戏 mods/OTTO。

发行包只包含 OTTO.dll、OTTO.pck、OTTO.json。游戏与 BaseLib DLL 不随包分发。正式版使用 dist/OTTO，两个版本不要同时安装。测试探针不随发行包安装。
