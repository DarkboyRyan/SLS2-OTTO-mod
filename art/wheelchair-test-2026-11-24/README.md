# OTTO 轮椅动画测试版

版本：v1.0.5-wheelchair-test.1。独立测试包位于 `OTTO/dist-test/OTTO-v1.0.5-wheelchair-test.1.zip`。

使用已确认的 A 版黑金队服坐姿和银色手动轮椅。攻击为蓄力、轮椅抬起、下砸和回弹，伤害在 0.42 秒砸地时结算；防御技能通过四个手推轮圈姿势播放，其他原有施放触发也使用这段动作。死亡为坐在直立轮椅上闭眼垂头睡着，保留死亡语音并停留在睡姿，复活恢复待机。伙伴显示位置略向右移，为轮椅脚踏留出空间。

## 安装与回退

解压后把整个 `OTTO` 文件夹覆盖游戏 `mods/OTTO`，保留 `assemblies/stable`、`assemblies/beta` 子目录。不要同时安装两份 OTTO。需要 BaseLib 3.4.8 及其 Compatibility Helper。重启游戏后，模组列表名称显示为“OTTO（轮椅动画测试版）”。

原正式发行包 `OTTO/dist-universal/OTTO-v1.0.4-universal.zip` 保留，可覆盖回退。

## 构建

执行 `bash OTTO/build-wheelchair-test.sh`。只有此脚本启用 `WheelchairTestBuild`，原构建和站姿场景保留。自定义轮椅场景生成工具为 `OTTO/tools/create_wheelchair_scene.gd`。

测试素材来自已确认的 imagegen A 版；睡姿提示为保持同一角色和轮椅，双眼闭合、下巴降低、头轻侧垂、手臂放松，轮椅保持直立，不倒地、不添加伤口，透明背景。源素材位于 `OTTO/OTTO/images/character/wheelchair`，动画关键帧和时间由 Godot AnimationPlayer 编排。

## 验证结果

macOS 隔离游戏副本内，正式版 v0.107.1 与 beta v0.113.0 均通过实际战斗验证：87 张卡牌注册、打击支付1费造成6点伤害并播放下砸、防御获得5格挡并播放推轮、V5回合末自动出牌、敌方行动后进入下回合、古神形态切换、伙伴显示与假商人无Spine回退、受击还原、睡姿保持、死亡语音触发及复活重置。构建无警告或错误。Windows 未实机验证，需玩家试运行。

测试源码和日志在 `.work/wheelchair-test-2026-11-24`；睡姿截图为同目录下的 `sleep-in-game-scene.png`。
