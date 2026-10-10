# Windows出牌卡住调查：当前证据

用户报告：Windows环境下OTTO的打击打不出去；询问正式分支与beta差异。

本机已核对：游戏release_info.json为v0.107.1，commit59260271，branch=v0.107.1；BaseLib为3.4.7，其清单最低游戏版本0.107.1。用户已确认Windows使用beta、本机使用正式分支；Windows完整版本号、BaseLib版本与当次日志尚未收到，不能确认版本不兼容是根因。

针对之前验收跳过真实UI流程的局限，新增独立游戏副本的完整战斗场景验收：TestMode=false，创建全套NRun/NCombatRoom/手牌/动画，走TryManualPlay、正式PlayCardAction、资源支付、出牌队列与卡牌动画。使用独立测试存档路径，shouldSave=false，不继续用户正式冒险。渲染为headless，仍保留真实场景节点、动画和Tween等待。

结果：OTTO打击CanPlay=true、IsValidTarget=true；实际扣1能量、造成6伤害、进入Discard，Played回调结束。无超时和结算异常。引擎退出时有无界面资源清理提示。

验收源码：tools/manual_strike_scene_probe.cs.txt；成功记录：art/manual-strike-scene-verification.log。该检查只证明本机v0.107.1/BaseLib3.4.7组合中的基础打击流程正常，不证明用户Windows环境兼容；尚未发布声称修复的版本。

下一步：取得Windows主菜单完整版本号、Steam分支、BaseLib版本和%APPDATA%/SlayTheSpire2/logs/godot.log。优先查MissingMethodException、TypeLoadException、Harmony补丁失败、其他Mod出牌钩子异常以及动画等待。若接口不同，针对实际Windows游戏程序集适配并编译，不直接要求用户切换分支或覆盖存档。
